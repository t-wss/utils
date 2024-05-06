namespace Twss.SetGame.SetChallenge.IncrementalAlgorithms;


/// <summary>Manages evaluation state for a "Set" card deck.</summary>
/// <remarks>
/// <para>The caller may use the property <see cref="Deck"/> to reference the deck to which the other
/// properties correspond to, or he may not use <see cref="Deck"/> and keep the deck reference somewhere else
/// (for example separate arrays for decks and their deck states).</para>
/// </remarks>
public record struct DeckState
{
  public int CombinationsTested { get; set; }

  public int CombinationsAreSets { get; set; }

  public SetCard[]? Deck { get; set; }

  public bool HasBeenEvaluated { get; set; }

  public bool HasBeenExtended { get; set; }

  /// <summary>Indicates whether the instance refers to a valid deck (not null and not empty).</summary>
  public bool HasDeck => Deck != null && Deck.Length > 0;

  /// <summary>Resets the properties except for <see cref="Deck"/> to their default values.</summary>
  public void ResetState()
  {
    CombinationsTested = 0;
    CombinationsAreSets = 0;
    HasBeenEvaluated = false;
    HasBeenExtended = false;
  }
}
