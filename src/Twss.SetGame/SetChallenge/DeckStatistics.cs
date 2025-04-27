namespace Twss.SetGame.SetChallenge;


/// <summary>Provides a container for statistical values when evaluating "Set" game decks.</summary>
/// <seealso cref="ISetChallenge"/>
public record DeckStatistics
{
  /// <summary>The number of cards per set deck these statistic values apply to.</summary>
  /// <remarks>
  /// Optional; it's up to the caller how to use this property.
  /// </remarks>
  public int DeckSize { get; init; }

  public long CombinationsCount { get; set; } = 0L;

  public long CombinationsAreSetsCount { get; set; } = 0L;

  public long DecksCount { get; set; } = 0L;

  public long DecksWithNoSetCount { get; set; } = 0L;

  /// <summary>Adds the values for one deck to the instance.</summary>
  /// <remarks>
  /// Adds given arguments to the corresponding properties, increments <see cref="DecksCount"/> by one
  /// and increments <see cref="DecksWithNoSetCount"/> if the deck has been evaluated
  /// (i.e. <paramref name="combinationsCount"/> is greater than zero) and no set was found
  /// (i.e. <paramref name="combinationsAreSetsCount"/> is zero).
  /// </remarks>
  public void AddOne(int combinationsCount, int combinationsAreSetsCount)
  {
    CombinationsCount += combinationsCount;
    CombinationsAreSetsCount += combinationsAreSetsCount;
    DecksCount++;
    if (combinationsCount > 0 && combinationsAreSetsCount == 0)
      DecksWithNoSetCount++;
  }
}
