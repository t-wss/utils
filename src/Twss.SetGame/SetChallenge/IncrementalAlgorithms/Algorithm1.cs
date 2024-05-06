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
/// <item>The algorithm manages decks in multiple buffers where each buffer contains only decks of one size.
/// There are multiple buffers per deck size, up to the specified size. The algoritm cycles through all the buffers,
/// evaluating the decks, extending them and adding the generated decks to another buffer (with appropriate deck size)
/// and removing the decks again after they have been processed.
/// Buffers are evaluated independently of each other, using multiple threads.</item>
/// <item>The algorithm avoids memory allocations; decks are copied between
/// pre-allocated <c>SetCard[]</c> arrays.</item>
/// <item><see cref="DeckEvaluatedCallback"/> is invoked for every deck evaluated,
/// also for those with a deck size lower than the specified size.</item>
/// </list>
/// </remarks>
/// <devremarks>
/// <para><see cref="RunAsync"/> is implemented using a template method pattern approach:
/// The 'skeleton' of the algorithm is implemented as <c>protected virtual</c> methods;
/// subclasses can override those methods and add to resp. modify the algorithm's behaviour.</para>
/// </devremarks>
public class Algorithm1 : IAlgorithm
{
  #region NESTED TYPES

  private class Parameters
  {
    /// <summary>The number of cycles/iterations between buffer configuration adaptations.</summary>
    internal const int AdaptBuffersPerIterations = 2000;

    /// <summary>The capacity (number of decks) of a single buffer.</summary>
    internal const int BufferSize = 10000;
  }

  #endregion NESTED TYPES

  public IAlgorithm.DeckEvaluatedAction? DeckEvaluatedCallback { get; set; }

  /// <summary>Gets the nominal deck size to evaluate (as given in <see cref="RunAsync"/>).</summary>
  public int DeckSize => (Buffers != null) ? Buffers.Length - 1 : 0;

  /// <summary>Gets the number of decks without sets that <see cref="RunAsync"/> has determined.</summary>
  public long DecksWithNoSetCount { get; protected set; }

  /// <summary>Gets or sets the statistics collection how often the algorithm had to wait
  /// for a buffer evaluation to complete before processing the buffer again.</summary>
  protected int[] AdaptBuffersAwaits { get; set; } = null!;

  /// <summary>Gets or sets the statistics collection how long (in ticks) the algorithm had to wait
  /// for a buffer evaluation to complete before processing the buffer again.</summary>
  protected long[] AdaptBuffersAwaitTimes { get; set; } = null!;

  /// <summary>Gets or sets the number of iterations that <see cref="AdaptBuffers"/> has been invoked.</summary>
  protected int AdaptBuffersIteration { get; set; }

  /// <summary>Array of lists of buffers; the array index is equal to
  /// the deck size of the corresponding buffers.</summary>
  protected List<DeckBuffer>[] Buffers { get; set; } = null!;

  /// <summary>Gets or sets the deck size which to process next;
  /// the value corresponds to the <see cref="Buffers"/> array index.</summary>
  protected int BuffersDeckSizeNext { get; set; }

  /// <summary>Gets or sets the index in the buffer list which to process next;
  /// the value is applicable to the buffer list <c>Buffers[BufferDeckSizeNext]</c>.</summary>
  protected int BuffersIdxNext { get; set; }
 
  protected SetCard[] CardsForExtend { get; set; } = null!;

  #region CORE ALGORITHM

  public async Task<long> RunAsync(int deckSize, SetCard[]? include, SetCard[]? exclude, CancellationToken cancellationToken)
  {
    BasicAlgorithm.ThrowIfRunArgsInvalid(deckSize, include, exclude);

    Initialize(deckSize, include, exclude);

    // Cycle through the buffers, process one buffer at a time.
    // Alternate between adding decks to evaluate (up to max. number of cards per deck), evaluation and cleanup.
    while (!IsRunCompleted())
    {
      cancellationToken.ThrowIfCancellationRequested();

      DeckBuffer buffer = await GetNextBuffer();
      CleanupBuffer(buffer);

      if (buffer.DeckSize > 1)
      {
        DeckBuffer sourceBuffer = GetSourceBuffer(buffer.DeckSize - 1);
        FillBuffer(buffer, sourceBuffer);
      }

      EvaluateBuffer(buffer);

      if (ShouldAdaptBuffers())
      {
        AdaptBuffers();
      }
    }

    return DecksWithNoSetCount;
  }

