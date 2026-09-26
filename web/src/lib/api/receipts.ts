import { apiFetch, apiFetchBlob } from './client'
import type {
  PagedResponse,
  ReceiptDetail,
  ReceiptFilters,
  ReceiptLineItem,
  ReceiptSummary,
  ReceiptUploadResponse,
  RecategorizeResponse,
  UpdateLineItemCategoryRequest,
  UpdateLineItemRequest,
} from './types'

export function uploadReceipt(file: File): Promise<ReceiptUploadResponse> {
  const formData = new FormData()
  formData.append('file', file)
  return apiFetch<ReceiptUploadResponse>('/receipts/', { method: 'POST', body: formData })
}

export function listReceipts(
  filters: ReceiptFilters = {},
  page = 1,
  pageSize = 20,
): Promise<PagedResponse<ReceiptSummary>> {
  return apiFetch<PagedResponse<ReceiptSummary>>('/receipts/', { query: { ...filters, page, pageSize } })
}

export function getReceipt(id: string): Promise<ReceiptDetail> {
  return apiFetch<ReceiptDetail>(`/receipts/${id}`)
}

export function confirmReceipt(id: string): Promise<void> {
  return apiFetch<void>(`/receipts/${id}/confirm`, { method: 'POST' })
}

export function getReceiptImage(id: string): Promise<Blob> {
  return apiFetchBlob(`/receipts/${id}/image`)
}

export function updateLineItem(
  receiptId: string,
  lineItemId: string,
  request: UpdateLineItemRequest,
): Promise<ReceiptLineItem> {
  return apiFetch<ReceiptLineItem>(`/receipts/${receiptId}/line-items/${lineItemId}`, {
    method: 'PUT',
    body: request,
  })
}

export function updateLineItemCategory(
  receiptId: string,
  lineItemId: string,
  request: UpdateLineItemCategoryRequest,
): Promise<ReceiptLineItem> {
  return apiFetch<ReceiptLineItem>(`/receipts/${receiptId}/line-items/${lineItemId}/category`, {
    method: 'PUT',
    body: request,
  })
}

export function recategorize(): Promise<RecategorizeResponse> {
  return apiFetch<RecategorizeResponse>('/receipts/recategorize', { method: 'POST' })
}
