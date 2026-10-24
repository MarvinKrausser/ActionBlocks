namespace ActionBlocks.Game.Items;

/// <summary>
/// An item that can be found inside pieces and used on a player.
/// To add a new item: implement this interface and register it in <see cref="ItemRegistry"/>.
/// The frontend needs a matching icon entry for <see cref="Id"/>.
/// </summary>
public interface IItem
{
    /// <summary>Stable identifier shared with the frontend (icons, colors).</summary>
    string Id { get; }

    /// <summary>Name shown when the item is used.</summary>
    string DisplayName { get; }

    /// <summary>Relative chance of this item being picked when an item spawns.</summary>
    int SpawnWeight => 1;

    void Apply(ItemContext context);
}

public sealed record ItemContext(PlayerGame User, PlayerGame Target, Random Random, double Now)
{
    public bool IsSelfTarget => User == Target;
}
