namespace ActionBlocks.Game.Items;

public sealed class AddGarbageLineItem : IItem
{
    public string Id => "add_line";
    public string DisplayName => "Add Line";

    public void Apply(ItemContext context) =>
        context.Target.InsertGarbageNow(1, context.Random.Next(context.Target.Board.Width));
}

public sealed class ClearBottomLineItem : IItem
{
    public string Id => "clear_line";
    public string DisplayName => "Clear Line";

    public void Apply(ItemContext context) => context.Target.RemoveBottomLine();
}

public sealed class ClearBoardItem : IItem
{
    public string Id => "nuke";
    public string DisplayName => "Wipe Out";

    public void Apply(ItemContext context) => context.Target.ClearBoard();
}

public sealed class SwapBoardItem : IItem
{
    public string Id => "swap";
    public string DisplayName => "Switch";

    public void Apply(ItemContext context)
    {
        if (!context.IsSelfTarget)
        {
            PlayerGame.SwapBoards(context.User, context.Target);
        }
    }
}

public sealed class InvisibilityItem(double durationSeconds = InvisibilityItem.DefaultDurationSeconds) : IItem
{
    public const string ItemId = "blind";
    public const double DefaultDurationSeconds = 4;

    public string Id => ItemId;
    public string DisplayName => "Blackout";
    public double? DurationSeconds => durationSeconds;

    // Stacks: a second Blackout adds its full duration to whatever is left.
    public void Apply(ItemContext context) =>
        context.Target.InvisibleUntil = Math.Max(context.Target.InvisibleUntil, context.Now) + durationSeconds;
}

public sealed class TransformationItem(double durationSeconds = TransformationItem.DefaultDurationSeconds) : IItem
{
    public const string ItemId = "transform";
    public const double DefaultDurationSeconds = 8;

    public string Id => ItemId;
    public string DisplayName => "Transformation";
    public double? DurationSeconds => durationSeconds;

    // Stacks like Blackout.
    public void Apply(ItemContext context) => context.Target.Transform(context.Now, durationSeconds);
}
