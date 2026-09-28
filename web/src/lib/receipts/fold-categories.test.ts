import { describe, expect, it } from 'vitest'
import { foldTopCategories } from './fold-categories'

function category(name: string, amount: number) {
  return { categoryId: name, categoryName: name, totalAmount: amount, lineItemCount: 1 }
}

describe('foldTopCategories', () => {
  it('returns categories unchanged at or under the limit', () => {
    const categories = Array.from({ length: 8 }, (_, i) => category(`c${i}`, 8 - i))
    expect(foldTopCategories(categories)).toEqual(categories)
  })

  it('folds everything past the limit into a single Other bucket', () => {
    const categories = Array.from({ length: 9 }, (_, i) => category(`c${i}`, 9 - i))

    const result = foldTopCategories(categories)

    expect(result).toHaveLength(8)
    expect(result.slice(0, 7)).toEqual(categories.slice(0, 7))
    expect(result[7]).toEqual({ categoryId: 'other', categoryName: 'Other', totalAmount: 2 + 1, lineItemCount: 2 })
  })

  it('returns an empty list unchanged', () => {
    expect(foldTopCategories([])).toEqual([])
  })
})
