/** Every sound effect the game can play. Add an id here, a recipe in synthSounds.ts and a trigger. */
export type SoundId =
  | 'move'
  | 'rotate'
  | 'hold'
  | 'hardDrop'
  | 'lock'
  | 'lineClear'
  | 'quad'
  | 'garbageIncoming'
  | 'itemPickup'
  | 'itemUse'
  | 'itemHit'
  | 'countdown'
  | 'go'
  | 'knockout'
  | 'lose'
  | 'win';

export interface PlayOptions {
  /** Effect strength, e.g. number of lines cleared. Synth recipes raise their pitch with it. */
  intensity?: number;
}

/**
 * One voice of a synth song. `pattern` is a space separated list of steps:
 * a note name ("C4", "F#3", "A#5") starts a note, "-" holds the previous note, "." is a rest.
 * Noise channels use "x" (short hit) and "o" (long hit) instead of notes.
 * Channels shorter than the longest one repeat.
 */
export interface SynthChannel {
  wave: OscillatorType | 'noise';
  volume: number;
  pattern: string;
}

export interface SynthSong {
  bpm: number;
  /** Steps per beat, 2 = eighth notes, 4 = sixteenth notes. */
  stepsPerBeat: number;
  /** How often the pattern repeats before the playlist moves to the next track. */
  loops: number;
  channels: SynthChannel[];
}

export type MusicTrack =
  | { id: string; title: string; kind: 'synth'; song: SynthSong }
  | { id: string; title: string; kind: 'file'; src: string };

export interface SoundSettings {
  /** Silences the sound effects. */
  sfxMuted: boolean;
  /** Turns off the background music. */
  musicMuted: boolean;
  /** 0..1 */
  sfxVolume: number;
  /** 0..1 */
  musicVolume: number;
  /** Ids of music tracks the player switched off. */
  disabledTracks: string[];
}

/** Everything the UI needs from the sound system. Implement it to replace the Web Audio engine. */
export interface SoundPlayer {
  /** Must be called from a user gesture once; browsers block audio before that. */
  unlock(): void;
  play(id: SoundId, options?: PlayOptions): void;
  startMusic(): void;
  stopMusic(): void;
  nextTrack(): void;
  currentTrack(): MusicTrack | null;
  /** Subscribe to track changes; returns the unsubscribe function. */
  onTrackChange(listener: () => void): () => void;
  /** 1 = normal speed. Only affects synth songs. */
  setMusicTempo(factor: number): void;
  applySettings(settings: SoundSettings): void;
}
