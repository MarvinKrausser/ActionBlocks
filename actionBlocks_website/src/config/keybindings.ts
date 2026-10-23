import type { InputAction } from '../net/types';

/** Actions bound to keys. Movement actions auto-repeat while held (see useKeyboardInput). */
export type BoundAction = 'MoveLeft' | 'MoveRight' | 'SoftDrop' | 'RotateCw' | 'RotateCcw' | 'HardDrop' | 'Hold' | 'UseItem';

/**
 * Key bindings by KeyboardEvent.code (physical key position, independent of layout).
 * KeyY is bound too so that "Z" works on QWERTZ keyboards.
 */
export const keyBindings: Record<BoundAction, string[]> = {
  MoveLeft: ['ArrowLeft', 'KeyA'],
  MoveRight: ['ArrowRight', 'KeyD'],
  SoftDrop: ['ArrowDown', 'KeyS'],
  RotateCw: ['ArrowUp', 'KeyW', 'KeyX', 'KeyE'],
  RotateCcw: ['KeyZ', 'KeyY', 'KeyQ', 'ControlLeft', 'ControlRight'],
  HardDrop: ['Space'],
  Hold: ['KeyC', 'ShiftLeft', 'ShiftRight'],
  UseItem: ['KeyF'],
};

/** Number row keys select the item target: 1 = yourself, 2..9 = opponents in display order. */
export const targetKeys = ['Digit1', 'Digit2', 'Digit3', 'Digit4', 'Digit5', 'Digit6', 'Digit7', 'Digit8', 'Digit9'];

/** Delayed auto shift and auto repeat rate in milliseconds. */
export const DAS_MS = 167;
export const ARR_MS = 33;

/** Human readable list for the help panel (plain ASCII, the pixel font has no arrow glyphs). */
export const controlsHelp: { keys: string; action: string }[] = [
  { keys: 'LEFT RIGHT / A D', action: 'Move' },
  { keys: 'DOWN / S', action: 'Soft drop' },
  { keys: 'SPACE', action: 'Hard drop' },
  { keys: 'UP / W / X', action: 'Rotate right' },
  { keys: 'Z / Q / CTRL', action: 'Rotate left' },
  { keys: 'C / SHIFT', action: 'Hold' },
  { keys: '1 - 9', action: 'Select target (1 = you)' },
  { keys: 'F', action: 'Use item on target' },
];

export const simpleActions: Partial<Record<BoundAction, InputAction>> = {
  RotateCw: 'RotateCw',
  RotateCcw: 'RotateCcw',
  HardDrop: 'HardDrop',
  Hold: 'Hold',
  UseItem: 'UseItem',
};
