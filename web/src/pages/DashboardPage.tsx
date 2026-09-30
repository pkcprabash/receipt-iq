import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { CategoryBreakdownChart } from '@/components/analytics/CategoryBreakdownChart'
import { DeltaIndicator } from '@/components/analytics/DeltaIndicator'
import { MonthComparisonTable } from '@/components/analytics/MonthComparisonTable'
import { MonthlyTrendChart } from '@/components/analytics/MonthlyTrendChart'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { getSpendByCategory, getSpendByMonth } from '@/lib/api/analytics'
import { compareCategorySpend } from '@/lib/receipts/category-comparison'
import { computeDelta } from '@/lib/receipts/delta'
import {
  currentMonthRange,
  presetRange,
  previousMonthRange,
  SPEND_RANGE_PRESETS,
  type SpendRangePreset,
} from '@/lib/receipts/date-range'
import { useReviewQueue } from '@/lib/receipts/review-queue'

const SELECT_CLASS =
  'h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30'

interface SummaryCardProps {
  title: string
  isLoading: boolean
  isError: boolean
  children: React.ReactNode
}

function SummaryCard({ title, isLoading, isError, children }: SummaryCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-muted-foreground">{title}</CardTitle>
      </CardHeader>
      <CardContent>
        {isLoading && <p className="text-2xl font-semibold text-muted-foreground">—</p>}
        {!isLoading && isError && <p className="text-sm text-destructive">Couldn’t load.</p>}
        {!isLoading && !isError && children}
      </CardContent>
    </Card>
  )
}

