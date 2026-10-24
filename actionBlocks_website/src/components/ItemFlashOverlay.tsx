import type { ItemFlash } from '../net/useGameConnection';
import styles from './ItemFlashOverlay.module.css';

/** Briefly shows the names of items that were used on a player, on top of their board. */
export function ItemFlashOverlay({ flashes, small = false }: { flashes: ItemFlash[]; small?: boolean }) {
  if (flashes.length === 0) return null;
  return (
    <div className={[styles.overlay, small ? styles.small : ''].join(' ')}>
      {flashes.map((f) => (
        <div key={f.key} className={styles.flash} style={{ color: f.color, borderColor: f.color }}>
          {f.text}
        </div>
      ))}
    </div>
  );
}
