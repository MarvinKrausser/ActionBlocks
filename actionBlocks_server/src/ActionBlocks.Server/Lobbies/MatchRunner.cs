using System.Diagnostics;
using ActionBlocks.Game;
using ActionBlocks.Server.Dtos;
using ActionBlocks.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace ActionBlocks.Server.Lobbies;

/// <summary>Advances every running match at a fixed rate and pushes snapshots to the lobby's clients.</summary>
public sealed class MatchRunner(LobbyManager lobbies, IHubContext<GameHub> hub, ILogger<MatchRunner> logger) : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(1000.0 / 60);
    private static readonly TimeSpan ResultsDelay = TimeSpan.FromSeconds(4);

    /// <summary>Snapshots are sent every n-th tick unless input made a lobby dirty.</summary>
    private const int BroadcastEveryTicks = 2;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TickInterval);
        var clock = Stopwatch.StartNew();
        var last = clock.Elapsed;
        long tick = 0;

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var now = clock.Elapsed;
            // Clamp to avoid huge jumps after a stall.
            var delta = Math.Min((now - last).TotalSeconds, 0.1);
            last = now;
            tick++;

            foreach (var lobby in lobbies.All)
            {
                try
                {
                    Step(lobby, delta, tick % BroadcastEveryTicks == 0);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to update lobby {Code}", lobby.Code);
                }
            }
        }
    }

    private void Step(Lobby lobby, double delta, bool broadcastTick)
    {
        MatchStateDto? snapshot = null;
        LobbyDto? lobbyUpdate = null;

        lock (lobby.Sync)
        {
            var match = lobby.Match;
            if (match is null)
            {
                return;
            }

            match.Update(delta);
            var events = match.DrainEvents();

            if (broadcastTick || lobby.Dirty || events.Count > 0)
            {
                snapshot = SnapshotMapper.ToDto(match, events);
                lobby.Dirty = false;
            }

            if (match.Phase == MatchPhase.Finished)
            {
                lobby.MatchFinishedAt ??= DateTimeOffset.UtcNow;
                if (DateTimeOffset.UtcNow - lobby.MatchFinishedAt >= ResultsDelay)
                {
                    lobby.EndMatch();
                    lobbyUpdate = lobby.ToDto();
                }
            }
        }

        if (snapshot is not null)
        {
            _ = hub.Clients.Group(lobby.Code).SendAsync(GameHub.MatchState, snapshot);
        }
        if (lobbyUpdate is not null)
        {
            _ = hub.Clients.Group(lobby.Code).SendAsync(GameHub.LobbyUpdated, lobbyUpdate);
        }
    }
}
