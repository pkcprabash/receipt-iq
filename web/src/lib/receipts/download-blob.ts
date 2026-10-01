// Browser-only plumbing for saving a fetched Blob as a file — no business logic to
// unit test here (same reasoning as the object-URL handling in ReceiptImage).
export function downloadBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}
