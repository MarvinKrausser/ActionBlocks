import { useEffect, useRef } from 'react';
import type { PieceInfo } from '../net/types';
import { drawPreviewPiece, setupCanvas } from '../render/draw';
import styles from './PiecePreview.module.css';

interface Props {
  title: string;
  pieces: (PieceInfo | null)[];
  cell: number;
  dimmed?: boolean;
}

const BOX_CELLS_W = 4;
const BOX_CELLS_H = 2.6;

/** Shows the hold slot or the next queue. */
export function PiecePreview({ title, pieces, cell, dimmed = false }: Props) {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const boxW = BOX_CELLS_W * cell + cell;
  const boxH = BOX_CELLS_H * cell;

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = setupCanvas(canvas, boxW, boxH * Math.max(1, pieces.length));
    ctx.fillStyle = '#000';
    ctx.fillRect(0, 0, boxW, boxH * Math.max(1, pieces.length));
    pieces.forEach((piece, i) => {
      if (piece) drawPreviewPiece(ctx, piece, 0, i * boxH, boxW, boxH, i === 0 ? cell : Math.round(cell * 0.8), dimmed);
    });
  }, [pieces, cell, boxW, boxH, dimmed]);

  return (
    <div className={styles.preview}>
      <div className={styles.title}>{title}</div>
      <canvas ref={canvasRef} className={styles.canvas} />
    </div>
  );
}
