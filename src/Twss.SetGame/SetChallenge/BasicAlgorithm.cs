using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Twss.Utils.Collections;


namespace Twss.SetGame.SetChallenge;


/// <summary>Basic implementation of the "Set Challenge" <see cref="IAlgorithm"/>.</summary>
public sealed class BasicAlgorithm : IAlgorithm
{
  public IAlgorithm.DeckEvaluatedAction? DeckEvaluatedCallback { get; set; }

  public Task<long> RunAsync(int deckSize, SetCard[]? include, SetCard[]? exclude, CancellationToken cancellationToken)
  {
    ThrowIfRunArgsInvalid(deckSize, include, exclude);

    long decksWithNoSets = 0L;

    foreach (var deck in GenerateDecks(deckSize, include, exclude))
    {
      if (cancellationToken.IsCancellationRequested)
        return Task.FromCanceled<long>(cancellationToken);

      (int combinationsTested, int combinationsAreSets) = ContainsSet(deck);

      if (combinationsTested > 0 && combinationsAreSets == 0)
        decksWithNoSets++;

      DeckEvaluatedCallback?.Invoke(deck, combinationsTested, combinationsAreSets);
    }

    return Task.FromResult(decksWithNoSets);
  }

  private IEnumerable<SetCard[]> GenerateDecks(int deckSize, SetCard[]? include, SetCard[]? exclude)
  {
    IEnumerable<SetCard> cardsToChooseFrom = SetCard.CardGame;

    if (exclude != null && exclude.Length > 0)
    {
      cardsToChooseFrom = cardsToChooseFrom.Except(exclude);
    }

    if (include != null && include.Length > 0)
    {
      // When include is given then use a temporary array, copy include and fill the reset with permutations.
      cardsToChooseFrom = cardsToChooseFrom.Except(include);

      SetCard[] deck = new SetCard[deckSize];
      include.CopyTo(deck, 0);

      int permutationSize = deckSize - include.Length;
      IEnumerable<SetCard[]> permutationGenerator = cardsToChooseFrom.ToArray()
        .EnumerateNChooseK(permutationSize, new SetCard[permutationSize]);
      foreach (SetCard[] permutation in permutationGenerator)
      {
        permutation.CopyTo(deck, include.Length);
        yield return deck;
      }
    }
    else
    {
      IEnumerable<SetCard[]> permutationGenerator = cardsToChooseFrom.ToArray()
        .EnumerateNChooseK(deckSize, new SetCard[deckSize]);
      foreach (SetCard[] permutation in permutationGenerator)
        yield return permutation;
    }
  }

  #region STATIC UTILITY METHODS

  public static (int combinationsTested, int combinationsAreSets) ContainsSet(SetCard[] deck)
  {
    Debug.Assert(deck != null && deck.Length > 0);

    int combinationsTested = 0;

    foreach (var cards in BasicAlgorithm.Get3CardCombinations(deck))
    {
      combinationsTested++;
      if (SetCard.CheckIsSet(cards.Item1, cards.Item2, cards.Item3))
        return (combinationsTested, 1);
    }

    return (combinationsTested, 0);
  }

  public static IEnumerable<(SetCard, SetCard, SetCard)> Get3CardCombinations(SetCard[] deck)
  {
    int count = deck.Length;
    for (int i = 0; i < count; i++)
      for (int j = i + 1; j < count; j++)
        for (int k = j + 1; k < count; k++)
          yield return (deck[i], deck[j], deck[k]);
  }

  /// <summary>Throws an argument exception if any of the given arguments doesn't conform
  /// to the restrictions of the <see cref="IAlgorithm.RunAsync"/> method.</summary>
  /// <inheritdoc cref="IAlgorithm.RunAsync" path="//param"/>
  /// <inheritdoc cref="IAlgorithm.RunAsync" path="//exception"/>
  public static void ThrowIfRunArgsInvalid(int deckSize, SetCard[]? include, SetCard[]? exclude)
  {
    if (deckSize < 3 || deckSize > SetCard.CardGame.Count)
      throw new ArgumentOutOfRangeException(nameof(deckSize));

    if (include != null && include.Length > 0)
    {
      if (!SetCard.CheckDeckValid(include))
        throw new ArgumentException($"{nameof(include)} contains invalid or duplicate elements.");

      if (include.Length > deckSize)
        throw new ArgumentException($"{nameof(include)} contains more elements than given {nameof(deckSize)}.");
    }

    if (exclude != null && exclude.Length > 0)
    {
      if (!SetCard.CheckDeckValid(exclude))
        throw new ArgumentException($"{nameof(exclude)} contains invalid or duplicate elements.");

      if (include != null && include.Length > 0 && exclude.Intersect(include).Count() > 0)
        throw new ArgumentException($"{nameof(exclude)} contains elements which are also in {nameof(include)}.");
    }
  }

  #endregion STATIC UTILITY METHODS
}