  /// <summary>Initializes the instance from given <see cref="RunAsync"/> arguments.
  /// Fills the buffer with an initial collection of decks which are then going to be extended
  /// (see also <see cref="Algorithm1"/> description).</summary>
  protected virtual void Initialize(int deckSize, SetCard[]? include, SetCard[]? exclude)
  {
    DecksWithNoSetCount = 0L;

    (SetCard[][] initialDecks, SetCard[] additionalCards) = Algorithm0.CreateInitialDecks(include, exclude);

    // Special case 'include': CreateInitialDecks() produces one initial deck (i.e. the include cards);
    // this cannot be fed into ExtendDecks() as-is later; instead create proper initial decks.
    if (include != null && include.Length > 0 && deckSize > include.Length
      && initialDecks.Length == 1 && initialDecks[0].SequenceEqual(include))
    {
      SetCard[] initialDeck = initialDecks[0];
      Debug.Assert(initialDeck.Intersect(additionalCards).Count() == 0);
      initialDecks = additionalCards.Select(card => initialDeck.Append(card).ToArray()).ToArray();
    }

    // Start with two buffers per deck size; AdaptBuffers() may change that later.
    Buffers = new List<DeckBuffer>[deckSize + 1];
    for (int i = 0; i <= deckSize; i++)
    {
      Buffers[i] = new List<DeckBuffer>()
      {
        new DeckBuffer(i, Parameters.BufferSize),
        new DeckBuffer(i, Parameters.BufferSize)
      };
    }

    foreach (SetCard[] initialDeck in initialDecks)
    {
      List<DeckBuffer> bufferList = Buffers[initialDeck.Length];
      bufferList.First().Add(initialDeck);
    }

    BuffersDeckSizeNext = initialDecks.First().Length;
    BuffersIdxNext = 0;

    CardsForExtend = additionalCards;

    AdaptBuffersAwaits = new int[deckSize + 1];
    AdaptBuffersAwaitTimes = new long[deckSize + 1];
    AdaptBuffersIteration = 0;
  }

  protected virtual bool IsRunCompleted()
  {
    for(int i = 1; i < Buffers.Length; i++)
    {
      List<DeckBuffer> bufferList = Buffers[i];
      for (int j = 0; j < bufferList.Count; j++)
      {
        if (bufferList[j].DeckCount > 0)
          return false;
      }
    }

    return true;
  }

  /// <summary>Gets the next buffer to process from the <see cref="Buffers"/> collection.</summary>
  protected virtual async Task<DeckBuffer> GetNextBuffer()
  {
    Debug.Assert(BuffersDeckSizeNext >= 0 && BuffersDeckSizeNext < Buffers.Length);

    // Cycle through buffers per deck size, in descending deck size order.
    if (BuffersIdxNext >= Buffers[BuffersDeckSizeNext].Count)
    {
      BuffersDeckSizeNext--;
      BuffersIdxNext = 0;
    }
    if (BuffersDeckSizeNext < 1)
    {
      BuffersDeckSizeNext = Buffers.Length - 1;
    }

    DeckBuffer buffer = Buffers[BuffersDeckSizeNext][BuffersIdxNext];

    BuffersIdxNext++; // Prepare for next iteration.

    // If there's a running evaluation then wait for it to complete.
    // Collect statistics on getting buffers and waiting for them.
    if (!buffer.EvaluationTask.IsCompleted)
    {
      long timestamp = Stopwatch.GetTimestamp();
      AdaptBuffersAwaits[buffer.DeckSize]++;

      // Note: await has been tested to be more performant than Task.GetAwaiter().GetResult() and SpinWait.SpinOnce().
      await buffer.EvaluationTask;

      AdaptBuffersAwaitTimes[buffer.DeckSize] += Stopwatch.GetTimestamp() - timestamp;
    }

    return buffer;
  }

  /// <summary>Removes decks from the buffer which have been processed completely.
  /// Invokes <see cref="ProcessDeckEvaluated"/> for each deck before removing it.</summary>
  protected virtual void CleanupBuffer(DeckBuffer buffer)
  {
    Debug.Assert(buffer != null && buffer.EvaluationTask.IsCompleted);

    for (int i = buffer.DeckCount - 1; i >= 0; i--)
    {
      // Keep deck when it still has to be evaluated/derived.
      if (!(buffer.GetHasBeenEvaluated(i) && (buffer.GetHasBeenExtended(i) || buffer.DeckSize >= DeckSize)))
        continue;

      ProcessDeckEvaluated(buffer.Decks[i], buffer.GetCombinationsTested(i), buffer.GetCombinationsAreSets(i));
      buffer.RemoveAt(i);
    }
  }

  /// <summary>Gets a buffer containing decks with given <paramref name="deckSize"/>
  /// to be used as source for filling another buffer.</summary>
  protected virtual DeckBuffer GetSourceBuffer(int deckSize)
  {
    Debug.Assert(deckSize >= 0 && deckSize <= Buffers.Length);
    List<DeckBuffer> bufferList = Buffers[deckSize];
    return bufferList.MaxBy(buffer => buffer.DeckCount)!;
  }

