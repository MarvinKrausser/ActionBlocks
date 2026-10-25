using System.Text;
using ActionBlocks.Game;
using ActionBlocks.Game.Core;

namespace ActionBlocks.Server.Dtos;

/// <summary>Converts game objects into compact network snapshots. Only the visible rows are sent.</summary>
public static class SnapshotMapper
{
    public static MatchStateDto ToDto(Match match, IReadOnlyList<MatchEvent> events) => new(
        match.Phase.ToString().ToLowerInvariant(),
        Math.Round(match.CountdownRemaining, 2),
        match.WinnerId,
        match.Settings.BoardWidth,
        match.Settings.VisibleRows,
        match.Players.Select(p => ToDto(p, match)).ToList(),
        events.Select(ToDto).ToList());

    private static PlayerStateDto ToDto(PlayerGame player, Match match)
    {
        var board = player.Board;
        var cells = new StringBuilder(board.Width * board.VisibleRows);
        var items = new List<ItemCellDto>();
        for (var y = board.BufferRows; y < board.Height; y++)
        {
            for (var x = 0; x < board.Width; x++)
            {
                var cell = board[x, y];
                cells.Append((char)('0' + cell.Color));
                if (cell.ItemId is not null)
                {
                    items.Add(new ItemCellDto(x, y - board.BufferRows, cell.ItemId));
                }
            }
        }

        return new PlayerStateDto(
            player.PlayerId,
            player.Name,
            player.IsAlive,
            match.Places.TryGetValue(player.PlayerId, out var place) ? place : null,
            cells.ToString(),
            items,
            player.Active is { } active && player.IsAlive ? ToDto(active, board) : null,
            player.IsAlive ? player.Ghost() is { } ghost ? ToCells(ghost, board) : null : null,
            player.Hold is { } hold ? ToDto(hold) : null,
            player.HoldUsed,
            player.Next.Select(ToDto).ToList(),
            player.Inventory.ToList(),
            player.Score,
            player.Lines,
            player.Level,
            player.PendingGarbage,
            Math.Round(Math.Max(0, player.InvisibleUntil - match.Now), 2),
            Math.Round(Math.Max(0, player.TransformedUntil - match.Now), 2));
    }

    private static ActivePieceDto ToDto(ActivePiece piece, Board board) =>
        new((int)piece.Type, ToCells(piece, board), ToItems(piece.Spec));

    private static int[][] ToCells(ActivePiece piece, Board board) =>
        piece.Cells().Select(c => new[] { c.X, c.Y - board.BufferRows }).ToArray();

    private static PieceDto ToDto(PieceSpec spec) => new((int)spec.Type, ToItems(spec));

    private static string?[] ToItems(PieceSpec spec) => spec.Items?.ToArray() ?? new string?[PieceDefinitions.Minos(spec.Type, 0).Count];

    private static MatchEventDto ToDto(MatchEvent e) => e switch
    {
        ItemUsedEvent used => new MatchEventDto("itemUsed", TargetId: used.TargetId, ItemId: used.ItemId, ItemName: used.ItemName),
        PlayerEliminatedEvent eliminated => new MatchEventDto("eliminated", PlayerId: eliminated.PlayerId, Place: eliminated.Place),
        _ => new MatchEventDto("unknown"),
    };
}
