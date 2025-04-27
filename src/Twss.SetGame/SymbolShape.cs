namespace Twss.SetGame;


/// <summary>Specifies the shape of the symbols on a Set card.</summary>
/// <remarks>
/// The integer values are aligned with the bit positions in the <see cref="SetCard.Id"/> property.
/// See <see cref="SetCard"/> class documentation for more information.
/// </remarks>
public enum SymbolShape
{
  Diamond = 0x00000100, // reserved space is bits 8-9
  Squiggle = 0x00000400, // reserved space is bits 10-11
  Oval = 0x00001000 // reserved space is bits 12-13
}
