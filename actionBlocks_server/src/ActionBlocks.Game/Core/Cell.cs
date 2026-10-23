namespace ActionBlocks.Game.Core;

/// <summary>One board cell. <see cref="ItemId"/> is set when the cell carries an item.</summary>
public readonly record struct Cell(byte Color, string? ItemId = null)
{
    public static readonly Cell Empty = new(CellColor.Empty);

    public bool IsEmpty => Color == CellColor.Empty;
}

public readonly record struct Point(int X, int Y)
{
    public static Point operator +(Point a, Point b) => new(a.X + b.X, a.Y + b.Y);
}
