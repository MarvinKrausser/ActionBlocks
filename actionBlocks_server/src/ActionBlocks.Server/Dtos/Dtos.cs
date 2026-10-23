namespace ActionBlocks.Server.Dtos;

public sealed record LobbyPlayerDto(string Id, string Name, bool IsHost);

public sealed record ResultDto(string Id, string Name, int Place, int Score, int Lines);

public sealed record LobbyDto(
    string Code,
    string HostId,
    string Phase,
    int MaxPlayers,
    IReadOnlyList<LobbyPlayerDto> Players,
    IReadOnlyList<ResultDto>? LastResults);

public sealed record JoinResultDto(string PlayerId, LobbyDto Lobby);

public sealed record PieceDto(int Type, string? ItemId, int ItemIndex);

public sealed record ItemCellDto(int X, int Y, string ItemId);

public sealed record ActivePieceDto(int Type, int[][] Cells, int ItemIndex, string? ItemId);

public sealed record PlayerStateDto(
    string Id,
    string Name,
    bool Alive,
    int? Place,
    string Board,
    IReadOnlyList<ItemCellDto> Items,
    ActivePieceDto? Active,
    int[][]? Ghost,
    PieceDto? Hold,
    bool HoldUsed,
    IReadOnlyList<PieceDto> Next,
    IReadOnlyList<string> Inventory,
    int Score,
    int Lines,
    int Level,
    int PendingGarbage,
    double InvisibleRemaining);

/// <summary>A match event. <see cref="Type"/> is "itemUsed" or "eliminated".</summary>
public sealed record MatchEventDto(
    string Type,
    string? TargetId = null,
    string? ItemId = null,
    string? ItemName = null,
    string? PlayerId = null,
    int? Place = null);

public sealed record MatchStateDto(
    string Phase,
    double Countdown,
    string? WinnerId,
    int Width,
    int Height,
    IReadOnlyList<PlayerStateDto> Players,
    IReadOnlyList<MatchEventDto> Events);

public sealed record ItemInfoDto(string Id, string Name);
