import { useQueryClient } from '@tanstack/react-query'
import { useCallback, useEffect, useRef, useState } from 'react'
import { uploadReceipt } from '../api/receipts'
import { addPendingUpload, listPendingUploads, removePendingUpload, type PendingUpload } from './upload-queue-db'

export function useOfflineUploadQueue() {
  const queryClient = useQueryClient()
  const [pending, setPending] = useState<PendingUpload[]>([])
  const [isFlushing, setIsFlushing] = useState(false)
  // A ref, not the isFlushing state, guards re-entrancy — state updates are async and
  // the 'online' event + the mount-time call could otherwise both pass the check at once.
  const isFlushingRef = useRef(false)

  const refresh = useCallback(async () => {
    setPending(await listPendingUploads())
  }, [])

  const flush = useCallback(async () => {
    if (isFlushingRef.current || !navigator.onLine) {
      return
    }
    isFlushingRef.current = true
    setIsFlushing(true)
    try {
      for (const item of await listPendingUploads()) {
        try {
          await uploadReceipt(new File([item.file], item.fileName, { type: item.fileType }))
          await removePendingUpload(item.id)
        } catch {
          // Stop rather than hammer the rest of the queue through a connection that's
          // still flaky — the next 'online' event or app load retries from here.
          break
        }
      }
    } finally {
      await refresh()
      isFlushingRef.current = false
      setIsFlushing(false)
      void queryClient.invalidateQueries({ queryKey: ['receipts'] })
    }
  }, [queryClient, refresh])

  useEffect(() => {
    void refresh()
    void flush()
    window.addEventListener('online', flush)
    return () => window.removeEventListener('online', flush)
  }, [flush, refresh])

  const queueUpload = useCallback(
    async (file: File) => {
      await addPendingUpload(file)
      await refresh()
    },
    [refresh],
  )

  return { pending, isFlushing, queueUpload, flush }
}
