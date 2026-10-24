# Action Blocks

Online player-vs-player falling-block game with items.

**Developer documentation:** see [`doc/`](doc/README.md) for architecture, game rules, protocol, frontend, deployment and how-to guides.

| Component | Path | Tech |
|---|---|---|
| Game server (logic, lobbies, players) | `actionBlocks_server/` | ASP.NET Core 10, SignalR |
| Website | `actionBlocks_website/` | React 19 + Vite + TypeScript, CSS Modules, served by nginx |

## Gameplay

- 10x20 field, hold, 5-piece preview, ghost piece, bag randomizer, wall kicks, 0.5 s lock delay (15 move resets).
- Garbage from line clears (attack table incl. spins, back-to-back, combos) goes to opponents in a **round-robin cycle**.
- 1 in 10 pieces carries an **item** inside one of its blocks. Clear the line containing that block to collect it (max 10, FIFO).
- **1-9** selects the item target (1 = you, 2.. = opponents in the order shown). **F** uses the first item on the target.
- You never see who targets you, but every opponent's items are shown beneath their mini field.
- Item names flash on top of the field they hit.
- Retro sound effects and a background music playlist, all synthesized in the browser. Any sound can be swapped for an audio file. The speaker button in the upper right corner opens volume sliders and mute buttons for effects and music. **M** mutes everything, **B** toggles the music, **N** skips the track.

Controls: arrows or WASD to move/rotate/soft drop, Space for hard drop, Z/Q/Ctrl to rotate left, C/Shift to hold.

## Local development

```bash
# server on :5080
cd actionBlocks_server && dotnet run --project src/ActionBlocks.Server --urls http://localhost:5080
dotnet test   # game logic tests

# website on :5173, proxies /hub to the server
cd actionBlocks_website && npm install && npm run dev
```

## Deployment

```bash
docker network create actionBlocks_website   # if not already created
docker network create actionBlocks_server
docker compose up -d --build
```

The website uses relative URLs. It works under a sub-path such as `/website/`, whether or not the proxy strips that prefix.

The reverse proxy (attached to both networks) should route:

- the site to `actionblocks-website:80`
- `/hub` to `actionblocks-server:8080`, with WebSocket upgrade headers forwarded

| Variable | Container | Purpose |
|---|---|---|
| `SERVER_URL` | website | Hub URL as seen by the browser. Empty means `<site origin>/hub`. Written into `config.js` at container start. |
| `ALLOWED_ORIGINS` | server | Comma-separated CORS origins. Empty allows any origin. |
| `HUB_PATH` | server | Hub endpoint path (default `/hub`). |
| `PATH_BASE` | server | Prefix to accept when the proxy forwards a sub-path unstripped, e.g. `/server`. |
| `Game__*` | server | Overrides any `GameSettings` value, e.g. `Game__ItemSpawnChance=0.2`. |

## Extending

**Adding an item**

1. Create a class implementing `IItem` in `actionBlocks_server/src/ActionBlocks.Game/Items/`.
2. Register it in `ItemRegistry.Default`.
3. Add an entry with the same id to `itemVisuals` in `actionBlocks_website/src/theme/assets.ts`, and add an icon in `public/assets/items/`.

**Changing rules**

- Values: `GameSettings`.
- Behaviour behind interfaces: `IRotationSystem`, `IRandomizer`, `IGarbageCalculator`, `IScoreCalculator`, `IGarbageTargeter`.
- The match flow lives in `Match`, and the per-player simulation in `PlayerGame`.

**Changing graphics**

- Every image path is in `src/theme/assets.ts`. You can set an image logo there with `uiAssets.logo`.
- Block colors are in `src/theme/colors.ts`.
- Block drawing style is in `src/render/draw.ts`.
- Key bindings are in `src/config/keybindings.ts`.
- Sound files and music tracks are registered in `src/theme/assets.ts` (`soundFiles`, `musicTracks`). All volume levels are in `src/config/sound.ts`.
