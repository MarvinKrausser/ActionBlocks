import type { PieceInfo, PlayerState } from '../net/types';
import { getImage, itemVisual } from '../theme/assets';
import { blockColors, boardColors } from '../theme/colors';
import { pieceShapes } from './shapes';

/** Prepares a canvas for crisp pixel drawing at the given CSS size. */
export function setupCanvas(canvas: HTMLCanvasElement, cssWidth: number, cssHeight: number): CanvasRenderingContext2D {
  const dpr = window.devicePixelRatio || 1;
  const w = Math.round(cssWidth * dpr);
  const h = Math.round(cssHeight * dpr);
  if (canvas.width !== w || canvas.height !== h) {
    canvas.width = w;
    canvas.height = h;
    canvas.style.width = `${cssWidth}px`;
    canvas.style.height = `${cssHeight}px`;
  }
  const ctx = canvas.getContext('2d')!;
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
  ctx.imageSmoothingEnabled = false;
  return ctx;
}

export function shade(hex: string, amount: number): string {
  const n = parseInt(hex.slice(1), 16);
  const adjust = (c: number) => Math.max(0, Math.min(255, Math.round(amount > 0 ? c + (255 - c) * amount : c * (1 + amount))));
  const r = adjust((n >> 16) & 255);
  const g = adjust((n >> 8) & 255);
  const b = adjust(n & 255);
  return `rgb(${r}, ${g}, ${b})`;
}

/** A single block in a simple retro bevel style. */
export function drawBlock(ctx: CanvasRenderingContext2D, x: number, y: number, size: number, color: string) {
  const edge = Math.max(1, Math.floor(size / 7));
  ctx.fillStyle = color;
  ctx.fillRect(x, y, size, size);
  ctx.fillStyle = shade(color, 0.45);
  ctx.fillRect(x, y, size, edge);
  ctx.fillRect(x, y, edge, size);
  ctx.fillStyle = shade(color, -0.45);
  ctx.fillRect(x, y + size - edge, size, edge);
  ctx.fillRect(x + size - edge, y, edge, size);
}

export function drawItemIcon(ctx: CanvasRenderingContext2D, x: number, y: number, size: number, itemId: string) {
  const visual = itemVisual(itemId);
  const image = getImage(visual.icon);
  const inset = Math.max(1, Math.floor(size / 8));
  if (image.complete && image.naturalWidth > 0) {
    ctx.drawImage(image, x + inset, y + inset, size - inset * 2, size - inset * 2);
  } else {
    ctx.fillStyle = '#000';
    ctx.fillRect(x + inset, y + inset, size - inset * 2, size - inset * 2);
    ctx.fillStyle = visual.color;
    ctx.fillRect(x + size / 3, y + size / 3, size / 3, size / 3);
  }
}

function drawGhostCell(ctx: CanvasRenderingContext2D, x: number, y: number, size: number, color: string) {
  ctx.globalAlpha = boardColors.ghostAlpha;
  ctx.fillStyle = color;
  ctx.fillRect(x, y, size, size);
  ctx.globalAlpha = 1;
  const edge = Math.max(1, Math.floor(size / 10));
  ctx.strokeStyle = color;
  ctx.lineWidth = edge;
  ctx.strokeRect(x + edge / 2, y + edge / 2, size - edge, size - edge);
}

export interface BoardDrawOptions {
  cell: number;
  /** Hide locked cells and the ghost piece (blackout item). */
  hideStack?: boolean;
  showGhost?: boolean;
  showGrid?: boolean;
}

export function drawBoard(
  ctx: CanvasRenderingContext2D,
  state: PlayerState,
  width: number,
  height: number,
  { cell, hideStack = false, showGhost = true, showGrid = true }: BoardDrawOptions,
) {
  ctx.fillStyle = boardColors.background;
  ctx.fillRect(0, 0, width * cell, height * cell);

  if (showGrid) {
    ctx.fillStyle = boardColors.grid;
    for (let x = 1; x < width; x++) ctx.fillRect(x * cell, 0, 1, height * cell);
    for (let y = 1; y < height; y++) ctx.fillRect(0, y * cell, width * cell, 1);
  }

  if (!hideStack) {
    for (let y = 0; y < height; y++) {
      for (let x = 0; x < width; x++) {
        const value = state.board.charCodeAt(y * width + x) - 48;
        if (value > 0) drawBlock(ctx, x * cell, y * cell, cell, blockColors[value] ?? '#fff');
      }
    }
    for (const item of state.items) drawItemIcon(ctx, item.x * cell, item.y * cell, cell, item.itemId);
  }

  const active = state.active;
  if (!active) return;
  const color = blockColors[active.type] ?? '#fff';

  if (showGhost && !hideStack && state.ghost) {
    const activeKeys = new Set(active.cells.map(([x, y]) => `${x},${y}`));
    for (const [x, y] of state.ghost) {
      if (y >= 0 && !activeKeys.has(`${x},${y}`)) drawGhostCell(ctx, x * cell, y * cell, cell, color);
    }
  }

  active.cells.forEach(([x, y], index) => {
    if (y < 0) return;
    drawBlock(ctx, x * cell, y * cell, cell, color);
    const itemId = active.items[index];
    if (itemId) drawItemIcon(ctx, x * cell, y * cell, cell, itemId);
  });
}

/** Draws a queued/held piece centered in a box of boxCells x 2 cells. */
export function drawPreviewPiece(
  ctx: CanvasRenderingContext2D,
  piece: PieceInfo,
  offsetX: number,
  offsetY: number,
  boxWidth: number,
  boxHeight: number,
  cell: number,
  dimmed = false,
) {
  const shape = pieceShapes[piece.type];
  if (!shape) return;
  const w = Math.max(...shape.map(([x]) => x)) + 1;
  const h = Math.max(...shape.map(([, y]) => y)) + 1;
  const ox = offsetX + (boxWidth - w * cell) / 2;
  const oy = offsetY + (boxHeight - h * cell) / 2;
  const color = dimmed ? '#555555' : (blockColors[piece.type] ?? '#fff');
  shape.forEach(([x, y], index) => {
    const px = Math.round(ox + x * cell);
    const py = Math.round(oy + y * cell);
    drawBlock(ctx, px, py, cell, color);
    const itemId = piece.items[index];
    if (itemId) drawItemIcon(ctx, px, py, cell, itemId);
  });
}
