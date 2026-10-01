import { apiFetch } from './client'
import type { Budget, BudgetRequest } from './types'

export function listBudgets(): Promise<Budget[]> {
  return apiFetch<Budget[]>('/budgets/')
}

export function createBudget(request: BudgetRequest): Promise<{ id: string }> {
  return apiFetch<{ id: string }>('/budgets/', { method: 'POST', body: request })
}

export function updateBudget(id: string, request: BudgetRequest): Promise<void> {
  return apiFetch<void>(`/budgets/${id}`, { method: 'PUT', body: request })
}

export function deleteBudget(id: string): Promise<void> {
  return apiFetch<void>(`/budgets/${id}`, { method: 'DELETE' })
}
