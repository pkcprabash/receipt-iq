import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { getStoredToken, subscribeToAuthChanges } from './token-store'

interface AuthContextValue {
  isAuthenticated: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState(() => getStoredToken())

  useEffect(() => subscribeToAuthChanges(() => setToken(getStoredToken())), [])

  const value = useMemo<AuthContextValue>(() => ({ isAuthenticated: token !== null }), [token])

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
