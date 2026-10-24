import { useEffect, useRef } from 'react';
import type { ActivePieceState, MatchState, PlayerState } from '../net/types';
import { useSound } from './SoundContext';

interface Previous {
  me?: PlayerState;
  phase?: MatchState['phase'];
  countdown?: number;
}

/** Shape of a piece independent of its position, to tell rotations from moves. */
function shapeKey(piece: ActivePieceState) {
  const minX = Math.min(...piece.cells.map(([x]) => x));
  const minY = Math.min(...piece.cells.map(([, y]) => y));
  return piece.cells
    .map(([x, y]) => `${x - minX},${y - minY}`)
    .sort()
    .join(' ');
}

const queueKey = (p: PlayerState) => p.next.map((n) => n.type).join('');

/**
 * Plays sounds for everything that changes between two match snapshots. Snapshots are
 * server-confirmed, so blocked moves or rotations stay silent.
 */
export function useGameSounds(match: MatchState, playerId: string) {
  const { player } = useSound();
  const previous = useRef<Previous>({});

  useEffect(() => {
    const before = previous.current;
    const me = match.players.find((p) => p.id === playerId);
    const countdown = Math.ceil(match.countdown);

    if (match.phase === 'countdown' && countdown > 0 && countdown !== before.countdown) player.play('countdown');
    if (match.phase === 'running' && before.phase !== 'running') {
      if (before.phase === 'countdown') player.play('go');
      player.startMusic();
    }
    if (match.phase === 'finished' && before.phase !== 'finished') {
      player.stopMusic();
      if (match.winnerId === playerId) player.play('win');
    }

    for (const event of match.events) {
      if (event.type === 'itemUsed' && event.targetId === playerId) player.play('itemHit');
      else if (event.type === 'eliminated' && event.playerId === playerId) player.play('lose');
      // The last knockout ends the match; the win sound plays instead.
      else if (event.type === 'eliminated' && match.phase !== 'finished') player.play('knockout');
    }

    const old = before.me;
    if (me && old && me.alive) {
      const cleared = me.lines - old.lines;
      if (cleared >= 4) player.play('quad');
      else if (cleared > 0) player.play('lineClear', { intensity: cleared });

      const held = !old.holdUsed && me.holdUsed;
      const spawned = queueKey(me) !== queueKey(old);
      if (held) player.play('hold');
      else if (spawned && cleared === 0) player.play('lock');

      if (!held && !spawned && me.active && old.active) {
        if (shapeKey(me.active) !== shapeKey(old.active)) player.play('rotate');
        else if (me.active.cells[0][0] !== old.active.cells[0][0]) player.play('move');
      }

      if (me.inventory.length > old.inventory.length) player.play('itemPickup');
      else if (me.inventory.length < old.inventory.length) player.play('itemUse');
      if (me.pendingGarbage > old.pendingGarbage) player.play('garbageIncoming');

      player.setMusicTempo(Math.min(1.3, 1 + (me.level - 1) * 0.03));
    }

    previous.current = { me, phase: match.phase, countdown };
  }, [match, playerId, player]);

  useEffect(
    () => () => {
      player.stopMusic();
      player.setMusicTempo(1);
    },
    [player],
  );
}
