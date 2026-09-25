import { useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import { getReceiptImage } from '@/lib/api/receipts'

export function ReceiptImage({ receiptId, contentType }: { receiptId: string; contentType: string }) {
  const imageQuery = useQuery({
    queryKey: ['receipt-image', receiptId],
    queryFn: () => getReceiptImage(receiptId),
    staleTime: Infinity,
  })
  const [objectUrl, setObjectUrl] = useState<string | null>(null)

  // Object URLs must be revoked when replaced or on unmount, or the blob stays in memory.
  useEffect(() => {
    if (!imageQuery.data) {
      return
    }
    const url = URL.createObjectURL(imageQuery.data)
    // oxlint-disable-next-line react/set-state-in-effect -- the object URL is an external resource tied to this blob
    setObjectUrl(url)
    return () => {
      URL.revokeObjectURL(url)
      setObjectUrl(null)
    }
  }, [imageQuery.data])

  if (imageQuery.isError) {
    return <p className="text-sm text-destructive">Couldn’t load the original image.</p>
  }

  if (!objectUrl) {
    return <p className="text-sm text-muted-foreground">Loading image…</p>
  }

  if (contentType === 'application/pdf') {
    return (
      <div className="space-y-2">
        <iframe src={objectUrl} title="Original receipt" className="h-[36rem] w-full rounded-md border" />
        <a href={objectUrl} target="_blank" rel="noreferrer" className="text-sm underline">
          Open PDF in a new tab
        </a>
      </div>
    )
  }

  return (
    <a href={objectUrl} target="_blank" rel="noreferrer" title="Open full size">
      <img src={objectUrl} alt="Original receipt" className="max-h-[36rem] w-full rounded-md border object-contain" />
    </a>
  )
}
