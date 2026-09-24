export interface Dimensions {
  width: number
  height: number
}

// Scales down to fit within maxDimension on the longest side, preserving aspect ratio.
// Never scales up — a small source image is left alone.
export function calculateTargetDimensions(width: number, height: number, maxDimension: number): Dimensions {
  if (width <= maxDimension && height <= maxDimension) {
    return { width, height }
  }

  const scale = maxDimension / Math.max(width, height)
  return {
    width: Math.round(width * scale),
    height: Math.round(height * scale),
  }
}
