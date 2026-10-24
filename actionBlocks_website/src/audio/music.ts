import { noteToFreq } from './pitch';
import { noise } from './synthSounds';
import type { SynthChannel, SynthSong } from './types';

type Step = { freq: number; length: number } | { hit: 'short' | 'long' } | null;

interface ParsedChannel {
  wave: SynthChannel['wave'];
  volume: number;
  steps: Step[];
}

function parseChannel(channel: SynthChannel): ParsedChannel {
  const tokens = channel.pattern.trim().split(/\s+/);
  const steps: Step[] = [];
  let lastNote: { freq: number; length: number } | null = null;
  for (const token of tokens) {
    if (token === '-') {
      if (lastNote) lastNote.length++;
      steps.push(null);
    } else if (token === '.') {
      lastNote = null;
      steps.push(null);
    } else if (token === 'x' || token === 'o') {
      lastNote = null;
      steps.push({ hit: token === 'x' ? 'short' : 'long' });
    } else {
      lastNote = { freq: noteToFreq(token), length: 1 };
      steps.push(lastNote);
    }
  }
  return { wave: channel.wave, volume: channel.volume, steps };
}

const LOOKAHEAD_S = 0.12;
const TIMER_MS = 25;

/**
 * Plays a SynthSong with a lookahead scheduler: a timer regularly schedules the notes of the next
 * ~120 ms on the audio clock, so playback stays exact even when the timer itself is late.
 */
export class SynthSongPlayer {
  private readonly channels: ParsedChannel[];
  private readonly loopSteps: number;
  private readonly output: GainNode;
  private timer: number | undefined;
  private step = 0;
  private loopsDone = 0;
  private nextTime = 0;
  private tempo = 1;

  constructor(
    private readonly ctx: AudioContext,
    out: AudioNode,
    private readonly song: SynthSong,
    private readonly onEnded: () => void,
  ) {
    this.channels = song.channels.map(parseChannel);
    this.loopSteps = Math.max(...this.channels.map((c) => c.steps.length));
    this.output = ctx.createGain();
    this.output.connect(out);
  }

  setTempo(factor: number) {
    this.tempo = factor;
  }

  start() {
    this.nextTime = this.ctx.currentTime + 0.05;
    this.timer = window.setInterval(() => this.schedule(), TIMER_MS);
    this.schedule();
  }

  stop() {
    window.clearInterval(this.timer);
    this.timer = undefined;
    // Fade out quickly; notes already scheduled ahead are cut off with the output node.
    const t = this.ctx.currentTime;
    this.output.gain.setValueAtTime(this.output.gain.value, t);
    this.output.gain.linearRampToValueAtTime(0, t + 0.05);
    window.setTimeout(() => this.output.disconnect(), 100);
  }

  private stepDuration() {
    return 60 / (this.song.bpm * this.tempo) / this.song.stepsPerBeat;
  }

  private schedule() {
    while (this.timer !== undefined && this.nextTime < this.ctx.currentTime + LOOKAHEAD_S) {
      const duration = this.stepDuration();
      for (const channel of this.channels) this.playStep(channel, channel.steps[this.step % channel.steps.length], duration);
      this.nextTime += duration;
      if (++this.step >= this.loopSteps) {
        this.step = 0;
        if (++this.loopsDone >= this.song.loops) {
          this.onEnded();
          return;
        }
      }
    }
  }

  private playStep(channel: ParsedChannel, step: Step, stepDuration: number) {
    if (!step) return;
    const start = this.nextTime - this.ctx.currentTime;
    if ('hit' in step) {
      const long = step.hit === 'long';
      noise(this.ctx, this.output, {
        start,
        duration: long ? 0.12 : 0.04,
        volume: channel.volume,
        filter: long ? 'bandpass' : 'highpass',
        filterFrom: long ? 1800 : 7000,
      });
      return;
    }
    if (channel.wave === 'noise') return;
    const t = this.nextTime;
    const end = t + step.length * stepDuration * 0.92;
    const osc = this.ctx.createOscillator();
    const gain = this.ctx.createGain();
    osc.type = channel.wave;
    osc.frequency.setValueAtTime(step.freq, t);
    gain.gain.setValueAtTime(0, t);
    gain.gain.linearRampToValueAtTime(channel.volume, t + 0.005);
    gain.gain.setValueAtTime(channel.volume * 0.8, Math.max(t + 0.006, end - 0.03));
    gain.gain.linearRampToValueAtTime(0, end);
    osc.connect(gain).connect(this.output);
    osc.start(t);
    osc.stop(end + 0.01);
  }
}