  /// <summary>Fills given <paramref name="buffer"/> by extending decks
  /// in given <paramref name="sourceBuffer"/>.</summary>
  protected virtual void FillBuffer(DeckBuffer buffer, DeckBuffer sourceBuffer)
  {
    Debug.Assert(buffer != null && buffer.EvaluationTask.IsCompleted);
    Debug.Assert(sourceBuffer != null && buffer.DeckSize == sourceBuffer.DeckSize + 1);
 
    for (int i = sourceBuffer.DeckCount - 1; i >= 0; i--)
    {
      // Only consider decks which haven't been derived yet (obviously)
      // and have been evaluated to determine whether they contain sets (see below).
      if (sourceBuffer.GetHasBeenExtended(i) || !sourceBuffer.GetHasBeenEvaluated(i))
        continue;

      // Extending one deck adds up to CardsForExtend new decks; break before buffer size is reached.
      if (buffer.DeckCount >= buffer.BufferSize - CardsForExtend.Length)
        break;

      // Extend decks by adding one card;
      // extend only if the deck doesn't contain sets (only interested in decks without sets).
      if (sourceBuffer.GetCombinationsAreSets(i) == 0)
      {
        SetCard[] deck = sourceBuffer.Decks[i];
        foreach (SetCard[] extended in Algorithm0.ExtendDeck(deck, CardsForExtend))
          buffer.Add(extended);
      }

      sourceBuffer.SetHasBeenExtended(i, true);
    }
  }

  /// <summary>Evaluates all decks in given <paramref name="buffer"/> which have not been evaluated yet.
  /// Evaluation is run asynchronously on the thread pool; a task that represents the asynchronous evaluation
  /// operation is set at <c>buffer.EvaluationTask</c>.</summary>
  protected virtual void EvaluateBuffer(DeckBuffer buffer)
  {
    Debug.Assert(buffer != null && buffer.EvaluationTask.IsCompleted);

    buffer.EvaluationTask = Task.Run(() =>
    {
      // Assumes that non-evaluated decks are appended at the end of the buffer
      // => Buffer evaluation in reverse direction can stop when encountering the first evaluated deck
      //    and still the whole buffer will be evaluated.
      for (int i = buffer.DeckCount - 1; i >= 0; i--)
      {
        if (buffer.GetHasBeenEvaluated(i))
          break;

        (int combinationsTested, int combinationsAreSets) = BasicAlgorithm.ContainsSet(buffer.Decks[i]);
        buffer.SetCombinationsTested(i, combinationsTested);
        buffer.SetCombinationsAreSets(i, combinationsAreSets);
        buffer.SetHasBeenEvaluated(i, true);
      }

      Debug.Assert(Enumerable.Range(0, buffer.DeckCount).All(i => buffer.GetHasBeenEvaluated(i)));
    });
  }

  // Only adapt every n iterations as parametrized.
  protected virtual bool ShouldAdaptBuffers()
  {
    return AdaptBuffersIteration++ >= Parameters.AdaptBuffersPerIterations;
  }

  /// <summary>Adapts the algorithm's buffer configuration based on collected buffer wait statistics.</summary>
  protected virtual void AdaptBuffers()
  {
    // Find deck size which had to be waited most upon; when it's significant then add buffers.
    // Threshold currently hard-coded.
    var waitMostOn = Enumerable.Range(0, AdaptBuffersAwaits.Length)
      .Select(i => new { DeckSize = i, Amount = AdaptBuffersAwaits[i] })
      .MaxBy(pair => pair.Amount)!;

    if (waitMostOn.Amount > AdaptBuffersIteration / 100)
    {
      for (int deckSize = waitMostOn.DeckSize; deckSize < Buffers.Length; deckSize++)
      {
        List<DeckBuffer> bufferList = Buffers[deckSize];
        if (bufferList.Count < Environment.ProcessorCount)
          bufferList.Add(new DeckBuffer(deckSize, Parameters.BufferSize));
      }
    }

    // For deck sizes with unused buffers remove a buffer.
    for (int i = Buffers.Length - 1; i >= 0; i--)
    {
      List<DeckBuffer> bufferList = Buffers[i];
      if (AdaptBuffersAwaits[i] == 0 && bufferList.Count > 1)
      {
        DeckBuffer? emptyBuffer = bufferList.FirstOrDefault(buffer => buffer.DeckCount == 0);
        if (emptyBuffer != null)
        {
          bufferList.Remove(emptyBuffer);
        }
      }
    }

    // Reset statistics.
    AdaptBuffersIteration = 0;
    Array.Fill(AdaptBuffersAwaits, 0);
    Array.Fill(AdaptBuffersAwaitTimes, 0L);
  }

  #endregion CORE ALGORITHM

  /// <summary>Given <paramref name="deck"/> has been processed completely;
  /// notify observer (if any) and update <see cref="DecksWithNoSetCount"/>.</summary>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  protected void ProcessDeckEvaluated(SetCard[] deck, int combinationsTested, int combinationsAreSets)
  {
    DeckEvaluatedCallback?.Invoke(deck, combinationsTested, combinationsAreSets);

    if (deck.Length == DeckSize && combinationsTested > 0 && combinationsAreSets == 0)
      DecksWithNoSetCount++;
  }
}
