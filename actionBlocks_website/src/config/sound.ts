import type { SoundId } from '../audio/types';

/**
 * Central mix for developers. Every value multiplies the level a sound was designed with:
 * 1 = as designed, 0 = silent, above 1 = louder (up to about 2 is sensible).
 * The players' volume sliders apply on top of this.
 */
export const soundMix = {
  /** Applied to everything. */
  master: 1,
  /** All sound effects. */
  sfx: 1,
  /** All background music. */
  music: 0.1,
  /** Per sound effect. Every SoundId must be listed, so new effects can't be forgotten. */
  effects: {
    move: 1,
    rotate: 1,
    hold: 1,
    hardDrop: 1,
    lock: 1,
    lineClear: 1,
    quad: 1,
    garbageIncoming: 1,
    itemPickup: 1,
    itemUse: 1,
    itemHit: 1,
    countdown: 1,
    go: 1,
    knockout: 1,
    lose: 1,
    win: 1,
  } satisfies Record<SoundId, number>,
  /** Per music track, by its id in `musicTracks` (theme/assets.ts). Tracks not listed use 1. */
  tracks: {
    pixel_rush: 1,
    neon_stack: 1,
    last_line: 1,
  } as Partial<Record<string, number>>,
};

/** Initial slider positions (0..1) for players without saved settings. */
export const defaultSfxVolume = 0.6;
export const defaultMusicVolume = 0.35;
