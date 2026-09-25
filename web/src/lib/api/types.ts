export interface RegisterRequest {
  email: string
  password: string
  displayName: string
}

export interface LoginRequest {
  email: string
  password: string
}

export interface AuthResponse {
  token: string
  expiresAtUtc: string
}

export interface ReceiptUploadResponse {
  id: string
  uploadedAtUtc: string
  status: string
}

export interface ReceiptLineItem {
  id: string
  description: string
  amount: number
  categoryId: string | null
}

export interface ReceiptSummary {
  id: string
  uploadedAtUtc: string
  purchaseDate: string | null
  totalAmount: number | null
  merchantId: string | null
  merchantName: string | null
  dominantCategoryId: string | null
  status: string
}

export interface ReceiptDetail extends ReceiptSummary {
  imageContentType: string
  imageSizeBytes: number
  lineItems: ReceiptLineItem[]
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export interface Merchant {
  id: string
  name: string
}

export interface ReceiptFilters {
  from?: string
  to?: string
  categoryId?: string
  merchantId?: string
}

export interface Category {
  id: string
  name: string
}

export interface UpdateLineItemCategoryRequest {
  categoryId: string
}

export interface RecategorizeResponse {
  updatedCount: number
}
