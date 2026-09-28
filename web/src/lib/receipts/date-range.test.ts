import { describe, expect, it } from 'vitest'
import { currentMonthRange } from './date-range'

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
