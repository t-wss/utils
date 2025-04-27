namespace Twss.SetGame;


/// <summary>Specifies the shading of the symbols on a Set card.</summary>
/// <remarks>
/// The integer values are aligned with the bit positions in the <see cref="SetCard.Id"/> property.
/// See <see cref="SetCard"/> class documentation for more information.
/// </remarks>
public enum SymbolShading
{
  Open = 0x04000000, // reserved space is bits 26-27
  Solid = 0x10000000, // reserved space is bits 28-29
  Striped = 0x40000000 // reserved space is bits 30-31
}
