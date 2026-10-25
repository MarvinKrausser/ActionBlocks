using ActionBlocks.Game.Core;
using ActionBlocks.Game.Garbage;
using ActionBlocks.Game.Items;

namespace ActionBlocks.Game;

public enum InputAction
{
    MoveLeft,
    MoveRight,
    RotateCw,
    RotateCcw,
    SoftDropStart,
    SoftDropStop,
    HardDrop,
    Hold,
    UseItem,
}

/// <summary>
/// Simulation of a single player's field: falling piece, hold, queue, line clears, garbage and items.
/// The object is not thread-safe; the owning <see cref="Match"/> serializes access.
/// </summary>
public sealed class PlayerGame
{
    private readonly GameSettings _settings;
    private readonly IRandomizer _randomizer;
    private readonly ItemSpawner _itemSpawner;
    private readonly IRotationSystem _rotation;
    private readonly IGarbageCalculator _garbageCalculator;
    private readonly IScoreCalculator _scoreCalculator;
    private readonly Random _random;

    private readonly List<PieceSpec> _next = [];
    private readonly List<string> _inventory = [];
    private readonly List<int> _pendingGarbage = [];
    private readonly List<int> _outgoingAttacks = [];

    private double _gravityProgress;
    private double _lockTimer;
    private int _lockResets;
    private int _lowestY;
    private bool _lastActionWasRotation;
    private int _lastKickIndex;
    private bool _backToBackActive;

    public PlayerGame(
        string playerId,
        string name,
        GameSettings settings,
        IRandomizer randomizer,
        ItemSpawner itemSpawner,
        IRotationSystem rotation,
        IGarbageCalculator garbageCalculator,
        IScoreCalculator scoreCalculator,
        Random random)
    {
        PlayerId = playerId;
        Name = name;
        TargetId = playerId;
        _settings = settings;
        _randomizer = randomizer;
        _itemSpawner = itemSpawner;
        _rotation = rotation;
        _garbageCalculator = garbageCalculator;
        _scoreCalculator = scoreCalculator;
        _random = random;
        Board = new Board(settings.BoardWidth, settings.VisibleRows, settings.BufferRows);
    }

    public string PlayerId { get; }
    public string Name { get; }
    public Board Board { get; }
    public ActivePiece? Active { get; private set; }
    public PieceSpec? Hold { get; private set; }
    public bool HoldUsed { get; private set; }
    public IReadOnlyList<PieceSpec> Next => _next;
    public IReadOnlyList<string> Inventory => _inventory;
    public int PendingGarbage => _pendingGarbage.Sum();
    public bool IsSoftDropping { get; private set; }

    public int Score { get; private set; }
    public int Lines { get; private set; }
    public int Level { get; private set; } = 1;
    public int Combo { get; private set; } = -1;
    public ClearInfo? LastClear { get; private set; }

    public bool IsAlive { get; private set; } = true;
    public double Now { get; private set; }

    /// <summary>Match time until which locked cells and the ghost piece are hidden from this player.</summary>
    public double InvisibleUntil { get; set; }
    public bool IsInvisible => Now < InvisibleUntil;

    /// <summary>Match time until which this player's upcoming pieces are odd shapes.</summary>
    public double TransformedUntil { get; private set; }
    public bool IsTransformed => Now < TransformedUntil;

    /// <summary>The player selected as item target (own id = self).</summary>
    public string TargetId { get; set; }

    public void Start()
    {
        FillQueue();
        SpawnNext();
    }

