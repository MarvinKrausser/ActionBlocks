/** Spawn-orientation shapes by piece type (matches PieceDefinitions on the server), used for previews. */
export const pieceShapes: Record<number, [number, number][]> = {
  1: [[0, 0], [1, 0], [2, 0], [3, 0]],
  2: [[0, 0], [0, 1], [1, 1], [2, 1]],
  3: [[2, 0], [0, 1], [1, 1], [2, 1]],
  4: [[0, 0], [1, 0], [0, 1], [1, 1]],
  5: [[1, 0], [2, 0], [0, 1], [1, 1]],
  6: [[0, 0], [1, 0], [1, 1], [2, 1]],
  7: [[1, 0], [0, 1], [1, 1], [2, 1]],
};
