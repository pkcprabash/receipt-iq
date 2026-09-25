import { describe, expect, it } from 'vitest'
import {
  filtersFromSearchParams,
  pageFromSearchParams,
  searchParamsFromFilters,
} from './receipt-filters'

describe('receipt filters', () => {
  it('round-trips filters through search params', () => {
    const filters = { from: '2026-01-01', to: '2026-01-31', categoryId: 'cat-1', merchantId: 'm-1' }

    const params = searchParamsFromFilters(filters, 3)

    expect(filtersFromSearchParams(params)).toEqual(filters)
    expect(pageFromSearchParams(params)).toBe(3)
  })

  it('omits empty filters and the first page', () => {
    const params = searchParamsFromFilters({ from: '', categoryId: 'cat-1' }, 1)

    expect(params.toString()).toBe('categoryId=cat-1')
  })

  it('falls back to page 1 for invalid page values', () => {
    expect(pageFromSearchParams(new URLSearchParams('page=abc'))).toBe(1)
    expect(pageFromSearchParams(new URLSearchParams('page=-4'))).toBe(1)
    expect(pageFromSearchParams(new URLSearchParams(''))).toBe(1)
  })
})
