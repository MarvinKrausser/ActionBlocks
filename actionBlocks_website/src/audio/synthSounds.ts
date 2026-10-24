import { noteToFreq } from './pitch';
import type { PlayOptions, SoundId } from './types';

/*
 * The volumes in these recipes shape each sound and balance its parts against each other.
 * To make a whole sound louder or quieter, change its value in config/sound.ts instead.
 */

/** Builds one sound effect from oscillators and noise, connected to `out`. */
export type SynthRecipe = (ctx: AudioContext, out: AudioNode, options: PlayOptions) => void;

interface ToneOptions {
  type?: OscillatorType;
  from: number;
  to?: number;
  /** Delay in seconds from now. */
  start?: number;
  duration: number;
  volume?: number;
}

/** A single oscillator with an exponential fade out and an optional pitch slide. */
export function tone(ctx: AudioContext, out: AudioNode, { type = 'square', from, to = from, start = 0, duration, volume = 0.2 }: ToneOptions) {
  const t = ctx.currentTime + start;
  const osc = ctx.createOscillator();
  const gain = ctx.createGain();
  osc.type = type;
  osc.frequency.setValueAtTime(from, t);
  if (to !== from) osc.frequency.exponentialRampToValueAtTime(to, t + duration);
  gain.gain.setValueAtTime(volume, t);
  gain.gain.exponentialRampToValueAtTime(0.0001, t + duration);
  osc.connect(gain).connect(out);
  osc.start(t);
  osc.stop(t + duration + 0.02);
}

interface NoiseOptions {
  start?: number;
  duration: number;
  volume?: number;
  filter?: BiquadFilterType;
  filterFrom?: number;
  filterTo?: number;
}

const noiseBuffers = new WeakMap<BaseAudioContext, AudioBuffer>();

function noiseBuffer(ctx: BaseAudioContext): AudioBuffer {
  let buffer = noiseBuffers.get(ctx);
  if (!buffer) {
    buffer = ctx.createBuffer(1, ctx.sampleRate, ctx.sampleRate);
    const data = buffer.getChannelData(0);
    for (let i = 0; i < data.length; i++) data[i] = Math.random() * 2 - 1;
    noiseBuffers.set(ctx, buffer);
  }
  return buffer;
}

/** Filtered white noise burst, for drops, hits and drums. */
export function noise(
  ctx: AudioContext,
  out: AudioNode,
  { start = 0, duration, volume = 0.2, filter = 'lowpass', filterFrom = 4000, filterTo = filterFrom }: NoiseOptions,
) {
  const t = ctx.currentTime + start;
  const source = ctx.createBufferSource();
  source.buffer = noiseBuffer(ctx);
  const biquad = ctx.createBiquadFilter();
  biquad.type = filter;
  biquad.frequency.setValueAtTime(filterFrom, t);
  if (filterTo !== filterFrom) biquad.frequency.exponentialRampToValueAtTime(filterTo, t + duration);
  const gain = ctx.createGain();
  gain.gain.setValueAtTime(volume, t);
  gain.gain.exponentialRampToValueAtTime(0.0001, t + duration);
  source.connect(biquad).connect(gain).connect(out);
  source.start(t);
  source.stop(t + duration + 0.02);
}

/** Plays notes one after another, `step` seconds apart. */
function arpeggio(ctx: AudioContext, out: AudioNode, notes: string[], step: number, type: OscillatorType, volume: number, noteLength = step * 1.5) {
  notes.forEach((note, i) =>
    tone(ctx, out, { type, from: noteToFreq(note), start: i * step, duration: noteLength, volume }),
  );
}

export const synthSounds: Record<SoundId, SynthRecipe> = {
  move: (ctx, out) => tone(ctx, out, { from: 220, duration: 0.03, volume: 0.06 }),
  rotate: (ctx, out) => tone(ctx, out, { from: 440, to: 660, duration: 0.045, volume: 0.07 }),
  hold: (ctx, out) => {
    tone(ctx, out, { type: 'triangle', from: 330, to: 495, duration: 0.07, volume: 0.2 });
    tone(ctx, out, { type: 'triangle', from: 495, start: 0.05, duration: 0.08, volume: 0.18 });
  },
  hardDrop: (ctx, out) => {
    noise(ctx, out, { duration: 0.12, volume: 0.25, filterFrom: 3000, filterTo: 200 });
    tone(ctx, out, { from: 160, to: 60, duration: 0.1, volume: 0.12 });
  },
  lock: (ctx, out) => tone(ctx, out, { type: 'triangle', from: 130, to: 80, duration: 0.07, volume: 0.25 }),
  lineClear: (ctx, out, { intensity = 1 }) => {
    // Each extra line raises the sweep by a major third.
    const base = 523 * 2 ** (((Math.min(intensity, 3) - 1) * 4) / 12);
    tone(ctx, out, { from: base, to: base * 2, duration: 0.16, volume: 0.1 });
    tone(ctx, out, { type: 'triangle', from: base * 1.5, to: base * 3, start: 0.04, duration: 0.16, volume: 0.15 });
    noise(ctx, out, { duration: 0.18, volume: 0.06, filter: 'highpass', filterFrom: 2000, filterTo: 8000 });
  },
  quad: (ctx, out) => {
    arpeggio(ctx, out, ['C5', 'E5', 'G5', 'C6', 'E6'], 0.065, 'square', 0.09);
    tone(ctx, out, { type: 'triangle', from: noteToFreq('C6'), start: 0.33, duration: 0.4, volume: 0.2 });
  },
  garbageIncoming: (ctx, out) => tone(ctx, out, { type: 'sawtooth', from: 110, to: 70, duration: 0.2, volume: 0.09 }),
  itemPickup: (ctx, out) => {
    tone(ctx, out, { from: noteToFreq('E6'), duration: 0.06, volume: 0.08 });
    tone(ctx, out, { from: noteToFreq('B6'), start: 0.06, duration: 0.12, volume: 0.08 });
  },
  itemUse: (ctx, out) => {
    tone(ctx, out, { from: 660, to: 1760, duration: 0.14, volume: 0.08 });
    noise(ctx, out, { duration: 0.12, volume: 0.06, filter: 'highpass', filterFrom: 1000, filterTo: 6000 });
  },
  itemHit: (ctx, out) => {
    tone(ctx, out, { type: 'sawtooth', from: 600, to: 120, duration: 0.28, volume: 0.1 });
    noise(ctx, out, { duration: 0.22, volume: 0.12, filterFrom: 5000, filterTo: 300 });
  },
  countdown: (ctx, out) => tone(ctx, out, { from: noteToFreq('A4'), duration: 0.12, volume: 0.1 }),
  go: (ctx, out) => tone(ctx, out, { from: noteToFreq('A5'), duration: 0.35, volume: 0.1 }),
  knockout: (ctx, out) => {
    tone(ctx, out, { from: 400, to: 100, duration: 0.3, volume: 0.08 });
    noise(ctx, out, { start: 0.05, duration: 0.25, volume: 0.1, filterFrom: 1500, filterTo: 100 });
  },
  lose: (ctx, out) => arpeggio(ctx, out, ['G4', 'E4', 'C4', 'G3'], 0.18, 'triangle', 0.25, 0.22),
  win: (ctx, out) => arpeggio(ctx, out, ['C5', 'E5', 'G5', 'C6', 'G5', 'C6'], 0.1, 'square', 0.08, 0.16),
};
