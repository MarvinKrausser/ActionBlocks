import { useEffect, useRef, useState, type SyntheticEvent } from 'react';
import { useSound } from '../audio/SoundContext';
import type { SoundSettings } from '../audio/types';
import { uiAssets } from '../theme/assets';
import styles from './SoundMenu.module.css';

/** Give keyboard focus back to the game, so Space or the arrows don't operate the control. */
const blur = (e: SyntheticEvent<HTMLElement>) => e.currentTarget.blur();

interface ChannelProps {
  label: string;
  muted: boolean;
  volume: number;
  onToggle: () => void;
  onVolume: (volume: number) => void;
}

function Channel({ label, muted, volume, onToggle, onVolume }: ChannelProps) {
  return (
    <div className={[styles.channel, muted ? styles.muted : ''].join(' ')}>
      <button
        type="button"
        className={styles.iconButton}
        onClick={(e) => {
          onToggle();
          blur(e);
        }}
        title={muted ? `Unmute ${label.toLowerCase()}` : `Mute ${label.toLowerCase()}`}
        aria-pressed={muted}
      >
        <img src={muted ? uiAssets.soundOff : uiAssets.soundOn} alt={muted ? 'Muted' : 'On'} />
      </button>
      <span className={styles.label}>{label}</span>
      <input
        type="range"
        min={0}
        max={1}
        step={0.05}
        value={volume}
        aria-label={`${label} volume`}
        onChange={(e) => onVolume(Number(e.target.value))}
        onPointerUp={blur}
      />
    </div>
  );
}

/**
 * Sound button for the upper right corner. It opens an inline strip to its left with a mute button
 * and a volume slider for effects and music. It only changes settings; the game keeps running.
 */
export function SoundMenu() {
  const { settings, setSettings, toggleSfx, toggleMusic } = useSound();
  const [open, setOpen] = useState(false);
  const rootRef = useRef<HTMLDivElement>(null);

  // Publish the menu's current width (it grows when open) as a CSS variable, so headers can keep
  // their content clear of it, e.g. GamePage's lobby code and track label.
  useEffect(() => {
    const root = rootRef.current;
    if (!root) return;
    const style = document.documentElement.style;
    const observer = new ResizeObserver(() => style.setProperty('--sound-menu-width', `${root.offsetWidth}px`));
    observer.observe(root);
    return () => {
      observer.disconnect();
      style.removeProperty('--sound-menu-width');
    };
  }, []);

  useEffect(() => {
    if (!open) return;
    const onPointerDown = (e: PointerEvent) => {
      if (!rootRef.current?.contains(e.target as Node)) setOpen(false);
    };
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') setOpen(false);
    };
    document.addEventListener('pointerdown', onPointerDown);
    document.addEventListener('keydown', onKeyDown);
    return () => {
      document.removeEventListener('pointerdown', onPointerDown);
      document.removeEventListener('keydown', onKeyDown);
    };
  }, [open]);

  // Moving a muted slider turns that channel back on.
  const setVolume = (volume: 'sfxVolume' | 'musicVolume', mutedKey: 'sfxMuted' | 'musicMuted', value: number) => {
    const patch: Partial<SoundSettings> = { [volume]: value };
    if (settings[mutedKey]) patch[mutedKey] = false;
    setSettings(patch);
  };

  const allMuted = settings.sfxMuted && settings.musicMuted;

  return (
    <div className={styles.menu} ref={rootRef}>
      <button
        type="button"
        className={[styles.iconButton, styles.toggle, open ? styles.active : ''].join(' ')}
        onClick={(e) => {
          setOpen((o) => !o);
          blur(e);
        }}
        title="Sound"
        aria-expanded={open}
      >
        <img src={allMuted ? uiAssets.soundOff : uiAssets.soundOn} alt="Sound" />
      </button>
      {open && (
        <div className={styles.strip}>
          <Channel
            label="SFX"
            muted={settings.sfxMuted}
            volume={settings.sfxVolume}
            onToggle={toggleSfx}
            onVolume={(v) => setVolume('sfxVolume', 'sfxMuted', v)}
          />
          <Channel
            label="MUSIC"
            muted={settings.musicMuted}
            volume={settings.musicVolume}
            onToggle={toggleMusic}
            onVolume={(v) => setVolume('musicVolume', 'musicMuted', v)}
          />
        </div>
      )}
    </div>
  );
}
