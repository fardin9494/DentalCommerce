export type ApiError = {
  status: number
  message: string
  details?: unknown
}

export type Paginated<T> = {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}


