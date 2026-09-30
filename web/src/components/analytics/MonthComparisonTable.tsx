import { DeltaIndicator } from './DeltaIndicator'
import type { CategoryComparisonRow } from '@/lib/receipts/category-comparison'

export function MonthComparisonTable({ rows }: { rows: CategoryComparisonRow[] }) {
  if (rows.length === 0) {
    return <p className="text-sm text-muted-foreground">No confirmed, dated receipts in either month.</p>
  }

  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-left text-sm">
        <thead className="border-b bg-muted/50 text-muted-foreground">
          <tr>
            <th className="px-4 py-2 font-medium">Category</th>
            <th className="px-4 py-2 text-right font-medium">Last month</th>
            <th className="px-4 py-2 text-right font-medium">This month</th>
            <th className="px-4 py-2 text-right font-medium">Change</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => (
            <tr key={row.categoryId} className="border-b last:border-0">
              <td className="px-4 py-2">{row.categoryName}</td>
              <td className="px-4 py-2 text-right tabular-nums">${row.previousAmount.toFixed(2)}</td>
              <td className="px-4 py-2 text-right tabular-nums">${row.currentAmount.toFixed(2)}</td>
              <td className="px-4 py-2 text-right tabular-nums">
                <DeltaIndicator deltaAmount={row.deltaAmount} deltaPercent={row.deltaPercent} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
