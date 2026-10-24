import { createContext, useCallback, useContext, useEffect, useMemo, useState, useSyncExternalStore, type ReactNode } from 'react';
import { muteKey, musicKey, nextTrackKey } from '../config/keybindings';
import { WebAudioPlayer } from './AudioEngine';
import { loadSoundSettings, saveSoundSettings } from './settings';
import type { SoundPlayer, SoundSettings } from './types';

interface SoundApi {
  player: SoundPlayer;
  settings: SoundSettings;
  setSettings: (patch: Partial<SoundSettings>) => void;
  toggleSfx: () => void;
  toggleMusic: () => void;
}

const SoundContext = createContext<SoundApi | null>(null);

interface Props {
  children: ReactNode;
  /** Defaults to the Web Audio engine; pass another SoundPlayer to replace it. */
  createPlayer?: () => SoundPlayer;
}

export function SoundProvider({ children, createPlayer = () => new WebAudioPlayer() }: Props) {
  const [player] = useState(createPlayer);
  const [settings, setSettingsState] = useState(loadSoundSettings);

  useEffect(() => {
    player.applySettings(settings);
    saveSoundSettings(settings);
  }, [player, settings]);

  const setSettings = useCallback((patch: Partial<SoundSettings>) => setSettingsState((s) => ({ ...s, ...patch })), []);
  const toggleSfx = useCallback(() => setSettingsState((s) => ({ ...s, sfxMuted: !s.sfxMuted })), []);
  const toggleMusic = useCallback(() => setSettingsState((s) => ({ ...s, musicMuted: !s.musicMuted })), []);
  // Mutes everything if anything is audible, otherwise turns everything back on.
  const toggleAll = useCallback(
    () =>
      setSettingsState((s) => {
        const mute = !s.sfxMuted || !s.musicMuted;
        return { ...s, sfxMuted: mute, musicMuted: mute };
      }),
    [],
  );

  useEffect(() => {
    // Browsers only allow audio after a user gesture.
    const unlock = () => player.unlock();
    const onKeyDown = (e: KeyboardEvent) => {
      unlock();
      const target = e.target as HTMLElement | null;
      if (e.repeat || target?.tagName === 'INPUT' || target?.tagName === 'TEXTAREA') return;
      if (e.code === muteKey) toggleAll();
      else if (e.code === musicKey) toggleMusic();
      else if (e.code === nextTrackKey) player.nextTrack();
    };
    window.addEventListener('pointerdown', unlock);
    window.addEventListener('keydown', onKeyDown);
    return () => {
      window.removeEventListener('pointerdown', unlock);
      window.removeEventListener('keydown', onKeyDown);
    };
  }, [player, toggleAll, toggleMusic]);

  const api = useMemo(
    () => ({ player, settings, setSettings, toggleSfx, toggleMusic }),
    [player, settings, setSettings, toggleSfx, toggleMusic],
  );
  return <SoundContext.Provider value={api}>{children}</SoundContext.Provider>;
}

export function useSound(): SoundApi {
  const api = useContext(SoundContext);
  if (!api) throw new Error('useSound must be used inside <SoundProvider>');
  return api;
}

/** The music track that is playing right now, or null. */
export function useCurrentTrack() {
  const { player } = useSound();
  return useSyncExternalStore(
    useCallback((onChange) => player.onTrackChange(onChange), [player]),
    () => player.currentTrack(),
  );
}
