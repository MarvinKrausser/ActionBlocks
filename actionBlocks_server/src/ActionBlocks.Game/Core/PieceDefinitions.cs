namespace ActionBlocks.Game.Core;

/// <summary>
/// Shape data for all pieces. Coordinates are relative to the piece's bounding box, y grows downwards.
/// Rotation states are derived by rotating the spawn shape inside its box, which keeps the mino
/// order stable across rotations (needed to track which mino carries an item).
/// </summary>
public static class PieceDefinitions
{
    public const int RotationCount = 4;

    private static readonly Dictionary<PieceType, (int BoxSize, Point[] Spawn)> SpawnShapes = new()
    {
        [PieceType.I] = (4, [new(0, 1), new(1, 1), new(2, 1), new(3, 1)]),
        [PieceType.J] = (3, [new(0, 0), new(0, 1), new(1, 1), new(2, 1)]),
        [PieceType.L] = (3, [new(2, 0), new(0, 1), new(1, 1), new(2, 1)]),
        [PieceType.O] = (2, [new(0, 0), new(1, 0), new(0, 1), new(1, 1)]),
        [PieceType.S] = (3, [new(1, 0), new(2, 0), new(0, 1), new(1, 1)]),
        [PieceType.Z] = (3, [new(0, 0), new(1, 0), new(1, 1), new(2, 1)]),
        [PieceType.T] = (3, [new(1, 0), new(0, 1), new(1, 1), new(2, 1)]),
        [PieceType.Dot] = (1, [new(0, 0)]),
        [PieceType.SmallL] = (2, [new(0, 0), new(0, 1), new(1, 1)]),
        [PieceType.U] = (3, [new(0, 0), new(2, 0), new(0, 1), new(1, 1), new(2, 1)]),
        [PieceType.Plus] = (3, [new(1, 0), new(0, 1), new(1, 1), new(2, 1), new(1, 2)]),
        [PieceType.W] = (3, [new(0, 0), new(0, 1), new(1, 1), new(1, 2), new(2, 2)]),
    };

    private static readonly Dictionary<PieceType, Point[][]> Rotations = BuildRotations();

    public static IReadOnlyList<PieceType> All { get; } = Enum.GetValues<PieceType>();

    /// <summary>The seven four-cell shapes dealt by the randomizer.</summary>
    public static IReadOnlyList<PieceType> Standard { get; } = All.Where(t => t <= PieceType.T).ToArray();

    /// <summary>Oddball shapes that only appear while a player is transformed.</summary>
    public static IReadOnlyList<PieceType> Odd { get; } = All.Where(t => t > PieceType.T).ToArray();

    public static int BoxSize(PieceType type) => SpawnShapes[type].BoxSize;

    /// <summary>Mino offsets of a piece in the given rotation state (0 = spawn, 1 = R, 2 = 180, 3 = L).</summary>
    public static IReadOnlyList<Point> Minos(PieceType type, int rotation) => Rotations[type][Mod(rotation)];

    public static int Mod(int rotation) => ((rotation % RotationCount) + RotationCount) % RotationCount;

    private static Dictionary<PieceType, Point[][]> BuildRotations()
    {
        var result = new Dictionary<PieceType, Point[][]>();
        foreach (var (type, (size, spawn)) in SpawnShapes)
        {
            var states = new Point[RotationCount][];
            states[0] = spawn;
            for (var r = 1; r < RotationCount; r++)
            {
                // Clockwise rotation inside an n x n box: (x, y) -> (n - 1 - y, x)
                states[r] = states[r - 1].Select(p => new Point(size - 1 - p.Y, p.X)).ToArray();
            }
            result[type] = states;
        }
        return result;
    }
}
