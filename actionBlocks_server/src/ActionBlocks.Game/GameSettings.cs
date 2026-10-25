using ActionBlocks.Game.Items;

namespace ActionBlocks.Game;

/// <summary>Tunable rules for a match. Change values here (or pass a custom instance) to alter gameplay.</summary>
public sealed record GameSettings
{
    public int BoardWidth { get; init; } = 10;
    public int VisibleRows { get; init; } = 20;
    public int BufferRows { get; init; } = 20;
    public int PreviewCount { get; init; } = 5;

    public double LockDelaySeconds { get; init; } = 0.5;
    public int MaxLockResets { get; init; } = 15;

    /// <summary>Soft drop moves this many times faster than normal gravity.</summary>
    public double SoftDropFactor { get; init; } = 20;
    public double MaxSoftDropRowsPerSecond { get; init; } = 60;

    public int LinesPerLevel { get; init; } = 10;
    public int MaxLevel { get; init; } = 15;

    /// <summary>Chance (0..1) that each mino of a newly generated piece carries an item.</summary>
    public double ItemSpawnChance { get; init; } = 0.1;
    public int MaxInventory { get; init; } = 10;

    /// <summary>
    /// Per-item tuning by item id (e.g. "blind"). Items missing here keep weight 1 and their default duration.
    /// Usually filled from config/game.json; see <see cref="ItemRegistry.FromSettings"/>. Ids are case-insensitive.
    /// </summary>
    public Dictionary<string, ItemSettings> Items { get; init; } = new();

    public double CountdownSeconds { get; init; } = 3;

    /// <summary>Seconds per row for a level (marathon speed curve).</summary>
    public double GravitySecondsPerRow(int level)
    {
        var l = Math.Clamp(level, 1, MaxLevel);
        return Math.Pow(0.8 - (l - 1) * 0.007, l - 1);
    }

    /// <summary>Problems with the item settings, as readable messages. Empty = valid.</summary>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        if (double.IsNaN(ItemSpawnChance) || ItemSpawnChance is < 0 or > 1)
        {
            errors.Add($"ItemSpawnChance must be between 0 and 1 (is {ItemSpawnChance}).");
        }
        foreach (var (id, item) in Items)
        {
            var known = ItemRegistry.Default.Get(id);
            if (known is null)
            {
                var ids = string.Join(", ", ItemRegistry.Default.All.Select(i => i.Id));
                errors.Add($"Items.{id}: unknown item. Known items: {ids}.");
                continue;
            }
            if (item.Weight is { } weight && (double.IsNaN(weight) || weight < 0))
            {
                errors.Add($"Items.{id}.Weight must be 0 or more (is {weight}).");
            }
            if (item.DurationSeconds is { } duration)
            {
                if (known.DurationSeconds is null)
                {
                    errors.Add($"Items.{id}: this item acts instantly and has no DurationSeconds.");
                }
                else if (double.IsNaN(duration) || duration <= 0)
                {
                    errors.Add($"Items.{id}.DurationSeconds must be more than 0 (is {duration}).");
                }
            }
        }
        return errors;
    }
}

/// <summary>Tuning for one item. Null = keep the default.</summary>
public sealed record ItemSettings
{
    /// <summary>Relative spawn weight: the item's share is weight / sum of all weights. 0 = never spawns.</summary>
    public double? Weight { get; init; }

    /// <summary>Effect length for timed items (Blackout, Transformation).</summary>
    public double? DurationSeconds { get; init; }
}
