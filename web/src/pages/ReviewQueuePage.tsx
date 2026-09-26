import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { useReviewQueue } from '@/lib/receipts/review-queue'

const PAGE_SIZE = 20

export function ReviewQueuePage() {
  const [page, setPage] = useState(1)
  const queueQuery = useReviewQueue(page, PAGE_SIZE)
  const data = queueQuery.data
  const totalPages = data ? Math.max(Math.ceil(data.totalCount / data.pageSize), 1) : 1

  return (
    <div>
      <h1 className="text-2xl font-semibold">Review queue</h1>
      <p className="mt-2 text-muted-foreground">
        These receipts were extracted with low confidence. Check the details against the original, fix anything
        that’s off, then confirm.
      </p>

      <div className="mt-6">
        {queueQuery.isPending && <p className="text-sm text-muted-foreground">Loading…</p>}

        {queueQuery.isError && (
          <p className="text-sm text-destructive">Couldn’t load the review queue. Try again in a moment.</p>
        )}

        {data && data.items.length === 0 && (
          <p className="text-sm text-muted-foreground">You’re all caught up — nothing needs review.</p>
        )}

        {data && data.items.length > 0 && (
          <ul className="divide-y rounded-lg border">
            {data.items.map((receipt) => (
              <li key={receipt.id} className="flex items-center justify-between gap-4 px-4 py-3 text-sm">
                <div>
                  <p className="font-medium">{receipt.merchantName ?? 'Unknown merchant'}</p>
                  <p className="text-muted-foreground">
                    {receipt.purchaseDate
                      ? new Date(`${receipt.purchaseDate}T00:00:00`).toLocaleDateString()
                      : 'No date'}
                    {' · '}
                    {receipt.totalAmount !== null ? `$${receipt.totalAmount.toFixed(2)}` : 'No total'}
                  </p>
                </div>
                <Button variant="outline" size="sm" render={<Link to={`/receipts/${receipt.id}`} />} nativeButton={false}>
                  Review
                </Button>
              </li>
            ))}
          </ul>
        )}

        {data && data.totalCount > data.pageSize && (
          <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
            <span>
              Page {data.page} of {totalPages}
            </span>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>
                Previous
              </Button>
              <Button variant="outline" size="sm" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>
                Next
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