    public void Update(double deltaSeconds, double now)
    {
        var wasTransformed = IsTransformed;
        Now = now;
        if (wasTransformed && !IsTransformed)
        {
            RestoreTransformedPieces();
        }
        if (!IsAlive || Active is null)
        {
            return;
        }

        var secondsPerRow = _settings.GravitySecondsPerRow(Level);
        var rowsPerSecond = 1 / secondsPerRow;
        if (IsSoftDropping)
        {
            rowsPerSecond = Math.Min(rowsPerSecond * _settings.SoftDropFactor, _settings.MaxSoftDropRowsPerSecond);
        }

        _gravityProgress += deltaSeconds * rowsPerSecond;
        while (_gravityProgress >= 1)
        {
            _gravityProgress -= 1;
            if (!TryMove(0, 1))
            {
                _gravityProgress = 0;
                break;
            }
            if (IsSoftDropping)
            {
                Score += 1;
            }
        }

        if (IsGrounded())
        {
            _lockTimer += deltaSeconds;
            if (_lockTimer >= _settings.LockDelaySeconds || _lockResets >= _settings.MaxLockResets)
            {
                Lock();
            }
        }
    }

    public void HandleInput(InputAction action)
    {
        if (!IsAlive || Active is null)
        {
            if (action == InputAction.SoftDropStop)
            {
                IsSoftDropping = false;
            }
            return;
        }

        switch (action)
        {
            case InputAction.MoveLeft:
                TryShift(-1);
                break;
            case InputAction.MoveRight:
                TryShift(1);
                break;
            case InputAction.RotateCw:
                TryRotate(1);
                break;
            case InputAction.RotateCcw:
                TryRotate(-1);
                break;
            case InputAction.SoftDropStart:
                IsSoftDropping = true;
                break;
            case InputAction.SoftDropStop:
                IsSoftDropping = false;
                break;
            case InputAction.HardDrop:
                HardDrop();
                break;
            case InputAction.Hold:
                HoldPiece();
                break;
            case InputAction.UseItem:
                // Items need a target and are resolved by the match.
                break;
        }
    }

    /// <summary>Where the active piece would land.</summary>
    public ActivePiece? Ghost()
    {
        if (Active is null)
        {
            return null;
        }
        var ghost = Active;
        while (Board.Fits(ghost.Moved(0, 1)))
        {
            ghost = ghost.Moved(0, 1);
        }
        return ghost;
    }

    /// <summary>Queues incoming garbage; it rises when the player next locks a piece without clearing lines.</summary>
    public void QueueGarbage(int lines)
    {
        if (lines > 0 && IsAlive)
        {
            _pendingGarbage.Add(lines);
        }
    }

    /// <summary>Returns and clears the attacks produced since the last call.</summary>
    public IReadOnlyList<int> DrainOutgoingAttacks()
    {
        var result = _outgoingAttacks.ToList();
        _outgoingAttacks.Clear();
        return result;
    }

    public string? TakeFirstItem()
    {
        if (_inventory.Count == 0)
        {
            return null;
        }
        var item = _inventory[0];
        _inventory.RemoveAt(0);
        return item;
    }

    public void AddItem(string itemId)
    {
        if (_inventory.Count < _settings.MaxInventory)
        {
            _inventory.Add(itemId);
        }
    }

    public void InsertGarbageNow(int lines, int holeColumn)
    {
        if (!IsAlive)
        {
            return;
        }
        if (Board.AddGarbage(lines, holeColumn))
        {
            TopOut();
            return;
        }
        ResolveActiveOverlap();
    }

    public void RemoveBottomLine()
    {
        if (!IsAlive)
        {
            return;
        }
        Board.RemoveBottomRow();
        ResolveActiveOverlap();
    }

    public void ClearBoard()
    {
        if (IsAlive)
        {
            Board.Clear();
        }
    }

    public static void SwapBoards(PlayerGame a, PlayerGame b)
    {
        if (!a.IsAlive || !b.IsAlive)
        {
            return;
        }
        a.Board.SwapContents(b.Board);
        a.ResolveActiveOverlap();
        b.ResolveActiveOverlap();
    }

    public void Forfeit() => TopOut();

    /// <summary>
    /// Turns the queue into odd shapes for <paramref name="seconds"/> (added to any time left).
    /// The falling piece stays as it is; pieces still queued or held change back when the time runs out.
    /// </summary>
    public void Transform(double now, double seconds)
    {
        TransformedUntil = Math.Max(TransformedUntil, now) + seconds;
        for (var i = 0; i < _next.Count; i++)
        {
            if (_next[i].Original is null)
            {
                _next[i] = TransformRandomly(_next[i]);
            }
        }
    }

