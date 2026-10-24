using ActionBlocks.Game.Core;
using ActionBlocks.Game.Garbage;
using ActionBlocks.Game.Items;

namespace ActionBlocks.Game.Tests;

internal sealed class FixedRandomizer(params PieceType[] sequence) : IRandomizer
{
    private int _index;

    public PieceType Next() => sequence[_index++ % sequence.Length];
}

internal static class TestHelpers
{
    public static PlayerGame CreatePlayer(string id = "p1", double itemChance = 0, params PieceType[] pieces) =>
        new(
            id,
            id,
            new GameSettings { ItemSpawnChance = itemChance },
            new FixedRandomizer(pieces.Length == 0 ? [PieceType.I] : pieces),
            new ItemSpawner(ItemRegistry.Default, itemChance, new Random(1)),
            new SuperRotationSystem(),
            new StandardGarbageCalculator(),
            new StandardScoreCalculator(),
            new Random(1));

    public static int Bottom(Board board) => board.Height - 1;

    /// <summary>Fills a row except the given columns.</summary>
    public static void FillRow(Board board, int y, params int[] holes)
    {
        for (var x = 0; x < board.Width; x++)
        {
            board[x, y] = holes.Contains(x) ? Cell.Empty : new Cell(CellColor.Garbage);
        }
    }

    public static int FilledCount(Board board) =>
        Enumerable.Range(0, board.Height).Sum(y => Enumerable.Range(0, board.Width).Count(x => !board[x, y].IsEmpty));
}
