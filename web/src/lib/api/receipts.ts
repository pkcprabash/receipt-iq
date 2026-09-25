import { apiFetch } from './client'
import type {
  PagedResponse,
  ReceiptDetail,
  ReceiptFilters,
  ReceiptLineItem,
  ReceiptSummary,
  ReceiptUploadResponse,
  RecategorizeResponse,
  UpdateLineItemCategoryRequest,
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
