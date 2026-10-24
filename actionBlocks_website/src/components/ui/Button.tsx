import type { ButtonHTMLAttributes } from 'react';
import styles from './Button.module.css';

interface Props extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger';
  size?: 'normal' | 'small';
}

export function Button({ variant = 'primary', size = 'normal', className, ...rest }: Props) {
  return (
    <button
      className={[styles.button, styles[variant], size === 'small' ? styles.small : '', className].filter(Boolean).join(' ')}
      {...rest}
    />
  );
}
