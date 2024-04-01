using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace Twss.SetGame;


/// <summary>Represents a card from the "Set" card game.</summary>
/// <remarks>
/// <para>The static <see cref="CardGame"/> property contains the whole "Set" pack of cards (81 cards).
/// The <see cref="Index"/> of a card is equal to the index position of the card in <see cref="CardGame"/>.</para>
/// <para>An instance created using the <see langword="default"/> keyword is considered invalid;
/// call <see cref="IsValid"/> to check.</para>
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Set_(card_game)"/>
/// <devremarks>
/// <para>The <see cref="Id"/> property contains the bit-encoded values of the other SetCard properties:
/// <list type="bullet">
/// <item>Bit 0-7: <see cref="Index"/></item>
/// <item>Bit 8-13: <see cref="Shape"/></item>
/// <item>Bit 14-19: <see cref="Count"/></item>
/// <item>Bit 20-25: <see cref="Color"/></item>
/// <item>Bit 26-31: <see cref="Shading"/></item>
/// </list></para>
/// </devremarks>
public readonly struct SetCard : IEquatable<SetCard>
{
  private readonly int _id;

  private SetCard(int index, SymbolShape shape, SymbolCount count, SymbolColor color, SymbolShading shading)
  {
    // Technically there's no need for argument checks as the constructor is private;
    // keep them anyways as 'documentation'.
    if (index < 0 || index >= 81)
      throw new ArgumentOutOfRangeException(nameof(index));
    if (shape != SymbolShape.Diamond && shape != SymbolShape.Squiggle && shape != SymbolShape.Oval)
      throw new ArgumentException(nameof(shape));
    if (count != SymbolCount.One && count != SymbolCount.Two && count != SymbolCount.Three)
      throw new ArgumentException(nameof(count));
    if (color != SymbolColor.Purple && color != SymbolColor.Green && color != SymbolColor.Red)
      throw new ArgumentException(nameof(color));
    if (shading != SymbolShading.Open && shading != SymbolShading.Solid && shading != SymbolShading.Striped)
      throw new ArgumentException(nameof(shading));

    _id = index + (int)shape + (int)count  + (int)color + (int)shading;
  }

  /// <summary>Gets the "Set" pack of cards (81 cards).</summary>
  public static IReadOnlyList<SetCard> CardGame { get; } = CreateCardGame();

  public readonly SymbolShape Shape => (SymbolShape)(_id & 0x00003F00);

  public readonly SymbolCount Count => (SymbolCount)(_id & 0x000FC000);

  public readonly SymbolColor Color => (SymbolColor)(_id & 0x03F00000);

  public readonly SymbolShading Shading => (SymbolShading)(_id & 0xFC000000);

  public readonly int Id => _id;

  /// <summary>Gets the order index of the SetCard.</summary>
  public readonly int Index => (_id & 0x000000FF);

  /// <summary>Indicates whether the <see cref="SetCard"/> represents a valid card
  /// or if it has been <see langword="default"/> initialized.</summary>
  public bool IsValid => Id != 0;

  /// <summary>Checks if a <paramref name="deck"/> is valid, i.e. not empty
  /// and contains only distinct cards from <see cref="SetCard.CardGame"/>.</summary>
  public static bool CheckDeckValid(ReadOnlySpan<SetCard> deck)
  {
    int count = deck.Length;
    if (count <= 0)
      return false;

    for (int i = 0; i < count; i++)
    {
      var card = deck[i];
      int cardIndex = card.Index;
      if (cardIndex < 0 || cardIndex >= CardGame.Count || !card.Equals(CardGame[cardIndex]))
        return false;

      for (int j = i - 1; j >= 0; j--)
      {
        if (card.Equals(deck[j]))
          return false;
      }
    }

    return true;
  }

  public static bool CheckIsSet(SetCard card1, SetCard card2, SetCard card3)
  {
    // Every of the 4 features must have either 3 same values or 3 different values.
    // Use the fact that enum values are laid out systematically: Add the ids then check the bit groups.
    uint value = (uint)(card1.Id + card2.Id + card3.Id);

    uint shapeValue = value & 0x00003F00;
    if (shapeValue != 0x00000300 // 3 diamonds
      && shapeValue != 0x00000C00 // 3 squiggles
      && shapeValue != 0x00003000 // 3 ovals
      && shapeValue != 0x00001500) // 1 diamond, 1 squiggle, 1 oval
      return false;

    uint countValue = value & 0x000FC000;
    if (countValue != 0x0000C000 // 3 ones
      && countValue != 0x00030000 // 3 twos
      && countValue != 0x000C0000 // 3 threes
      && countValue != 0x00054000) // 1 one, 1 two, 1 three
      return false;

    uint colorValue = value & 0x03F00000;
    if (colorValue != 0x00300000 // 3 purples
      && colorValue != 0x00C00000 // 3 greens
      && colorValue != 0x03000000 // 3 reds
      && colorValue != 0x01500000) // 1 purple, 1 green, 1 red
      return false;

    uint shadingValue = value & 0xFC000000;
    if (shadingValue != 0x0C000000 // 3 open
      && shadingValue != 0x30000000 // 3 solid
      && shadingValue != 0xC0000000 // 3 striped
      && shadingValue != 0x54000000) // 1 open, 1 solid, 1 striped
      return false;

    return true;
  }

  // Roughly same performance as CheckIsSet().
  public static bool CheckIsSet2(SetCard card1, SetCard card2, SetCard card3)
  {
    // Every of the 4 features must have either 3 same values or 3 different values.
    // Use the fact that enum values are laid out systematically: Add the ids then check the bit groups.
    uint value = (uint)(card1.Id + card2.Id + card3.Id);

    uint shapeValue = (value & 0x00003F00) >> 8;
    if (shapeValue != 0x00000003 // 3 diamonds
      && shapeValue != 0x0000000C // 3 squiggles
      && shapeValue != 0x00000030 // 3 ovals
      && shapeValue != 0x00000015) // 1 diamond, 1 squiggle, 1 oval
      return false;

    uint countValue = (value & 0x000FC000) >> 14;
    if (countValue != 0x00000003 // 3 ones
      && countValue != 0x0000000C // 3 twos
      && countValue != 0x00000030 // 3 threes
      && countValue != 0x00000015) // 1 one, 1 two, 1 three
      return false;

    uint colorValue = (value & 0x03F00000) >> 20;
    if (colorValue != 0x00000003 // 3 purples
      && colorValue != 0x0000000C // 3 greens
      && colorValue != 0x00000030 // 3 reds
      && colorValue != 0x00000015) // 1 purple, 1 green, 1 red
      return false;

    uint shadingValue = (value & 0xFC000000) >> 26;
    if (shadingValue != 0x00000003 // 3 open
      && shadingValue != 0x0000000C // 3 solid
      && shadingValue != 0x00000030 // 3 striped
      && shadingValue != 0x00000015) // 1 open, 1 solid, 1 striped
      return false;

    return true;
  }

  [Obsolete("CheckIsSet() is faster than this one.")]
  public static bool CheckIsSetByProperties(SetCard card1, SetCard card2, SetCard card3)
  {
    bool expectShapeEqual = card1.Shape == card2.Shape;
    bool shapeEqual = expectShapeEqual
      ? (card2.Shape == card3.Shape)
      : (card1.Shape != card3.Shape && card2.Shape != card3.Shape);
    if (!shapeEqual)
      return false;

    bool expectCountEqual = card1.Count == card2.Count;
    bool countEqual = expectCountEqual
      ? (card2.Count == card3.Count)
      : (card1.Count != card3.Count && card2.Count != card3.Count);
    if (!countEqual)
      return false;

    bool expectColorEqual = card1.Color == card2.Color;
    bool colorEqual = expectColorEqual
      ? (card2.Color == card3.Color)
      : (card1.Color != card3.Color && card2.Color != card3.Color);
    if (!colorEqual)
      return false;

    bool expectShadingEqual = card1.Shading == card2.Shading;
    bool shadingEqual = expectShadingEqual
      ? (card2.Shading == card3.Shading)
      : (card1.Shading != card3.Shading && card2.Shading != card3.Shading);
    if (!shadingEqual)
      return false;

    return true;
  }

  public bool Equals(SetCard other)
  {
    return Id == other.Id;
  }

  public override bool Equals([NotNullWhen(true)] object? obj)
  {
    return obj is SetCard setCard && Equals(setCard);
  }

  public override int GetHashCode()
  {
    return Id;
  }

  public override string ToString()
  {
    return $"#{Index} {Count} {Color} {Shading} {Shape}";
  }

  public static bool operator==(SetCard card1, SetCard card2)
  {
    return card1.Equals(card2);
  }

  public static bool operator!=(SetCard card1, SetCard card2)
  {
    return !card1.Equals(card2);
  }

  private static SetCard[] CreateCardGame()
  {
    var packOfCards = new SetCard[81];
    int i = 0;

    foreach (SymbolShape shape in new[] { SymbolShape.Diamond, SymbolShape.Squiggle, SymbolShape.Oval })
    {
      foreach (SymbolCount count in new[] { SymbolCount.One, SymbolCount.Two, SymbolCount.Three })
      {
        foreach (SymbolColor color in new[] { SymbolColor.Purple, SymbolColor.Green, SymbolColor.Red })
        {
          foreach (SymbolShading shading in new[] { SymbolShading.Open, SymbolShading.Solid, SymbolShading.Striped })
          {
            packOfCards[i] = new SetCard(i, shape, count, color, shading);
            i++;
          }
        }
      }
    }

    return packOfCards;
  }
}


public enum SymbolShape
{
  Diamond = 0x00000100, // reserved space is bits 8-9
  Squiggle = 0x00000400, // reserved space is bits 10-11
  Oval = 0x00001000 // reserved space is bits 12-13
}


public enum SymbolCount
{
  One = 0x00004000, // reserved space is bits 14-15
  Two = 0x00010000, // reserved space is bits 16-17
  Three = 0x00040000 // reserved space is bits 18-19
}


public enum SymbolColor
{
  Purple = 0x00100000, // reserved space is bits 20-21
  Green = 0x00400000, // reserved space is bits 22-23
  Red = 0x01000000 // reserved space is bits 24-25
}


public enum SymbolShading
{
  Open = 0x04000000, // reserved space is bits 26-27
  Solid = 0x10000000, // reserved space is bits 28-29
  Striped = 0x40000000 // reserved space is bits 30-31
}
