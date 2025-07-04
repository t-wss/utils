using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace Twss.SetGame.SetChallenge.Algorithm2;


/// <summary>Implementation of the "Set Challenge" (see <see cref="ISetChallenge"/>)
/// which evaluates Set decks incrementally.</summary>
/// <remarks>
/// <list type="bullet">
/// <item>The algorithm evaluates decks with incrementing size, starting with all single card decks
/// resp. with a deck containing given <c>include</c> cards; then every deck is extended by adding one card
/// from the remaining cards (except for given <c>include</c>/<c>exclude</c> cards).
/// This process is repeated recursively up to the specified deck size.</item>
/// <item>The algorithm only extends a deck if it doesn't contain any sets: Extending a deck with sets
/// will only yield decks which contain sets; as the goal is to determine the number of decks without sets
/// these decks (and their extended ones) are not of interest. This approach reduces the total number of decks
/// to be evaluated, especially for larger deck sizes.</item>
/// <item>The algorithm alternates between generating/extending decks and writing them to a buffer, evaluating
/// those decks and removing them from the buffer again. Evaluation happens in batches using multiple threads.</item>
/// <item><see cref="DeckEvaluatedCallback"/> is invoked for every deck evaluated,
/// also for those with a deck size lower than the specified size.</item>
/// </list>
/// </remarks>
public class Algorithm2 : ISetChallenge
{
  #region NESTED TYPES

  private class Parameters
  {
    // 1000 seems to be a sweet spot (tested 100, 1000, 10000).
    internal const int BatchMaxIterations = 800;

    /// <summary>The number of threads to run batch evaluation on in parallel.</summary>
    internal static readonly int Parallelism = Math.Max(1, Environment.ProcessorCount - 4);
  }

  #endregion NESTED TYPES

  /// <inheritdoc />
  public ISetChallenge.DeckEvaluatedAction? DeckEvaluatedCallback { get; set; }

  /// <summary>Gets the nominal deck size to evaluate (as given in <see cref="RunAsync"/>).</summary>
  public int DeckSize { get; protected set; }

  /// <summary>Gets the number of decks without sets that <see cref="RunAsync"/> has determined.</summary>
  public long DecksWithNoSetCount { get; protected set; }

  private List<RunContext> RunContexts { get; } = new();

  #region CORE ALGORITHM

  public async Task<long> RunAsync(int deckSize, SetCard[]? include, SetCard[]? exclude, CancellationToken cancellationToken)
  {
    ISetChallenge.ThrowIfRunArgsInvalid(deckSize, include, exclude);

    Initialize(deckSize, include, exclude);

    int runContextIdx = 0;

    while (!IsRunCompleted())
    {
      cancellationToken.ThrowIfCancellationRequested();

      // Cycle through run contexts.
      runContextIdx = (runContextIdx + 1) % RunContexts.Count;
      RunContext runContext = RunContexts[runContextIdx];

      // Alternate between evaluation and cleanup.
      if (runContext.RunTask != null)
      {
        await runContext.RunTask;
        runContext.RunTask = null;
      }

      Cleanup(runContext);

      if (runContext.Decks.Count > 0)
      {
        runContext.RunTask = Task.Run(() => Evaluate(runContext, Parameters.BatchMaxIterations));
      }
      else
      {
        Debug.Assert(runContext.DecksEvaluated.Count == 0);
        Debug.Assert(runContext.RunTask == null);

        RunContexts.RemoveAt(runContextIdx);
      }
    }

    return DecksWithNoSetCount;
  }

  /// <summary>Initializes the instance from given <see cref="RunAsync"/> arguments.</summary>
  protected virtual void Initialize(int deckSize, SetCard[]? include, SetCard[]? exclude)
  {
    DeckSize = deckSize;
    DecksWithNoSetCount = 0L;

    (SetCard[] packOfCardsEffective, Deck[] initialDecks) = CreateInitialDecks(
      SetCard.CardGame.ToArray(),
      include ?? Array.Empty<SetCard>(),
      exclude ?? Array.Empty<SetCard>());

    int parallelism = Parameters.Parallelism;
    for (int i = 0; i < parallelism; i++)
      RunContexts.Add(new RunContext(deckSize, packOfCardsEffective));

    // Distribute initial decks on the available RunContexts.
    for (int i = 0; i < initialDecks.Length; i++)
      RunContexts[i % parallelism].Decks.Add(initialDecks[i]);
  }

  /// <summary>Determines if the algorithm execution has completed.</summary>
  protected virtual bool IsRunCompleted()
  {
    return (RunContexts.Count == 0) || (RunContexts.All(runContext => runContext.Decks.Count == 0));
  }

  #endregion CORE ALGORITHM

  internal void Cleanup(RunContext runContext)
  {
    foreach (Deck deck in runContext.DecksEvaluated)
    {
      Debug.Assert(deck.HasBeenEvaluated);
      ProcessDeckEvaluated(deck);
    }

    runContext.DecksEvaluated.Clear();
  }

