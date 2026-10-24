import { useEffect, useState, type MouseEvent } from 'react';
import { SoundMenu } from './SoundMenu';
import { Button } from './ui/Button';
import styles from './CornerControls.module.css';

const CONFIRM_MS = 3000;

interface Props {
  /** Shows a Leave button below the sound button. */
  onLeave?: () => void;
  /** Ask for a second click before leaving, e.g. because leaving forfeits a running match. */
  leaveConfirm?: boolean;
}

/** Sound menu and optional Leave button, fixed in the upper right corner on every screen. */
export function CornerControls({ onLeave, leaveConfirm = false }: Props) {
  const [confirming, setConfirming] = useState(false);

  useEffect(() => {
    if (!confirming) return;
    const timer = window.setTimeout(() => setConfirming(false), CONFIRM_MS);
    return () => window.clearTimeout(timer);
  }, [confirming]);

  const onLeaveClick = (e: MouseEvent<HTMLButtonElement>) => {
    // Keep keyboard focus off the button so Space (hard drop) doesn't press it.
    e.currentTarget.blur();
    if (leaveConfirm && !confirming) setConfirming(true);
    else onLeave?.();
  };

  return (
    <div className={styles.corner}>
      <SoundMenu />
      {onLeave && (
        <Button
          size="small"
          variant={confirming ? 'danger' : 'secondary'}
          onClick={onLeaveClick}
          title={leaveConfirm ? 'Leaving gives up the running match' : 'Leave the lobby'}
        >
          {confirming ? 'Sure?' : 'Leave'}
        </Button>
      )}
    </div>
  );
}
