import { itemVisual } from '../theme/assets';
import styles from './EffectTimers.module.css';

/** A timed item effect on a player: the item id and the seconds it has left. */
export type Effect = [itemId: string, remaining: number];

/** Seconds left on a player's timed item effects (Blackout, Transformation), in the corner of their board. */
export function EffectTimers({ effects, small = false }: { effects: Effect[]; small?: boolean }) {
  const active = effects.filter(([, remaining]) => remaining > 0);
  if (active.length === 0) return null;
  return (
    <div className={[styles.timers, small ? styles.small : ''].join(' ')}>
      {active.map(([itemId, remaining]) => {
        const visual = itemVisual(itemId);
        return (
          <div
            key={itemId}
            className={[styles.timer, remaining < 1 ? styles.ending : ''].join(' ')}
            style={{ color: visual.color, borderColor: visual.color }}
            title={visual.name}
          >
            <img className={styles.icon} src={visual.icon} alt="" />
            {remaining.toFixed(1)}s
          </div>
        );
      })}
    </div>
  );
}
