using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.Intrinsics;


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
  /// Optimized implementation using bit operations.
  /// </remarks>
  public static bool CheckIsSetBitOperations(SetCard card1, SetCard card2, SetCard card3)
  {
    // Every of the 4 features must have either 3 same values or 3 different values.
    // Use the fact that SetCard property enum values are laid out systematically:
    //   - Every value of every feature 'occupies' 2 bits in the Id.
    //   - Sum the card ids => the feature value-specific bits 'count' the number of occurrences.
    //   - Note: SetCard.Id also contains the card index (lower 8 bits)
    //     => adding 3 cards doesn't overflow the 'occupied' bit range => no masking of index required.
    //   - Put a bit mask on the whole feature and compare to the expected results.

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

  /// <inheritdoc cref="CheckIsSetDefault(SetCard, SetCard, SetCard)" path="/summary" />
  /// <remarks>
  /// Optimized implementation using bit operations.
  /// </remarks>
  public static bool CheckIsSetBitOperationsWithShift(SetCard card1, SetCard card2, SetCard card3)
  {
    // Every of the 4 features must have either 3 same values or 3 different values.
    // Use the fact that SetCard property enum values are laid out systematically:
    //   - Every value of every feature 'occupies' 2 bits in the Id.
    //   - Sum the card ids => the feature value-specific bits 'count' the number of occurrences.
    //   - Note: SetCard.Id also contains the card index (lower 8 bits)
    //     => adding 3 cards doesn't overflow the 'occupied' bit range => no masking of index required.
    //   - Put a bit mask on the whole feature and bit shift the result
    //     => use the fact that the bit layout for every feature is the same, just in different bit positions.
    //   - Compare the result to the expected results (same bit masks for every feature after the bit shift).

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

  /// <inheritdoc cref="CheckIsSetDefault(SetCard, SetCard, SetCard)" path="/summary" />
  /// <remarks>
  /// Optimized implementation using SIMD intrinsics.
  /// </remarks>
  public static bool CheckIsSetVector128(SetCard card1, SetCard card2, SetCard card3)
  {
    // Every of the 4 features must have either 3 same values or 3 different values.
    // Use the fact that SetCard property enum values are laid out systematically:
    //   - Every value of every feature 'occupies' 2 bits in the Id.
    //   - Sum the card ids => the feature value-specific bits 'count' the number of occurrences.
    //   - Note: SetCard.Id also contains the card index (lower 8 bits)
    //     => adding 3 cards doesn't overflow the 'occupied' bit range => no masking of index required.
    //   - Put a bit mask on the whole feature and bit shift the result
    //     => use the fact that the bit layout for every feature is the same, just in different bit positions.
    //   - Load the feature-masked sum value into a SIMD vector.
    //   - Put the expected results into a SIMD vector => can compare all feature values in a single operation.

    uint value = card1.Id + card2.Id + card3.Id;
    Vector128<uint> valueVec = Vector128.Create(value);

    // For all color, count, shading, shape properties the bit masks have the same layout.
    Vector128<uint> featureMask = Vector128.Create(
      0x00000003U, // 3 of first property (Purple resp. One resp. Open resp. Diamond)
      0x0000000CU, // 3 of second property (Green resp. Two resp. Solid resp. Squiggle)
      0x00000030U, // 3 of third property (Red resp. Three resp. Striped resp. Oval)
      0x00000015U); // 1 of each property

    Vector128<uint> colorVec = Vector128.Create((value & SetCard.SymbolColorBitMask) >> 20);
    bool colorMatch = Vector128.EqualsAny<uint>(colorVec, featureMask);
    if (!colorMatch)
      return false;

    Vector128<uint> countVec = Vector128.Create((value & SetCard.SymbolCountBitMask) >> 14);
    bool countMatch = Vector128.EqualsAny<uint>(countVec, featureMask);
    if (!countMatch)
      return false;

    Vector128<uint> shadingVec = Vector128.Create((value & SetCard.SymbolShadingBitMask) >> 26);
    bool shadingMatch = Vector128.EqualsAny<uint>(shadingVec, featureMask);
    if (!shadingMatch)
      return false;

    Vector128<uint> shapeVec = Vector128.Create((value & SetCard.SymbolShapeBitMask) >> 8);
    bool shapeMatch = Vector128.EqualsAny<uint>(shapeVec, featureMask);
    if (!shapeMatch)
      return false;

    return true;
  }

  // Proof-of-concept implementation using Vector256. Does not provide any benefits.
  public static bool CheckIsSetVector256(SetCard card1, SetCard card2, SetCard card3)
  {
    uint value = card1.Id + card2.Id + card3.Id;

    Vector256<uint> valueVec = Vector256.Create(value);
    Vector256<uint> colorCountVec = Vector256.Create(value & (SetCard.SymbolColorBitMask | SetCard.SymbolCountBitMask));
    Vector256<uint> shadingShapeVec = Vector256.Create(value & (SetCard.SymbolShadingBitMask | SetCard.SymbolShapeBitMask));

    Vector256<uint> colorCountMask = Vector256.Create(
      0x00300000U, // 3 purples
      0x00C00000U, // 3 greens
      0x03000000U, // 3 reds
      0x01500000U, // 1 purple, 1 green, 1 red

      0x0000C000U, // 3 ones
      0x00030000U, // 3 twos
      0x000C0000U, // 3 threes
      0x00054000U // 1 one, 1 two, 1 three
      );

    Vector256<uint> shadingShapeMask = Vector256.Create(
      0x0C000000U, // 3 open
      0x30000000U, // 3 solid
      0xC0000000U, // 3 striped
      0x54000000U, // 1 open, 1 solid, 1 striped

      0x00000300U, // 3 diamonds
      0x00000C00U, // 3 squiggles
      0x00003000U, // 3 ovals
      0x00001500U // 1 diamond, 1 squiggle, 1 oval
      );

    // See also https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/vectorization-guidelines.md.

    Vector256<uint> colorCountMatch = valueVec & colorCountMask;
    colorCountMatch = Vector256.Equals<uint>(colorCountMatch, colorCountMask);
    uint colorCountMatchBits = colorCountMatch.ExtractMostSignificantBits();
    if (BitOperations.PopCount(colorCountMatchBits) != 2)
      return false;

    Vector256<uint> shadingShapeMatch = shadingShapeVec & shadingShapeMask;
    shadingShapeMatch = Vector256.Equals<uint>(shadingShapeMatch, shadingShapeMask);
    uint shadingShapeMatchBits = shadingShapeMatch.ExtractMostSignificantBits();
    if (BitOperations.PopCount(shadingShapeMatchBits) != 2)
      return false;

    return true;
  }

  // Proof-of-concept implementation using Vector512. Does not provide any benefits.
  public static bool CheckIsSetVector512(SetCard card1, SetCard card2, SetCard card3)
  {
    uint sum = card1.Id + card2.Id + card3.Id;
    Vector512<uint> input = Vector512.Create(sum);

    Vector512<uint> propertyMask = Vector512.Create(
      SetCard.SymbolColorBitMask, SetCard.SymbolColorBitMask, SetCard.SymbolColorBitMask, SetCard.SymbolColorBitMask,
      SetCard.SymbolCountBitMask, SetCard.SymbolCountBitMask, SetCard.SymbolCountBitMask, SetCard.SymbolCountBitMask,
      SetCard.SymbolShadingBitMask, SetCard.SymbolShadingBitMask, SetCard.SymbolShadingBitMask, SetCard.SymbolShadingBitMask,
      SetCard.SymbolShapeBitMask, SetCard.SymbolShapeBitMask, SetCard.SymbolShapeBitMask, SetCard.SymbolShapeBitMask
      );

    Vector512<uint> propertyValuesMask = Vector512.Create(
      0x00300000U, // 3 purples
      0x00C00000U, // 3 greens
      0x03000000U, // 3 reds
      0x01500000U, // 1 purple, 1 green, 1 red

      0x0000C000U, // 3 ones
      0x00030000U, // 3 twos
      0x000C0000U, // 3 threes
      0x00054000U, // 1 one, 1 two, 1 three

      0x0C000000U, // 3 open
      0x30000000U, // 3 solid
      0xC0000000U, // 3 striped
      0x54000000U, // 1 open, 1 solid, 1 striped

      0x00000300U, // 3 diamonds
      0x00000C00U, // 3 squiggles
      0x00003000U, // 3 ovals
      0x00001500U // 1 diamond, 1 squiggle, 1 oval
      );

    Vector512<uint> vector512 = input & propertyMask;
    Vector512<uint> result = Vector512.Equals<uint>(vector512, propertyValuesMask);

    Vector128<uint> zero = Vector128<uint>.Zero;

    if (Vector128.EqualsAll(result.GetLower().GetLower(), zero)
      || Vector128.EqualsAll(result.GetLower().GetUpper(), zero)
      || Vector128.EqualsAll(result.GetUpper().GetLower(), zero)
      || Vector128.EqualsAll(result.GetUpper().GetUpper(), zero))
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
