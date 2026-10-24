import { uiAssets } from '../theme/assets';
import { blockColors } from '../theme/colors';
import styles from './Logo.module.css';

const TEXT = 'ACTION BLOCKS';

export function Logo({ small = false }: { small?: boolean }) {
  if (uiAssets.logo) {
    return <img className={small ? styles.imageSmall : styles.image} src={uiAssets.logo} alt="Action Blocks" />;
  }
  let colorIndex = 0;
  return (
    <h1 className={small ? styles.small : styles.logo} aria-label="Action Blocks">
      {TEXT.split('').map((ch, i) =>
        ch === ' ' ? (
          <span key={i} className={styles.space} />
        ) : (
          <span key={i} style={{ color: blockColors[(colorIndex++ % 7) + 1] }}>
            {ch}
          </span>
        ),
      )}
    </h1>
  );
}
