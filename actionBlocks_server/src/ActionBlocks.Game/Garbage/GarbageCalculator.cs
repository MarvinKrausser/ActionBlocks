namespace ActionBlocks.Game.Garbage;

public enum SpinKind
{
    None,
    Mini,
    Full,
}

/// <summary>Everything that is known about a lock that cleared (or did not clear) lines.</summary>
public sealed record ClearInfo(int Lines, SpinKind Spin, bool BackToBack, int Combo, bool PerfectClear)
{
    /// <summary>Quads and spins that clear lines keep a back-to-back chain going.</summary>
    public bool IsDifficult => Lines == 4 || (Spin != SpinKind.None && Lines > 0);
}

public interface IGarbageCalculator
{
    int LinesToSend(ClearInfo clear);
}

/// <summary>Line attack table used by common multiplayer rule sets.</summary>
public sealed class StandardGarbageCalculator : IGarbageCalculator
{
    private static readonly int[] ComboTable = [0, 0, 1, 1, 1, 2, 2, 3, 3, 4, 4, 4, 5];

    public int LinesToSend(ClearInfo clear)
    {
        if (clear.Lines == 0)
        {
            return 0;
        }

        var lines = clear.Spin switch
        {
            SpinKind.Full => clear.Lines * 2,
            SpinKind.Mini => clear.Lines - 1,
            _ => clear.Lines == 4 ? 4 : clear.Lines - 1,
        };

        if (clear.BackToBack)
        {
            lines += 1;
        }

        lines += ComboTable[Math.Clamp(clear.Combo, 0, ComboTable.Length - 1)];

        if (clear.PerfectClear)
        {
            lines += 10;
        }

        return lines;
    }
}

public interface IScoreCalculator
{
    int Score(ClearInfo clear, int level);
}

public sealed class StandardScoreCalculator : IScoreCalculator
{
    public int Score(ClearInfo clear, int level)
    {
        var basePoints = (clear.Spin, clear.Lines) switch
        {
            (SpinKind.Full, 0) => 400,
            (SpinKind.Full, 1) => 800,
            (SpinKind.Full, 2) => 1200,
            (SpinKind.Full, _) => 1600,
            (SpinKind.Mini, 0) => 100,
            (SpinKind.Mini, 1) => 200,
            (SpinKind.Mini, _) => 400,
            (_, 1) => 100,
            (_, 2) => 300,
            (_, 3) => 500,
            (_, 4) => 800,
            _ => 0,
        };

        var points = clear.BackToBack ? basePoints * 3 / 2 : basePoints;
        if (clear.Lines > 0 && clear.Combo > 0)
        {
            points += 50 * clear.Combo;
        }
        return points * level;
    }
}