    private PieceSpec TransformRandomly(PieceSpec spec) =>
        spec.Transform(PieceDefinitions.Odd[_random.Next(PieceDefinitions.Odd.Count)]);

    private void RestoreTransformedPieces()
    {
        for (var i = 0; i < _next.Count; i++)
        {
            _next[i] = _next[i].Restore();
        }
        Hold = Hold?.Restore();
    }

    private void FillQueue()
    {
        while (_next.Count < Math.Max(1, _settings.PreviewCount))
        {
            var spec = _itemSpawner.Decorate(_randomizer.Next());
            _next.Add(IsTransformed ? TransformRandomly(spec) : spec);
        }
    }

    private void SpawnNext()
    {
        var spec = _next[0];
        _next.RemoveAt(0);
        FillQueue();
        Spawn(spec);
    }

    private void Spawn(PieceSpec spec)
    {
        var box = PieceDefinitions.BoxSize(spec.Type);
        // Spawn in the two rows above the visible field, centered and rounded to the left.
        var piece = new ActivePiece(spec, (Board.Width - box) / 2, Board.BufferRows - 2, 0);
        if (!Board.Fits(piece))
        {
            Active = piece;
            TopOut();
            return;
        }
        Active = piece;
        _gravityProgress = 0;
        _lockTimer = 0;
        _lockResets = 0;
        _lastActionWasRotation = false;
        _lowestY = piece.Y;
        // Pieces move down one row immediately after appearing.
        TryMove(0, 1);
    }

    private bool IsGrounded() => Active is not null && !Board.Fits(Active.Moved(0, 1));

    private bool TryMove(int dx, int dy)
    {
        if (Active is null)
        {
            return false;
        }
        var moved = Active.Moved(dx, dy);
        if (!Board.Fits(moved))
        {
            return false;
        }
        Active = moved;
        _lastActionWasRotation = false;
        if (moved.Y > _lowestY)
        {
            _lowestY = moved.Y;
            _lockResets = 0;
            _lockTimer = 0;
        }
        return true;
    }

    private void TryShift(int dx)
    {
        var wasGrounded = IsGrounded();
        if (TryMove(dx, 0))
        {
            RegisterLockReset(wasGrounded);
        }
    }

    private void TryRotate(int direction)
    {
        if (Active is null)
        {
            return;
        }
        var wasGrounded = IsGrounded();
        var result = _rotation.TryRotate(Board, Active, direction);
        if (result is null)
        {
            return;
        }
        Active = result.Value.Piece;
        _lastActionWasRotation = true;
        _lastKickIndex = result.Value.KickIndex;
        if (Active.Y > _lowestY)
        {
            _lowestY = Active.Y;
            _lockResets = 0;
            _lockTimer = 0;
        }
        RegisterLockReset(wasGrounded);
    }

    private void RegisterLockReset(bool wasGrounded)
    {
        if (wasGrounded || IsGrounded())
        {
            _lockResets++;
            _lockTimer = 0;
        }
    }

    private void HardDrop()
    {
        var distance = 0;
        while (Active is not null && Board.Fits(Active.Moved(0, 1)))
        {
            Active = Active.Moved(0, 1);
            _lastActionWasRotation = false;
            distance++;
        }
        Score += distance * 2;
        Lock();
    }

    private void HoldPiece()
    {
        if (HoldUsed || Active is null)
        {
            return;
        }
        // A piece left over from an expired Transformation changes back once it is put away.
        var current = IsTransformed ? Active.Spec : Active.Spec.Restore();
        HoldUsed = true;
        if (Hold is null)
        {
            Hold = current;
            SpawnNext();
        }
        else
        {
            var held = Hold;
            Hold = current;
            Spawn(held);
        }
    }

