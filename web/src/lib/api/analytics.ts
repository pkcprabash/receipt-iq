import { apiFetch } from './client'
import type { AnalyticsRange, SpendByCategory, SpendByMerchant, SpendByMonth } from './types'

export function getSpendByCategory(range: AnalyticsRange = {}): Promise<SpendByCategory> {
  return apiFetch<SpendByCategory>('/analytics/spend-by-category', { query: { ...range } })
}

export function getSpendByMonth(range: AnalyticsRange = {}): Promise<SpendByMonth> {
  return apiFetch<SpendByMonth>('/analytics/spend-by-month', { query: { ...range } })
}

export function getSpendByMerchant(range: AnalyticsRange = {}): Promise<SpendByMerchant> {
  return apiFetch<SpendByMerchant>('/analytics/spend-by-merchant', { query: { ...range } })
}
