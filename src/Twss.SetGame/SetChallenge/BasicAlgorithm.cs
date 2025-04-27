using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Twss.Utils.Collections;


namespace Twss.SetGame.SetChallenge;


/// <summary>Basic implementation of the "Set Challenge", see <see cref="ISetChallenge"/>.</summary>
public sealed class BasicAlgorithm : ISetChallenge
{
  public ISetChallenge.DeckEvaluatedAction? DeckEvaluatedCallback { get; set; }

  public Task<long> RunAsync(int deckSize, SetCard[]? include, SetCard[]? exclude, CancellationToken cancellationToken)
  {
    ISetChallenge.ThrowIfRunArgsInvalid(deckSize, include, exclude);

    long decksWithNoSets = 0L;

    foreach (var deck in GenerateDecks(deckSize, include, exclude))
    {
      if (cancellationToken.IsCancellationRequested)
        return Task.FromCanceled<long>(cancellationToken);

      (int combinationsTested, int combinationsAreSets) = SetMethods.CountSets(
        deck,
        SetMethods.CheckIsSetBitOperations1,
        true);

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
}
