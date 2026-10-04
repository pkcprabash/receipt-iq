import { type DBSchema, openDB } from 'idb'

export interface PendingUpload {
  id: string
  fileName: string
  fileType: string
  file: Blob
  queuedAt: string
}

interface OfflineQueueSchema extends DBSchema {
  'pending-uploads': {
    key: string
    value: PendingUpload
  }
}

const DB_NAME = 'receiptiq-offline'
const STORE_NAME = 'pending-uploads'

function getDb() {
  return openDB<OfflineQueueSchema>(DB_NAME, 1, {
    upgrade(db) {
      db.createObjectStore(STORE_NAME, { keyPath: 'id' })
    },
  })
}

export async function addPendingUpload(file: File): Promise<PendingUpload> {
  const pending: PendingUpload = {
    id: crypto.randomUUID(),
    fileName: file.name,
    fileType: file.type,
    file,
    queuedAt: new Date().toISOString(),
  }
  const db = await getDb()
  await db.put(STORE_NAME, pending)
  return pending
}

export async function listPendingUploads(): Promise<PendingUpload[]> {
  const db = await getDb()
  const all = await db.getAll(STORE_NAME)
  return all.sort((a, b) => a.queuedAt.localeCompare(b.queuedAt))
}

export async function removePendingUpload(id: string): Promise<void> {
  const db = await getDb()
  await db.delete(STORE_NAME, id)
}
