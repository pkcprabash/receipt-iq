import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../api/client'
import { shouldQueueOffline } from './should-queue-offline'

describe('shouldQueueOffline', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('queues on a fetch TypeError (the browser never reached the server)', () => {
    vi.stubGlobal('navigator', { onLine: true })
    expect(shouldQueueOffline(new TypeError('Failed to fetch'))).toBe(true)
  })

  it('queues any error at all while the browser reports itself offline', () => {
    vi.stubGlobal('navigator', { onLine: false })
    expect(shouldQueueOffline(new Error('something else'))).toBe(true)
  })

  it('does not queue an ApiError — the server responded, retrying would just fail again', () => {
    vi.stubGlobal('navigator', { onLine: true })
    expect(shouldQueueOffline(new ApiError(400, 'Bad request'))).toBe(false)
  })

  it('does not queue an ApiError even when offline — it already got a real response', () => {
    vi.stubGlobal('navigator', { onLine: false })
    expect(shouldQueueOffline(new ApiError(401, 'Unauthorized'))).toBe(false)
  })

  it('does not queue an unrelated error while online', () => {
    vi.stubGlobal('navigator', { onLine: true })
    expect(shouldQueueOffline(new Error('unexpected'))).toBe(false)
  })
})
