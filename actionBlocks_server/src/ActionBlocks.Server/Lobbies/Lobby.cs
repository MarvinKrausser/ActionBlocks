using ActionBlocks.Game;
using ActionBlocks.Server.Dtos;

namespace ActionBlocks.Server.Lobbies;

public sealed record LobbyPlayer(string Id, string ConnectionId, string Name);

/// <summary>A group of players sharing a lobby code. All access goes through <see cref="Sync"/>.</summary>
public sealed class Lobby(string code)
{
    public const int MaxPlayers = 8;

    private readonly List<LobbyPlayer> _players = [];

    public string Code { get; } = code;
    public object Sync { get; } = new();
    public IReadOnlyList<LobbyPlayer> Players => _players;
    public string HostId { get; private set; } = "";

    public Match? Match { get; private set; }
    public DateTimeOffset? MatchFinishedAt { get; set; }
    public IReadOnlyList<ResultDto>? LastResults { get; private set; }

    /// <summary>Set when a snapshot should be sent on the next tick regardless of the broadcast rate.</summary>
    public bool Dirty { get; set; }

    public bool IsPlaying => Match is not null;

    public LobbyPlayer? FindByConnection(string connectionId) =>
        _players.FirstOrDefault(p => p.ConnectionId == connectionId);

    public void Add(LobbyPlayer player)
    {
        _players.Add(player);
        if (_players.Count == 1)
        {
            HostId = player.Id;
        }
    }

    public void Remove(LobbyPlayer player)
    {
        _players.Remove(player);
        Match?.Forfeit(player.Id);
        if (HostId == player.Id)
        {
            HostId = _players.FirstOrDefault()?.Id ?? "";
        }
    }

    public void StartMatch(GameSettings settings)
    {
        Match = new Match(_players.Select(p => new MatchPlayer(p.Id, p.Name)), settings);
        MatchFinishedAt = null;
        Dirty = true;
    }

    public void EndMatch()
    {
        if (Match is null)
        {
            return;
        }
        LastResults = Match.Players
            .Select(p => new ResultDto(p.PlayerId, p.Name, Match.Places.GetValueOrDefault(p.PlayerId, 1), p.Score, p.Lines))
            .OrderBy(r => r.Place)
            .ToList();
        Match = null;
        MatchFinishedAt = null;
    }

    public LobbyDto ToDto() => new(
        Code,
        HostId,
        IsPlaying ? "playing" : "waiting",
        MaxPlayers,
        _players.Select(p => new LobbyPlayerDto(p.Id, p.Name, p.Id == HostId)).ToList(),
        LastResults);
}
