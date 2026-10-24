import { soundMix } from '../config/sound';
import { musicTracks, soundFiles } from '../theme/assets';
import { MusicPlaylist } from './MusicPlaylist';
import { defaultSoundSettings } from './settings';
import { synthSounds } from './synthSounds';
import type { MusicTrack, PlayOptions, SoundId, SoundPlayer, SoundSettings } from './types';

/** The same sound is not restarted more often than this, e.g. many moves inside one snapshot. */
const THROTTLE_MS = 30;

const log = (id: SoundId) => {
  if (import.meta.env.DEV) console.debug('[sound]', id);
};

/**
 * Default SoundPlayer built on the Web Audio API. Effects use a registered file from
 * `soundFiles` when it loaded, otherwise the synth recipe; music comes from `musicTracks`.
 */
export class WebAudioPlayer implements SoundPlayer {
  private ctx: AudioContext | null = null;
  private sfxGain: GainNode | null = null;
  private musicGain: GainNode | null = null;
  /** One gain node per effect, set from soundMix.effects. */
  private readonly effectGains = new Map<SoundId, GainNode>();
  private playlist: MusicPlaylist | null = null;
  private settings: SoundSettings = defaultSoundSettings;
  private wantMusic = false;
  private tempo = 1;
  private readonly buffers = new Map<string, Promise<AudioBuffer | null>>();
  private readonly loaded = new Map<string, AudioBuffer>();
  private readonly lastPlayed = new Map<SoundId, number>();
  private readonly trackListeners = new Set<() => void>();

  unlock() {
    if (!this.ctx) {
      try {
        this.ctx = new AudioContext();
      } catch {
        return; // No Web Audio support: the game simply stays silent.
      }
      const master = this.ctx.createGain();
      master.gain.value = soundMix.master;
      master.connect(this.ctx.destination);
      this.sfxGain = this.ctx.createGain();
      this.musicGain = this.ctx.createGain();
      this.sfxGain.connect(master);
      this.musicGain.connect(master);
      this.playlist = new MusicPlaylist(this.ctx, this.musicGain, musicTracks, (src) => this.loadBuffer(src));
      this.playlist.onChange(() => this.trackListeners.forEach((l) => l()));
      this.applySettings(this.settings);
      this.playlist.setTempo(this.tempo);
      Object.values(soundFiles).forEach((src) => void this.loadBuffer(src));
      if (this.wantMusic && !this.settings.musicMuted) this.playlist.start();
    }
    if (this.ctx.state === 'suspended') void this.ctx.resume();
  }

  play(id: SoundId, options: PlayOptions = {}) {
    const ctx = this.ctx;
    if (!ctx || !this.sfxGain || this.settings.sfxMuted || ctx.state !== 'running') return;
    if (soundMix.effects[id] <= 0) return;

    const now = performance.now();
    if (now - (this.lastPlayed.get(id) ?? -Infinity) < THROTTLE_MS) return;
    this.lastPlayed.set(id, now);
    log(id);

    const file = soundFiles[id];
    const buffer = file ? this.loaded.get(file) : undefined;
    if (buffer) {
      const source = ctx.createBufferSource();
      source.buffer = buffer;
      source.connect(this.effectGain(id));
      source.start();
    } else {
      synthSounds[id](ctx, this.effectGain(id), options);
    }
  }

  private effectGain(id: SoundId): GainNode {
    let gain = this.effectGains.get(id);
    if (!gain) {
      gain = this.ctx!.createGain();
      gain.gain.value = soundMix.effects[id];
      gain.connect(this.sfxGain!);
      this.effectGains.set(id, gain);
    }
    return gain;
  }

  startMusic() {
    this.wantMusic = true;
    if (!this.settings.musicMuted) this.playlist?.start();
  }

  stopMusic() {
    this.wantMusic = false;
    this.playlist?.stop();
  }

  nextTrack() {
    if (this.playlist?.playing) this.playlist.next();
  }

  currentTrack(): MusicTrack | null {
    return this.playlist?.currentTrack() ?? null;
  }

  onTrackChange(listener: () => void) {
    this.trackListeners.add(listener);
    return () => void this.trackListeners.delete(listener);
  }

  setMusicTempo(factor: number) {
    this.tempo = factor;
    this.playlist?.setTempo(factor);
  }

  applySettings(settings: SoundSettings) {
    this.settings = settings;
    if (!this.ctx || !this.sfxGain || !this.musicGain) return;
    const t = this.ctx.currentTime;
    // Squared for a more natural loudness curve on the sliders.
    this.sfxGain.gain.setTargetAtTime(settings.sfxMuted ? 0 : settings.sfxVolume ** 2 * soundMix.sfx, t, 0.02);
    this.musicGain.gain.setTargetAtTime(settings.musicVolume ** 2 * soundMix.music, t, 0.02);
    this.playlist?.setDisabled(settings.disabledTracks);
    // Music that is switched off doesn't run at all, so the header shows no track.
    if (settings.musicMuted) this.playlist?.stop();
    else if (this.wantMusic) this.playlist?.start();
  }

  private loadBuffer(src: string): Promise<AudioBuffer | null> {
    let promise = this.buffers.get(src);
    if (!promise) {
      const ctx = this.ctx!;
      promise = fetch(src)
        .then((response) => (response.ok ? response.arrayBuffer() : Promise.reject(new Error(response.statusText))))
        .then((data) => ctx.decodeAudioData(data))
        .then((buffer) => {
          this.loaded.set(src, buffer);
          return buffer;
        })
        .catch((error: unknown) => {
          console.warn(`Could not load sound ${src}, using the synth sound instead.`, error);
          return null;
        });
      this.buffers.set(src, promise);
    }
    return promise;
  }
}
