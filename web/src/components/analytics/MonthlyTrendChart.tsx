import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import type { MonthSpend } from '@/lib/api/types'
import { formatMonthLabel } from '@/lib/receipts/date-range'

const AXIS_TICK_STYLE = { fill: 'var(--muted-foreground)', fontSize: 12 }

function formatAmount(value: number): string {
  return `$${value.toFixed(0)}`
}

function ChartTooltip({ active, payload }: { active?: boolean; payload?: { payload: MonthSpend }[] }) {
  if (!active || !payload?.length) {
    return null
  }

  const month = payload[0].payload
  return (
    <div className="rounded-md border bg-card px-3 py-2 text-sm shadow-sm">
      <p className="font-semibold tabular-nums">${month.totalAmount.toFixed(2)}</p>
      <p className="text-muted-foreground">
        {formatMonthLabel(month.month)} · {month.receiptCount} receipt{month.receiptCount === 1 ? '' : 's'}
      </p>
    </div>
  )
}

export function MonthlyTrendChart({ months }: { months: MonthSpend[] }) {
  if (months.length === 0) {
    return <p className="text-sm text-muted-foreground">No confirmed, dated receipts in this range.</p>
  }

  return (
    <ResponsiveContainer width="100%" height={280}>
      <AreaChart data={months} margin={{ top: 8, right: 12, bottom: 4, left: 4 }}>
        <CartesianGrid vertical={false} stroke="var(--border)" />
        <XAxis
          dataKey="month"
          tickFormatter={formatMonthLabel}
          tickLine={false}
          axisLine={{ stroke: 'var(--border)' }}
          tick={AXIS_TICK_STYLE}
          minTickGap={24}
        />
        <YAxis tickLine={false} axisLine={false} tick={AXIS_TICK_STYLE} tickFormatter={formatAmount} width={56} />
        <Tooltip content={<ChartTooltip />} cursor={{ stroke: 'var(--border)' }} />
        <Area
          type="monotone"
          dataKey="totalAmount"
          stroke="var(--viz-series-1)"
          strokeWidth={2}
          fill="var(--viz-series-1)"
          fillOpacity={0.1}
          dot={false}
          activeDot={{ r: 5, stroke: 'var(--card)', strokeWidth: 2, fill: 'var(--viz-series-1)' }}
        />
      </AreaChart>
    </ResponsiveContainer>
  )
}
