export interface Delta {
  deltaAmount: number
  // null when there's nothing to compare against (the previous period was zero) — a
  // percent change from zero is undefined, not infinite or 100%.
  deltaPercent: number | null
}

export function computeDelta(current: number, previous: number): Delta {
  return {
    deltaAmount: current - previous,
    deltaPercent: previous === 0 ? null : ((current - previous) / previous) * 100,
  }
}
