// Local-calendar month bounds, formatted as the DateOnly strings the API expects.
// Built from the parts rather than toISOString(), which would shift across UTC midnight.
export function currentMonthRange(today: Date = new Date()): { from: string; to: string } {
  const year = today.getFullYear()
  const month = today.getMonth()
  const lastDay = new Date(year, month + 1, 0).getDate()
  return { from: formatDate(year, month, 1), to: formatDate(year, month, lastDay) }
}

export function formatDate(year: number, month: number, day: number): string {
  return `${year}-${String(month + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}

export type SpendRangePreset = 'this-month' | 'last-3-months' | 'last-6-months' | 'last-12-months' | 'all-time'

const PRESET_MONTHS_BACK: Record<Exclude<SpendRangePreset, 'all-time'>, number> = {
  'this-month': 0,
  'last-3-months': 2,
  'last-6-months': 5,
  'last-12-months': 11,
}

export const SPEND_RANGE_PRESETS: { value: SpendRangePreset; label: string }[] = [
  { value: 'this-month', label: 'This month' },
  { value: 'last-3-months', label: 'Last 3 months' },
  { value: 'last-6-months', label: 'Last 6 months' },
  { value: 'last-12-months', label: 'Last 12 months' },
  { value: 'all-time', label: 'All time' },
]

// 'all-time' omits both bounds so the API runs from the first month with spending.
export function presetRange(preset: SpendRangePreset, today: Date = new Date()): { from?: string; to?: string } {
  if (preset === 'all-time') {
    return {}
  }

  const monthsBack = PRESET_MONTHS_BACK[preset]
  const startYear = today.getFullYear()
  const startMonth = today.getMonth() - monthsBack
  const start = new Date(startYear, startMonth, 1)
  const end = new Date(today.getFullYear(), today.getMonth() + 1, 0)

  return { from: formatDate(start.getFullYear(), start.getMonth(), 1), to: formatDate(end.getFullYear(), end.getMonth(), end.getDate()) }
}

// "yyyy-MM" -> "Sep 2026", for chart axis ticks and tooltips.
export function formatMonthLabel(month: string): string {
  const [year, monthNumber] = month.split('-').map(Number)
  return new Date(year, monthNumber - 1, 1).toLocaleDateString(undefined, { month: 'short', year: 'numeric' })
}
