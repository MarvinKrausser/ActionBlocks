import type { SynthSong } from './types';

/*
 * Original background tunes for the synth sequencer (see SynthChannel for the pattern format).
 * Each line of a pattern is one bar of eighth notes. Channel volumes balance the voices of a
 * song against each other; the overall level of each track is set in config/sound.ts.
 */

const pixelRush: SynthSong = {
  bpm: 150,
  stepsPerBeat: 2,
  loops: 8,
  channels: [
    {
      wave: 'triangle',
      volume: 0.3,
      pattern: `
        A2 A3 A2 A3 A2 A3 A2 A3
        F2 F3 F2 F3 F2 F3 F2 F3
        C3 C4 C3 C4 C3 C4 C3 C4
        G2 G3 G2 G3 G2 G3 G2 G3`,
    },
    {
      wave: 'square',
      volume: 0.07,
      pattern: `
        A4 .  C5 .  E5 .  D5 C5
        C5 -  A4 .  F4 .  A4 .
        G4 .  C5 .  E5 .  G5 E5
        D5 -  -  .  B4 .  G4 .
        A4 .  C5 .  E5 .  A5 .
        G5 F5 E5 .  C5 .  A4 .
        E5 .  D5 .  C5 .  B4 .
        B4 -  D5 -  G5 -  -  .`,
    },
    { wave: 'noise', volume: 0.05, pattern: 'x . o . x x o .' },
  ],
};

const neonStack: SynthSong = {
  bpm: 132,
  stepsPerBeat: 2,
  loops: 6,
  channels: [
    {
      wave: 'triangle',
      volume: 0.3,
      pattern: `
        E2 .  E2 E3 .  E2 E3 .
        C2 .  C2 C3 .  C2 C3 .
        G2 .  G2 G3 .  G2 G3 .
        D2 .  D2 D3 .  D2 D3 .`,
    },
    {
      wave: 'square',
      volume: 0.06,
      pattern: `
        B4 -  -  .  G4 -  E4 .
        C5 -  -  .  E5 -  D5 .
        B4 -  -  .  D5 -  G5 .
        F#5 - -  -  D5 -  -  .
        E5 -  -  .  B4 -  G4 .
        A4 -  G4 -  E4 -  C5 .
        B4 -  D5 -  G5 -  F#5 .
        E5 -  -  -  -  -  -  .`,
    },
    {
      wave: 'triangle',
      volume: 0.08,
      pattern: `
        .  .  G4 .  .  .  B4 .
        .  .  G4 .  .  .  E4 .
        .  .  B4 .  .  .  D5 .
        .  .  A4 .  .  .  F#4 .`,
    },
    { wave: 'noise', volume: 0.05, pattern: 'x . o . x x o .' },
  ],
};

const lastLine: SynthSong = {
  bpm: 168,
  stepsPerBeat: 2,
  loops: 8,
  channels: [
    {
      wave: 'triangle',
      volume: 0.3,
      pattern: `
        D2 D2 D3 D2 D2 D3 D2 D3
        A#1 A#1 A#2 A#1 A#1 A#2 A#1 A#2
        C2 C2 C3 C2 C2 C3 C2 C3
        A1 A1 A2 A1 A1 A2 A1 A2`,
    },
    {
      wave: 'square',
      volume: 0.06,
      pattern: `
        D5 F5 A5 F5 D5 F5 A5 F5
        D5 F5 A#5 F5 D5 F5 A#5 F5
        E5 G5 C6 G5 E5 G5 C6 G5
        C#5 E5 A5 E5 C#5 E5 A5 E5
        A5 -  G5 -  F5 -  E5 F5
        D5 -  -  .  A#4 - D5 .
        E5 -  F5 -  G5 -  E5 C5
        C#5 - E5 -  A5 -  -  .`,
    },
    { wave: 'noise', volume: 0.05, pattern: 'x x o x x x o x' },
  ],
};

export const songs = { pixelRush, neonStack, lastLine };
