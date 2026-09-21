const STORAGE_KEY = 'receiptiq.auth'

export interface StoredAuth {
  token: string
  expiresAtUtc: string
}

export function getStoredAuth(): StoredAuth | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as StoredAuth
  } catch {
    return null
  }
}

export function setStoredAuth(auth: StoredAuth): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(auth))
}

export function clearStoredAuth(): void {
  localStorage.removeItem(STORAGE_KEY)
}

// Treats an expired token as absent so callers never send one the API will reject.
export function getStoredToken(): string | null {
  const auth = getStoredAuth()
  if (!auth) {
    return null
  }

  if (new Date(auth.expiresAtUtc).getTime() <= Date.now()) {
    clearStoredAuth()
    return null
  }

  return auth.token
}
