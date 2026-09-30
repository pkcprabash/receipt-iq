import type { CategorySpend } from '../api/types'
import { computeDelta, type Delta } from './delta'

export interface CategoryComparisonRow extends Delta {
  categoryId: string
  categoryName: string
  currentAmount: number
  previousAmount: number
}

// Outer-joins two single-period breakdowns by category, so a category that only
// spent in one of the two periods still shows up, at zero on the other side.
export function compareCategorySpend(current: CategorySpend[], previous: CategorySpend[]): CategoryComparisonRow[] {
  const byId = new Map<string, { name: string; current: number; previous: number }>()

  for (const entry of current) {
    byId.set(entry.categoryId, { name: entry.categoryName, current: entry.totalAmount, previous: 0 })
  }
  for (const entry of previous) {
    const existing = byId.get(entry.categoryId)
    if (existing) {
      existing.previous = entry.totalAmount
    } else {
      byId.set(entry.categoryId, { name: entry.categoryName, current: 0, previous: entry.totalAmount })
    }
  }

  return Array.from(byId.entries())
    .map(([categoryId, { name, current, previous }]) => ({
      categoryId,
      categoryName: name,
      currentAmount: current,
      previousAmount: previous,
      ...computeDelta(current, previous),
    }))
    .sort((a, b) => b.currentAmount - a.currentAmount)
}
