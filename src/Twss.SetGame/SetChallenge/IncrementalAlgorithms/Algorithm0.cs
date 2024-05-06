using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace Twss.SetGame.SetChallenge.IncrementalAlgorithms;


/// <summary>Implementation of the "Set Challenge" (see <see cref="IAlgorithm"/>)
/// which evaluates Set decks incrementally in batches.</summary>
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
/// <devremarks>
/// <para><see cref="RunAsync"/> is implemented using a template method pattern approach:
/// The 'skeleton' of the algorithm is implemented as <c>protected virtual</c> methods;
/// subclasses can override those methods and add to resp. modify the algorithm's behaviour.</para>
/// </devremarks>
public class Algorithm0 : IAlgorithm
{
  #region NESTED TYPES

  private class Parameters
  {
    internal const int BatchSizeMin = 100;

    internal const int BatchSizeSyncLimit = 20;

    /// <summary>The total size of the deck buffer (shared for all deck sizes).</summary>
    internal const int BufferSize = 50000;

    /// <summary>The number of decks to extend per round which don't have the nominal deck size.</summary>
    /// <remarks>
    /// <para>The buffer contains decks of all sizes (up to the nominal deck size);
    /// The extend mechanism must prefer extending larger decks to avoid starving the available buffer space
    /// (as decks with the nominal deck size are the ones which will not be extended
    /// and are definitely removed after evaluation, freeing up buffer space).</para>
    /// <para>A simple approach to avoid buffer starvation is to limit extension of smaller decks per iteration
    /// (although it is sub-optimal regarding performance).</para>
    /// <para>Adjust the value corresponding to nominal deck size and <see cref="BufferSize"/>.
    /// When algorithm execution stalls (<see cref="IAlgorithm.DeckEvaluatedCallback"/> is not invoked anymore)
    /// then the value is too high.</para>
    /// </remarks>
    internal const int ExtendDecksBelowNominalSizeCount = 100;

    /// <summary>The number of threads to run batch evaluation on in parallel.</summary>
    internal static readonly int Parallelism = Math.Max(1, Environment.ProcessorCount - 4);
  }

  #endregion NESTED TYPES

  public IAlgorithm.DeckEvaluatedAction? DeckEvaluatedCallback { get; set; }

  /// <summary>Gets the nominal deck size to evaluate (as given in <see cref="RunAsync"/>).</summary>
  public int DeckSize { get; protected set; }

  /// <summary>Gets the number of decks without sets that <see cref="RunAsync"/> has determined.</summary>
  public long DecksWithNoSetCount { get; protected set; }

  protected DeckState[] Buffer { get; set; } = null!;
 
  /// <summary>Gets or sets the number of buffer elements actually in use.</summary>
  protected int BufferCount { get; set; }

  protected SetCard[] CardsForExtend { get; set; } = null!;

  #region CORE ALGORITHM

  public async Task<long> RunAsync(int deckSize, SetCard[]? include, SetCard[]? exclude, CancellationToken cancellationToken)
  {
    BasicAlgorithm.ThrowIfRunArgsInvalid(deckSize, include, exclude);

    Initialize(deckSize, include, exclude);

    // Alternate between adding decks to evaluate (up to max. number of cards per deck), evaluation and cleanup.
    while (!IsRunCompleted())
    {
      cancellationToken.ThrowIfCancellationRequested();

      await EvaluateAsync();
      ExtendDecks();
      Cleanup();
    }

    return DecksWithNoSetCount;
  }

