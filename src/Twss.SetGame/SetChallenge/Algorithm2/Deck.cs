using System;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace Twss.SetGame.SetChallenge.Algorithm2;


/// <summary>Represents a deck with its evaluation results for the <see cref="Algorithm2"/>.</summary>
/// <remarks>
/// <list type="bullet">
/// <item><see cref="Deck"/>s are built incrementally: A deck references a <see cref="Base"/> deck
/// (i. e. it contains the same cards) and has one additional <see cref="Card"/>.
/// This is the same principle as a single linked list.</item>
/// <item>This incremental approach uses less memory than having each deck reference all the cards it contains;
/// on the other hand, it creates more objects, thus increasing GC pressure.
/// Measurements have shown that this approach is beneficial for performance.</item>
/// </list>
/// </remarks>
[DebuggerDisplay("{ToDebuggerDisplay(),nq}")]
internal sealed class Deck
{
  internal Deck(Deck? @base, SetCard card)
  {
    Base = @base;
    Card = card;
    DeckSize = ((Base is not null) ? Base.DeckSize : 0) + 1;
  }

  public Deck? Base { get; }

  public SetCard Card { get; }

  public int DeckSize { get; }

  public int CombinationsTested { get; private set; } = -1;

  public int CombinationsAreSets { get; private set; } = -1;

  public bool HasBeenEvaluated
  {
    get => CombinationsTested >= 0;
  }

  public void SetEvaluated(int combinationsTested, int combinationsAreSets)
  {
    Debug.Assert(combinationsTested >= 0);
    Debug.Assert(combinationsAreSets >= 0 && combinationsAreSets <= combinationsTested);

    CombinationsTested = combinationsTested;
    CombinationsAreSets = combinationsAreSets;
  } 

  /// <summary>Writes the cards of the represented deck into given <paramref name="buffer"/>.</summary>
  public void WriteDeck(Span<SetCard> buffer)
  {
    Debug.Assert(buffer.Length >= DeckSize);

    Deck current = this;
    for (int i = DeckSize - 1; i >= 0; i--)
    {
      buffer[i] = current.Card;
      current = current.Base!;
    }
  }

  /// <summary>Writes the cards of the represented deck into a new array and returns it.</summary>
  /// <remarks>
  /// Use <see cref="WriteDeck"/> instead when possible to avoid memory allocations.
  /// </remarks>
  public SetCard[] ToArray()
  {
    SetCard[] result = new SetCard[DeckSize];
    WriteDeck(result);
    return result;
  }

  private string ToDebuggerDisplay()
  {
    SetCard[] deck = ToArray();

    StringBuilder sb = new();
    sb.Append("#");
    sb.AppendJoin(" #", deck.Select(card => card.Index));
    string toDisplay = sb.ToString();
    return toDisplay;
  }
}
