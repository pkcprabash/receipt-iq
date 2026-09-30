import type { Delta } from '@/lib/receipts/delta'

interface DeltaIndicatorProps extends Delta {
  // Spending less than the prior period reads as an improvement, more as a flag —
  // never the reverse. Matches the app's existing amber/green status convention
  // (see ReceiptStatusBadge) rather than introducing a separate status palette.
  goodDirection?: 'down' | 'up'
}

export function DeltaIndicator({ deltaAmount, deltaPercent, goodDirection = 'down' }: DeltaIndicatorProps) {
  if (deltaPercent === null) {
    return <span className="text-muted-foreground">New</span>
  }

  if (Math.round(deltaAmount * 100) === 0) {
    return <span className="text-muted-foreground">No change</span>
  }

  const isIncrease = deltaAmount > 0
  const isGood = isIncrease ? goodDirection === 'up' : goodDirection === 'down'

  return (
    <span className={isGood ? 'text-green-700' : 'text-amber-700'}>
      {isIncrease ? '▲' : '▼'} {Math.abs(deltaPercent).toFixed(0)}% (${Math.abs(deltaAmount).toFixed(2)})
    </span>
  )
}
