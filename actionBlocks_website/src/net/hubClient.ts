import * as signalR from '@microsoft/signalr';
import { getServerUrl } from '../config/runtimeConfig';
import type { InputAction, ItemInfo, JoinResult, Lobby, MatchState } from './types';

export interface HubHandlers {
  onLobbyUpdated: (lobby: Lobby) => void;
  onMatchState: (state: MatchState) => void;
  onStatus: (status: ConnectionStatus) => void;
}

export type ConnectionStatus = 'connecting' | 'connected' | 'reconnecting' | 'disconnected';

/** Thin typed wrapper around the SignalR connection to the game hub. */
export class HubClient {
  private readonly connection: signalR.HubConnection;
  private stopped = false;

  constructor(private readonly handlers: HubHandlers) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(getServerUrl())
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('LobbyUpdated', handlers.onLobbyUpdated);
    this.connection.on('MatchState', handlers.onMatchState);
    this.connection.onreconnecting(() => handlers.onStatus('reconnecting'));
    this.connection.onreconnected(() => handlers.onStatus('connected'));
    this.connection.onclose(() => {
      handlers.onStatus('disconnected');
      if (!this.stopped) this.scheduleStart();
    });
  }

  async start(): Promise<void> {
    this.handlers.onStatus('connecting');
    try {
      await this.connection.start();
      this.handlers.onStatus('connected');
    } catch {
      this.handlers.onStatus('disconnected');
      this.scheduleStart();
    }
  }

  stop(): void {
    this.stopped = true;
    void this.connection.stop();
  }

  createLobby(name: string) {
    return this.invoke<JoinResult>('CreateLobby', name);
  }

  joinLobby(code: string, name: string) {
    return this.invoke<JoinResult>('JoinLobby', code, name);
  }

  leaveLobby() {
    return this.invoke<void>('LeaveLobby');
  }

  startGame() {
    return this.invoke<void>('StartGame');
  }

  /** Items with their spawn share and duration from the server config. */
  getItems() {
    return this.invoke<ItemInfo[]>('GetItems');
  }

  /** Fire-and-forget input; ordering is preserved by the connection. */
  input(action: InputAction) {
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      void this.connection.send('Input', action);
    }
  }

  selectTarget(targetId: string) {
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      void this.connection.send('SelectTarget', targetId);
    }
  }

  private scheduleStart() {
    window.setTimeout(() => {
      if (!this.stopped && this.connection.state === signalR.HubConnectionState.Disconnected) void this.start();
    }, 2000);
  }

  private async invoke<T>(method: string, ...args: unknown[]): Promise<T> {
    try {
      return await this.connection.invoke<T>(method, ...args);
    } catch (e) {
      throw new Error(cleanHubError(e));
    }
  }
}

function cleanHubError(e: unknown): string {
  const message = e instanceof Error ? e.message : String(e);
  const marker = 'HubException: ';
  const index = message.indexOf(marker);
  return index >= 0 ? message.slice(index + marker.length) : 'Connection problem, please try again.';
}
