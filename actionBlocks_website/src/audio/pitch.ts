const SEMITONES: Record<string, number> = { C: 0, D: 2, E: 4, F: 5, G: 7, A: 9, B: 11 };

/** Converts a note name like "A4", "C#5" or "Bb3" to a frequency in Hz (A4 = 440). */
export function noteToFreq(name: string): number {
  const match = /^([A-G])([#b]?)(-?\d)$/.exec(name);
  if (!match) throw new Error(`Invalid note name: ${name}`);
  const [, letter, accidental, octave] = match;
  const semitone = SEMITONES[letter] + (accidental === '#' ? 1 : accidental === 'b' ? -1 : 0);
  const midi = (Number(octave) + 1) * 12 + semitone;
  return 440 * 2 ** ((midi - 69) / 12);
}
