namespace ActionBlocks.Game.Core;

/// <summary>
/// The grid of locked cells. Row 0 is the top of the hidden buffer zone; the visible playfield
/// starts at <see cref="BufferRows"/>.
/// </summary>
public sealed class Board
{
    private List<Cell[]> _rows;

    public Board(int width, int visibleRows, int bufferRows)
    {
        Width = width;
        VisibleRows = visibleRows;
        BufferRows = bufferRows;
        _rows = Enumerable.Range(0, Height).Select(_ => NewRow()).ToList();
    }

    public int Width { get; }
    public int VisibleRows { get; }
    public int BufferRows { get; }
    public int Height => VisibleRows + BufferRows;

    public Cell this[int x, int y]
    {
        get => _rows[y][x];
        set => _rows[y][x] = value;
    }

    public bool IsInside(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    /// <summary>True if the position is outside the board or occupied.</summary>
    public bool IsBlocked(int x, int y) => !IsInside(x, y) || !_rows[y][x].IsEmpty;

    public bool Fits(ActivePiece piece) => piece.Cells().All(c => !IsBlocked(c.X, c.Y));

    public bool IsEmpty => _rows.All(r => r.All(c => c.IsEmpty));

    public void Place(ActivePiece piece)
    {
        var i = 0;
        foreach (var c in piece.Cells())
        {
            var item = piece.Spec.Items is { } items && i < items.Count ? items[i] : null;
            _rows[c.Y][c.X] = new Cell((byte)piece.Type, item);
            i++;
        }
    }

    public IReadOnlyList<int> FullRows() =>
        Enumerable.Range(0, Height).Where(y => _rows[y].All(c => !c.IsEmpty)).ToList();

    /// <summary>Removes the given rows, drops everything above, and returns the item ids the rows contained.</summary>
    public IReadOnlyList<string> ClearRows(IReadOnlyCollection<int> rows)
    {
        var items = new List<string>();
        var remaining = new List<Cell[]>(Height);
        for (var y = 0; y < Height; y++)
        {
            if (rows.Contains(y))
            {
                items.AddRange(_rows[y].Where(c => c.ItemId is not null).Select(c => c.ItemId!));
            }
            else
            {
                remaining.Add(_rows[y]);
            }
        }
        while (remaining.Count < Height)
        {
            remaining.Insert(0, NewRow());
        }
        _rows = remaining;
        return items;
    }

    /// <summary>
    /// Pushes garbage rows in from the bottom. Returns true if a filled cell was pushed out of the top
    /// of the buffer zone (a top out).
    /// </summary>
    public bool AddGarbage(int count, int holeColumn)
    {
        var overflow = false;
        for (var i = 0; i < count; i++)
        {
            overflow |= _rows[0].Any(c => !c.IsEmpty);
            _rows.RemoveAt(0);
            var row = Enumerable.Repeat(new Cell(CellColor.Garbage), Width).ToArray();
            row[Math.Clamp(holeColumn, 0, Width - 1)] = Cell.Empty;
            _rows.Add(row);
        }
        return overflow;
    }

    public void RemoveBottomRow()
    {
        _rows.RemoveAt(Height - 1);
        _rows.Insert(0, NewRow());
    }

    public void Clear() => _rows = Enumerable.Range(0, Height).Select(_ => NewRow()).ToList();

    /// <summary>Exchanges all locked cells with another board of the same size.</summary>
    public void SwapContents(Board other)
    {
        if (other.Width != Width || other.Height != Height)
        {
            throw new InvalidOperationException("Boards must have equal dimensions to swap.");
        }
        (_rows, other._rows) = (other._rows, _rows);
    }

    private Cell[] NewRow() => Enumerable.Repeat(Cell.Empty, Width).ToArray();
}
