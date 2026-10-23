namespace ActionBlocks.Game.Items;

/// <summary>All items that exist in the game. Register new items here.</summary>
public sealed class ItemRegistry
{
    private readonly Dictionary<string, IItem> _items;

    public ItemRegistry(IEnumerable<IItem> items)
    {
        _items = items.ToDictionary(i => i.Id);
    }

    public static ItemRegistry Default { get; } = new(
    [
        new AddGarbageLineItem(),
        new ClearBottomLineItem(),
        new ClearBoardItem(),
        new SwapBoardItem(),
        new InvisibilityItem(),
    ]);

    public IReadOnlyCollection<IItem> All => _items.Values;

    public IItem? Get(string id) => _items.GetValueOrDefault(id);

    public IItem PickRandom(Random random)
    {
        var total = _items.Values.Sum(i => i.SpawnWeight);
        var roll = random.Next(total);
        foreach (var item in _items.Values)
        {
            roll -= item.SpawnWeight;
            if (roll < 0)
            {
                return item;
            }
        }
        return _items.Values.First();
    }
}

/// <summary>Decides whether a new piece carries an item and in which of its minos.</summary>
public sealed class ItemSpawner(ItemRegistry registry, double chance, Random random)
{
    public Core.PieceSpec Decorate(Core.PieceType type)
    {
        if (registry.All.Count == 0 || random.NextDouble() >= chance)
        {
            return new Core.PieceSpec(type);
        }
        var item = registry.PickRandom(random);
        return new Core.PieceSpec(type, item.Id, random.Next(4));
    }
}
