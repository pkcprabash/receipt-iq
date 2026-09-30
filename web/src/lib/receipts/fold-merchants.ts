import type { MerchantSpend } from '../api/types'

// Same reasoning as fold-categories: keep a merchant bar chart readable by folding
// a long tail into "Other" rather than growing the chart forever.
export const MERCHANT_CHART_LIMIT = 7

export function foldTopMerchants(merchants: MerchantSpend[]): MerchantSpend[] {
  if (merchants.length <= MERCHANT_CHART_LIMIT + 1) {
    return merchants
  }

  const visible = merchants.slice(0, MERCHANT_CHART_LIMIT)
  const folded = merchants.slice(MERCHANT_CHART_LIMIT)

  return [
    ...visible,
    {
      merchantId: null,
      merchantName: 'Other',
      totalAmount: folded.reduce((sum, m) => sum + m.totalAmount, 0),
      receiptCount: folded.reduce((sum, m) => sum + m.receiptCount, 0),
    },
  ]
}
