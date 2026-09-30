import { Bar, BarChart, CartesianGrid, LabelList, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { MerchantSpend } from '@/lib/api/types'
import { foldTopMerchants } from '@/lib/receipts/fold-merchants'

const AXIS_TICK_STYLE = { fill: 'var(--muted-foreground)', fontSize: 12 }
const BAR_HEIGHT = 40
const MIN_CHART_HEIGHT = 160

function formatAmount(value: number): string {
  return `$${value.toFixed(2)}`
}

function formatAmountLabel(value: unknown): string {
  return typeof value === 'number' ? formatAmount(value) : ''
}

function ChartTooltip({ active, payload }: { active?: boolean; payload?: { payload: MerchantSpend }[] }) {
  if (!active || !payload?.length) {
    return null
  }

  const merchant = payload[0].payload
  return (
    <div className="rounded-md border bg-card px-3 py-2 text-sm shadow-sm">
      <p className="font-semibold tabular-nums">{formatAmount(merchant.totalAmount)}</p>
      <p className="text-muted-foreground">
        {merchant.merchantName} · {merchant.receiptCount} receipt{merchant.receiptCount === 1 ? '' : 's'}
      </p>
    </div>
  )
}

export function MerchantBreakdownChart({ merchants }: { merchants: MerchantSpend[] }) {
  if (merchants.length === 0) {
    return <p className="text-sm text-muted-foreground">No confirmed, dated receipts in this range.</p>
  }

  const data = foldTopMerchants(merchants)
  const height = Math.max(MIN_CHART_HEIGHT, data.length * BAR_HEIGHT)

  return (
    <ResponsiveContainer width="100%" height={height}>
      <BarChart data={data} layout="vertical" margin={{ top: 4, right: 48, bottom: 4, left: 4 }}>
        <CartesianGrid horizontal={false} stroke="var(--border)" />
        <XAxis type="number" tickLine={false} axisLine={false} tick={AXIS_TICK_STYLE} tickFormatter={formatAmount} />
        <YAxis
          type="category"
          dataKey="merchantName"
          width={140}
          tickLine={false}
          axisLine={{ stroke: 'var(--border)' }}
          tick={AXIS_TICK_STYLE}
        />
        <Tooltip content={<ChartTooltip />} cursor={{ fill: 'var(--muted)' }} />
        <Bar dataKey="totalAmount" fill="var(--viz-series-1)" radius={[0, 4, 4, 0]} maxBarSize={24}>
          <LabelList dataKey="totalAmount" position="right" formatter={formatAmountLabel} style={{ fill: 'var(--foreground)', fontSize: 12 }} />
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  )
}
