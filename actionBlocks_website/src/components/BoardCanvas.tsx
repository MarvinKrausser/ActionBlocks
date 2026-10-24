import { useEffect, useRef, type ReactNode } from 'react';
import type { PlayerState } from '../net/types';
import { drawBoard, setupCanvas } from '../render/draw';
import styles from './BoardCanvas.module.css';

interface Props {
  state: PlayerState;
  width: number;
  height: number;
  cell: number;
  hideStack?: boolean;
  highlighted?: boolean;
  /** Rendered on top of the board (flashes, countdown, results). */
  children?: ReactNode;
}

export function BoardCanvas({ state, width, height, cell, hideStack, highlighted, children }: Props) {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = setupCanvas(canvas, width * cell, height * cell);
    drawBoard(ctx, state, width, height, { cell, hideStack, showGrid: cell >= 14 });
  }, [state, width, height, cell, hideStack]);

  return (
    <div className={[styles.frame, highlighted ? styles.highlighted : '', state.alive ? '' : styles.dead].join(' ')}>
      <canvas ref={canvasRef} className={styles.canvas} />
      {!state.alive && <div className={styles.out}>{state.place ? `#${state.place}` : 'OUT'}</div>}
      {children}
    </div>
  );
}
