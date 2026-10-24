import { soundMix } from '../config/sound';
import { SynthSongPlayer } from './music';
import type { MusicTrack } from './types';

const log = (...args: unknown[]) => {
  if (import.meta.env.DEV) console.debug('[music]', ...args);
};

/**
 * Plays the registered music tracks one after another in shuffled order. Every enabled track is
 * played once before the order is reshuffled. File tracks advance when they end, synth tracks
 * after their configured number of loops.
 */
export class MusicPlaylist {
  private queue: string[] = [];
  private disabled = new Set<string>();
  private current: MusicTrack | null = null;
  private lastPlayed: string | null = null;
  private stopCurrent: (() => void) | null = null;
  private synth: SynthSongPlayer | null = null;
  private tempo = 1;
  /** Incremented on every start/stop so late async loads and end callbacks of old tracks are ignored. */
  private generation = 0;
  private readonly listeners = new Set<() => void>();

  constructor(
    private readonly ctx: AudioContext,
    private readonly out: AudioNode,
    private readonly tracks: MusicTrack[],
    private readonly loadBuffer: (src: string) => Promise<AudioBuffer | null>,
  ) {}

  get playing() {
    return this.stopCurrent !== null;
  }

  currentTrack() {
    return this.current;
  }

  onChange(listener: () => void) {
    this.listeners.add(listener);
    return () => void this.listeners.delete(listener);
  }

  setDisabled(ids: string[]) {
    this.disabled = new Set(ids);
    this.queue = this.queue.filter((id) => !this.disabled.has(id));
    if (this.current && this.disabled.has(this.current.id) && this.playing) this.next();
  }

  setTempo(factor: number) {
    this.tempo = factor;
    this.synth?.setTempo(factor);
  }

  start() {
    if (!this.playing) this.next();
  }

  stop() {
    this.generation++;
    this.stopCurrent?.();
    this.stopCurrent = null;
    this.synth = null;
    if (this.current) log('stop', this.current.id);
    this.setCurrent(null);
  }

  next() {
    this.stop();
    const track = this.takeNext();
    if (!track) return;
    const generation = this.generation;
    this.setCurrent(track);
    log('play', track.id);

    // Each track gets its own level from the central mix.
    const trackGain = this.ctx.createGain();
    trackGain.gain.value = soundMix.tracks[track.id] ?? 1;
    trackGain.connect(this.out);

    // Marks playback active right away so start() during a file load doesn't start a second track.
    this.stopCurrent = () => trackGain.disconnect();
    const ended = () => {
      if (generation === this.generation) this.next();
    };

    if (track.kind === 'synth') {
      const player = new SynthSongPlayer(this.ctx, trackGain, track.song, ended);
      player.setTempo(this.tempo);
      player.start();
      this.synth = player;
      this.stopCurrent = () => {
        player.stop();
        // Disconnect after the player's short fade out.
        window.setTimeout(() => trackGain.disconnect(), 100);
      };
      return;
    }

    void this.loadBuffer(track.src).then((buffer) => {
      if (generation !== this.generation) return;
      if (!buffer) {
        log('skip, could not load', track.src);
        // Avoid spinning when every enabled track fails to load.
        window.setTimeout(ended, 500);
        return;
      }
      const source = this.ctx.createBufferSource();
      source.buffer = buffer;
      source.connect(trackGain);
      source.onended = ended;
      source.start();
      this.stopCurrent = () => {
        source.onended = null;
        source.stop();
        source.disconnect();
        trackGain.disconnect();
      };
    });
  }

  private takeNext(): MusicTrack | null {
    const enabled = this.tracks.filter((t) => !this.disabled.has(t.id));
    if (enabled.length === 0) return null;
    if (this.queue.length === 0) {
      this.queue = shuffle(enabled.map((t) => t.id));
      // Don't repeat the last track right after reshuffling.
      if (this.queue.length > 1 && this.queue[0] === this.lastPlayed) this.queue.push(this.queue.shift()!);
    }
    const id = this.queue.shift()!;
    this.lastPlayed = id;
    return this.tracks.find((t) => t.id === id) ?? null;
  }

  private setCurrent(track: MusicTrack | null) {
    if (this.current === track) return;
    this.current = track;
    this.listeners.forEach((l) => l());
  }
}

function shuffle<T>(items: T[]): T[] {
  for (let i = items.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [items[i], items[j]] = [items[j], items[i]];
  }
  return items;
}
