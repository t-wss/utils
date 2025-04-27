namespace Twss.SetGame;


/// <summary>Specifies the color of the symbols on a Set card.</summary>
/// <remarks>
/// The integer values are aligned with the bit positions in the <see cref="SetCard.Id"/> property.
/// See <see cref="SetCard"/> class documentation for more information.
/// </remarks>
public enum SymbolColor
{
  Purple = 0x00100000, // reserved space is bits 20-21
  Green = 0x00400000, // reserved space is bits 22-23
  Red = 0x01000000 // reserved space is bits 24-25
}