  /// <summary>Initializes the instance from given <see cref="RunAsync"/> arguments.
  /// Fills the buffer with an initial collection of decks which are then going to be extended
  /// (see also <see cref="Algorithm0"/> description).</summary>
  protected virtual void Initialize(int deckSize, SetCard[]? include, SetCard[]? exclude)
  {
    DeckSize = deckSize;
    DecksWithNoSetCount = 0L;

    (SetCard[][] initialDecks, SetCard[] additionalCards) = CreateInitialDecks(include, exclude);

    // Special case 'include': CreateInitialDecks() produces one initial deck (i.e. the include cards);
    // this cannot be fed into ExtendDecks() as-is later; instead create proper initial decks.
    if (include != null && include.Length > 0 && deckSize > include.Length
      && initialDecks.Length == 1 && initialDecks[0].SequenceEqual(include))
    {
      SetCard[] initialDeck = initialDecks[0];
      Debug.Assert(initialDeck.Intersect(additionalCards).Count() == 0);
      initialDecks = additionalCards.Select(card => initialDeck.Append(card).ToArray()).ToArray();
    }

    Buffer = new DeckState[Parameters.BufferSize];
    for (int i = 0; i < initialDecks.Length; i++)
      Buffer[i] = new DeckState() { Deck = initialDecks[i] };

    BufferCount = initialDecks.Length;
    CardsForExtend = additionalCards;
  }

  /// <summary>Determines if the algorithm execution has completed.</summary>
  protected virtual bool IsRunCompleted()
  {
    return BufferCount <= 0;
  }

  /// <summary>Evaluates all decks in the buffer which have not been evaluated yet.
  /// Evaluation is run asynchronously on the thread pool.</summary>
  /// <returns>A task that represents the asynchronous evaluation operation.</returns>
  protected virtual Task EvaluateAsync()
  {
    IEnumerable<Memory<DeckState>> batches = FindDecksToEvaluate();
    IList<Task> evaluations = EvaluateBatchesAsync(batches);
    WaitForEvaluate(evaluations);

    // Assert that all decks have been evaluated.
    // Note: The LINQ version (commented out) is measurably slower than the explicit implementation.
    // Debug.Assert(Buffer.Take(BufferCount).All(a => a.HasBeenEvaluated));
    Debug.Assert(AreAllEvaluated(Buffer, BufferCount));

    return Task.CompletedTask;

    // Nested methods.

    bool AreAllEvaluated(DeckState[] decks, int count)
    {
      for (int i = count - 1; i >= 0; i--)
        if (!decks[i].HasBeenEvaluated)
          return false;
      return true;
    };
  }

  /// <summary>Extends decks and adds them to the buffer until it is filled.
  /// See also <see cref="Algorithm0"/> description.</summary>
  protected virtual void ExtendDecks()
  {
    // See parameter description for an explanation.
    int toExtendDecksBelowNominalDeckSize = Parameters.ExtendDecksBelowNominalSizeCount;

    for (int i = BufferCount - 1; i >= 0; i--)
    {
      ref DeckState deckState = ref Buffer[i];
      Debug.Assert(deckState.HasDeck);

      if (deckState.HasBeenExtended || !deckState.HasBeenEvaluated)
        continue;

      // Extending one deck will add up to CardsForExtend new decks; break before buffer size is reached.
      if (BufferCount >= Buffer.Length - CardsForExtend.Length)
        break;

      // Extend deck by adding one card if
      // - below nominal deck size
      // - the deck doesn't contain sets (we're only interested in decks without sets).
      if (deckState.Deck!.Length < DeckSize && deckState.CombinationsAreSets == 0)
      {
        foreach (SetCard[] extended in ExtendDeck(deckState.Deck, CardsForExtend))
        {
          Buffer[BufferCount] = new DeckState() { Deck = extended.ToArray() };
          BufferCount++;
        }
      }

      deckState.HasBeenExtended = true;

      // Avoid starving the buffer.
      if (deckState.Deck.Length < DeckSize - 1)
        toExtendDecksBelowNominalDeckSize--;
      if (toExtendDecksBelowNominalDeckSize == 0)
        break;
    }
  }

