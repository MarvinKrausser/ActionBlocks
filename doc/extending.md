# Extending the Game

Step-by-step guides for the most common changes.

- [Add a new item](#add-a-new-item)
- [Change a numeric rule](#change-a-numeric-rule)
- [Replace a rule implementation](#replace-a-rule-implementation)
- [Add new state to the snapshot](#add-new-state-to-the-snapshot)
- [Add a new hub method](#add-a-new-hub-method)
- [Add a new match event](#add-a-new-match-event)
- [Change graphics and colors](#change-graphics-and-colors)
- [Change key bindings](#change-key-bindings)

---

## Add a new item

Example: a **"Speed Up"** item that makes the target's pieces fall fast for 5 seconds.

### 1. Server: implement `IItem`

Add a class in `actionBlocks_server/src/ActionBlocks.Game/Items/Items.cs`, or in a new file in that folder:

```csharp
public sealed class SpeedUpItem : IItem
{
    public string Id => "speed";           // stable id, also used by the frontend
    public string DisplayName => "Speed Up";
    public int SpawnWeight => 1;           // optional, default 1 (higher = more common)

    public void Apply(ItemContext context) =>
        context.Target.FastUntil = Math.Max(context.Target.FastUntil, context.Now + 5);
}
```

`ItemContext` gives you the `User`, the `Target`, a shared `Random` and the match time `Now`. Use `context.IsSelfTarget` to special-case self-use.

### 2. Server: add any new state the item needs

If existing `PlayerGame` methods (`InsertGarbageNow`, `RemoveBottomLine`, `ClearBoard`, `SwapBoards`, `InvisibleUntil`) aren't enough, add a property or method to `PlayerGame`. For the example:

```csharp
public double FastUntil { get; set; }
// in Update(): if (Now < FastUntil) rowsPerSecond *= 4;
```

If the board changes under the active piece, call `ResolveActiveOverlap()` afterwards. That method is private, so add your board-changing method to `PlayerGame` itself, as the existing item hooks do.

### 3. Server: register it

In `Items/ItemRegistry.cs`:

```csharp
public static ItemRegistry Default { get; } = new(
[
    new AddGarbageLineItem(),
    ...
    new SpeedUpItem(),
]);
```

### 4. Server: test it

Add a test to `tests/ActionBlocks.Game.Tests/ItemAndMatchTests.cs`, and update `Registry_contains_five_unique_items` to the new count.

### 5. Website: add the visuals

Add an icon at `actionBlocks_website/public/assets/items/speed.svg`. Any image format works; the existing icons are 8×8 pixel-art SVGs. Then add an entry to `itemVisuals` in `src/theme/assets.ts`:

```ts
speed: {
  icon: asset('assets/items/speed.svg'),
  name: 'Speed Up',
  color: '#ff9b26',
  description: 'Target pieces fall very fast for 5 seconds.',
},
```

The id key must match `IItem.Id`. Unknown ids still work but show `unknown.svg`.

### 6. Website: show the effect (only if needed)

If the item needs a visual effect, as Blackout does, expose the state in the snapshot (see [below](#add-new-state-to-the-snapshot)) and use it in `GamePage` / `draw.ts`.

---

## Change a numeric rule

All numbers are in `ActionBlocks.Game/GameSettings.cs`. There are two ways to change one:

- **Change the default** in `GameSettings.cs` and rebuild.
- **Override it per deployment** without rebuilding: set `Game__<Property>` on the server container, e.g. in `docker-compose.yml`:

  ```yaml
  actionblocks-server:
    environment:
      Game__ItemSpawnChance: "0.15"
      Game__MaxInventory: "6"
      Game__LockDelaySeconds: "0.6"
  ```

To add a **new** setting, add an `init` property with a default to `GameSettings` and read it where needed. Configuration binding picks it up automatically.

If you change the board width or height, the website adapts automatically, because `MatchState.width`/`height` drive the rendering.

---

## Replace a rule implementation

The rules sit behind interfaces (see [Architecture](architecture.md#server-internals-at-a-glance)). To swap one, implement the interface and pass it where `Match` builds the players. That happens in the `Match` constructor (`Match.cs`):

```csharp
var rotation = new SuperRotationSystem();     // IRotationSystem
var garbage  = new StandardGarbageCalculator(); // IGarbageCalculator
var score    = new StandardScoreCalculator();   // IScoreCalculator
... new SevenBagRandomizer(new Random(pieceSeed)) // IRandomizer
```

The garbage targeter can already be passed to the `Match` constructor (`IGarbageTargeter? garbageTargeter`). For example, to send garbage to the player with the highest score, implement `IGarbageTargeter.NextTarget`. Then, in `Lobby.StartMatch`, pass `new Match(players, settings, garbageTargeter: new MyTargeter())`.

If you want rules selectable per lobby, add constructor parameters to `Match` in the same style and choose them in `Lobby.StartMatch`.

---

## Add new state to the snapshot

1. **Game:** expose the value as a public property on `PlayerGame` (or `Match`).
2. **DTO:** add a parameter to `PlayerStateDto` (or `MatchStateDto`) in `ActionBlocks.Server/Dtos/Dtos.cs`.
3. **Mapper:** fill it in `SnapshotMapper.ToDto`.
4. **Website types:** add the camelCase field to `PlayerState` (or `MatchState`) in `src/net/types.ts`.
5. **Website UI:** use it in `GamePage`, `MiniBoard` or `draw.ts`.

Keep snapshots small. They are sent ~30 times per second for every player to every client.

**Private information.** Snapshots go to everyone in the lobby. Don't put anything there that opponents must not see, such as the selected target. If you need per-player private data, send a separate message to `Clients.Client(connectionId)` from `MatchRunner`.

---

## Add a new hub method

1. Add a public method to `GameHub`. If it touches a lobby, use `WithPlayer(...)` or `lock (lobby.Sync)`. Throw `HubException` with a user-readable message on failure.
2. Add a wrapper to `HubClient` in `src/net/hubClient.ts`. Use `invoke` when you need a result or an error, `send` for fire-and-forget.
3. Expose it from `useGameConnection` in its `actions` object.
4. Document it in [Protocol](protocol.md).

---

## Add a new match event

1. Add a record deriving from `MatchEvent` in `Match.cs` and add it to `_events` where it happens. If the source is inside `PlayerGame`, collect it there and drain it in `Match.AfterStateChange`, the same way outgoing attacks are drained.
2. Map it in `SnapshotMapper.ToDto(MatchEvent)` to a `MatchEventDto` with a new `type` string, adding DTO fields if needed.
3. Handle the `type` in `useGameConnection`'s `onMatchState`, e.g. to show a "DOUBLE!" popup.

---

## Change graphics and colors

| Change | Edit |
|---|---|
| Item icon | Replace the file in `public/assets/items/`, or point `itemVisuals[id].icon` elsewhere |
| Logo | Put an image in `public/assets/`, set `uiAssets.logo: asset('assets/logo.png')` |
| Block colors | `src/theme/colors.ts` (`blockColors[1..7]` pieces, `[8]` garbage) |
| Block style (bevel, texture, …) | `drawBlock` in `src/render/draw.ts` |
| Ghost look | `drawGhostCell` in `draw.ts`, `boardColors.ghostAlpha` |
| UI colors, borders, font | CSS variables in `src/theme/global.css` |
| A component's layout | Its `*.module.css` file |

In production, the files in `public/assets/` are served with `no-cache`, so replaced images show up right after a redeploy.

---

## Change key bindings

Edit `src/config/keybindings.ts`:

- `keyBindings` maps each action to a list of `KeyboardEvent.code` values. Use physical codes such as `KeyW` or `ArrowLeft`; a site like keycode.info shows them.
- `targetKeys` lists the keys for target slots 1–9.
- `DAS_MS` / `ARR_MS` set the auto-repeat timing.
- `controlsHelp` holds the texts shown in the lobby's Controls panel. Keep them ASCII.

To add a new **action**, add it to `BoundAction` and `keyBindings`. If it maps 1:1 to a server action, add it to `simpleActions`. A new server action also needs an `InputAction` value on the server, handled in `PlayerGame.HandleInput` or `Match.HandleInput`, plus the matching string in `src/net/types.ts`.
