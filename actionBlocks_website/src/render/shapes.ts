/** Spawn-orientation shapes by piece type (matches PieceDefinitions on the server), used for previews. */
export const pieceShapes: Record<number, [number, number][]> = {
  1: [[0, 0], [1, 0], [2, 0], [3, 0]],
  2: [[0, 0], [0, 1], [1, 1], [2, 1]],
  3: [[2, 0], [0, 1], [1, 1], [2, 1]],
  4: [[0, 0], [1, 0], [0, 1], [1, 1]],
  5: [[1, 0], [2, 0], [0, 1], [1, 1]],
  6: [[0, 0], [1, 0], [1, 1], [2, 1]],
  7: [[1, 0], [0, 1], [1, 1], [2, 1]],
  // Odd shapes, only dealt during a Transformation.
  9: [[0, 0]],
  10: [[0, 0], [0, 1], [1, 1]],
  11: [[0, 0], [2, 0], [0, 1], [1, 1], [2, 1]],
  12: [[1, 0], [0, 1], [1, 1], [2, 1], [1, 2]],
  13: [[0, 0], [0, 1], [1, 1], [1, 2], [2, 2]],
};
