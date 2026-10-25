namespace ActionBlocks.Game.Core;

/// <summary>
/// Piece shapes. Numeric values double as cell colors: 1-7 are the seven four-cell shapes,
/// 9-13 the odd shapes dealt only during a Transformation (8 is garbage).
/// </summary>
public enum PieceType : byte
{
    I = 1,
    J = 2,
    L = 3,
    O = 4,
    S = 5,
    Z = 6,
    T = 7,
    Dot = 9,
    SmallL = 10,
    U = 11,
    Plus = 12,
    W = 13,
}

public static class CellColor
{
    public const byte Empty = 0;
    public const byte Garbage = 8;
}
