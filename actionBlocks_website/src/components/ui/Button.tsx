import type { ButtonHTMLAttributes } from 'react';
import styles from './Button.module.css';

interface Props extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary';
}

export function Button({ variant = 'primary', className, ...rest }: Props) {
  return <button className={[styles.button, styles[variant], className].filter(Boolean).join(' ')} {...rest} />;
}
