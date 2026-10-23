using ActionBlocks.Game.Core;
using ActionBlocks.Game.Items;
using ActionBlocks.Game.Targeting;
using static ActionBlocks.Game.Tests.TestHelpers;

namespace ActionBlocks.Game.Tests;

public class ItemTests
{
    private static ItemContext Context(PlayerGame user, PlayerGame target, double now = 0) =>
        new(user, target, new Random(3), now);

    [Fact]
    public void Add_line_inserts_one_garbage_row()
    {
        var user = CreatePlayer("a");
        var target = CreatePlayer("b");

        new AddGarbageLineItem().Apply(Context(user, target));

        Assert.Equal(9, FilledCount(target.Board));
        Assert.Equal(0, FilledCount(user.Board));
    }

    [Fact]
    public void Clear_line_removes_bottom_row()
    {
        var target = CreatePlayer("b");
        FillRow(target.Board, Bottom(target.Board), 0);
        target.Board[0, Bottom(target.Board) - 1] = new Cell(1);

        new ClearBottomLineItem().Apply(Context(target, target));

        Assert.Equal(1, FilledCount(target.Board));
        Assert.False(target.Board[0, Bottom(target.Board)].IsEmpty);
    }

    [Fact]
    public void Wipe_out_clears_the_board()
    {
        var target = CreatePlayer("b");
        FillRow(target.Board, Bottom(target.Board), 0);

        new ClearBoardItem().Apply(Context(target, target));

        Assert.True(target.Board.IsEmpty);
    }

    [Fact]
    public void Switch_exchanges_boards()
    {
        var user = CreatePlayer("a");
        var target = CreatePlayer("b");
        FillRow(target.Board, Bottom(target.Board), 0);

        new SwapBoardItem().Apply(Context(user, target));

        Assert.Equal(9, FilledCount(user.Board));
        Assert.True(target.Board.IsEmpty);
    }

    [Fact]
    public void Switch_pushes_active_piece_up_when_it_would_overlap()
    {
        var user = CreatePlayer("a");
        var target = CreatePlayer("b");
        user.Start();
        for (var y = 20; y < target.Board.Height; y++)
        {
            FillRow(target.Board, y, 0);
        }

        new SwapBoardItem().Apply(Context(user, target));

        Assert.True(user.IsAlive);
        Assert.True(user.Board.Fits(user.Active!));
    }

    [Fact]
    public void Blackout_hides_board_for_four_seconds()
    {
        var target = CreatePlayer("b");
        target.Start();

        new InvisibilityItem().Apply(Context(target, target, now: 10));

        target.Update(0.01, 13.9);
        Assert.True(target.IsInvisible);
        target.Update(0.01, 14.1);
        Assert.False(target.IsInvisible);
    }

    [Fact]
    public void Blackout_durations_stack()
    {
        var target = CreatePlayer("b");
        target.Start();

        new InvisibilityItem().Apply(Context(target, target, now: 10));
        new InvisibilityItem().Apply(Context(target, target, now: 12));

        target.Update(0.01, 17.9);
        Assert.True(target.IsInvisible);
        target.Update(0.01, 18.1);
        Assert.False(target.IsInvisible);
    }

    [Fact]
    public void Blackout_after_expiry_starts_fresh()
    {
        var target = CreatePlayer("b");
        target.Start();

        new InvisibilityItem().Apply(Context(target, target, now: 10));
        new InvisibilityItem().Apply(Context(target, target, now: 20));

        target.Update(0.01, 23.9);
        Assert.True(target.IsInvisible);
        target.Update(0.01, 24.1);
        Assert.False(target.IsInvisible);
    }

    [Fact]
    public void Registry_contains_five_unique_items()
    {
        Assert.Equal(5, ItemRegistry.Default.All.Select(i => i.Id).Distinct().Count());
    }
}

public class MatchTests
{
    private static Match CreateMatch(params string[] ids) =>
        new(ids.Select(id => new MatchPlayer(id, id)), new GameSettings { CountdownSeconds = 0 }, seed: 1);

    [Fact]
    public void Using_an_item_hits_selected_target_and_emits_event()
    {
        var match = CreateMatch("a", "b");
        var a = match.Find("a")!;
        var b = match.Find("b")!;
        a.AddItem("add_line");
        match.SelectTarget("a", "b");

        match.HandleInput("a", InputAction.UseItem);

        Assert.Empty(a.Inventory);
        Assert.Equal(9, FilledCount(b.Board));
        var used = Assert.Single(match.DrainEvents().OfType<ItemUsedEvent>());
        Assert.Equal("b", used.TargetId);
    }

    [Fact]
    public void Last_player_alive_wins()
    {
        var match = CreateMatch("a", "b", "c");

        match.Forfeit("b");
        Assert.Equal(MatchPhase.Running, match.Phase);
        match.Forfeit("c");

        Assert.Equal(MatchPhase.Finished, match.Phase);
        Assert.Equal("a", match.WinnerId);
        Assert.Equal(3, match.Places["b"]);
        Assert.Equal(2, match.Places["c"]);
    }

    [Fact]
    public void Target_falls_back_to_self_when_target_is_eliminated()
    {
        var match = CreateMatch("a", "b", "c");
        match.SelectTarget("a", "b");

        match.Forfeit("b");

        Assert.Equal("a", match.Find("a")!.TargetId);
    }

    [Fact]
    public void Garbage_targets_cycle_through_living_opponents()
    {
        var match = CreateMatch("a", "b", "c");
        var targeter = new CyclingGarbageTargeter();
        var a = match.Find("a")!;

        var targets = Enumerable.Range(0, 4).Select(_ => targeter.NextTarget(a, match.Players)!.PlayerId).ToList();

        Assert.Equal(["b", "c", "b", "c"], targets);
    }

    [Fact]
    public void Countdown_delays_the_start()
    {
        var match = new Match([new MatchPlayer("a", "a")], new GameSettings { CountdownSeconds = 1 });
        Assert.Equal(MatchPhase.Countdown, match.Phase);
        Assert.Null(match.Find("a")!.Active);

        match.Update(1.01);

        Assert.Equal(MatchPhase.Running, match.Phase);
        Assert.NotNull(match.Find("a")!.Active);
    }
}
