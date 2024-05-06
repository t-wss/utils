using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;


namespace Twss.SetGame.SetChallenge.IncrementalAlgorithms;


// Note: Tests show that in Release mode the Get*/Set* accessor methods are
//       as performant as managing DeckData structs and properties manually.

/// <summary>Buffer for "Set" card decks for evaluation.</summary>
/// <remarks>
/// The <see cref="Decks"/> buffer contains pre-allocated deck arrays for the deck size specified in the constructor.
/// Adding decks happens by copying the array contents.
/// </remarks>
public sealed class DeckBuffer
{
  private readonly ImmutableArray<SetCard[]> _decks;

  private int _deckCount;

  private readonly DeckState[] _deckState;

  public DeckBuffer(int deckSize, int bufferSize)
  {
    if (deckSize < 0 || deckSize > SetCard.CardGame.Count)
      throw new ArgumentOutOfRangeException(nameof(deckSize));
    if (bufferSize < 1)
      throw new ArgumentOutOfRangeException($"{nameof(bufferSize)} must be at least 1.");

    _decks = Enumerable.Range(0, bufferSize).Select(_ => new SetCard[deckSize]).ToImmutableArray();
    _deckState = new DeckState[bufferSize];
    _deckCount = 0;
  }

  public int BufferSize => _decks.Length;

  public ImmutableArray<SetCard[]> Decks => _decks;

  public int DeckSize => _decks[0].Length;

  /// <summary>Gets or sets the number of <see cref="Decks"/> actually in use.</summary>
  public int DeckCount => _deckCount;

  /// <summary>Gets or sets a task representing a running deck evaluation on the buffer.</summary>
  /// <remarks>
  /// Do not set to null; set to <see cref="Task.CompletedTask"/> instead when there's no current running operation.
  /// </remarks>
  public Task EvaluationTask { get; set; } = Task.CompletedTask;

  public int GetCombinationsAreSets(int index)
  {
    return _deckState[index].CombinationsAreSets;
  }

  public int GetCombinationsTested(int index)
  {
    return _deckState[index].CombinationsTested;
  }

  public bool GetHasBeenEvaluated(int index)
  {
    return _deckState[index].HasBeenEvaluated;
  }

  public bool GetHasBeenExtended(int index)
  {
    return _deckState[index].HasBeenExtended;
  }

  public void SetCombinationsAreSets(int index, int value)
  {
    _deckState[index].CombinationsAreSets = value;
  }

  public void SetCombinationsTested(int index, int value)
  {
    _deckState[index].CombinationsTested = value;
  }

  public void SetHasBeenEvaluated(int index, bool value)
  {
    _deckState[index].HasBeenEvaluated = value;
  }

  public void SetHasBeenExtended(int index, bool value)
  {
    _deckState[index].HasBeenExtended = value;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void Add(SetCard[] deck)
  {
    Debug.Assert(deck != null && deck.Length == DeckSize);
    Debug.Assert(DeckCount < BufferSize);

    deck.CopyTo(Decks[DeckCount], 0);
    _deckState[DeckCount].ResetState();
    _deckCount++;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public void RemoveAt(int index)
  {
    Debug.Assert(index >= 0 && index < DeckCount);

    // Remove element at given index by decrementing count;
    // when it's not the last element then move last element to the specified location.
    _deckCount--;
    if (index != DeckCount)
    {
      Decks[DeckCount].CopyTo(Decks[index], 0);
      _deckState[index] = _deckState[DeckCount];
    }
  }
}
