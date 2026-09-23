const STORAGE_KEY = 'receiptiq.auth'
const AUTH_CHANGED_EVENT = 'receiptiq:auth-changed'

export interface StoredAuth {
  token: string
  expiresAtUtc: string
}

// Lets React state (e.g. AuthProvider) stay in sync with changes made outside
// its own calls too, such as apiFetch clearing a token after a 401.
export function subscribeToAuthChanges(callback: () => void): () => void {
  window.addEventListener(AUTH_CHANGED_EVENT, callback)
  return () => window.removeEventListener(AUTH_CHANGED_EVENT, callback)
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
  window.dispatchEvent(new Event(AUTH_CHANGED_EVENT))
}

export function clearStoredAuth(): void {
  localStorage.removeItem(STORAGE_KEY)
  window.dispatchEvent(new Event(AUTH_CHANGED_EVENT))
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
