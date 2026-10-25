namespace ActionBlocks.Game.Items;

/// <summary>All items that exist in the game, each with its spawn weight. Register new items in <see cref="FromSettings"/>.</summary>
public sealed class ItemRegistry
{
    private readonly Dictionary<string, (IItem Item, double Weight)> _items;
    private readonly double _totalWeight;

    public ItemRegistry(IEnumerable<IItem> items)
        : this(items.Select(i => (i, 1.0)))
    {
    }

    public ItemRegistry(IEnumerable<(IItem Item, double Weight)> items)
    {
        _items = items.ToDictionary(e => e.Item.Id, e => (e.Item, Math.Max(0, e.Weight)), StringComparer.OrdinalIgnoreCase);
        _totalWeight = _items.Values.Sum(e => e.Weight);
    }

    /// <summary>The built-in items with their default durations and equal weights.</summary>
    public static ItemRegistry Default { get; } = FromSettings(new GameSettings());

    /// <summary>
    /// The built-in items with durations and weights from <see cref="GameSettings.Items"/>.
    /// Items missing there keep weight 1 and their default duration.
    /// </summary>
    public static ItemRegistry FromSettings(GameSettings settings)
    {
        // Item ids in the config are case-insensitive, like all configuration keys.
        var config = new Dictionary<string, ItemSettings>(StringComparer.OrdinalIgnoreCase);
        foreach (var (id, item) in settings.Items)
        {
            config[id] = item;
        }
        double Duration(string id, double fallback) => config.GetValueOrDefault(id)?.DurationSeconds ?? fallback;

        IItem[] items =
        [
            new AddGarbageLineItem(),
            new ClearBottomLineItem(),
            new ClearBoardItem(),
            new SwapBoardItem(),
            new InvisibilityItem(Duration(InvisibilityItem.ItemId, InvisibilityItem.DefaultDurationSeconds)),
            new TransformationItem(Duration(TransformationItem.ItemId, TransformationItem.DefaultDurationSeconds)),
        ];
        return new(items.Select(i => (i, config.GetValueOrDefault(i.Id)?.Weight ?? 1)));
    }

    public IReadOnlyCollection<IItem> All => _items.Values.Select(e => e.Item).ToList();

    /// <summary>False when every weight is 0, i.e. no item can spawn.</summary>
    public bool CanSpawn => _totalWeight > 0;

    public IItem? Get(string id) => _items.TryGetValue(id, out var e) ? e.Item : null;

    /// <summary>Fraction (0..1) of spawned items that are <paramref name="id"/>.</summary>
    public double Share(string id) =>
        _totalWeight > 0 && _items.TryGetValue(id, out var e) ? e.Weight / _totalWeight : 0;

    public IItem PickRandom(Random random)
    {
        var roll = random.NextDouble() * _totalWeight;
        IItem? last = null;
        foreach (var (item, weight) in _items.Values)
        {
            if (weight <= 0)
            {
                continue;
            }
            last = item;
            roll -= weight;
            if (roll < 0)
            {
                return item;
            }
        }
        return last ?? throw new InvalidOperationException("No item has a spawn weight above 0.");
    }
}

/// <summary>Decides which minos of a new piece carry an item. Each mino rolls <c>chance</c> (0..1) on its own.</summary>
public sealed class ItemSpawner(ItemRegistry registry, double chance, Random random)
{
    private readonly double _chance = double.IsNaN(chance) ? 0 : Math.Clamp(chance, 0, 1);

    public Core.PieceSpec Decorate(Core.PieceType type)
    {
        if (!registry.CanSpawn)
        {
            return new Core.PieceSpec(type);
        }
        var items = new string?[Core.PieceDefinitions.Minos(type, 0).Count];
        var any = false;
        for (var i = 0; i < items.Length; i++)
        {
            if (random.NextDouble() < _chance)
            {
                items[i] = registry.PickRandom(random).Id;
                any = true;
            }
        }
        return any ? new Core.PieceSpec(type, items) : new Core.PieceSpec(type);
    }
}
