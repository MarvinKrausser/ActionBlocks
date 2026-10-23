import { memo } from 'react';
import type { PlayerState } from '../net/types';
import type { ItemFlash } from '../net/useGameConnection';
import { BoardCanvas } from './BoardCanvas';
import { ItemBar } from './ItemBar';
import { ItemFlashOverlay } from './ItemFlashOverlay';
import styles from './MiniBoard.module.css';

interface Props {
  state: PlayerState;
  width: number;
  height: number;
  /** Number key that targets this player, if any. */
  slot?: number;
  selected: boolean;
  flashes: ItemFlash[];
  cell: number;
}

/** Small view of an opponent: board, name, target key and their current items. */
export const MiniBoard = memo(function MiniBoard({ state, width, height, slot, selected, flashes, cell }: Props) {
  return (
    <div className={[styles.mini, selected ? styles.selected : ''].join(' ')}>
      <div className={styles.header}>
        {slot !== undefined && <span className={styles.slot}>{slot}</span>}
        <span className={styles.name} title={state.name}>
          {state.name}
        </span>
      </div>
      <BoardCanvas state={state} width={width} height={height} cell={cell} highlighted={selected}>
        <ItemFlashOverlay flashes={flashes} small />
      </BoardCanvas>
      <div className={styles.items} style={{ width: width * cell + 6 }}>
        <ItemBar items={state.inventory} size={Math.max(10, cell)} />
      </div>
    </div>
  );
});
