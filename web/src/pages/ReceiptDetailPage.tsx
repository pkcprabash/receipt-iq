import { useQuery } from '@tanstack/react-query'
import { Link, useParams } from 'react-router-dom'
import { LineItemRow } from '@/components/receipts/LineItemRow'
import { ReceiptImage } from '@/components/receipts/ReceiptImage'
import { ReceiptStatusBadge } from '@/components/receipts/ReceiptStatusBadge'
import { listCategories } from '@/lib/api/categories'
import { ApiError } from '@/lib/api/client'
import { getReceipt } from '@/lib/api/receipts'

export function ReceiptDetailPage() {
  const { id = '' } = useParams()

  const receiptQuery = useQuery({
    queryKey: ['receipt', id],
    queryFn: () => getReceipt(id),
    retry: (failureCount, error) => !(error instanceof ApiError && error.status === 404) && failureCount < 3,
  })
  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: listCategories })

  const receipt = receiptQuery.data
  const categories = categoriesQuery.data ?? []

  if (receiptQuery.isPending) {
    return <p className="text-sm text-muted-foreground">Loading receipt…</p>
  }

  if (receiptQuery.isError || !receipt) {
    const notFound = receiptQuery.error instanceof ApiError && receiptQuery.error.status === 404
    return (
      <div className="space-y-2">
        <p className="text-sm text-destructive">
          {notFound ? 'Receipt not found.' : 'Couldn’t load this receipt.'}
        </p>
        <Link to="/receipts" className="text-sm underline">
          Back to receipts
        </Link>
      </div>
    )
  }

  const categoryName = categories.find((c) => c.id === receipt.dominantCategoryId)?.name
  const lineItemsTotal = receipt.lineItems.reduce((sum, item) => sum + item.amount, 0)
  const totalsDiffer = receipt.totalAmount !== null && Math.abs(receipt.totalAmount - lineItemsTotal) >= 0.005

  return (
    <div>
      <Link to="/receipts" className="text-sm text-muted-foreground hover:text-foreground">
        ← Back to receipts
      </Link>

      <div className="mt-3 flex flex-wrap items-center gap-3">
        <h1 className="text-2xl font-semibold">{receipt.merchantName ?? 'Unknown merchant'}</h1>
        <ReceiptStatusBadge status={receipt.status} />
      </div>

      <dl className="mt-4 grid grid-cols-2 gap-4 text-sm sm:grid-cols-4">
        <div>
          <dt className="text-muted-foreground">Purchased</dt>
          <dd>{receipt.purchaseDate ? new Date(`${receipt.purchaseDate}T00:00:00`).toLocaleDateString() : '—'}</dd>
        </div>
        <div>
          <dt className="text-muted-foreground">Total</dt>
          <dd>{receipt.totalAmount !== null ? `$${receipt.totalAmount.toFixed(2)}` : '—'}</dd>
        </div>
        <div>
          <dt className="text-muted-foreground">Category</dt>
          <dd>{categoryName ?? '—'}</dd>
        </div>
        <div>
          <dt className="text-muted-foreground">Uploaded</dt>
          <dd>{new Date(receipt.uploadedAtUtc).toLocaleString()}</dd>
        </div>
      </dl>

      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        <section>
          <h2 className="mb-2 font-medium">Original</h2>
          <ReceiptImage receiptId={receipt.id} contentType={receipt.imageContentType} />
        </section>

        <section>
          <h2 className="mb-2 font-medium">Line items</h2>
          {receipt.lineItems.length === 0 ? (
            <p className="text-sm text-muted-foreground">No line items were extracted.</p>
          ) : (
            <div className="overflow-x-auto rounded-lg border">
              <table className="w-full text-left text-sm">
                <thead className="border-b bg-muted/50 text-muted-foreground">
                  <tr>
                    <th className="px-4 py-2 font-medium">Item</th>
                    <th className="px-4 py-2 text-right font-medium">Amount</th>
                    <th className="px-4 py-2 font-medium">Category</th>
                    <th className="px-4 py-2" />
                  </tr>
                </thead>
                <tbody>
                  {receipt.lineItems.map((lineItem) => (
                    <LineItemRow
                      key={lineItem.id}
                      receiptId={receipt.id}
                      lineItem={lineItem}
                      categories={categories}
                    />
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {totalsDiffer && (
            <p className="mt-2 text-xs text-muted-foreground">
              Line items add up to ${lineItemsTotal.toFixed(2)}, which differs from the receipt total.
            </p>
          )}
        </section>
      </div>
    </div>
  )
}
