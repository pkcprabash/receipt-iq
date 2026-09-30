import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { MerchantBreakdownChart } from '@/components/analytics/MerchantBreakdownChart'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { getSpendByMerchant } from '@/lib/api/analytics'
import { presetRange, SPEND_RANGE_PRESETS, type SpendRangePreset } from '@/lib/receipts/date-range'

const SELECT_CLASS =
  'h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30'

function formatAmount(value: number): string {
  return `$${value.toFixed(2)}`
}

export function MerchantsPage() {
  const [rangePreset, setRangePreset] = useState<SpendRangePreset>('last-6-months')
  const range = presetRange(rangePreset)

  const merchantsQuery = useQuery({
    queryKey: ['analytics', 'spend-by-merchant', range],
    queryFn: () => getSpendByMerchant(range),
  })

  const merchants = merchantsQuery.data?.merchants ?? []

  return (
    <div>
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Top merchants</h1>
          <p className="mt-2 text-muted-foreground">Where your confirmed, dated receipts add up.</p>
        </div>
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

      <Card className="mt-6">
        <CardHeader>
          <CardTitle>By merchant</CardTitle>
        </CardHeader>
        <CardContent>
          {merchantsQuery.isPending && <p className="text-sm text-muted-foreground">Loading…</p>}
          {merchantsQuery.isError && <p className="text-sm text-destructive">Couldn’t load top merchants.</p>}
          {merchantsQuery.data && <MerchantBreakdownChart merchants={merchants} />}
        </CardContent>
      </Card>

      {merchants.length > 0 && (
        <div className="mt-4 overflow-x-auto rounded-lg border">
          <table className="w-full text-left text-sm">
            <thead className="border-b bg-muted/50 text-muted-foreground">
              <tr>
                <th className="px-4 py-2 font-medium">Merchant</th>
                <th className="px-4 py-2 text-right font-medium">Receipts</th>
                <th className="px-4 py-2 text-right font-medium">Total</th>
                <th className="px-4 py-2 text-right font-medium">Avg / receipt</th>
              </tr>
            </thead>
            <tbody>
              {merchants.map((merchant) => (
                <tr key={merchant.merchantId ?? 'unknown'} className="border-b last:border-0">
                  <td className="px-4 py-2">{merchant.merchantName}</td>
                  <td className="px-4 py-2 text-right tabular-nums">{merchant.receiptCount}</td>
                  <td className="px-4 py-2 text-right tabular-nums">{formatAmount(merchant.totalAmount)}</td>
                  <td className="px-4 py-2 text-right tabular-nums">
                    {formatAmount(merchant.totalAmount / merchant.receiptCount)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
