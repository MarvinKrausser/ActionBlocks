namespace ActionBlocks.Game.Core;

public interface IRotationSystem
{
    /// <summary>
    /// Tries to rotate <paramref name="piece"/> by <paramref name="direction"/> (+1 = clockwise, -1 = counter-clockwise).
    /// Returns the rotated piece and the index of the kick that was used, or null if no kick fits.
    /// </summary>
    (ActivePiece Piece, int KickIndex)? TryRotate(Board board, ActivePiece piece, int direction);
}

/// <summary>Standard rotation system with wall and floor kicks.</summary>
public sealed class SuperRotationSystem : IRotationSystem
{
    // Kick tables use the conventional y-up notation and are flipped when applied.
    private static readonly Dictionary<(int From, int To), Point[]> JlstzKicks = new()
    {
        [(0, 1)] = [new(0, 0), new(-1, 0), new(-1, 1), new(0, -2), new(-1, -2)],
        [(1, 0)] = [new(0, 0), new(1, 0), new(1, -1), new(0, 2), new(1, 2)],
        [(1, 2)] = [new(0, 0), new(1, 0), new(1, -1), new(0, 2), new(1, 2)],
        [(2, 1)] = [new(0, 0), new(-1, 0), new(-1, 1), new(0, -2), new(-1, -2)],
        [(2, 3)] = [new(0, 0), new(1, 0), new(1, 1), new(0, -2), new(1, -2)],
        [(3, 2)] = [new(0, 0), new(-1, 0), new(-1, -1), new(0, 2), new(-1, 2)],
        [(3, 0)] = [new(0, 0), new(-1, 0), new(-1, -1), new(0, 2), new(-1, 2)],
        [(0, 3)] = [new(0, 0), new(1, 0), new(1, 1), new(0, -2), new(1, -2)],
    };

    private static readonly Dictionary<(int From, int To), Point[]> IKicks = new()
    {
        [(0, 1)] = [new(0, 0), new(-2, 0), new(1, 0), new(-2, -1), new(1, 2)],
        [(1, 0)] = [new(0, 0), new(2, 0), new(-1, 0), new(2, 1), new(-1, -2)],
        [(1, 2)] = [new(0, 0), new(-1, 0), new(2, 0), new(-1, 2), new(2, -1)],
        [(2, 1)] = [new(0, 0), new(1, 0), new(-2, 0), new(1, -2), new(-2, 1)],
        [(2, 3)] = [new(0, 0), new(2, 0), new(-1, 0), new(2, 1), new(-1, -2)],
        [(3, 2)] = [new(0, 0), new(-2, 0), new(1, 0), new(-2, -1), new(1, 2)],
        [(3, 0)] = [new(0, 0), new(1, 0), new(-2, 0), new(1, -2), new(-2, 1)],
        [(0, 3)] = [new(0, 0), new(-1, 0), new(2, 0), new(-1, 2), new(2, -1)],
    };

    private static readonly Point[] NoKicks = [new(0, 0)];

    public (ActivePiece Piece, int KickIndex)? TryRotate(Board board, ActivePiece piece, int direction)
    {
        var from = PieceDefinitions.Mod(piece.Rotation);
        var to = PieceDefinitions.Mod(piece.Rotation + direction);
        var kicks = piece.Type switch
        {
            PieceType.O => NoKicks,
            PieceType.I => IKicks[(from, to)],
            _ => JlstzKicks[(from, to)],
        };

        for (var i = 0; i < kicks.Length; i++)
        {
            var candidate = piece with { Rotation = to, X = piece.X + kicks[i].X, Y = piece.Y - kicks[i].Y };
            if (board.Fits(candidate))
            {
                return (candidate, i);
            }
        }
        return null;
    }
}
