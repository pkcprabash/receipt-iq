import { apiFetch } from './client'
import type { Category, CategoryRequest } from './types'

export function listCategories(): Promise<Category[]> {
  return apiFetch<Category[]>('/categories/')
}

export function createCategory(request: CategoryRequest): Promise<Category> {
  return apiFetch<Category>('/categories/', { method: 'POST', body: request })
}

export function renameCategory(id: string, request: CategoryRequest): Promise<Category> {
  return apiFetch<Category>(`/categories/${id}`, { method: 'PUT', body: request })
}

export function deleteCategory(id: string): Promise<void> {
  return apiFetch<void>(`/categories/${id}`, { method: 'DELETE' })
}
