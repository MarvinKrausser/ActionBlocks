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

    /// <summary>Chance (0..1) that a newly generated piece carries an item.</summary>
    public double ItemSpawnChance { get; init; } = 0.1;
    public int MaxInventory { get; init; } = 10;

    public double CountdownSeconds { get; init; } = 3;

    /// <summary>Seconds per row for a level (marathon speed curve).</summary>
    public double GravitySecondsPerRow(int level)
    {
        var l = Math.Clamp(level, 1, MaxLevel);
        return Math.Pow(0.8 - (l - 1) * 0.007, l - 1);
    }
}
