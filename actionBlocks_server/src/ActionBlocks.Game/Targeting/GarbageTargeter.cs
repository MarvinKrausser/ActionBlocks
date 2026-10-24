namespace ActionBlocks.Game.Targeting;

public interface IGarbageTargeter
{
    /// <summary>Picks who receives the next garbage attack from <paramref name="sender"/>, or null if nobody can.</summary>
    PlayerGame? NextTarget(PlayerGame sender, IReadOnlyList<PlayerGame> players);
}

/// <summary>Cycles through the living opponents of each sender in turn.</summary>
public sealed class CyclingGarbageTargeter : IGarbageTargeter
{
    private readonly Dictionary<string, int> _cursor = new();

    public PlayerGame? NextTarget(PlayerGame sender, IReadOnlyList<PlayerGame> players)
    {
        var opponents = players.Where(p => p != sender).ToList();
        if (opponents.Count == 0)
        {
            return null;
        }

        var start = _cursor.GetValueOrDefault(sender.PlayerId);
        for (var i = 0; i < opponents.Count; i++)
        {
            var index = (start + i) % opponents.Count;
            if (opponents[index].IsAlive)
            {
                _cursor[sender.PlayerId] = index + 1;
                return opponents[index];
            }
        }
        return null;
    }
}