    private SpinKind DetectSpin(ActivePiece piece)
    {
        if (piece.Type != PieceType.T || !_lastActionWasRotation)
        {
            return SpinKind.None;
        }

        // Corners of the 3x3 box: top-left, top-right, bottom-right, bottom-left.
        Point[] corners = [new(0, 0), new(2, 0), new(2, 2), new(0, 2)];
        var occupied = corners.Select(c => Board.IsBlocked(piece.X + c.X, piece.Y + c.Y)).ToArray();
        if (occupied.Count(o => o) < 3)
        {
            return SpinKind.None;
        }

        // The two corners on the side the T points to.
        var rotation = PieceDefinitions.Mod(piece.Rotation);
        var front = (occupied[rotation], occupied[(rotation + 1) % 4]);
        var fullFront = front is (true, true);
        return fullFront || _lastKickIndex == 4 ? SpinKind.Full : SpinKind.Mini;
    }

    private void Lock()
    {
        if (Active is null)
        {
            return;
        }

        var piece = Active;
        var spin = DetectSpin(piece);
        var lockOut = piece.Cells().All(c => c.Y < Board.BufferRows);
        Board.Place(piece);
        Active = null;

        if (lockOut)
        {
            TopOut();
            return;
        }

        var rows = Board.FullRows();
        foreach (var item in Board.ClearRows(rows))
        {
            AddItem(item);
        }

        var lines = rows.Count;
        Combo = lines > 0 ? Combo + 1 : -1;
        var difficult = lines == 4 || (spin != SpinKind.None && lines > 0);
        var backToBack = lines > 0 && difficult && _backToBackActive;
        if (lines > 0)
        {
            _backToBackActive = difficult;
        }

        var clear = new ClearInfo(lines, spin, backToBack, Math.Max(Combo, 0), lines > 0 && Board.IsEmpty);
        if (lines > 0 || spin != SpinKind.None)
        {
            LastClear = clear;
        }

        Score += _scoreCalculator.Score(clear, Level);
        Lines += lines;
        Level = Math.Min(_settings.MaxLevel, 1 + Lines / _settings.LinesPerLevel);

        var attack = CancelPendingGarbage(_garbageCalculator.LinesToSend(clear));
        if (attack > 0)
        {
            _outgoingAttacks.Add(attack);
        }

        if (lines == 0 && !ApplyPendingGarbage())
        {
            return;
        }

        HoldUsed = false;
        SpawnNext();
    }

    /// <summary>Uses outgoing lines to cancel incoming garbage first; returns what is left to send.</summary>
    private int CancelPendingGarbage(int attack)
    {
        while (attack > 0 && _pendingGarbage.Count > 0)
        {
            var cancel = Math.Min(attack, _pendingGarbage[0]);
            attack -= cancel;
            _pendingGarbage[0] -= cancel;
            if (_pendingGarbage[0] == 0)
            {
                _pendingGarbage.RemoveAt(0);
            }
        }
        return attack;
    }

    /// <summary>Raises all pending garbage. Returns false if the player topped out.</summary>
    private bool ApplyPendingGarbage()
    {
        foreach (var chunk in _pendingGarbage)
        {
            if (Board.AddGarbage(chunk, _random.Next(Board.Width)))
            {
                _pendingGarbage.Clear();
                TopOut();
                return false;
            }
        }
        _pendingGarbage.Clear();
        return true;
    }

    /// <summary>After the board changed under the active piece, push the piece up until it fits again.</summary>
    private void ResolveActiveOverlap()
    {
        if (Active is null || Board.Fits(Active))
        {
            return;
        }
        for (var dy = 1; dy <= Board.Height; dy++)
        {
            var candidate = Active.Moved(0, -dy);
            if (Board.Fits(candidate))
            {
                Active = candidate;
                _lowestY = candidate.Y;
                return;
            }
        }
        TopOut();
    }

    private void TopOut()
    {
        IsAlive = false;
        IsSoftDropping = false;
        _pendingGarbage.Clear();
    }
}
