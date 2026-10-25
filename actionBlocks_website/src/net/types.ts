// Mirrors the server DTOs in actionBlocks_server/src/ActionBlocks.Server/Dtos/Dtos.cs

export interface LobbyPlayer {
  id: string;
  name: string;
  isHost: boolean;
}

export interface MatchResult {
  id: string;
  name: string;
  place: number;
  score: number;
  lines: number;
}

export interface Lobby {
  code: string;
  hostId: string;
  phase: 'waiting' | 'playing';
  maxPlayers: number;
  players: LobbyPlayer[];
  lastResults: MatchResult[] | null;
}

/** An item of the server's current config (config/game.json). */
export interface ItemInfo {
  id: string;
  name: string;
  /** Fraction (0..1) of spawned items that are this item; 0 = disabled. */
  share: number;
  /** Effect length for timed items, null for instant ones. */
  durationSeconds: number | null;
}

export interface JoinResult {
  playerId: string;
  lobby: Lobby;
}

export interface PieceInfo {
  type: number;
  /** items[i] is the item in mino i, null if none. */
  items: (string | null)[];
}

export interface ItemCell {
  x: number;
  y: number;
  itemId: string;
}

export interface ActivePieceState {
  type: number;
  cells: [number, number][];
  /** items[i] is the item in cells[i], null if none. */
  items: (string | null)[];
}

export interface PlayerState {
  id: string;
  name: string;
  alive: boolean;
  place: number | null;
  /** One character per visible cell, row by row: '0' empty, '1'-'7' piece colors, '8' garbage,
   *  '9'-'=' (values 9-13) odd shapes from Transformation. Decode with charCodeAt - 48. */
  board: string;
  items: ItemCell[];
  active: ActivePieceState | null;
  ghost: [number, number][] | null;
  hold: PieceInfo | null;
  holdUsed: boolean;
  next: PieceInfo[];
  inventory: string[];
  score: number;
  lines: number;
  level: number;
  pendingGarbage: number;
  invisibleRemaining: number;
  transformedRemaining: number;
}

export interface MatchEvent {
  type: 'itemUsed' | 'eliminated' | string;
  targetId?: string | null;
  itemId?: string | null;
  itemName?: string | null;
  playerId?: string | null;
  place?: number | null;
}

export interface MatchState {
  phase: 'countdown' | 'running' | 'finished';
  countdown: number;
  winnerId: string | null;
  width: number;
  height: number;
  players: PlayerState[];
  events: MatchEvent[];
}

/** Must match ActionBlocks.Game.InputAction on the server. */
export type InputAction =
  | 'MoveLeft'
  | 'MoveRight'
  | 'RotateCw'
  | 'RotateCcw'
  | 'SoftDropStart'
  | 'SoftDropStop'
  | 'HardDrop'
  | 'Hold'
  | 'UseItem';
