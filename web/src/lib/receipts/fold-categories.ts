import type { CategorySpend } from '../api/types'

// Keeps a category bar chart readable: past this many bars, the long tail folds
// into a single "Other" bucket rather than growing the chart forever.
export const CATEGORY_CHART_LIMIT = 7

// `categories` is expected pre-sorted by amount descending (as the API returns it).
// Only folds when there's more than one item to fold — otherwise renaming the
// last category "Other" would hide it for no reason.
export function foldTopCategories(categories: CategorySpend[]): CategorySpend[] {
  if (categories.length <= CATEGORY_CHART_LIMIT + 1) {
    return categories
  }

  const visible = categories.slice(0, CATEGORY_CHART_LIMIT)
  const folded = categories.slice(CATEGORY_CHART_LIMIT)

  return [
    ...visible,
    {
      categoryId: 'other',
      categoryName: 'Other',
      totalAmount: folded.reduce((sum, c) => sum + c.totalAmount, 0),
      lineItemCount: folded.reduce((sum, c) => sum + c.lineItemCount, 0),
    },
  ]
}
