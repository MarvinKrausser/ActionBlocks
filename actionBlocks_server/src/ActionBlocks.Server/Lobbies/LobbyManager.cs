using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ActionBlocks.Server.Lobbies;

public sealed class LobbyException(string message) : Exception(message);

/// <summary>Keeps all lobbies in memory and tracks which connection belongs to which lobby.</summary>
public sealed class LobbyManager
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 6;
    private const int MaxNameLength = 16;

    private readonly ConcurrentDictionary<string, Lobby> _lobbies = new();
    private readonly ConcurrentDictionary<string, string> _connectionLobby = new();

    public IEnumerable<Lobby> All => _lobbies.Values;

    public (Lobby Lobby, LobbyPlayer Player) Create(string connectionId, string name)
    {
        while (true)
        {
            var lobby = new Lobby(GenerateCode());
            if (_lobbies.TryAdd(lobby.Code, lobby))
            {
                return Join(connectionId, lobby.Code, name);
            }
        }
    }

    public (Lobby Lobby, LobbyPlayer Player) Join(string connectionId, string code, string name)
    {
        if (_connectionLobby.ContainsKey(connectionId))
        {
            throw new LobbyException("You are already in a lobby.");
        }
        var normalized = (code ?? "").Trim().ToUpperInvariant();
        if (!_lobbies.TryGetValue(normalized, out var lobby))
        {
            throw new LobbyException("Lobby not found.");
        }

        lock (lobby.Sync)
        {
            if (!_lobbies.ContainsKey(normalized))
            {
                throw new LobbyException("Lobby not found.");
            }
            if (lobby.Players.Count >= Lobby.MaxPlayers)
            {
                throw new LobbyException("Lobby is full.");
            }
            var player = new LobbyPlayer(Guid.NewGuid().ToString("N")[..12], connectionId, CleanName(name, lobby.Players.Count + 1));
            lobby.Add(player);
            _connectionLobby[connectionId] = lobby.Code;
            return (lobby, player);
        }
    }

    public Lobby? FindByConnection(string connectionId) =>
        _connectionLobby.TryGetValue(connectionId, out var code) ? _lobbies.GetValueOrDefault(code) : null;

    /// <summary>Removes the connection from its lobby. Returns the lobby if it still has players.</summary>
    public Lobby? Leave(string connectionId)
    {
        if (!_connectionLobby.TryRemove(connectionId, out var code) || !_lobbies.TryGetValue(code, out var lobby))
        {
            return null;
        }
        lock (lobby.Sync)
        {
            if (lobby.FindByConnection(connectionId) is { } player)
            {
                lobby.Remove(player);
            }
            if (lobby.Players.Count == 0)
            {
                _lobbies.TryRemove(code, out _);
                return null;
            }
            return lobby;
        }
    }

    private static string GenerateCode() =>
        string.Create(CodeLength, 0, (span, _) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
            }
        });

    private static string CleanName(string? name, int fallbackNumber)
    {
        var cleaned = new string((name ?? "").Where(c => !char.IsControl(c)).ToArray()).Trim();
        if (cleaned.Length > MaxNameLength)
        {
            cleaned = cleaned[..MaxNameLength];
        }
        return cleaned.Length == 0 ? $"Player {fallbackNumber}" : cleaned;
    }
}
