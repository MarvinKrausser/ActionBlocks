namespace ActionBlocks.Game.Core;

/// <summary>The seven four-cell piece shapes. Numeric values double as cell colors (1-7).</summary>
public enum PieceType : byte
{
    I = 1,
    J = 2,
    L = 3,
    O = 4,
    S = 5,
    Z = 6,
    T = 7,
}

public static class CellColor
{
    public const byte Empty = 0;
    public const byte Garbage = 8;
}
