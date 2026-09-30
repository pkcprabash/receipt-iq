import { describe, expect, it } from 'vitest'
import { currentMonthRange, formatMonthLabel, presetRange, previousMonthRange } from './date-range'

describe('currentMonthRange', () => {
  it('spans the full month for a mid-month date', () => {
    expect(currentMonthRange(new Date(2026, 8, 15))).toEqual({ from: '2026-09-01', to: '2026-09-30' })
  })

  it('handles a leap-year February', () => {
    expect(currentMonthRange(new Date(2028, 1, 3))).toEqual({ from: '2028-02-01', to: '2028-02-29' })
  })

  it('handles a December date without rolling into next year', () => {
    expect(currentMonthRange(new Date(2026, 11, 31))).toEqual({ from: '2026-12-01', to: '2026-12-31' })
  })
})

describe('presetRange', () => {
  it('matches currentMonthRange for this-month', () => {
    const today = new Date(2026, 8, 15)
    expect(presetRange('this-month', today)).toEqual(currentMonthRange(today))
  })

  it('spans the last 3 months including the current one', () => {
    expect(presetRange('last-3-months', new Date(2026, 8, 15))).toEqual({ from: '2026-07-01', to: '2026-09-30' })
  })

  it('crosses a year boundary for last-12-months', () => {
    expect(presetRange('last-12-months', new Date(2026, 2, 10))).toEqual({ from: '2025-04-01', to: '2026-03-31' })
  })

  it('omits both bounds for all-time', () => {
    expect(presetRange('all-time', new Date(2026, 8, 15))).toEqual({})
  })
})

describe('formatMonthLabel', () => {
  it('formats a yyyy-MM string as a short month and year', () => {
    expect(formatMonthLabel('2026-09')).toBe('Sep 2026')
    expect(formatMonthLabel('2026-01')).toBe('Jan 2026')
  })
})

describe('previousMonthRange', () => {
  it('spans the month before a mid-month date', () => {
    expect(previousMonthRange(new Date(2026, 8, 15))).toEqual({ from: '2026-08-01', to: '2026-08-31' })
  })

  it('rolls back across a year boundary for January', () => {
    expect(previousMonthRange(new Date(2026, 0, 5))).toEqual({ from: '2025-12-01', to: '2025-12-31' })
  })
})