export function DashboardPage() {
  const thisMonthRange = currentMonthRange()
  const lastMonthRange = previousMonthRange()
  const [rangePreset, setRangePreset] = useState<SpendRangePreset>('last-6-months')
  const chartRange = presetRange(rangePreset)

  const monthQuery = useQuery({
    queryKey: ['analytics', 'spend-by-month', thisMonthRange],
    queryFn: () => getSpendByMonth(thisMonthRange),
  })
  const categoryQuery = useQuery({
    queryKey: ['analytics', 'spend-by-category', thisMonthRange],
    queryFn: () => getSpendByCategory(thisMonthRange),
  })
  const previousMonthQuery = useQuery({
    queryKey: ['analytics', 'spend-by-month', lastMonthRange],
    queryFn: () => getSpendByMonth(lastMonthRange),
  })
  const previousCategoryQuery = useQuery({
    queryKey: ['analytics', 'spend-by-category', lastMonthRange],
    queryFn: () => getSpendByCategory(lastMonthRange),
  })
  // pageSize 1: only the total is needed for the card.
  const reviewQuery = useReviewQueue(1, 1)

  const trendQuery = useQuery({
    queryKey: ['analytics', 'spend-by-month', chartRange],
    queryFn: () => getSpendByMonth(chartRange),
  })
  const breakdownQuery = useQuery({
    queryKey: ['analytics', 'spend-by-category', chartRange],
    queryFn: () => getSpendByCategory(chartRange),
  })

  const thisMonth = monthQuery.data?.months[0]
  const topCategory = categoryQuery.data?.categories[0]
  const previousMonth = previousMonthQuery.data?.months[0]

  const comparisonIsLoading = monthQuery.isPending || previousMonthQuery.isPending || categoryQuery.isPending || previousCategoryQuery.isPending
  const comparisonIsError = monthQuery.isError || previousMonthQuery.isError || categoryQuery.isError || previousCategoryQuery.isError
  const spendDelta =
    thisMonth && previousMonth ? computeDelta(thisMonth.totalAmount, previousMonth.totalAmount) : null
  const receiptDelta =
    thisMonth && previousMonth ? computeDelta(thisMonth.receiptCount, previousMonth.receiptCount) : null
  const comparisonRows =
    categoryQuery.data && previousCategoryQuery.data
      ? compareCategorySpend(categoryQuery.data.categories, previousCategoryQuery.data.categories)
      : []

  return (
    <div>
      <h1 className="text-2xl font-semibold">Dashboard</h1>
      <p className="mt-2 text-muted-foreground">Your spending at a glance.</p>

      <div className="mt-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <SummaryCard title="Spent this month" isLoading={monthQuery.isPending} isError={monthQuery.isError}>
          <p className="text-2xl font-semibold tabular-nums">${(thisMonth?.totalAmount ?? 0).toFixed(2)}</p>
        </SummaryCard>

        <SummaryCard title="Receipts this month" isLoading={monthQuery.isPending} isError={monthQuery.isError}>
          <p className="text-2xl font-semibold tabular-nums">{thisMonth?.receiptCount ?? 0}</p>
        </SummaryCard>

        <SummaryCard title="Needs review" isLoading={reviewQuery.isPending} isError={reviewQuery.isError}>
          <p className="text-2xl font-semibold tabular-nums">{reviewQuery.data?.totalCount ?? 0}</p>
        </SummaryCard>

        <SummaryCard title="Top category this month" isLoading={categoryQuery.isPending} isError={categoryQuery.isError}>
          {topCategory ? (
            <>
              <p className="text-2xl font-semibold">{topCategory.categoryName}</p>
              <p className="text-sm text-muted-foreground">${topCategory.totalAmount.toFixed(2)}</p>
            </>
          ) : (
            <p className="text-2xl font-semibold text-muted-foreground">—</p>
          )}
        </SummaryCard>
      </div>

      <p className="mt-4 text-sm text-muted-foreground">
        Only confirmed, dated receipts count toward these totals — check the Review page if a number looks low.
      </p>

      <h2 className="mt-8 text-lg font-medium">This month vs last month</h2>

      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-muted-foreground">Total spend</CardTitle>
          </CardHeader>
          <CardContent>
            {comparisonIsLoading && <p className="text-2xl font-semibold text-muted-foreground">—</p>}
            {!comparisonIsLoading && comparisonIsError && <p className="text-sm text-destructive">Couldn’t load.</p>}
            {!comparisonIsLoading && !comparisonIsError && (
              <>
                <p className="text-2xl font-semibold tabular-nums">${(thisMonth?.totalAmount ?? 0).toFixed(2)}</p>
                <p className="mt-1 text-sm">
                  {spendDelta ? <DeltaIndicator {...spendDelta} /> : <span className="text-muted-foreground">—</span>}
                  <span className="ml-1 text-muted-foreground">vs last month</span>
                </p>
              </>
            )}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-muted-foreground">Receipts</CardTitle>
          </CardHeader>
          <CardContent>
            {comparisonIsLoading && <p className="text-2xl font-semibold text-muted-foreground">—</p>}
            {!comparisonIsLoading && comparisonIsError && <p className="text-sm text-destructive">Couldn’t load.</p>}
            {!comparisonIsLoading && !comparisonIsError && (
              <>
                <p className="text-2xl font-semibold tabular-nums">{thisMonth?.receiptCount ?? 0}</p>
                <p className="mt-1 text-sm">
                  {receiptDelta ? <DeltaIndicator {...receiptDelta} /> : <span className="text-muted-foreground">—</span>}
                  <span className="ml-1 text-muted-foreground">vs last month</span>
                </p>
              </>
            )}
          </CardContent>
        </Card>
      </div>

      <div className="mt-4">
        <Card>
          <CardHeader>
            <CardTitle>By category</CardTitle>
          </CardHeader>
          <CardContent>
            {comparisonIsLoading && <p className="text-sm text-muted-foreground">Loading…</p>}
            {!comparisonIsLoading && comparisonIsError && (
              <p className="text-sm text-destructive">Couldn’t load the comparison.</p>
            )}
            {!comparisonIsLoading && !comparisonIsError && <MonthComparisonTable rows={comparisonRows} />}
          </CardContent>
        </Card>
      </div>

      <div className="mt-8 flex items-center justify-between">
        <h2 className="text-lg font-medium">Trends</h2>
        <select
          aria-label="Date range"
          className={SELECT_CLASS}
          value={rangePreset}
          onChange={(e) => setRangePreset(e.target.value as SpendRangePreset)}
        >
          {SPEND_RANGE_PRESETS.map((preset) => (
            <option key={preset.value} value={preset.value}>
              {preset.label}
            </option>
          ))}
        </select>
      </div>

      <div className="mt-4 grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Monthly spending</CardTitle>
          </CardHeader>
          <CardContent>
            {trendQuery.isPending && <p className="text-sm text-muted-foreground">Loading…</p>}
            {trendQuery.isError && <p className="text-sm text-destructive">Couldn’t load monthly spending.</p>}
            {trendQuery.data && <MonthlyTrendChart months={trendQuery.data.months} />}
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>Spend by category</CardTitle>
          </CardHeader>
          <CardContent>
            {breakdownQuery.isPending && <p className="text-sm text-muted-foreground">Loading…</p>}
            {breakdownQuery.isError && <p className="text-sm text-destructive">Couldn’t load the category breakdown.</p>}
            {breakdownQuery.data && <CategoryBreakdownChart categories={breakdownQuery.data.categories} />}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
