import { describe, expect, it } from 'vitest'
import { foldTopMerchants } from './fold-merchants'

function merchant(name: string, amount: number) {
  return { merchantId: name, merchantName: name, totalAmount: amount, receiptCount: 1 }
}

describe('foldTopMerchants', () => {
  it('returns merchants unchanged at or under the limit', () => {
    const merchants = Array.from({ length: 8 }, (_, i) => merchant(`m${i}`, 8 - i))
    expect(foldTopMerchants(merchants)).toEqual(merchants)
  })

  it('folds everything past the limit into a single Other bucket', () => {
    const merchants = Array.from({ length: 9 }, (_, i) => merchant(`m${i}`, 9 - i))

    const result = foldTopMerchants(merchants)

    expect(result).toHaveLength(8)
    expect(result[7]).toEqual({ merchantId: null, merchantName: 'Other', totalAmount: 2 + 1, receiptCount: 2 })
  })
})
