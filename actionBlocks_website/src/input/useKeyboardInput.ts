import { useEffect, useRef } from 'react';
import { ARR_MS, DAS_MS, keyBindings, simpleActions, targetKeys, type BoundAction } from '../config/keybindings';
import type { InputAction } from '../net/types';

interface Options {
  enabled: boolean;
  onAction: (action: InputAction) => void;
  /** Called with 1-based target slot (1 = self). */
  onTarget: (slot: number) => void;
}

const codeToAction = new Map<string, BoundAction>(
  (Object.entries(keyBindings) as [BoundAction, string[]][]).flatMap(([action, codes]) =>
    codes.map((code) => [code, action] as const),
  ),
);

type Direction = 'MoveLeft' | 'MoveRight';

/**
 * Translates keyboard events into game inputs. Left/right use delayed auto shift (DAS) with
 * auto repeat (ARR); when both are held, the most recently pressed direction wins.
 */
export function useKeyboardInput({ enabled, onAction, onTarget }: Options) {
  const handlers = useRef({ onAction, onTarget });
  handlers.current = { onAction, onTarget };

  useEffect(() => {
    if (!enabled) return;

    const heldDirections: Direction[] = [];
    const heldCodes = new Set<string>();
    let dasTimer: number | undefined;
    let arrTimer: number | undefined;
    let softDropKeys = 0;

    const stopRepeat = () => {
      window.clearTimeout(dasTimer);
      window.clearInterval(arrTimer);
      dasTimer = arrTimer = undefined;
    };

    const startRepeat = (direction: Direction) => {
      stopRepeat();
      handlers.current.onAction(direction);
      dasTimer = window.setTimeout(() => {
        arrTimer = window.setInterval(() => handlers.current.onAction(direction), ARR_MS);
      }, DAS_MS);
    };

    const isTyping = (e: KeyboardEvent) => {
      const target = e.target as HTMLElement | null;
      return target?.tagName === 'INPUT' || target?.tagName === 'TEXTAREA';
    };

    const onKeyDown = (e: KeyboardEvent) => {
      if (isTyping(e)) return;

      const slot = targetKeys.indexOf(e.code);
      if (slot >= 0) {
        e.preventDefault();
        if (!e.repeat) handlers.current.onTarget(slot + 1);
        return;
      }

      const action = codeToAction.get(e.code);
      if (!action) return;
      e.preventDefault();
      if (e.repeat || heldCodes.has(e.code)) return;
      heldCodes.add(e.code);

      if (action === 'MoveLeft' || action === 'MoveRight') {
        heldDirections.push(action);
        startRepeat(action);
      } else if (action === 'SoftDrop') {
        if (softDropKeys++ === 0) handlers.current.onAction('SoftDropStart');
      } else {
        const simple = simpleActions[action];
        if (simple) handlers.current.onAction(simple);
      }
    };

    const onKeyUp = (e: KeyboardEvent) => {
      if (!heldCodes.delete(e.code)) return;
      const action = codeToAction.get(e.code);

      if (action === 'MoveLeft' || action === 'MoveRight') {
        const index = heldDirections.indexOf(action);
        if (index >= 0) heldDirections.splice(index, 1);
        const remaining = heldDirections[heldDirections.length - 1];
        if (remaining) startRepeat(remaining);
        else stopRepeat();
      } else if (action === 'SoftDrop') {
        softDropKeys = Math.max(0, softDropKeys - 1);
        if (softDropKeys === 0) handlers.current.onAction('SoftDropStop');
      }
    };

    const releaseAll = () => {
      stopRepeat();
      heldDirections.length = 0;
      heldCodes.clear();
      if (softDropKeys > 0) handlers.current.onAction('SoftDropStop');
      softDropKeys = 0;
    };

    window.addEventListener('keydown', onKeyDown);
    window.addEventListener('keyup', onKeyUp);
    window.addEventListener('blur', releaseAll);
    return () => {
      releaseAll();
      window.removeEventListener('keydown', onKeyDown);
      window.removeEventListener('keyup', onKeyUp);
      window.removeEventListener('blur', releaseAll);
    };
  }, [enabled]);
}
