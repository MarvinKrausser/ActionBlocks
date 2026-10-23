using ActionBlocks.Game.Core;
using static ActionBlocks.Game.Tests.TestHelpers;

namespace ActionBlocks.Game.Tests;

public class PlayerGameTests
{
    [Fact]
    public void Piece_spawns_centered_and_falls_with_gravity()
    {
        var player = CreatePlayer(pieces: PieceType.T);
        player.Start();
        var startY = player.Active!.Y;
        Assert.Equal(3, player.Active.X);

        player.Update(1.1, 1.1);

        Assert.Equal(startY + 1, player.Active!.Y);
    }

    [Fact]
    public void Hard_drop_clears_line_and_collects_item_in_row()
    {
        var player = CreatePlayer(pieces: PieceType.I);
        var bottom = Bottom(player.Board);
        FillRow(player.Board, bottom, 3, 4, 5, 6);
        player.Board[0, bottom] = new Cell(CellColor.Garbage, "swap");
        player.Start();

        player.HandleInput(InputAction.HardDrop);

        Assert.Equal(1, player.Lines);
        Assert.Equal(["swap"], player.Inventory);
        Assert.Equal(0, FilledCount(player.Board));
    }

    [Fact]
    public void Item_inside_piece_is_collected_when_its_row_clears()
    {
        var player = CreatePlayer(itemChance: 1, pieces: PieceType.I);
        FillRow(player.Board, Bottom(player.Board), 3, 4, 5, 6);
        player.Start();
        var itemId = player.Active!.Spec.ItemId;
        Assert.NotNull(itemId);

        player.HandleInput(InputAction.HardDrop);

        Assert.Equal([itemId!], player.Inventory);
    }

    [Fact]
    public void Item_stays_on_board_when_row_is_not_cleared()
    {
        var player = CreatePlayer(itemChance: 1, pieces: PieceType.I);
        player.Start();

        player.HandleInput(InputAction.HardDrop);

        Assert.Empty(player.Inventory);
        var bottom = Bottom(player.Board);
        Assert.Contains(Enumerable.Range(0, 10), x => player.Board[x, bottom].ItemId is not null);
    }

    [Fact]
    public void Hold_swaps_piece_once_per_lock()
    {
        var player = CreatePlayer(pieces: [PieceType.T, PieceType.O, PieceType.S]);
        player.Start();

        player.HandleInput(InputAction.Hold);
        Assert.Equal(PieceType.T, player.Hold!.Type);
        Assert.Equal(PieceType.O, player.Active!.Type);

        player.HandleInput(InputAction.Hold);
        Assert.Equal(PieceType.O, player.Active!.Type);
    }

    [Fact]
    public void Pending_garbage_rises_after_a_lock_without_clear()
    {
        var player = CreatePlayer(pieces: PieceType.O);
        player.Start();
        player.QueueGarbage(2);

        player.HandleInput(InputAction.HardDrop);

        var bottom = Bottom(player.Board);
        Assert.Equal(9, Enumerable.Range(0, 10).Count(x => player.Board[x, bottom].Color == CellColor.Garbage));
        Assert.Equal(0, player.PendingGarbage);
    }

    [Fact]
    public void Double_clear_sends_one_line()
    {
        var player = CreatePlayer(pieces: PieceType.O);
        var bottom = Bottom(player.Board);
        FillRow(player.Board, bottom, 4, 5);
        FillRow(player.Board, bottom - 1, 4, 5);
        player.Board[0, bottom - 2] = new Cell(1); // avoid a perfect clear
        player.Start();

        player.HandleInput(InputAction.HardDrop);

        Assert.Equal([1], player.DrainOutgoingAttacks());
    }

    [Fact]
    public void Stacking_to_the_top_ends_the_game()
    {
        var player = CreatePlayer(pieces: PieceType.O);
        player.Start();
        for (var i = 0; i < 30 && player.IsAlive; i++)
        {
            player.HandleInput(InputAction.HardDrop);
        }
        Assert.False(player.IsAlive);
    }

    [Fact]
    public void Inventory_is_capped()
    {
        var player = CreatePlayer();
        for (var i = 0; i < 20; i++)
        {
            player.AddItem("nuke");
        }
        Assert.Equal(new GameSettings().MaxInventory, player.Inventory.Count);
    }
}
