import { defaultMusicVolume, defaultSfxVolume } from '../config/sound';
import type { SoundSettings } from './types';

const SETTINGS_KEY = 'actionblocks.sound';

export const defaultSoundSettings: SoundSettings = {
  sfxMuted: false,
  musicMuted: false,
  sfxVolume: defaultSfxVolume,
  musicVolume: defaultMusicVolume,
  disabledTracks: [],
};

const volume = (value: unknown, fallback: number) =>
  typeof value === 'number' && value >= 0 && value <= 1 ? value : fallback;

export function loadSoundSettings(): SoundSettings {
  try {
    const stored = JSON.parse(localStorage.getItem(SETTINGS_KEY) ?? '{}') as Partial<SoundSettings> & { muted?: boolean };
    // Older versions had a single "muted" flag for everything.
    const legacyMuted = stored.muted === true;
    return {
      sfxMuted: stored.sfxMuted === true || legacyMuted,
      musicMuted: stored.musicMuted === true || legacyMuted,
      sfxVolume: volume(stored.sfxVolume, defaultSoundSettings.sfxVolume),
      musicVolume: volume(stored.musicVolume, defaultSoundSettings.musicVolume),
      disabledTracks: Array.isArray(stored.disabledTracks) ? stored.disabledTracks.filter((id) => typeof id === 'string') : [],
    };
  } catch {
    return defaultSoundSettings;
  }
}

export function saveSoundSettings(settings: SoundSettings) {
  try {
    localStorage.setItem(SETTINGS_KEY, JSON.stringify(settings));
  } catch {
    // Storage may be unavailable (private mode); settings are just not remembered.
  }
}
