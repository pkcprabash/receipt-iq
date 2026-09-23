import { useMutation } from '@tanstack/react-query'
import { type ChangeEvent, useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/api/client'
import { uploadReceipt } from '@/lib/api/receipts'

export function UploadPage() {
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)

  const mutation = useMutation({ mutationFn: uploadReceipt })

  // Object URLs are only valid for the lifetime of the page — revoke the old
  // one whenever it's replaced or the component unmounts.
  useEffect(() => {
    return () => {
      if (previewUrl) {
        URL.revokeObjectURL(previewUrl)
      }
    }
  }, [previewUrl])

  function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0] ?? null
    setSelectedFile(file)
    mutation.reset()
    setPreviewUrl(file ? URL.createObjectURL(file) : null)
  }

  function handleUpload() {
    if (selectedFile) {
      mutation.mutate(selectedFile)
    }
  }

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

        {previewUrl && selectedFile?.type.startsWith('image/') && (
          <img src={previewUrl} alt="Receipt preview" className="max-h-80 rounded-md border" />
        )}

        <Button onClick={handleUpload} disabled={!selectedFile || mutation.isPending}>
          {mutation.isPending ? 'Uploading…' : 'Upload'}
        </Button>

        {mutation.isError && (
          <p className="text-sm text-destructive">
            {mutation.error instanceof ApiError ? mutation.error.message : 'Upload failed.'}
          </p>
        )}

        {mutation.isSuccess && (
          <p className="text-sm text-green-600">
            Uploaded — status: {mutation.data.status}. It'll finish processing in the background.
          </p>
        )}
      </div>
    </div>
  )
}
