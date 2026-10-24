using ActionBlocks.Game;
using ActionBlocks.Server.Dtos;
using ActionBlocks.Server.Lobbies;
using Microsoft.AspNetCore.SignalR;

namespace ActionBlocks.Server.Hubs;

/// <summary>
/// Client-facing API. Server-to-client messages: "LobbyUpdated" (LobbyDto) and "MatchState" (MatchStateDto).
/// </summary>
public sealed class GameHub(LobbyManager lobbies, GameSettings settings) : Hub
{
    public const string LobbyUpdated = "LobbyUpdated";
    public const string MatchState = "MatchState";

    public async Task<JoinResultDto> CreateLobby(string name)
    {
        var (lobby, player) = Run(() => lobbies.Create(Context.ConnectionId, name));
        return await Joined(lobby, player);
    }

    public async Task<JoinResultDto> JoinLobby(string code, string name)
    {
        var (lobby, player) = Run(() => lobbies.Join(Context.ConnectionId, code, name));
        return await Joined(lobby, player);
    }

    public async Task LeaveLobby()
    {
        var lobby = lobbies.FindByConnection(Context.ConnectionId);
        if (lobby is null)
        {
            return;
        }
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, lobby.Code);
        await LeaveAndNotify();
    }

    public async Task StartGame()
    {
        var lobby = lobbies.FindByConnection(Context.ConnectionId) ?? throw new HubException("You are not in a lobby.");
        LobbyDto dto;
        lock (lobby.Sync)
        {
            var player = lobby.FindByConnection(Context.ConnectionId);
            if (player is null || player.Id != lobby.HostId)
            {
                throw new HubException("Only the host can start the game.");
            }
            if (lobby.IsPlaying)
            {
                throw new HubException("A game is already running.");
            }
            lobby.StartMatch(settings);
            dto = lobby.ToDto();
        }
        await Clients.Group(lobby.Code).SendAsync(LobbyUpdated, dto);
    }

    public void Input(InputAction action) =>
        WithPlayer((lobby, player) =>
        {
            lobby.Match?.HandleInput(player.Id, action);
            lobby.Dirty = true;
        });

    public void SelectTarget(string targetId) =>
        WithPlayer((lobby, player) => lobby.Match?.SelectTarget(player.Id, targetId));

    public IReadOnlyList<ItemInfoDto> GetItems() =>
        Game.Items.ItemRegistry.Default.All.Select(i => new ItemInfoDto(i.Id, i.DisplayName)).ToList();

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveAndNotify();
        await base.OnDisconnectedAsync(exception);
    }

    private async Task<JoinResultDto> Joined(Lobby lobby, LobbyPlayer player)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, lobby.Code);
        LobbyDto dto;
        lock (lobby.Sync)
        {
            dto = lobby.ToDto();
        }
        await Clients.OthersInGroup(lobby.Code).SendAsync(LobbyUpdated, dto);
        return new JoinResultDto(player.Id, dto);
    }

    private async Task LeaveAndNotify()
    {
        var lobby = lobbies.Leave(Context.ConnectionId);
        if (lobby is null)
        {
            return;
        }
        LobbyDto dto;
        lock (lobby.Sync)
        {
            lobby.Dirty = true;
            dto = lobby.ToDto();
        }
        await Clients.Group(lobby.Code).SendAsync(LobbyUpdated, dto);
    }

    private void WithPlayer(Action<Lobby, LobbyPlayer> action)
    {
        var lobby = lobbies.FindByConnection(Context.ConnectionId);
        if (lobby is null)
        {
            return;
        }
        lock (lobby.Sync)
        {
            if (lobby.FindByConnection(Context.ConnectionId) is { } player)
            {
                action(lobby, player);
            }
        }
    }

    private static T Run<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (LobbyException e)
        {
            throw new HubException(e.Message);
        }
    }
}
