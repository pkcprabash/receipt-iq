import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getStoredAuth, setStoredAuth } from '../auth/token-store'
import { ApiError, apiFetch } from './client'

describe('apiFetch', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('does not attach an Authorization header when no token is stored', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ok: true }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await apiFetch('/categories/')

    const [, init] = fetchMock.mock.calls[0]
    expect((init.headers as Headers).has('Authorization')).toBe(false)
  })

  it('attaches a Bearer Authorization header when a token is stored', async () => {
    setStoredAuth({ token: 'my-token', expiresAtUtc: new Date(Date.now() + 60_000).toISOString() })
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ok: true }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await apiFetch('/receipts/')

    const [, init] = fetchMock.mock.calls[0]
    expect((init.headers as Headers).get('Authorization')).toBe('Bearer my-token')
  })

  it('clears the stored token when the API returns 401', async () => {
    setStoredAuth({ token: 'stale-token', expiresAtUtc: new Date(Date.now() + 60_000).toISOString() })
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 401 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(apiFetch('/receipts/')).rejects.toBeInstanceOf(ApiError)
    expect(getStoredAuth()).toBeNull()
  })

  it('throws ApiError with the server message on failure', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(new Response(JSON.stringify({ message: 'Unknown category.' }), { status: 400 }))
    vi.stubGlobal('fetch', fetchMock)

    await expect(apiFetch('/receipts/x')).rejects.toMatchObject({ status: 400, message: 'Unknown category.' })
  })

  it('sends a JSON body with Content-Type for plain objects', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({}), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await apiFetch('/auth/login', { method: 'POST', body: { email: 'a@b.com', password: 'x' } })

    const [, init] = fetchMock.mock.calls[0]
    expect((init.headers as Headers).get('Content-Type')).toBe('application/json')
    expect(init.body).toBe(JSON.stringify({ email: 'a@b.com', password: 'x' }))
  })

  it('sends FormData bodies without setting Content-Type manually', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({}), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    const formData = new FormData()
    await apiFetch('/receipts/', { method: 'POST', body: formData })

    const [, init] = fetchMock.mock.calls[0]
    expect((init.headers as Headers).has('Content-Type')).toBe(false)
    expect(init.body).toBe(formData)
  })

  it('builds query strings, skipping undefined values', async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({}), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)

    await apiFetch('/receipts/', { query: { page: 2, pageSize: 20, unused: undefined } })

    const [url] = fetchMock.mock.calls[0]
    const parsed = new URL(url as string)
    expect(parsed.searchParams.get('page')).toBe('2')
    expect(parsed.searchParams.get('pageSize')).toBe('20')
    expect(parsed.searchParams.has('unused')).toBe(false)
  })
})
