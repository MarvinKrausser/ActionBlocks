using ActionBlocks.Game.Core;
using ActionBlocks.Game.Garbage;
using ActionBlocks.Game.Items;
using ActionBlocks.Game.Targeting;

namespace ActionBlocks.Game;

public enum MatchPhase
{
    Countdown,
    Running,
    Finished,
}

public abstract record MatchEvent;

/// <summary>An item hit a player. Deliberately does not say who used it.</summary>
public sealed record ItemUsedEvent(string TargetId, string ItemId, string ItemName) : MatchEvent;

public sealed record PlayerEliminatedEvent(string PlayerId, int Place) : MatchEvent;

public sealed record MatchPlayer(string Id, string Name);

/// <summary>
/// One round between a set of players. Owns every <see cref="PlayerGame"/>, routes garbage and items
/// between them and decides the winner. Not thread-safe; callers serialize access.
/// </summary>
public sealed class Match
{
    private readonly List<PlayerGame> _players;
    private readonly ItemRegistry _items;
    private readonly IGarbageTargeter _garbageTargeter;
    private readonly Random _random;
    private readonly List<MatchEvent> _events = [];
    private readonly Dictionary<string, int> _places = new();

    public Match(
        IEnumerable<MatchPlayer> players,
        GameSettings? settings = null,
        ItemRegistry? items = null,
        IGarbageTargeter? garbageTargeter = null,
        int? seed = null)
    {
        Settings = settings ?? new GameSettings();
        _items = items ?? ItemRegistry.Default;
        _garbageTargeter = garbageTargeter ?? new CyclingGarbageTargeter();
        _random = seed is null ? new Random() : new Random(seed.Value);

        // Every player receives the same piece sequence, but items are rolled independently.
        var pieceSeed = _random.Next();
        var rotation = new SuperRotationSystem();
        var garbage = new StandardGarbageCalculator();
        var score = new StandardScoreCalculator();
        _players = players.Select(p =>
        {
            var playerRandom = new Random(_random.Next());
            return new PlayerGame(
                p.Id,
                p.Name,
                Settings,
                new SevenBagRandomizer(new Random(pieceSeed)),
                new ItemSpawner(_items, Settings.ItemSpawnChance, playerRandom),
                rotation,
                garbage,
                score,
                playerRandom);
        }).ToList();

        CountdownRemaining = Settings.CountdownSeconds;
        Phase = CountdownRemaining > 0 ? MatchPhase.Countdown : MatchPhase.Running;
        if (Phase == MatchPhase.Running)
        {
            StartPlayers();
        }
    }

    public GameSettings Settings { get; }
    public IReadOnlyList<PlayerGame> Players => _players;
    public MatchPhase Phase { get; private set; }
    public double CountdownRemaining { get; private set; }
    public double Now { get; private set; }
    public string? WinnerId { get; private set; }
    public IReadOnlyDictionary<string, int> Places => _places;
    public ItemRegistry Items => _items;

    public PlayerGame? Find(string playerId) => _players.FirstOrDefault(p => p.PlayerId == playerId);

    public IReadOnlyList<MatchEvent> DrainEvents()
    {
        var result = _events.ToList();
        _events.Clear();
        return result;
    }

    public void Update(double deltaSeconds)
    {
        switch (Phase)
        {
            case MatchPhase.Countdown:
                CountdownRemaining -= deltaSeconds;
                if (CountdownRemaining <= 0)
                {
                    CountdownRemaining = 0;
                    Phase = MatchPhase.Running;
                    StartPlayers();
                }
                return;
            case MatchPhase.Finished:
                return;
        }

        Now += deltaSeconds;
        foreach (var player in _players)
        {
            player.Update(deltaSeconds, Now);
        }
        AfterStateChange();
    }

    public void HandleInput(string playerId, InputAction action)
    {
        if (Phase != MatchPhase.Running || Find(playerId) is not { IsAlive: true } player)
        {
            return;
        }

        if (action == InputAction.UseItem)
        {
            UseItem(player);
        }
        else
        {
            player.HandleInput(action);
        }
        AfterStateChange();
    }

    /// <summary>Selects who receives the player's items. Unknown or eliminated targets fall back to self.</summary>
    public void SelectTarget(string playerId, string targetId)
    {
        if (Find(playerId) is not { } player)
        {
            return;
        }
        player.TargetId = Find(targetId) is { IsAlive: true } ? targetId : playerId;
    }

    public void Forfeit(string playerId)
    {
        if (Find(playerId) is { IsAlive: true } player)
        {
            player.Forfeit();
            AfterStateChange();
        }
    }

    private void StartPlayers()
    {
        foreach (var player in _players)
        {
            player.Start();
        }
        AfterStateChange();
    }

    private void UseItem(PlayerGame user)
    {
        var target = Find(user.TargetId) is { IsAlive: true } t ? t : user;
        if (user.Inventory.Count == 0)
        {
            return;
        }
        var itemId = user.TakeFirstItem()!;
        if (_items.Get(itemId) is not { } item)
        {
            return;
        }
        item.Apply(new ItemContext(user, target, _random, Now));
        _events.Add(new ItemUsedEvent(target.PlayerId, item.Id, item.DisplayName));
    }

    private void AfterStateChange()
    {
        foreach (var player in _players)
        {
            foreach (var attack in player.DrainOutgoingAttacks())
            {
                _garbageTargeter.NextTarget(player, _players)?.QueueGarbage(attack);
            }
        }

        var alive = _players.Count(p => p.IsAlive);
        // Players eliminated in the same step share the best place still available.
        var newlyEliminated = _players.Where(p => !p.IsAlive && !_places.ContainsKey(p.PlayerId)).ToList();
        foreach (var player in newlyEliminated)
        {
            _places[player.PlayerId] = alive + 1;
            _events.Add(new PlayerEliminatedEvent(player.PlayerId, alive + 1));
        }

        foreach (var player in _players.Where(p => Find(p.TargetId) is { IsAlive: false }))
        {
            player.TargetId = player.PlayerId;
        }

        var finished = _players.Count > 1 ? alive <= 1 : alive == 0;
        if (Phase == MatchPhase.Running && finished)
        {
            Phase = MatchPhase.Finished;
            var winner = _players.FirstOrDefault(p => p.IsAlive);
            if (winner is not null)
            {
                _places[winner.PlayerId] = 1;
                WinnerId = winner.PlayerId;
            }
            else if (_players.Count > 0)
            {
                WinnerId = _players.MinBy(p => _places[p.PlayerId])!.PlayerId;
            }
        }
    }
}
