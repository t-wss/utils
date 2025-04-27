using System;
using System.Diagnostics.CodeAnalysis;


namespace Twss.SetGame;


/// <summary>Represents a card from the "Set" card game.</summary>
/// <remarks>
/// <para>The static <see cref="CardGame"/> property contains the whole "Set" pack of cards (81 cards).
/// The <see cref="Index"/> of a card is equal to the index position of the card in <see cref="CardGame"/>.</para>
/// <para>An instance created using the <see langword="default"/> keyword is considered invalid;
/// call <see cref="IsDefaultInitialized"/> to check.</para>
/// <para>The <see cref="Id"/> property contains the bit-encoded values of all the other SetCard properties:
/// <list type="bullet">
/// <item>Bit 0-7: <see cref="Index"/></item>
/// <item>Bit 8-13: <see cref="Shape"/></item>
/// <item>Bit 14-19: <see cref="Count"/></item>
/// <item>Bit 20-25: <see cref="Color"/></item>
/// <item>Bit 26-31: <see cref="Shading"/></item>
/// </list>
/// Every indiviual property value has a reserved space of 2 bits; this allows to evaluate card combinations
/// for all card properties at once using bit operations.
/// </para>
/// </remarks>
/// <seealso href="https://en.wikipedia.org/wiki/Set_(card_game)"/>
public readonly struct SetCard : IEquatable<SetCard>
{
  #region CONSTANTS

  public const uint IndexBitMask = 0x000000FF;

  public const uint SymbolColorBitMask = 0x03F00000;

  public const uint SymbolCountBitMask = 0x000FC000;

  public const uint SymbolShadingBitMask = 0xFC000000;

  public const uint SymbolShapeBitMask = 0x00003F00;

  #endregion CONSTANTS

  private readonly uint _id;

  private SetCard(uint index, SymbolShape shape, SymbolCount count, SymbolColor color, SymbolShading shading)
  {
    // Technically there's no need for argument checks as the constructor is private;
    // keep them anyways as 'documentation'.
    if (index >= 81)
      throw new ArgumentOutOfRangeException(nameof(index));
    if (shape != SymbolShape.Diamond && shape != SymbolShape.Squiggle && shape != SymbolShape.Oval)
      throw new ArgumentException($"{nameof(shape)} must be one of Diamond, Squiggle or Oval.");
    if (count != SymbolCount.One && count != SymbolCount.Two && count != SymbolCount.Three)
      throw new ArgumentException($"{nameof(count)} must be one of One, Two or Three.");
    if (color != SymbolColor.Purple && color != SymbolColor.Green && color != SymbolColor.Red)
      throw new ArgumentException($"{nameof(color)} must be one of Purple, Green or Red.");
    if (shading != SymbolShading.Open && shading != SymbolShading.Solid && shading != SymbolShading.Striped)
      throw new ArgumentException($"{nameof(shading)} must be one of Open, Solid or Striped.");

    _id = index + (uint)shape + (uint)count  + (uint)color + (uint)shading;
  }

  /// <summary>Gets the "Set" pack of cards (81 cards).</summary>
  /// <remarks>Creates a new collection on every invocation.</remarks>
  public static SetCard[] CardGame { get; } = CreateCardGame();

  public readonly SymbolColor Color
  {
    get => (SymbolColor)(_id & SymbolColorBitMask);
  }

  public readonly SymbolCount Count
  {
    get => (SymbolCount)(_id & SymbolCountBitMask);
  }

  public readonly uint Id
  {
    get => _id;
  }

  /// <summary>Gets the order index of the SetCard.</summary>
  public readonly int Index
  {
    get => (int)(_id & IndexBitMask);
  }

  /// <summary>Indicates whether the <see cref="SetCard"/> instance has been <see langword="default"/> initialized
  /// (i.e. doesn't represent a valid card).</summary>
  public bool IsDefaultInitialized
  {
    get => Id != 0;
  }

  public readonly SymbolShading Shading
  {
    get => (SymbolShading)(_id & SymbolShadingBitMask);
  }

  public readonly SymbolShape Shape
  {
    get => (SymbolShape)(_id & SymbolShapeBitMask);
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
    return (int)Id;
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
    uint i = 0;

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
