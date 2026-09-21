import { clearStoredAuth, setStoredAuth } from '../auth/token-store'
import { apiFetch } from './client'
import type { AuthResponse, LoginRequest, RegisterRequest } from './types'

export async function register(request: RegisterRequest): Promise<AuthResponse> {
  const result = await apiFetch<AuthResponse>('/auth/register', { method: 'POST', body: request })
  setStoredAuth(result)
  return result
}

export async function login(request: LoginRequest): Promise<AuthResponse> {
  const result = await apiFetch<AuthResponse>('/auth/login', { method: 'POST', body: request })
  setStoredAuth(result)
  return result
}

export function logout(): void {
  clearStoredAuth()
}
