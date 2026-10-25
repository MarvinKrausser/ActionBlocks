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
        new TransformationItem(),
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

/// <summary>Decides which minos of a new piece carry an item. Each mino rolls <c>chance</c> (0..1) on its own.</summary>
public sealed class ItemSpawner(ItemRegistry registry, double chance, Random random)
{
    private readonly double _chance = double.IsNaN(chance) ? 0 : Math.Clamp(chance, 0, 1);

    public Core.PieceSpec Decorate(Core.PieceType type)
    {
        if (registry.All.Count == 0)
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
