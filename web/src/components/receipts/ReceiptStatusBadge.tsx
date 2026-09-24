const STATUS_STYLES: Record<string, string> = {
  Uploaded: 'bg-muted text-muted-foreground',
  Processing: 'bg-blue-100 text-blue-800',
  NeedsReview: 'bg-amber-100 text-amber-800',
  Confirmed: 'bg-green-100 text-green-800',
  Failed: 'bg-red-100 text-red-800',
}

export const TERMINAL_RECEIPT_STATUSES = new Set(['Confirmed', 'NeedsReview', 'Failed'])

export function ReceiptStatusBadge({ status }: { status: string }) {
  const className = STATUS_STYLES[status] ?? STATUS_STYLES.Uploaded

  return (
    <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${className}`}>
      {status}
    </span>
  )
}
