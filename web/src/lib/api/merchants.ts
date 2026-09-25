import { apiFetch } from './client'
import type { Merchant } from './types'

export function listMerchants(): Promise<Merchant[]> {
  return apiFetch<Merchant[]>('/merchants/')
}
