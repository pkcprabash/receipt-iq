import type { ReceiptFilters } from '../api/types'

const FILTER_KEYS = ['from', 'to', 'categoryId', 'merchantId'] as const

export function filtersFromSearchParams(params: URLSearchParams): ReceiptFilters {
  const filters: ReceiptFilters = {}
  for (const key of FILTER_KEYS) {
    const value = params.get(key)
    if (value) {
      filters[key] = value
    }
  }
  return filters
}

// Empty values are dropped so cleared filters don't linger in the URL.
export function searchParamsFromFilters(filters: ReceiptFilters, page = 1): URLSearchParams {
  const params = new URLSearchParams()
  for (const key of FILTER_KEYS) {
    const value = filters[key]
    if (value) {
      params.set(key, value)
    }
  }
  if (page > 1) {
    params.set('page', String(page))
  }
  return params
}

export function pageFromSearchParams(params: URLSearchParams): number {
  const page = Number.parseInt(params.get('page') ?? '', 10)
  return Number.isInteger(page) && page > 1 ? page : 1
}
