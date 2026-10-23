# Action Blocks – Developer Documentation

Action Blocks is an online player-vs-player falling-block game. Players clear lines to send garbage to each other. They also collect **items** hidden inside pieces and use them on a player of their choice.

The project consists of two deployable components:

| Component | Folder | Technology | Responsibility |
|---|---|---|---|
| Game server | `actionBlocks_server/` | C# / ASP.NET Core 10, SignalR | Runs all game logic, manages lobbies and players, pushes game state to clients |
| Website | `actionBlocks_website/` | React 19, TypeScript, Vite, CSS Modules, nginx | Renders the game, captures keyboard input, sends it to the server |

## Reading order

New to the project? Read these in order:

1. [Architecture](architecture.md): how the pieces fit together, and why the server is authoritative.
2. [Game rules](game-rules.md): what the game actually does, with all numbers.
3. [Server](server.md): a walkthrough of the C# code.
4. [Protocol](protocol.md): the SignalR API between website and server.
5. [Frontend](frontend.md): a walkthrough of the React code.
6. [Development](development.md): running, testing and debugging locally.
7. [Deployment](deployment.md): Docker, compose, reverse proxy and environment variables.
8. [Extending](extending.md): step-by-step guides for common changes (new item, new rule, new graphics, …).

## Repository layout

```
.
├── actionBlocks_server/          C# solution (game logic + web host + tests)
│   ├── src/ActionBlocks.Game/    Pure game logic library (no ASP.NET dependency)
│   ├── src/ActionBlocks.Server/  ASP.NET Core host: SignalR hub, lobbies, tick loop
│   ├── tests/ActionBlocks.Game.Tests/  xUnit tests for the game logic
│   └── Dockerfile
├── actionBlocks_website/         React frontend
│   ├── src/                      Application code
│   ├── public/                   Static files copied as-is (graphics, config.js)
│   ├── docker/                   nginx config + container start script
│   └── Dockerfile
├── doc/                          This documentation
├── docker-compose.yml            Runs both containers
├── .env.example                  Template for deployment variables
├── instructions.md               The original project brief
└── tetris_instructions/          Reference material for the base rules (saved web page)
```

## Glossary

| Term | Meaning |
|---|---|
| **Board / field** | The 10×20 grid of a player. Internally it has 20 extra hidden rows above (the *buffer*). |
| **Piece** | One of the 7 four-cell shapes (internally named I, J, L, O, S, Z, T). |
| **Mino** | One of the four cells of a piece. |
| **Active piece** | The piece currently falling and controlled by the player. |
| **Ghost** | Preview of where the active piece would land. In the UI it is called the "drop shadow". |
| **Hold** | Slot where a player can park one piece. It can be used once per piece. |
| **Garbage** | Gray lines with one hole that rise from the bottom. They are sent by opponents' line clears. |
| **Item** | A special ability attached to one mino of a piece. You collect it by clearing that mino's row. |
| **Target** | The player your items are used on. It is selected with the number keys. |
| **Snapshot** | The full match state the server pushes to all clients many times per second. |
| **Lobby** | A group of players sharing a 6-character code. It hosts consecutive matches. |
| **Match** | One round inside a lobby, from countdown to winner. |

> Naming note: the project intentionally avoids the trademarked name of the original game in all user-facing text and graphics. Internally, the standard single-letter piece names (I, J, L, …) are used because they are the common technical vocabulary.