  /// <summary>Removes decks from the buffer which have been processed completely.
  /// Invokes <see cref="ProcessDeckEvaluated"/> for each deck before removing it.</summary>
  protected virtual void Cleanup()
  {
    int skippedCount = 0;

    for (int i = BufferCount - 1; i >= 0; i--)
    {
      ref DeckState deckState = ref Buffer[i];
      SetCard[] deck = deckState.Deck!;

      // Keep deck when it still has to be evaluated/extended.
      if (!(deckState.HasBeenEvaluated && (deckState.HasBeenExtended || deck.Length >= DeckSize)))
      {
        skippedCount++;
        continue;
      }

      ProcessDeckEvaluated(deck, deckState.CombinationsTested, deckState.CombinationsAreSets);

      // Remove element by decrementing count; when it's not the last element (i.e. elements have been skipped)
      // then move last element to the current location.
      BufferCount--;
      if (skippedCount > 0)
        Buffer[i] = Buffer[BufferCount];
    }
  }

  #endregion CORE ALGORITHM

  protected void Evaluate(Memory<DeckState> decks)
  {
    foreach (ref DeckState deckState in decks.Span)
    {
      Debug.Assert(deckState.HasDeck);

      (int combinationsTested, int combinationsAreSets) = BasicAlgorithm.ContainsSet(deckState.Deck!);

      deckState.CombinationsTested = combinationsTested;
      deckState.CombinationsAreSets = combinationsAreSets;
      deckState.HasBeenEvaluated = true;
    }
  }

  /// <summary>Evaluate in batches in parallel on the thread pool. When there are only few analyses
  /// (less than min. batch size parameter) then evaluate them synchronously.</summary>
  protected virtual IList<Task> EvaluateBatchesAsync(IEnumerable<Memory<DeckState>> batches)
  {
    List<Task> tasks = new();

    foreach (Memory<DeckState> batch in batches)
    {
      int batchSize = Math.Max(Parameters.BatchSizeMin, batch.Length / Parameters.Parallelism);

      foreach (Memory<DeckState> chunk in ChunkMemory(new[] { batch }, batchSize))
      {
        if (batch.Length > Parameters.BatchSizeSyncLimit)
          tasks.Add(Task.Run(() => Evaluate(chunk)));
        else
          Evaluate(chunk);
      }
    }

    return tasks;
  }

  /// <summary>Finds batches in the buffer which represent non-evaluated decks.</summary>
  protected virtual IEnumerable<Memory<DeckState>> FindDecksToEvaluate()
  {
    int batchCount = 0;

    for (int i = BufferCount - 1; i >= 0; i--)
    {
      if (!Buffer[i].HasBeenEvaluated)
      {
        batchCount++;
      }
      else
      {
        // Create a slice for all the consecutive non-evaluated decks before, then reset.
        if (batchCount > 0)
        {
          Debug.Assert(i + 1 + batchCount <= BufferCount);
          Memory<DeckState> batch = Buffer.AsMemory(i + 1, batchCount);
          yield return batch;

          batchCount = 0;
        }
      }
    }

    // Final batch at the beginning of the buffer (if any).
    if (batchCount > 0)
    {
      Memory<DeckState> batch = Buffer.AsMemory(0, batchCount);
      yield return batch;
    }
  }

  /// <summary>Given <paramref name="deck"/> has been processed completely;
  /// notify observer (if any) and update <see cref="DecksWithNoSetCount"/>.</summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  protected void ProcessDeckEvaluated(SetCard[] deck, int combinationsTested, int combinationsAreSets)
  {
    DeckEvaluatedCallback?.Invoke(deck, combinationsTested, combinationsAreSets);

    if (deck.Length == DeckSize && combinationsTested > 0 && combinationsAreSets == 0)
      DecksWithNoSetCount++;
  }

  protected virtual void WaitForEvaluate(IList<Task> tasks)
  {
    IEnumerable<Task> runningTasks = tasks.Where(t => !t.IsCompleted);
    if (runningTasks.Any())
      Task.WhenAll(runningTasks).GetAwaiter().GetResult();
  }

  #region STATIC UTILITY METHODS

