using ActionBlocks.Game.Core;
using ActionBlocks.Game.Items;
using static ActionBlocks.Game.Tests.TestHelpers;

namespace ActionBlocks.Game.Tests;

public class ConfigTests
{
    private static GameSettings WithItems(params (string Id, ItemSettings Item)[] items) =>
        new() { Items = items.ToDictionary(i => i.Id, i => i.Item, StringComparer.OrdinalIgnoreCase) };

    private static ItemContext Context(PlayerGame target, double now) => new(target, target, new Random(3), now);

    [Fact]
    public void Empty_settings_keep_the_built_in_defaults()
    {
        var registry = ItemRegistry.FromSettings(new GameSettings());

        Assert.Equal(6, registry.All.Count);
        Assert.All(registry.All, i => Assert.Equal(1.0 / 6, registry.Share(i.Id), 6));
        Assert.Equal(4, registry.Get("blind")!.DurationSeconds);
        Assert.Equal(8, registry.Get("transform")!.DurationSeconds);
        Assert.Null(registry.Get("nuke")!.DurationSeconds);
        Assert.Empty(new GameSettings().Validate());
    }

    [Fact]
    public void Configured_durations_change_the_effect_length()
    {
        var registry = ItemRegistry.FromSettings(WithItems(
            ("blind", new ItemSettings { DurationSeconds = 2 }),
            ("transform", new ItemSettings { DurationSeconds = 3 })));
        var target = CreatePlayer("b");
        target.Start();

        registry.Get("blind")!.Apply(Context(target, 10));
        registry.Get("transform")!.Apply(Context(target, 10));

        target.Update(0.01, 11.9);
        Assert.True(target.IsInvisible);
        target.Update(0.01, 12.1);
        Assert.False(target.IsInvisible);
        Assert.True(target.IsTransformed);
        target.Update(0.01, 13.1);
        Assert.False(target.IsTransformed);
    }

    [Fact]
    public void Only_items_with_weight_spawn()
    {
        var settings = WithItems(
            ("add_line", new ItemSettings { Weight = 0 }),
            ("clear_line", new ItemSettings { Weight = 0 }),
            ("nuke", new ItemSettings { Weight = 0 }),
            ("swap", new ItemSettings { Weight = 0 }),
            ("transform", new ItemSettings { Weight = 0 }));
        var spawner = new ItemSpawner(ItemRegistry.FromSettings(settings), 1, new Random(1));

        var items = Enumerable.Range(0, 50).SelectMany(_ => spawner.Decorate(PieceType.T).Items!).ToHashSet();

        Assert.Equal(["blind"], items);
    }

    [Fact]
    public void Weights_split_the_item_chance()
    {
        var settings = WithItems(
            ("add_line", new ItemSettings { Weight = 3 }),
            ("nuke", new ItemSettings { Weight = 1 }),
            ("clear_line", new ItemSettings { Weight = 0 }),
            ("swap", new ItemSettings { Weight = 0 }),
            ("blind", new ItemSettings { Weight = 0 }),
            ("transform", new ItemSettings { Weight = 0 }));
        var registry = ItemRegistry.FromSettings(settings);
        var random = new Random(7);

        var picks = Enumerable.Range(0, 4000).Select(_ => registry.PickRandom(random).Id).ToList();

        Assert.Equal(0.75, registry.Share("add_line"));
        Assert.Equal(0.25, registry.Share("nuke"));
        Assert.InRange(picks.Count(p => p == "add_line") / 4000.0, 0.72, 0.78);
        Assert.Equal(4000, picks.Count(p => p is "add_line" or "nuke"));
    }

    [Fact]
    public void All_weights_zero_means_no_items()
    {
        var settings = new GameSettings
        {
            Items = ItemRegistry.Default.All.ToDictionary(i => i.Id, _ => new ItemSettings { Weight = 0 }),
        };
        var spawner = new ItemSpawner(ItemRegistry.FromSettings(settings), 1, new Random(1));

        Assert.Empty(settings.Validate());
        Assert.False(spawner.Decorate(PieceType.T).HasItem);
    }

    [Fact]
    public void Item_ids_are_case_insensitive()
    {
        var settings = new GameSettings { Items = new() { ["Blind"] = new ItemSettings { DurationSeconds = 2 } } };

        Assert.Empty(settings.Validate());
        Assert.Equal(2, ItemRegistry.FromSettings(settings).Get("blind")!.DurationSeconds);
    }

    [Fact]
    public void Missing_items_keep_weight_one()
    {
        var registry = ItemRegistry.FromSettings(WithItems(("nuke", new ItemSettings { Weight = 5 })));

        Assert.Equal(0.5, registry.Share("nuke"));
        Assert.Equal(0.1, registry.Share("blind"), 6);
    }

    [Fact]
    public void Validate_reports_every_problem()
    {
        var settings = WithItems(
            ("add_line", new ItemSettings { Weight = -1 }),
            ("blind", new ItemSettings { DurationSeconds = 0 }),
            ("nuke", new ItemSettings { DurationSeconds = 5 }),
            ("laser", new ItemSettings { Weight = 1 })) with { ItemSpawnChance = 1.5 };

        var errors = settings.Validate();

        Assert.Equal(5, errors.Count);
        Assert.Contains(errors, e => e.StartsWith("ItemSpawnChance"));
        Assert.Contains(errors, e => e.StartsWith("Items.add_line.Weight"));
        Assert.Contains(errors, e => e.StartsWith("Items.blind.DurationSeconds"));
        Assert.Contains(errors, e => e.StartsWith("Items.nuke: this item acts instantly"));
        Assert.Contains(errors, e => e.StartsWith("Items.laser: unknown item"));
    }
}
