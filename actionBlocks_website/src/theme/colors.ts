/**
 * Block colors by cell value (see PlayerState.board). Swap these to restyle the blocks.
 * Index 0 is the empty cell, 1-7 the standard pieces, 8 garbage, 9-13 the Transformation shapes.
 */
export const blockColors: Record<number, string> = {
  1: '#3fe3f2',
  2: '#3a66ff',
  3: '#ff9b26',
  4: '#ffe23f',
  5: '#4fe35f',
  6: '#ff4256',
  7: '#c452ff',
  8: '#8c8c8c',
  9: '#ff6fd8',
  10: '#2fd6a3',
  11: '#b8ff3f',
  12: '#f2f2f2',
  13: '#ff7a3a',
};

export const boardColors = {
  background: '#000000',
  grid: '#141414',
  border: '#ffffff',
  ghostAlpha: 0.28,
};
