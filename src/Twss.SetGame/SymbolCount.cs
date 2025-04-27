namespace Twss.SetGame;


/// <summary>Specifies the count of the symbols on a Set card.</summary>
/// <remarks>
/// The integer values are aligned with the bit positions in the <see cref="SetCard.Id"/> property.
/// See <see cref="SetCard"/> class documentation for more information.
/// </remarks>
public enum SymbolCount
{
  One = 0x00004000, // reserved space is bits 14-15
  Two = 0x00010000, // reserved space is bits 16-17
  Three = 0x00040000 // reserved space is bits 18-19
}
