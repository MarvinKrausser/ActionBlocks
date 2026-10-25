namespace ActionBlocks.Game.Items;

/// <summary>
/// An item that can be found inside pieces and used on a player.
/// To add a new item: implement this interface and add it to <see cref="ItemRegistry.FromSettings"/>.
/// The frontend needs a matching icon entry for <see cref="Id"/>.
/// </summary>
public interface IItem
{
    /// <summary>Stable identifier shared with the frontend (icons, colors).</summary>
    string Id { get; }

    /// <summary>Name shown when the item is used.</summary>
    string DisplayName { get; }

    /// <summary>How long the effect lasts, or null for items that act instantly.</summary>
    double? DurationSeconds => null;

    void Apply(ItemContext context);
}

public sealed record ItemContext(PlayerGame User, PlayerGame Target, Random Random, double Now)
{
    public bool IsSelfTarget => User == Target;
}