  /// <summary>Streams the elements of given <paramref name="source"/>;
  /// any element with a size greater than given <paramref name="chunkSize"/> is chunked
  /// and the chunks are enumerated as separate elements.</summary>
  /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
  /// <exception cref="ArgumentOutOfRangeException"><paramref name="chunkSize"/> is less than 1.</exception>
  public static IEnumerable<Memory<T>> ChunkMemory<T>(IEnumerable<Memory<T>> source, int chunkSize)
  {
    if (source == null)
      throw new ArgumentNullException(nameof(source));
    if (chunkSize < 1)
      throw new ArgumentOutOfRangeException(nameof(chunkSize), $"{nameof(chunkSize)} is less than 1.");

    foreach (Memory<T> element in source)
    {
      if (element.Length <= chunkSize)
      {
        yield return element;
      }
      else
      {
        Memory<T> chunk = element;
        while (chunk.Length > chunkSize)
        {
          yield return chunk.Slice(0, chunkSize);
          chunk = chunk.Slice(chunkSize);
        }
        if (chunk.Length > 0)
        {
          yield return chunk;
        }
      }
    }
  }

  /// <summary>Creates a base for building/generating "Set" game card decks incrementally.</summary>
  /// <returns>A collection of <c>initialDecks</c> and corresponding <c>additionalCards</c>.</returns>
  public static (SetCard[][] initialDecks, SetCard[] additionalCards) CreateInitialDecks(
    SetCard[]? include,
    SetCard[]? exclude)
  {
    SetCard[][] initialDecks;
    IEnumerable<SetCard> additionalCards = SetCard.CardGame;

    if (exclude != null && exclude.Length > 0)
      additionalCards = additionalCards.Except(exclude);

    if (include == null || include.Length == 0)
    {
      // No specific cards to include in all decks => start with 1-card decks for every card of the Set game.
      initialDecks = additionalCards.Select(card => new SetCard[] { card }).ToArray();
    }
    else
    {
      initialDecks = new[] { include.OrderBy(card => card.Index).ToArray() };
      additionalCards = additionalCards.Except(include);
    }

    return (initialDecks, additionalCards.ToArray());
  }

  /// <summary>Extends given <paramref name="deck"/>: Generates decks containing all cards
  /// from <paramref name="deck"/> plus an additional one from given <paramref name="cards"/>
  /// with a higher order index than the last card in given <paramref name="deck"/>.</summary>
  /// <returns>A sequence of extended decks. <b>Note:</b> Each enumerated deck is always the same
  /// <c>SetCard[]</c> array instance with updated content. Evaluate each yielded element immediately
  /// or copy the array before moving the enumerator forward.</returns>
  /// <remarks>
  /// <para>The implementation assumes that cards in a deck are ordered by <see cref="SetCard.Index"/>;
  /// that way the last element of a <c>SetCard[]</c> deck is always the one with the highest index
  /// which facliltates extending (no need to max by index over all cards, just inspect the last one).</para>
  /// <para>Special case: When <see cref="RunAsync"/> has been invoked with <c>include</c> cards
  /// then decks to extend must contain at least one additional card (which 'hides' the include cards' index value;
  /// otherwise the <see cref="ExtendDeck"/> implementation won't work.</para>
  /// </remarks>
  public static IEnumerable<SetCard[]> ExtendDeck(SetCard[] deck, SetCard[] cards)
  {
    Debug.Assert(SetCard.CheckDeckValid(deck));

    int maxCardIndex = deck[deck.Length - 1].Index;
    SetCard[] extendedDeck = new SetCard[deck.Length + 1];

    for (int i = 0; i < cards.Length; i++)
    {
      SetCard card = cards[i];
      if (card.Index <= maxCardIndex)
        continue;

      deck.CopyTo(extendedDeck, 0);
      extendedDeck[deck.Length] = card;
      yield return extendedDeck;
    }
  }

  #endregion STATIC UTILITY METHODS
}