  /// <summary>Creates a base for building/generating "Set" game card decks incrementally.</summary>
  /// <returns>The effective pack of cards to use (after evaluating <paramref name="include"/>
  /// and <paramref name="exclude"/>) and a collection of initial decks to evaluate and extend;
  /// all initial decks contain the <paramref name="include"/> cards at the beginning (if any)
  /// and cards from <c>packOfCardsEffective</c> in ascending index position order.</returns>
  internal static (SetCard[] packOfCardsEffective, Deck[] initialDecks) CreateInitialDecks(
    SetCard[] packOfCards,
    SetCard[] include,
    SetCard[] exclude)
  {
    Debug.Assert(SetMethods.CheckDeckValid(packOfCards));

    // Effective pack of cards is the given one adjusted by given include/exclude (if any).
    SetCard[] packOfCardsEffective = packOfCards;
    if (exclude.Length > 0)
    {
      Debug.Assert(SetMethods.CheckDeckValid(exclude));
      packOfCardsEffective = packOfCardsEffective.Except(exclude).ToArray();
    }

    if (include.Length > 0)
    {
      Debug.Assert(SetMethods.CheckDeckValid(include));
      packOfCardsEffective = include.Concat(packOfCardsEffective.Except(include)).ToArray();
    }

    // baseDeck is the base for initialDecks; it is null (=> empty deck) when include is not given;
    // otherwise it contains the include cards in the order they are given.
    Deck? baseDeck = null;
    if (include.Length > 0)
    {
      for (int i = 0; i < include.Length; i++)
      {
        baseDeck = new Deck(baseDeck, include[i]);
      }
    }

    // initialDecks is filled with all distinct decks of baseDeck + 1 card.
    List<Deck> initialDecks = new(packOfCardsEffective.Length);
    if (packOfCardsEffective.Length == 0)
    {
      // Corner case: Effectively empty pack of cards => no initial decks.
    }
    else if (baseDeck != null && baseDeck.DeckSize == packOfCardsEffective.Length)
    {
      // Corner case: Given include is the only deck.
      initialDecks.Add(baseDeck);
    }
    else
    {
      for (int i = (baseDeck?.DeckSize ?? 0); i < packOfCardsEffective.Length; i++)
      {
        Deck deck = new(baseDeck, packOfCardsEffective[i]);
        initialDecks.Add(deck);
      }
    }

    return (packOfCardsEffective, initialDecks.ToArray());
  }

  internal static void Evaluate(RunContext runContext, int maxIterations)
  {
    // Can't stackalloc in a loop; instead stackalloc a buffer here which will be big enough in any case and slice in the loop.
    Span<SetCard> buffer = stackalloc SetCard[128]; // Technically, SetCard.CardGame.Count would be enough.

    for (int iteration = 0; iteration < maxIterations; iteration++)
    {
      // Always take the currently last deck from the list to evaluate.
      //   - Evaluation includes extending a deck and putting the extended decks back into the list
      //     => the last decks in the list always have the largest deck size.
      //   - Extending stops at nominal deck size
      //     => There's a natural limit how many decks get buffered.
      int deckIdx = runContext.Decks.Count - 1;
      if (deckIdx < 0)
        break;

      Deck deck = runContext.Decks[deckIdx];
      runContext.Decks.RemoveAt(deckIdx);

      if (!deck.HasBeenEvaluated)
      {
        Span<SetCard> bufferSlice = buffer.Slice(0, deck.DeckSize);
        deck.WriteDeck(bufferSlice);
        (int combinationsTested, int combinationsAreSets) = SetMethods.CountSets(bufferSlice, SetMethods.CheckIsSetBitOperations, true);
        deck.SetEvaluated(combinationsTested, combinationsAreSets);
      }

      // Derive extended decks if
      //   - the deck is smaller than the nominal deck size
      //   - the deck doesn't contain any sets (if it did then it would be pointless to extend it).
      //   - the deck doesn't contain any sets BUT combinationsTested is zero
      //     => the deck is smaller than 3 cards and can't be evaluated but still needs to be extended.
      if (deck.DeckSize < runContext.NominalDeckSize
        && (deck.CombinationsAreSets == 0
          || deck.CombinationsTested == 0))
      {
        IEnumerable<Deck> newDecks = ExtendDeck(deck, runContext.PackOfCards);
        runContext.Decks.AddRange(newDecks);
      }

      runContext.DecksEvaluated.Add(deck);
    }
  }

  internal static IEnumerable<Deck> ExtendDeck(Deck deck, SetCard[] packOfCards)
  {
    int idx = Array.IndexOf<SetCard>(packOfCards, deck.Card);
    Debug.Assert(idx >= 0);

    for (idx = idx + 1; idx < packOfCards.Length; idx++)
    {
      SetCard card = packOfCards[idx];
      Deck newDeck = new(deck, card);
      yield return newDeck;
    }
  }

  /// <summary>Given <paramref name="deck"/> has been processed completely;
  /// notify observer (if any) and update <see cref="DecksWithNoSetCount"/>.</summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  internal void ProcessDeckEvaluated(Deck deck)
  {
    ISetChallenge.DeckEvaluatedAction? callback = DeckEvaluatedCallback;
    if (callback != null)
    {
      Span<SetCard> buffer = stackalloc SetCard[deck.DeckSize];
      deck.WriteDeck(buffer);
      callback(buffer, deck.CombinationsTested, deck.CombinationsAreSets);
    }

    if (deck.DeckSize == DeckSize && deck.CombinationsTested > 0 && deck.CombinationsAreSets == 0)
    {
      DecksWithNoSetCount++;
    }
  }
}
