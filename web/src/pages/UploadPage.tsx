import { useMutation, useQuery } from '@tanstack/react-query'
import { type ChangeEvent, useEffect, useState } from 'react'
import { ReceiptStatusBadge, TERMINAL_RECEIPT_STATUSES } from '@/components/receipts/ReceiptStatusBadge'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/api/client'
import { getReceipt, uploadReceipt } from '@/lib/api/receipts'
import { compressImage } from '@/lib/upload/compress-image'

function formatBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`
  }
  if (bytes < 1024 * 1024) {
    return `${Math.round(bytes / 1024)} KB`
  }
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function UploadPage() {
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [originalSize, setOriginalSize] = useState<number | null>(null)
  const [isCompressing, setIsCompressing] = useState(false)

  const uploadMutation = useMutation({ mutationFn: uploadReceipt })
  const receiptId = uploadMutation.data?.id ?? null

  const receiptQuery = useQuery({
    queryKey: ['receipt', receiptId],
    queryFn: () => getReceipt(receiptId!),
    enabled: receiptId !== null,
    refetchInterval: (query) => {
      const currentStatus = query.state.data?.status
      return currentStatus && TERMINAL_RECEIPT_STATUSES.has(currentStatus) ? false : 1500
    },
  })

  // Object URLs are only valid for the lifetime of the page — revoke the old
  // one whenever it's replaced or the component unmounts.
  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl)
      }
    }
  }, [previewUrl])

  async function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null
    uploadMutation.reset()
    setPreviewUrl(null)
    setSelectedFile(null)
    setOriginalSize(null)

    if (!file) {
      return
    }

    setIsCompressing(true)
    try {
      setOriginalSize(file.size)
      const processed = await compressImage(file)
      setSelectedFile(processed)
      setPreviewUrl(URL.createObjectURL(processed))
    } finally {
      setIsCompressing(false)
    }
  }

  function handleUpload() {
    if (selectedFile) {
      uploadMutation.mutate(selectedFile)
    }
  }

  const status = receiptQuery.data?.status ?? uploadMutation.data?.status
  const isTerminal = status !== undefined && TERMINAL_RECEIPT_STATUSES.has(status)

  return (
    <div className="max-w-md">
      <h1 className="text-2xl font-semibold">Upload a receipt</h1>
      <p className="mt-2 text-muted-foreground">
        Take a photo or choose an image or PDF of your receipt.
      </p>

      <div className="mt-6 space-y-4">
        <input
          type="file"
          accept="image/jpeg,image/png,application/pdf"
          capture="environment"
          onChange={handleFileChange}
          className="block w-full text-sm text-muted-foreground file:mr-4 file:rounded-md file:border-0 file:bg-secondary file:px-4 file:py-2 file:text-sm file:font-medium file:text-secondary-foreground"
        />

        {isCompressing && <p className="text-sm text-muted-foreground">Compressing…</p>}

        {previewUrl && selectedFile?.type.startsWith('image/') && (
          <img src={previewUrl} alt="Receipt preview" className="max-h-80 rounded-md border" />
        )}

        {selectedFile && originalSize !== null && originalSize !== selectedFile.size && (
          <p className="text-xs text-muted-foreground">
            Compressed from {formatBytes(originalSize)} to {formatBytes(selectedFile.size)}
          </p>
        )}

        <Button onClick={handleUpload} disabled={!selectedFile || isCompressing || uploadMutation.isPending}>
          {uploadMutation.isPending ? 'Uploading…' : 'Upload'}
        </Button>

        {uploadMutation.isError && (
          <p className="text-sm text-destructive">
            {uploadMutation.error instanceof ApiError ? uploadMutation.error.message : 'Upload failed.'}
          </p>
        )}

        {status && (
          <div className="flex items-center gap-2 text-sm">
            <span>Status:</span>
            <ReceiptStatusBadge status={status} />
            {!isTerminal && <span className="text-muted-foreground">Processing…</span>}
          </div>
        )}

        {receiptQuery.data?.status === 'Confirmed' && (
          <p className="text-sm text-green-700">
            {receiptQuery.data.totalAmount != null
              ? `Total: $${receiptQuery.data.totalAmount.toFixed(2)}`
              : 'Done — no total extracted.'}
          </p>
        )}

        {receiptQuery.data?.status === 'NeedsReview' && (
          <p className="text-sm text-amber-700">
            Extraction confidence was low — this receipt needs a manual look.
          </p>
        )}

        {receiptQuery.data?.status === 'Failed' && (
          <p className="text-sm text-destructive">Processing failed for this receipt.</p>
        )}
      </div>
    </div>
  )
}
