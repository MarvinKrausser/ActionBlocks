namespace ActionBlocks.Game.Core;

/// <summary>
/// A piece definition as it travels through the queue: its shape plus optional items
/// hidden inside its minos. <see cref="Items"/>[i] is the item in mino i (null = none).
/// <see cref="Original"/> is set on a transformed piece and holds the piece it was before.
/// </summary>
public sealed record PieceSpec(PieceType Type, IReadOnlyList<string?>? Items = null, PieceSpec? Original = null)
{
    public bool HasItem => Items?.Any(i => i is not null) == true;

    /// <summary>The same piece as <paramref name="type"/>. Items move to the new minos in order; extras are dropped.</summary>
    public PieceSpec Transform(PieceType type)
    {
        var original = Original ?? this;
        var carried = original.Items?.Where(i => i is not null).ToList() ?? [];
        string?[]? items = null;
        if (carried.Count > 0)
        {
            items = new string?[PieceDefinitions.Minos(type, 0).Count];
            for (var i = 0; i < Math.Min(items.Length, carried.Count); i++)
            {
                items[i] = carried[i];
            }
        }
        return new PieceSpec(type, items, original);
    }

    /// <summary>Undoes <see cref="Transform"/>.</summary>
    public PieceSpec Restore() => Original ?? this;
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
