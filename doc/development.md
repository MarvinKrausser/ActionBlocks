# Development

## Prerequisites

| Tool | Version | Used for |
|---|---|---|
| .NET SDK | 10.0 | Server and tests |
| Node.js + npm | 22 | Website |
| Docker + Compose | recent | Optional, for container builds |

## Running locally

Two terminals:

```bash
# 1. Game server on http://localhost:5080
cd actionBlocks_server
dotnet run --project src/ActionBlocks.Server --urls http://localhost:5080

# 2. Website on http://localhost:5173
cd actionBlocks_website
npm install
npm run dev
```

The Vite dev server proxies `/hub` (including WebSockets) to `http://localhost:5080`, so the default hub URL `<origin>/hub` just works. To point it at another server, use `DEV_SERVER_URL=http://host:port npm run dev`.

To play against yourself, open the site in two browser windows. Create a lobby in one and join with the code in the other. Keyboard focus decides which window receives input.

### Handy server settings for testing

Any `GameSettings` value can be overridden with an environment variable:

```bash
Game__ItemSpawnChance=0.9 Game__CountdownSeconds=1 \
  dotnet run --project src/ActionBlocks.Server --urls http://localhost:5080
```

That gives items on almost every piece and a near-instant start, which is useful for testing items.

## Commands

| Where | Command | Does |
|---|---|---|
| `actionBlocks_server/` | `dotnet build` | Builds all projects |
| | `dotnet test` | Runs the game logic tests |
| `actionBlocks_website/` | `npm run dev` | Dev server with hot reload |
| | `npm run typecheck` | TypeScript check only |
| | `npm run build` | Typecheck + production build into `dist/` |
| | `npm run preview` | Serves `dist/` locally |
| repo root | `docker compose build` | Builds both images (server image runs the tests) |
| | `docker compose config` | Validates the compose file and `.env` |

## Testing strategy

- **Game rules** live in `ActionBlocks.Game` and are covered by unit tests. When you change or add a rule, add a test in `tests/ActionBlocks.Game.Tests`. Use `TestHelpers.CreatePlayer` for a deterministic player: it takes a fixed piece sequence and an item chance (0 or 1). Use `FillRow` to build board situations.
- **Networking and lobby flow** has no automated tests yet. Test it manually, or with a small script client (see [Protocol → Writing a test client](protocol.md#writing-a-test-client)).
- **Bots are an effective test tool.** A script client that joins a lobby and plays with a simple heuristic shows how garbage and items work under real conditions: it clears lines, collects items and uses them on you. One works like this:
  1. Read your board from the snapshot.
  2. For every rotation and x position of the active piece, simulate dropping it and score the result: lines cleared minus holes, height and bumpiness.
  3. Send the best move's `RotateCw` / `MoveLeft` / `MoveRight` / `HardDrop` inputs.
  4. Send `UseItem` whenever the inventory is not empty.
- **Frontend** has no unit tests. `npm run build` (which includes the typecheck) catches type errors, and visual checks happen in the browser.

## Debugging tips

- **Server logs** go to stdout. Exceptions in the tick loop are logged per lobby as `Failed to update lobby {Code}`.
- **Inspect live snapshots.** In the browser devtools, open Network → WS → the `/hub` connection → Messages. Each `MatchState` frame shows the full state, and its records are separated by `\u001e`.
- `GET /health` on the server tells you whether the process is up.
- **Is the hub reachable?** `curl -X POST "<hub-url>/negotiate?negotiateVersion=1"` must return JSON listing the available transports.

## Code conventions

**Server**

- File-scoped namespaces, nullable reference types enabled, one main type per file. Small closely related types may share a file, e.g. all items in `Items.cs`.
- `ActionBlocks.Game` must stay free of ASP.NET or serialization dependencies. Mapping to the wire format happens in `ActionBlocks.Server/Dtos`.
- Game objects are not thread-safe. Only touch them inside `lock (lobby.Sync)`.
- Prefer adding a value to `GameSettings`, or an implementation of an existing interface, over hard-coding numbers in logic.

**Website**

- Function components and hooks. No state library: server state is owned by `useGameConnection`.
- Styling only through `*.module.css`, plus the CSS variables in `theme/global.css`. Don't use inline styles except for computed sizes and colors.
- Reference graphics and colors only through `theme/assets.ts` and `theme/colors.ts`.
- Keep `net/types.ts` in sync with the server DTOs.
- UI text in the pixel font: ASCII only.
- No trademarked names in user-facing text or assets.
