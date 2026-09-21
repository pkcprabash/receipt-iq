import { beforeEach, describe, expect, it } from 'vitest'
import { clearStoredAuth, getStoredAuth, getStoredToken, setStoredAuth } from './token-store'

describe('token-store', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('returns null when nothing is stored', () => {
    expect(getStoredAuth()).toBeNull()
    expect(getStoredToken()).toBeNull()
  })

  it('round-trips a stored auth token', () => {
    const future = new Date(Date.now() + 60_000).toISOString()
    setStoredAuth({ token: 'abc123', expiresAtUtc: future })

    expect(getStoredAuth()).toEqual({ token: 'abc123', expiresAtUtc: future })
    expect(getStoredToken()).toBe('abc123')
  })

  it('treats an expired token as absent and clears it', () => {
    const past = new Date(Date.now() - 60_000).toISOString()
    setStoredAuth({ token: 'expired', expiresAtUtc: past })

    expect(getStoredToken()).toBeNull()
    expect(getStoredAuth()).toBeNull()
  })

  it('clearStoredAuth removes the token', () => {
    setStoredAuth({ token: 'abc123', expiresAtUtc: new Date(Date.now() + 60_000).toISOString() })
    clearStoredAuth()

    expect(getStoredAuth()).toBeNull()
  })

  it('ignores malformed stored data', () => {
    localStorage.setItem('receiptiq.auth', 'not json')

    expect(getStoredAuth()).toBeNull()
  })
})
