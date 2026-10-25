# Game Rules (as implemented)

This page describes the rules **exactly as the code implements them**, with every number. The code locations are given so you can verify or change each rule. Unless noted otherwise, files are in `actionBlocks_server/src/ActionBlocks.Game/`.

## Match flow

1. The host clicks **Start game** in the lobby. Every player currently in the lobby becomes part of the match.
2. **Countdown** (`GameSettings.CountdownSeconds`, default 3 s). Boards are visible but no piece has spawned yet, and inputs are ignored. Target selection already works.
3. **Running.** Everyone receives the **same piece sequence** (shared seed), so the game is fair. Item rolls are independent per player.
4. A player is **eliminated** when they top out (see below) or disconnect.
5. **Finished** when at most one player is alive. In a single-player practice round it ends when that player tops out.
6. The result is shown for 4 seconds (`MatchRunner.ResultsDelay`). Then the lobby returns to the waiting state and shows a results table. The host can start the next round.

Players who join a lobby while a match is running watch as spectators. They take part from the next round on.

**Places:** a player eliminated while *n* players are still alive gets place *n + 1*. Players eliminated in the same tick share that place. The last survivor gets place 1 and is the winner.

## Playfield

| Rule | Value | Where |
|---|---|---|
| Width × visible height | 10 × 20 | `GameSettings.BoardWidth`, `VisibleRows` |
| Hidden buffer above the field | 20 rows | `GameSettings.BufferRows` |
| Next-piece preview | 5 | `GameSettings.PreviewCount` |

Pieces spawn in the two buffer rows directly above the visible field. They are horizontally centered, rounded to the left: columns 3–5 for 3-wide pieces, 3–6 for the long piece, 4–5 for the square. Every piece moves down one row immediately after spawning. See `PlayerGame.Spawn`.

## Pieces and randomizer

- There are 7 shapes. Each has 4 rotation states (0 = spawn, 1 = right, 2 = 180°, 3 = left). States are derived by rotating the spawn shape inside its bounding box (`Core/PieceDefinitions.cs`).
- The **bag randomizer** shuffles all 7 shapes into a bag and deals them one by one, then refills (`Core/Randomizer.cs`). This guarantees every shape appears exactly once in every 7 pieces. The odd Transformation shapes are never in the bag.

## Rotation (SRS)

Rotation uses the standard *Super Rotation System* kick tables (`Core/RotationSystem.cs`):

- When a rotation is blocked, up to 4 alternative offsets ("kicks") are tried in order.
- The long piece has its own kick table. The square never kicks.
- If no offset fits, the rotation does nothing.

## Movement, gravity and locking

| Rule | Value | Where |
|---|---|---|
| Gravity at level *L* | (0.8 − (L−1)·0.007)^(L−1) seconds per row. Level 1 = 1 s/row | `GameSettings.GravitySecondsPerRow` |
| Soft drop | 20× gravity, at most 60 rows/s | `SoftDropFactor`, `MaxSoftDropRowsPerSecond` |
| Lock delay | 0.5 s on the ground | `LockDelaySeconds` |
| Lock resets | max 15 moves/rotations while grounded | `MaxLockResets` |
| Level | 1 + lines ÷ 10, max 15 | `LinesPerLevel`, `MaxLevel` |

Lock-delay details (*move reset*, `PlayerGame.Update` / `RegisterLockReset`):

- While the piece rests on something, a timer runs. At 0.5 s the piece locks.
- Every successful move or rotation while grounded resets the timer and uses up one of 15 resets. When the resets are exhausted, the piece locks the moment it is grounded.
- When the piece reaches a row lower than ever before, both the timer and the reset counter start over.

**Hard drop** moves the piece straight down and locks it immediately.

**Hold** swaps the active piece with the hold slot. If the slot is empty, the next piece spawns instead. Hold can be used once per piece and becomes available again after the next lock. An item inside a held piece stays with it.

## Line clears, spins and scoring

When a piece locks (`PlayerGame.Lock`):

1. **Spin check.** The piece is a T, its last successful action was a rotation, and at least 3 of the 4 corners of its 3×3 box are blocked (walls count as blocked). It is a **full spin** if both corners on the pointing side are blocked, or if the last rotation used the 5th kick. Otherwise it is a **mini spin**.
2. **Lock out.** If all four cells are inside the hidden buffer, the player tops out.
3. Full rows are removed. **Items in those rows go to the player's inventory.**
4. **Combo.** It counts consecutive locks that clear lines. It is −1 when there is no streak, 0 for the first clear, 1 for the second clear in a row, and so on.
5. **Back-to-back.** It applies when the previous line clear was also "difficult" (4 lines, or a spin that clears lines) and this one is too.
6. **Perfect clear** is when the board is completely empty after the clear.

**Score** (`Garbage/GarbageCalculator.cs`, `StandardScoreCalculator`) is multiplied by the current level:

| Clear | Points |
|---|---|
| 1 / 2 / 3 / 4 lines | 100 / 300 / 500 / 800 |
| Spin with 0 / 1 / 2 / 3 lines | 400 / 800 / 1200 / 1600 |
| Mini spin with 0 / 1 / 2 lines | 100 / 200 / 400 |
| Back-to-back | ×1.5 on the above |
| Combo | +50 × combo |
| Soft drop | +1 per row (not multiplied) |
| Hard drop | +2 per row (not multiplied) |

