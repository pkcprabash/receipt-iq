// @vitest-environment node
//
// This module only touches IndexedDB, no DOM — running it under Node instead of jsdom
// sidesteps a real fake-indexeddb/jsdom incompatibility: jsdom's Blob/File globals
// aren't the ones fake-indexeddb's structured-clone recognizes, so a round-tripped
// Blob comes back as an empty object under jsdom. Node's native Blob/File don't have
// that problem, and that's what real browsers behave like anyway.
import 'fake-indexeddb/auto'
import { afterEach, describe, expect, it } from 'vitest'
import { addPendingUpload, listPendingUploads, removePendingUpload } from './upload-queue-db'

function makeFile(name: string, content: string): File {
  return new File([content], name, { type: 'image/png' })
}

afterEach(async () => {
  for (const item of await listPendingUploads()) {
    await removePendingUpload(item.id)
  }
})

describe('upload-queue-db', () => {
  it('stores a queued upload and lists it back with its content intact', async () => {
    const file = makeFile('receipt.png', 'fake-bytes')

    const added = await addPendingUpload(file)
    const all = await listPendingUploads()

    expect(all).toHaveLength(1)
    expect(all[0].id).toBe(added.id)
    expect(all[0].fileName).toBe('receipt.png')
    expect(all[0].fileType).toBe('image/png')
    await expect(all[0].file.text()).resolves.toBe('fake-bytes')
  })

  it('lists queued uploads oldest first', async () => {
    const first = await addPendingUpload(makeFile('a.png', 'a'))
    // queuedAt has millisecond resolution — without a gap, a same-millisecond second
    // add would tie and fall back to IndexedDB's (unrelated) key order.
    await new Promise((resolve) => setTimeout(resolve, 5))
    const second = await addPendingUpload(makeFile('b.png', 'b'))

    const all = await listPendingUploads()

    expect(all.map((item) => item.id)).toEqual([first.id, second.id])
  })

  it('removes a queued upload by id', async () => {
    const added = await addPendingUpload(makeFile('receipt.png', 'x'))

    await removePendingUpload(added.id)

    expect(await listPendingUploads()).toHaveLength(0)
  })
})
