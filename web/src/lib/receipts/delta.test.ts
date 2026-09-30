import { describe, expect, it } from 'vitest'
import { computeDelta } from './delta'

describe('computeDelta', () => {
  it('computes a positive change', () => {
    expect(computeDelta(120, 100)).toEqual({ deltaAmount: 20, deltaPercent: 20 })
  })

  it('computes a negative change', () => {
    expect(computeDelta(80, 100)).toEqual({ deltaAmount: -20, deltaPercent: -20 })
  })

  it('returns a null percent when the previous period was zero', () => {
    expect(computeDelta(50, 0)).toEqual({ deltaAmount: 50, deltaPercent: null })
  })

  it('reports no change as zero, not null', () => {
    expect(computeDelta(50, 50)).toEqual({ deltaAmount: 0, deltaPercent: 0 })
  })
})
