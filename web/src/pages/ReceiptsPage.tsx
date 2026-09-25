import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router-dom'
import { ReceiptStatusBadge } from '@/components/receipts/ReceiptStatusBadge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { listCategories } from '@/lib/api/categories'
import { listMerchants } from '@/lib/api/merchants'
import { listReceipts } from '@/lib/api/receipts'
import type { ReceiptFilters } from '@/lib/api/types'
import {
  filtersFromSearchParams,
  pageFromSearchParams,
  searchParamsFromFilters,
} from '@/lib/receipts/receipt-filters'

const PAGE_SIZE = 20

const SELECT_CLASS =
  'h-8 w-full rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30'

function formatDate(value: string): string {
  return new Date(`${value}T00:00:00`).toLocaleDateString()
}

function formatAmount(value: number | null): string {
  return value === null ? '—' : `$${value.toFixed(2)}`
}

export function ReceiptsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const filters = filtersFromSearchParams(searchParams)
  const page = pageFromSearchParams(searchParams)
  const hasFilters = Object.keys(filters).length > 0

  const receiptsQuery = useQuery({
    queryKey: ['receipts', filters, page],
    queryFn: () => listReceipts(filters, page, PAGE_SIZE),
    placeholderData: keepPreviousData,
  })
  const categoriesQuery = useQuery({ queryKey: ['categories'], queryFn: listCategories })
  const merchantsQuery = useQuery({ queryKey: ['merchants'], queryFn: listMerchants })

  const categoryNames = new Map(categoriesQuery.data?.map((c) => [c.id, c.name]))

  function updateFilter(key: keyof ReceiptFilters, value: string) {
    setSearchParams(searchParamsFromFilters({ ...filters, [key]: value }))
  }

  function goToPage(nextPage: number) {
    setSearchParams(searchParamsFromFilters(filters, nextPage))
  }

  const data = receiptsQuery.data
  const totalPages = data ? Math.max(Math.ceil(data.totalCount / data.pageSize), 1) : 1
  const rangeInvalid = filters.from && filters.to && filters.from > filters.to

  return (
    <div>
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold">Receipts</h1>
        <Button render={<Link to="/upload" />} nativeButton={false}>
          Upload receipt
        </Button>
      </div>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-5 lg:items-end">
        <div className="space-y-1.5">
          <Label htmlFor="filter-from">From</Label>
          <Input
            id="filter-from"
            type="date"
            value={filters.from ?? ''}
            onChange={(e) => updateFilter('from', e.target.value)}
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="filter-to">To</Label>
          <Input
            id="filter-to"
            type="date"
            value={filters.to ?? ''}
            onChange={(e) => updateFilter('to', e.target.value)}
          />
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="filter-category">Category</Label>
          <select
            id="filter-category"
            className={SELECT_CLASS}
            value={filters.categoryId ?? ''}
            onChange={(e) => updateFilter('categoryId', e.target.value)}
          >
            <option value="">All categories</option>
            {categoriesQuery.data?.map((category) => (
              <option key={category.id} value={category.id}>
                {category.name}
              </option>
            ))}
          </select>
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="filter-merchant">Merchant</Label>
          <select
            id="filter-merchant"
            className={SELECT_CLASS}
            value={filters.merchantId ?? ''}
            onChange={(e) => updateFilter('merchantId', e.target.value)}
          >
            <option value="">All merchants</option>
            {merchantsQuery.data?.map((merchant) => (
              <option key={merchant.id} value={merchant.id}>
                {merchant.name}
              </option>
            ))}
          </select>
        </div>
        <Button variant="outline" disabled={!hasFilters} onClick={() => setSearchParams({})}>
          Clear filters
        </Button>
      </div>

      {rangeInvalid && <p className="mt-2 text-sm text-destructive">“From” must not be after “To”.</p>}

      <div className="mt-6">
        {receiptsQuery.isPending && <p className="text-sm text-muted-foreground">Loading receipts…</p>}

        {receiptsQuery.isError && (
          <p className="text-sm text-destructive">Couldn’t load receipts. Try again in a moment.</p>
        )}

        {data && data.items.length === 0 && (
          <p className="text-sm text-muted-foreground">
            {hasFilters ? 'No receipts match these filters.' : 'No receipts yet — upload your first one.'}
          </p>
        )}

        {data && data.items.length > 0 && (
          <div className="overflow-x-auto rounded-lg border">
            <table className="w-full text-left text-sm">
              <thead className="border-b bg-muted/50 text-muted-foreground">
                <tr>
                  <th className="px-4 py-2 font-medium">Date</th>
                  <th className="px-4 py-2 font-medium">Merchant</th>
                  <th className="px-4 py-2 font-medium">Category</th>
                  <th className="px-4 py-2 text-right font-medium">Total</th>
                  <th className="px-4 py-2 font-medium">Status</th>
                </tr>
              </thead>
              <tbody className={receiptsQuery.isPlaceholderData ? 'opacity-60' : undefined}>
                {data.items.map((receipt) => (
                  <tr key={receipt.id} className="border-b last:border-0">
                    <td className="px-4 py-2">
                      <Link to={`/receipts/${receipt.id}`} className="font-medium hover:underline">
                        {receipt.purchaseDate ? formatDate(receipt.purchaseDate) : 'Undated'}
                      </Link>
                    </td>
                    <td className="px-4 py-2">{receipt.merchantName ?? '—'}</td>
                    <td className="px-4 py-2">
                      {receipt.dominantCategoryId
                        ? (categoryNames.get(receipt.dominantCategoryId) ?? '—')
                        : '—'}
                    </td>
                    <td className="px-4 py-2 text-right tabular-nums">{formatAmount(receipt.totalAmount)}</td>
                    <td className="px-4 py-2">
                      <ReceiptStatusBadge status={receipt.status} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {data && data.totalCount > 0 && (
          <div className="mt-4 flex items-center justify-between text-sm text-muted-foreground">
            <span>
              {data.totalCount} receipt{data.totalCount === 1 ? '' : 's'} · page {data.page} of {totalPages}
            </span>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => goToPage(page - 1)}>
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => goToPage(page + 1)}
              >
                Next
              </Button>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