## Garbage (attacks)

**Attack size** (`StandardGarbageCalculator`):

| Clear | Lines sent |
|---|---|
| Single / Double / Triple / Quad | 0 / 1 / 2 / 4 |
| Spin Single / Double / Triple | 2 / 4 / 6 |
| Mini spin Single / Double | 0 / 1 |
| Back-to-back bonus | +1 |
| Combo bonus (by combo count 0, 1, 2, …) | 0, 0, 1, 1, 1, 2, 2, 3, 3, 4, 4, 4, 5 (capped) |
| Perfect clear | +10 |

**Cancelling.** Outgoing lines first cancel the player's own pending incoming garbage. Only the rest is sent.

**Targeting.** Garbage is **not** sent to the selected item target. It goes round-robin through the living opponents, in match order: each sender has its own cursor, and dead players are skipped (`Targeting/GarbageTargeter.cs`).

**Arrival.** Incoming garbage waits in a queue (the red meter next to the board). It rises when the receiver next locks a piece **without** clearing a line. Each attack becomes a block of gray rows sharing one random hole column.

## Top out (elimination)

A player is eliminated when any of these happens:

- **Block out:** a new piece spawns overlapping existing blocks.
- **Lock out:** a piece locks completely inside the hidden buffer.
- **Garbage overflow:** garbage pushes a filled cell out of the top of the buffer.
- **No room after an item:** an item changed the board under the active piece and the piece cannot be pushed up to a free spot.
- **Disconnect:** the player leaves or closes the tab.

## Items

### Spawning and collecting

- Each of the four minos of a **newly generated** piece has its own 10% chance (`GameSettings.ItemSpawnChance`, env `ITEM_SPAWN_CHANCE`) to carry an item, so one piece can carry several items (up to four). Each item is picked separately. The roll happens when the piece enters the queue, so items are already visible in the preview and in hold (`Items/ItemRegistry.cs`, `ItemSpawner`).
- When the piece locks, the item stays in that cell of the board. Item cells move with the board: they drop when lines below them clear, rise with garbage, and travel along when boards are swapped.
- **Clearing the row that contains the item cell** moves the item into the player's inventory.
- If the cell is destroyed another way (for example by *Clear Line* or *Wipe Out*), the item is lost.

### Inventory and use

- The inventory holds at most 10 items (`MaxInventory`). Items collected beyond that are discarded.
- Items queue up first in, first out. **F** always uses the **oldest** item, the leftmost one in the bar.
- **Number keys** select the target: `1` is yourself, `2`–`9` are the opponents in the order they are displayed.
- Unknown or eliminated targets are ignored. If the current target gets eliminated, the selection falls back to yourself, on both server and client.
- Using an item with an empty inventory does nothing.

### Visibility rules

- **Nobody sees who targets whom.** The selected target is never broadcast.
- Everyone sees every opponent's inventory in the item bar beneath their mini board.
- Every client is told when an item hits someone (`ItemUsedEvent`): which player was hit and which item, but not who used it. The item name flashes on top of the hit player's board. That is the big board for yourself, or the mini board for an opponent.

### The six items

| Id | Name | Effect | Class |
|---|---|---|---|
| `add_line` | Add Line | Immediately inserts 1 garbage row (random hole) at the bottom of the target's board. This can top the target out. | `AddGarbageLineItem` |
| `clear_line` | Clear Line | Removes the bottom row of the target. Items in it are lost. | `ClearBottomLineItem` |
| `nuke` | Wipe Out | Removes **all** locked blocks of the target. The active piece is unaffected. | `ClearBoardItem` |
| `swap` | Switch | The user and the target exchange all locked blocks. Active pieces, hold, queues, inventories and pending garbage stay with their owners. If an active piece now overlaps, it is pushed up. Does nothing when used on yourself. | `SwapBoardItem` |
| `blind` | Blackout | For 4 seconds, the target cannot see their locked blocks or their ghost piece. Only the falling piece stays visible. Durations stack: each further Blackout adds 4 s to the time that is left. | `InvisibilityItem` |
| `transform` | Transformation | For 8 seconds, the target's upcoming pieces become odd shapes: Dot, small L, U, Plus or W (random). The whole next queue changes at once; the falling piece does not. Items inside a piece move to the new shape. When the time runs out, pieces still in the queue or hold change back to what they were. Durations stack like Blackout. | `TransformationItem` |

Every item can also be used on yourself: target `1`. *Wipe Out* and *Clear Line* are useful defensively.

## Controls

Bindings use physical key positions (`KeyboardEvent.code`), so they work on any keyboard layout. They are defined in `actionBlocks_website/src/config/keybindings.ts`.

| Action | Keys |
|---|---|
| Move left / right | ← → or A D |
| Soft drop (hold) | ↓ or S |
| Hard drop | Space |
| Rotate clockwise | ↑, W, X, E |
| Rotate counter-clockwise | Z (also the key labeled Y on QWERTZ), Q, Ctrl |
| Hold | C, Shift |
| Select item target | 1–9 (1 = yourself) |
| Use item | F |
| All sound on / off | M |
| Background music on / off (effects stay on) | B |
| Volume and mute for effects and music | Speaker button, upper right corner |
| Next music track | N |

Holding left/right repeats the move after 167 ms, then every 33 ms (DAS/ARR). When both directions are held, the most recently pressed one wins.
