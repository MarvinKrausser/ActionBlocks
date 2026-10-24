import { itemVisual } from '../theme/assets';
import styles from './BlackoutTimer.module.css';

/** Seconds left on a player's blackout, in the corner of their board. */
export function BlackoutTimer({ remaining, small = false }: { remaining: number; small?: boolean }) {
  if (remaining <= 0) return null;
  const visual = itemVisual('blind');
  return (
    <div
      className={[styles.timer, small ? styles.small : '', remaining < 1 ? styles.ending : ''].join(' ')}
      style={{ color: visual.color, borderColor: visual.color }}
      title={visual.name}
    >
      <img className={styles.icon} src={visual.icon} alt="" />
      {remaining.toFixed(1)}s
    </div>
  );
}
