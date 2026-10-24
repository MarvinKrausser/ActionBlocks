namespace ActionBlocks.Game.Core;

public interface IRandomizer
{
    PieceType Next();
}

/// <summary>"Bag" randomizer: every piece shape once per shuffled bag.</summary>
public sealed class SevenBagRandomizer(Random random) : IRandomizer
{
    private readonly Queue<PieceType> _bag = new();

    public PieceType Next()
    {
        if (_bag.Count == 0)
        {
            var pieces = PieceDefinitions.All.ToArray();
            random.Shuffle(pieces);
            foreach (var p in pieces)
            {
                _bag.Enqueue(p);
            }
        }
        return _bag.Dequeue();
    }
}
