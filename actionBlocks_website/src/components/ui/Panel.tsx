import type { ReactNode } from 'react';
import styles from './Panel.module.css';

export function Panel({ title, children, className }: { title?: string; children: ReactNode; className?: string }) {
  return (
    <section className={[styles.panel, className].filter(Boolean).join(' ')}>
      {title && <h2 className={styles.title}>{title}</h2>}
      {children}
    </section>
  );
}
