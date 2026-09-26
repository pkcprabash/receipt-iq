import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { listReceipts } from '../api/receipts'

export const NEEDS_REVIEW_FILTERS = { status: 'NeedsReview' }

// Keyed under 'receipts' so any receipt mutation that invalidates the list refreshes the queue too.
export function useReviewQueue(page: number, pageSize: number) {
  return useQuery({
    queryKey: ['receipts', NEEDS_REVIEW_FILTERS, page, pageSize],
    queryFn: () => listReceipts(NEEDS_REVIEW_FILTERS, page, pageSize),
    placeholderData: keepPreviousData,
    refetchInterval: 30_000,
  })
}
