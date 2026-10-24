import { itemVisual } from '../theme/assets';
import styles from './ItemBar.module.css';

interface Props {
  items: string[];
  /** Icon size in px. */
  size: number;
  slots?: number;
}

/** A row of item icons; the leftmost item is used next. */
export function ItemBar({ items, size, slots }: Props) {
  const count = Math.max(slots ?? 0, items.length);
  return (
    <div className={styles.bar} style={{ minHeight: size + 6 }}>
      {Array.from({ length: count }, (_, i) => {
        const id = items[i];
        if (!id) return <span key={i} className={styles.empty} style={{ width: size, height: size }} />;
        const visual = itemVisual(id);
        return (
          <img
            key={i}
            className={[styles.icon, i === 0 ? styles.first : ''].join(' ')}
            src={visual.icon}
            alt={visual.name}
            title={visual.name}
            width={size}
            height={size}
          />
        );
      })}
    </div>
  );
}
