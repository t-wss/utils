using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Twss.SetGame;


/// <summary>Provides methods for the "Set" game.</summary>
/// <seealso cref="SetCard"/>
public static class SetMethods
{
  /// <summary>Checks if a <paramref name="deck"/> is valid, i.e. not empty
  /// and contains only distinct cards from <see cref="SetCard.CardGame"/>.</summary>
  public static bool CheckDeckValid(ReadOnlySpan<SetCard> deck)
  {
    int deckSize = deck.Length;
    if (deckSize <= 0)
      return false;

    SetCard[] cardGame = SetCard.CardGame;

    for (int i = 0; i < deckSize; i++)
    {
      var card = deck[i];
      int cardIndex = card.Index;
      if (cardIndex < 0 || cardIndex >= cardGame.Length || !card.Equals(cardGame[cardIndex]))
        return false;

      // Check for duplicates (current card vs. any of the previous cards).
      for (int j = i - 1; j >= 0; j--)
      {
        if (card.Equals(deck[j]))
          return false;
      }
    }

    return true;
  }

  /// <inheritdoc cref="CheckIsSetDefault(SetCard, SetCard, SetCard)" path="/summary" />
  /// <remarks>
  /// Every of the 4 features must have either 3 same values or 3 different values.
  /// Use the fact that enum values are laid out systematically: Add the card ids then check the bit groups.
  /// </remarks>
  public static bool CheckIsSetBitOperations1(SetCard card1, SetCard card2, SetCard card3)
  {
    uint value = card1.Id + card2.Id + card3.Id;

    uint colorValue = value & 0x03F00000;
    if (colorValue != 0x00300000 // 3 purples
      && colorValue != 0x00C00000 // 3 greens
      && colorValue != 0x03000000 // 3 reds
      && colorValue != 0x01500000) // 1 purple, 1 green, 1 red
      return false;

    uint countValue = value & 0x000FC000;
    if (countValue != 0x0000C000 // 3 ones
      && countValue != 0x00030000 // 3 twos
      && countValue != 0x000C0000 // 3 threes
      && countValue != 0x00054000) // 1 one, 1 two, 1 three
      return false;

    uint shadingValue = value & 0xFC000000;
    if (shadingValue != 0x0C000000 // 3 open
      && shadingValue != 0x30000000 // 3 solid
      && shadingValue != 0xC0000000 // 3 striped
      && shadingValue != 0x54000000) // 1 open, 1 solid, 1 striped
      return false;

    uint shapeValue = value & 0x00003F00;
    if (shapeValue != 0x00000300 // 3 diamonds
      && shapeValue != 0x00000C00 // 3 squiggles
      && shapeValue != 0x00003000 // 3 ovals
      && shapeValue != 0x00001500) // 1 diamond, 1 squiggle, 1 oval
      return false;

    return true;
  }

  /// <inheritdoc cref="CheckIsSetBitOperations1(SetCard, SetCard, SetCard)" />
  public static bool CheckIsSetBitOperations2(SetCard card1, SetCard card2, SetCard card3)
  {
    uint value = card1.Id + card2.Id + card3.Id;

    uint colorValue = (value & 0x03F00000) >> 20;
    if (colorValue != 0x00000003 // 3 purples
      && colorValue != 0x0000000C // 3 greens
      && colorValue != 0x00000030 // 3 reds
      && colorValue != 0x00000015) // 1 purple, 1 green, 1 red
      return false;

    uint countValue = (value & 0x000FC000) >> 14;
    if (countValue != 0x00000003 // 3 ones
      && countValue != 0x0000000C // 3 twos
      && countValue != 0x00000030 // 3 threes
      && countValue != 0x00000015) // 1 one, 1 two, 1 three
      return false;

    uint shadingValue = (value & 0xFC000000) >> 26;
    if (shadingValue != 0x00000003 // 3 open
      && shadingValue != 0x0000000C // 3 solid
      && shadingValue != 0x00000030 // 3 striped
      && shadingValue != 0x00000015) // 1 open, 1 solid, 1 striped
      return false;

    uint shapeValue = (value & 0x00003F00) >> 8;
    if (shapeValue != 0x00000003 // 3 diamonds
      && shapeValue != 0x0000000C // 3 squiggles
      && shapeValue != 0x00000030 // 3 ovals
      && shapeValue != 0x00000015) // 1 diamond, 1 squiggle, 1 oval
      return false;

    return true;
  }

  /// <summary>Checks if given <see cref="SetCard"/>s form a "Set"
  /// (i.e. all symbol properties are either the same or different).</summary>
  /// <remarks>
  /// Reference implementation: For each property, compare two cards whether they are equal;
  /// if they are then the third card must be equal as well; if they aren't then the third card must be different.
  /// </remarks>
  public static bool CheckIsSetDefault(SetCard card1, SetCard card2, SetCard card3)
  {
    bool expectColorEqual = card1.Color == card2.Color;
    bool colorMatch = expectColorEqual
      ? (card2.Color == card3.Color)
      : (card1.Color != card3.Color && card2.Color != card3.Color);
    if (!colorMatch)
      return false;

    bool expectCountEqual = card1.Count == card2.Count;
    bool countMatch = expectCountEqual
      ? (card2.Count == card3.Count)
      : (card1.Count != card3.Count && card2.Count != card3.Count);
    if (!countMatch)
      return false;

    bool expectShadingEqual = card1.Shading == card2.Shading;
    bool shadingMatch = expectShadingEqual
      ? (card2.Shading == card3.Shading)
      : (card1.Shading != card3.Shading && card2.Shading != card3.Shading);
    if (!shadingMatch)
      return false;

    bool expectShapeEqual = card1.Shape == card2.Shape;
    bool shapeMatch = expectShapeEqual
      ? (card2.Shape == card3.Shape)
      : (card1.Shape != card3.Shape && card2.Shape != card3.Shape);
    if (!shapeMatch)
      return false;

    return true;
  }

  /// <summary>Counts the number of "Sets" in given <paramref name="deck"/>, i.e. the number of 3-card combinations
  /// (distinct cards) where all symbol properties are either the same or different.</summary>
  /// <param name="deck">The "Set" cards to test.</param>
  /// <param name="checkIsSet">The method to use for checking if a combination is a "Set".</param>
  /// <param name="returnOnFirstSet">Indicates whether the method should short-circuit after the first found "Set".
  /// The returned <c>combinationsAreSets</c> value will be <c>0</c> or <c>1</c>.</param>
  public static (int combinationsTested, int combinationsAreSets) CountSets(
    SetCard[] deck,
    Func<SetCard, SetCard, SetCard, bool> checkIsSet,
    bool returnOnFirstSet)
  {
    Debug.Assert(deck != null && deck.Length > 0);

    int combinationsTested = 0;
    int combinationsAreSets = 0;

    foreach (var cards in SetMethods.Get3CardCombinations(deck))
    {
      combinationsTested++;
      if (checkIsSet(cards.Item1, cards.Item2, cards.Item3))
      {
        combinationsAreSets++;
        if (returnOnFirstSet)
          break;
      }
    }

    return (combinationsTested, combinationsAreSets);
  }

  /// <summary>Enumerates all 3-card combinations (distinct cards)
  /// that can be formed from the given <paramref name="deck"/>.</summary>
  public static IEnumerable<(SetCard, SetCard, SetCard)> Get3CardCombinations(SetCard[] deck)
  {
    int count = deck.Length;
    for (int i = 0; i < count; i++)
      for (int j = i + 1; j < count; j++)
        for (int k = j + 1; k < count; k++)
          yield return (deck[i], deck[j], deck[k]);
  }
}
