import { describe, expect, it } from 'vitest'
import { compareCategorySpend } from './category-comparison'

function spend(categoryId: string, categoryName: string, totalAmount: number) {
  return { categoryId, categoryName, totalAmount, lineItemCount: 1 }
}

describe('compareCategorySpend', () => {
  it('matches up a category present in both periods', () => {
    const rows = compareCategorySpend([spend('a', 'Groceries', 120)], [spend('a', 'Groceries', 100)])

    expect(rows).toEqual([
      { categoryId: 'a', categoryName: 'Groceries', currentAmount: 120, previousAmount: 100, deltaAmount: 20, deltaPercent: 20 },
    ])
  })

  it('shows a category only spent on this period as zero previously', () => {
    const rows = compareCategorySpend([spend('a', 'Travel', 200)], [])

    expect(rows[0]).toMatchObject({ currentAmount: 200, previousAmount: 0, deltaPercent: null })
  })

  it('shows a category dropped entirely as zero this period', () => {
    const rows = compareCategorySpend([], [spend('a', 'Dining', 50)])

    expect(rows[0]).toMatchObject({ currentAmount: 0, previousAmount: 50, deltaAmount: -50, deltaPercent: -100 })
  })

  it('sorts by current amount, largest first', () => {
    const rows = compareCategorySpend(
      [spend('a', 'A', 10), spend('b', 'B', 90)],
      [spend('a', 'A', 10), spend('b', 'B', 10)],
    )

    expect(rows.map((r) => r.categoryId)).toEqual(['b', 'a'])
  })
})
