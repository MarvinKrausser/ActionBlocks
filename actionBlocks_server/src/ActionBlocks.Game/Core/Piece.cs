namespace ActionBlocks.Game.Core;

/// <summary>
/// A piece definition as it travels through the queue: its shape plus an optional item
/// hidden inside one of its minos.
/// </summary>
public sealed record PieceSpec(PieceType Type, string? ItemId = null, int ItemMinoIndex = -1)
{
    public bool HasItem => ItemId is not null;
}

/// <summary>A piece placed somewhere on the board (position = top-left corner of its bounding box).</summary>
public sealed record ActivePiece(PieceSpec Spec, int X, int Y, int Rotation)
{
    public PieceType Type => Spec.Type;

    public IEnumerable<Point> Cells()
    {
        foreach (var m in PieceDefinitions.Minos(Spec.Type, Rotation))
        {
            yield return new Point(X + m.X, Y + m.Y);
        }
    }

    public ActivePiece Moved(int dx, int dy) => this with { X = X + dx, Y = Y + dy };
}
