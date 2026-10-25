using ActionBlocks.Game.Core;
using static ActionBlocks.Game.Tests.TestHelpers;

namespace ActionBlocks.Game.Tests;

public class CoreTests
{
    [Fact]
    public void T_piece_rotates_clockwise_to_point_right()
    {
        var cells = PieceDefinitions.Minos(PieceType.T, 1).ToHashSet();
        Assert.Equal(new HashSet<Point> { new(1, 0), new(1, 1), new(2, 1), new(1, 2) }, cells);
    }

    [Fact]
    public void I_piece_vertical_state_uses_third_column()
    {
        var cells = PieceDefinitions.Minos(PieceType.I, 1);
        Assert.All(cells, c => Assert.Equal(2, c.X));
    }

    [Fact]
    public void Four_rotations_return_to_spawn_shape()
    {
        foreach (var type in PieceDefinitions.All)
        {
            Assert.Equal(PieceDefinitions.Minos(type, 0), PieceDefinitions.Minos(type, 4));
        }
    }

    [Fact]
    public void Rotations_keep_the_mino_count()
    {
        foreach (var type in PieceDefinitions.All)
        {
            var count = PieceDefinitions.Minos(type, 0).Count;
            Assert.All(Enumerable.Range(1, 3), r => Assert.Equal(count, PieceDefinitions.Minos(type, r).Distinct().Count()));
        }
    }

    [Fact]
    public void Seven_bag_never_deals_odd_shapes()
    {
        var randomizer = new SevenBagRandomizer(new Random(42));

        Assert.All(Enumerable.Range(0, 70), _ => Assert.Contains(randomizer.Next(), PieceDefinitions.Standard));
    }

    [Fact]
    public void Seven_bag_deals_every_shape_once_per_bag()
    {
        var randomizer = new SevenBagRandomizer(new Random(42));
        for (var bag = 0; bag < 3; bag++)
        {
            var pieces = Enumerable.Range(0, 7).Select(_ => randomizer.Next()).ToHashSet();
            Assert.Equal(7, pieces.Count);
        }
    }

    [Fact]
    public void Rotation_against_wall_uses_kick()
    {
        var board = new Board(10, 20, 20);
        var srs = new SuperRotationSystem();
        // Vertical I hugging the left wall (column 0 in state L uses box column 1).
        var piece = new ActivePiece(new PieceSpec(PieceType.I), -1, 30, 3);
        Assert.True(board.Fits(piece));

        var result = srs.TryRotate(board, piece, 1);

        Assert.NotNull(result);
        Assert.True(result.Value.KickIndex > 0);
        Assert.True(board.Fits(result.Value.Piece));
    }

    [Fact]
    public void Clearing_rows_returns_contained_items_and_drops_rows_above()
    {
        var board = new Board(10, 20, 20);
        var bottom = Bottom(board);
        FillRow(board, bottom);
        board[3, bottom] = new Cell(CellColor.Garbage, "nuke");
        board[0, bottom - 1] = new Cell(1);

        var items = board.ClearRows(board.FullRows().ToList());

        Assert.Equal(["nuke"], items);
        Assert.False(board[0, bottom].IsEmpty);
        Assert.Equal(1, FilledCount(board));
    }

    [Fact]
    public void Garbage_pushes_rows_up_with_a_hole()
    {
        var board = new Board(10, 20, 20);
        board[5, Bottom(board)] = new Cell(1);

        var overflow = board.AddGarbage(2, 4);

        Assert.False(overflow);
        Assert.True(board[4, Bottom(board)].IsEmpty);
        Assert.False(board[5, Bottom(board) - 2].IsEmpty);
        Assert.Equal(19, FilledCount(board));
    }
}
