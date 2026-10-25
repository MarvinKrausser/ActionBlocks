import { useCallback, useEffect, useMemo, useState } from 'react';
import { useCurrentTrack, useSound } from '../audio/SoundContext';
import { useGameSounds } from '../audio/useGameSounds';
import { EffectTimers } from '../components/EffectTimers';
import { BoardCanvas } from '../components/BoardCanvas';
import { ItemBar } from '../components/ItemBar';
import { ItemFlashOverlay } from '../components/ItemFlashOverlay';
import { Logo } from '../components/Logo';
import { MiniBoard } from '../components/MiniBoard';
import { PiecePreview } from '../components/PiecePreview';
import { CornerControls } from '../components/CornerControls';
import { useKeyboardInput } from '../input/useKeyboardInput';
import type { InputAction, Lobby, MatchState } from '../net/types';
import type { GameActions, ItemFlash } from '../net/useGameConnection';
import { itemVisual } from '../theme/assets';
import styles from './GamePage.module.css';

interface Props {
  match: MatchState;
  lobby: Lobby;
  playerId: string;
  flashes: ItemFlash[];
  actions: GameActions;
}

const INVENTORY_SLOTS = 10;

function useWindowHeight() {
  const [height, setHeight] = useState(window.innerHeight);
  useEffect(() => {
    const onResize = () => setHeight(window.innerHeight);
    window.addEventListener('resize', onResize);
    return () => window.removeEventListener('resize', onResize);
  }, []);
  return height;
}

export function GamePage({ match, lobby, playerId, flashes, actions }: Props) {
  const me = match.players.find((p) => p.id === playerId);
  const opponents = useMemo(() => match.players.filter((p) => p.id !== playerId), [match.players, playerId]);
  const [targetId, setTargetId] = useState(playerId);
  const windowHeight = useWindowHeight();

  const cell = Math.max(16, Math.min(32, Math.floor((windowHeight - 170) / match.height)));
  const miniCell = opponents.length <= 2 ? 14 : opponents.length <= 4 ? 12 : 9;

  // Fall back to self when the selected opponent is gone or eliminated (the server does the same).
  const target = match.players.find((p) => p.id === targetId);
  useEffect(() => {
    if (targetId !== playerId && (!target || !target.alive)) setTargetId(playerId);
  }, [target, targetId, playerId]);

  const onTarget = useCallback(
    (slot: number) => {
      const next = slot === 1 ? me : opponents[slot - 2];
      if (!next || !next.alive) return;
      setTargetId(next.id);
      actions.selectTarget(next.id);
    },
    [me, opponents, actions],
  );

  const { player } = useSound();
  const track = useCurrentTrack();
  useGameSounds(match, playerId);

  // A hard drop can't be told apart from a normal lock in the snapshots, so it sounds on input.
  const onAction = useCallback(
    (action: InputAction) => {
      if (action === 'HardDrop') player.play('hardDrop');
      actions.sendInput(action);
    },
    [actions, player],
  );

  useKeyboardInput({
    enabled: !!me?.alive && match.phase !== 'finished',
    onAction,
    onTarget,
  });

  const flashesFor = (id: string) => flashes.filter((f) => f.targetId === id);
  const winner = match.players.find((p) => p.id === match.winnerId);
  const nextItem = me?.inventory[0];

  return (
    <main className={styles.page}>
      <header className={styles.header}>
        <Logo small />
        <div className={styles.headerInfo}>
          {track && <span className={styles.track}>MUSIC: {track.title} (N)</span>}
          <span className={styles.lobbyCode}>LOBBY {lobby.code}</span>
        </div>
      </header>
      <CornerControls
        onLeave={() => void actions.leaveLobby()}
        leaveConfirm={!!me?.alive && match.phase !== 'finished'}
      />

      <div className={styles.arena}>
        {me ? (
          <section className={styles.own}>
            <div className={styles.side}>
              <PiecePreview title="HOLD" pieces={[me.hold]} cell={Math.round(cell * 0.7)} dimmed={me.holdUsed} />
              <div className={styles.stats}>
                <Stat label="SCORE" value={me.score} />
                <Stat label="LINES" value={me.lines} />
                <Stat label="LEVEL" value={me.level} />
              </div>
              <div className={styles.slotHint}>
                <span className={[styles.slot, targetId === playerId ? styles.slotSelected : ''].join(' ')}>1</span>
                YOU
              </div>
            </div>

            <div className={styles.boardColumn}>
              <div className={styles.boardRow}>
                <div className={styles.garbageMeter} style={{ height: match.height * cell }}>
                  <div
                    className={styles.garbageFill}
                    style={{ height: Math.min(me.pendingGarbage, match.height) * cell }}
                  />
                </div>
                <BoardCanvas
                  state={me}
                  width={match.width}
                  height={match.height}
                  cell={cell}
                  hideStack={me.invisibleRemaining > 0}
                  highlighted={targetId === playerId}
                >
                  <ItemFlashOverlay flashes={flashesFor(me.id)} />
                  <EffectTimers
                    effects={[
                      ['blind', me.invisibleRemaining],
                      ['transform', me.transformedRemaining],
                    ]}
                  />
                  {match.phase === 'countdown' && (
                    <div className={styles.overlay}>
                      <span className={styles.countdown}>{Math.ceil(match.countdown)}</span>
                    </div>
                  )}
                  {match.phase === 'finished' && (
                    <div className={styles.overlay}>
                      <span className={styles.result}>
                        {match.winnerId === playerId ? 'YOU WIN!' : `${winner?.name ?? '?'} WINS`}
                      </span>
                    </div>
                  )}
                </BoardCanvas>
              </div>
              <div className={styles.inventory} style={{ width: match.width * cell + 6 }}>
                <ItemBar items={me.inventory} size={Math.max(16, Math.round(cell * 0.85))} slots={INVENTORY_SLOTS} />
                <div className={styles.useHint}>
                  F: {nextItem ? itemVisual(nextItem).name : '-'} {'>'} {target && target.id !== playerId ? target.name : 'YOU'}
                </div>
              </div>
            </div>

            <div className={styles.side}>
              <PiecePreview title="NEXT" pieces={me.next} cell={Math.round(cell * 0.7)} />
            </div>
          </section>
        ) : (
          <div className={styles.spectator}>SPECTATING</div>
        )}

        {opponents.length > 0 && (
          <section className={styles.opponents}>
            {opponents.map((p, i) => (
              <MiniBoard
                key={p.id}
                state={p}
                width={match.width}
                height={match.height}
                cell={me ? miniCell : 14}
                slot={me && i < 8 ? i + 2 : undefined}
                selected={p.id === targetId}
                flashes={flashesFor(p.id)}
              />
            ))}
          </section>
        )}
      </div>
    </main>
  );
}

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <div className={styles.stat}>
      <div className={styles.statLabel}>{label}</div>
      <div className={styles.statValue}>{value}</div>
    </div>
  );
}
