import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { itemVisual } from '../theme/assets';
import { HubClient, type ConnectionStatus } from './hubClient';
import type { InputAction, Lobby, MatchState } from './types';

/** A short message shown on top of a player's board when an item hits them. */
export interface ItemFlash {
  key: number;
  targetId: string;
  text: string;
  color: string;
}

const FLASH_MS = 1400;

export function useGameConnection() {
  const [status, setStatus] = useState<ConnectionStatus>('connecting');
  const [playerId, setPlayerId] = useState<string | null>(null);
  const [lobby, setLobby] = useState<Lobby | null>(null);
  const [match, setMatch] = useState<MatchState | null>(null);
  const [flashes, setFlashes] = useState<ItemFlash[]>([]);
  const clientRef = useRef<HubClient | null>(null);
  const flashKey = useRef(0);

  useEffect(() => {
    const client = new HubClient({
      onLobbyUpdated: (next) => {
        setLobby(next);
        if (next.phase === 'waiting') setMatch(null);
      },
      onMatchState: (state) => {
        setMatch(state);
        const newFlashes = state.events
          .filter((e) => e.type === 'itemUsed' && e.targetId)
          .map<ItemFlash>((e) => ({
            key: ++flashKey.current,
            targetId: e.targetId!,
            text: e.itemName ?? itemVisual(e.itemId ?? '').name,
            color: itemVisual(e.itemId ?? '').color,
          }));
        if (newFlashes.length > 0) {
          setFlashes((current) => [...current, ...newFlashes]);
          const keys = new Set(newFlashes.map((f) => f.key));
          window.setTimeout(() => setFlashes((current) => current.filter((f) => !keys.has(f.key))), FLASH_MS);
        }
      },
      onStatus: (s) => {
        setStatus(s);
        // The server forgets players whose connection drops, so local lobby state is stale.
        if (s !== 'connected') {
          setLobby(null);
          setMatch(null);
          setPlayerId(null);
        }
      },
    });
    clientRef.current = client;
    void client.start();
    return () => client.stop();
  }, []);

  const createLobby = useCallback(async (name: string) => {
    const result = await clientRef.current!.createLobby(name);
    setPlayerId(result.playerId);
    setLobby(result.lobby);
  }, []);

  const joinLobby = useCallback(async (code: string, name: string) => {
    const result = await clientRef.current!.joinLobby(code, name);
    setPlayerId(result.playerId);
    setLobby(result.lobby);
  }, []);

  const leaveLobby = useCallback(async () => {
    await clientRef.current?.leaveLobby();
    setLobby(null);
    setMatch(null);
    setPlayerId(null);
  }, []);

  const startGame = useCallback(() => clientRef.current!.startGame(), []);
  const sendInput = useCallback((action: InputAction) => clientRef.current?.input(action), []);
  const selectTarget = useCallback((targetId: string) => clientRef.current?.selectTarget(targetId), []);

  const actions = useMemo(
    () => ({ createLobby, joinLobby, leaveLobby, startGame, sendInput, selectTarget }),
    [createLobby, joinLobby, leaveLobby, startGame, sendInput, selectTarget],
  );

  return { status, playerId, lobby, match, flashes, actions };
}

export type GameActions = ReturnType<typeof useGameConnection>['actions'];
