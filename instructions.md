**I want you to build an online tetris like Player vs Player game.** For basic game instructions start with /tetris\_instructions/ . In addition there will be items which spawn randomly in pieces and collected via clearing that line. The service consists of the following components: a react app that serves as frontend, a C\# server, that contains the game logic and synchronizes and manages the players.

**The Game rules:** The base game should be like multiplayer Tetris. Keyboard controls only. In addition to arrowkeys, give the W, A, S, D \-keys the same functionality so both could be used. For the target selection in multiplayer use the number keys at the top of the keyboard.

The target will not be attacked via the “garbage” described on /tetris\_instructions/ but via items. Items are used on the F key. The number 1 will always be yourself. Garbage attacks as described on the website, should follow the rules of a cycling selection system instead of manual targeting. Show which player is currently selected by yourself via highlighting. You can not see who is targeting you, but you can see which items they currently have in a small bar beneath their window.

When Items are used on you, the name of the items is shortly flashed on top of your tetris window. The same happens on the miniature version of the other players window, when Items are used on them.

Items spawn on a piece with a chance of 1 to 10\. The Item will be “inside” one of the small blocks composing the piece. After the piece is placed the item can be obtained by clearing the line containing the small item block. The item should be visible via a small icon.

The Items: For now we will only add 5 Items. Make it easy to add more items.

- Add garbage-line to target  
- Clear bottom-line of target  
- Fully clear all placed pieces  
- swap your “window” or rather all placed pieces with the target window  
- Make every placed piece and the “shadow placement piece” of the invisible for 4 seconds

**React frontend:** Build the react website in /actionBlocks\_website/. Make sure to keep the css in modules. make sure that all used graphics can be exchanged later easily.

**Style:** Don't use any graphic styles or icons or names related to tetris, because of copyright. For the start use a simple pixel retro style. Clarity and contrast is important for now. The basic background should be black with white UI and slight variations of the vibrant Tetris colors as color scheme for the blocks.

**C\#, ASP.NET Core game server:** Build the game server in /actionBlocks\_server/. the server should contain a component for the game logic. The server should manage players and lobbies.  
The process is the following: a player opens the website, he creates a lobby, he joins that lobby. In that lobby there is a visible lobby code with which others can join that same lobby.  
Make sure the game logic is modular so it can be altered later.

infrastructure: all components should be dockerized. make a dockerfile for each component and one compose file for all components together. The website container will be inside an external docker network “actionBlocks\_website” and the game server will be inside the external docker network “actionBlocks\_server”. Both networks are connected to a reverse proxy container that already exists.  
