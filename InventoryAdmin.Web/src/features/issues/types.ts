import { z } from 'zod'

export const IssueStatusSchema = z.enum(['Draft', 'Posted', 'Canceled'])
export type IssueStatus = z.infer<typeof IssueStatusSchema>

// Status number mappings (for API)
export const IssueStatusMap: Record<IssueStatus, number> = {
  Draft: 1,
  Posted: 2,
  Canceled: 3,
}

export const IssueAllocationSchema = z.object({
  id: z.string().uuid(),
  stockItemId: z.string().uuid(),
  qty: z.number(),
  sku: z.string().nullable().optional(),
  lotNumber: z.string().nullable().optional(),
  expiryDate: z.string().nullable().optional(),
  shelfId: z.string().uuid().nullable().optional(),
  shelfName: z.string().nullable().optional(),
  warehouseId: z.string().uuid().nullable().optional(), // اختیاری برای سازگاری با رکوردهای قدیمی
  warehouseName: z.string().nullable().optional(),
  serials: z.array(z.object({ serialNumber: z.string(), status: z.string() })),
})
export type IssueAllocation = z.infer<typeof IssueAllocationSchema>

export const IssueLineSchema = z.object({
  id: z.string().uuid(),
  lineNo: z.number(),
  productId: z.string().uuid(),
  variantId: z.string().uuid().nullable().optional(),
  requestedQty: z.number(),
  allocatedQty: z.number(),
  remainingQty: z.number(),
  allocations: z.array(IssueAllocationSchema),
})
export type IssueLine = z.infer<typeof IssueLineSchema>

export const IssueDetailSchema = z.object({
  id: z.string().uuid(),
  warehouseId: z.string().uuid().nullable().optional(),
  status: IssueStatusSchema,
  externalRef: z.string().nullable().optional(),
  docDate: z.string(), // API returns DateTime as string, not ISO datetime
  postedAt: z.string().nullable().optional(),
  lines: z.array(IssueLineSchema),
})
export type IssueDetail = z.infer<typeof IssueDetailSchema>

export const CreateIssueSchema = z.object({
  warehouseId: z.string().uuid().optional().nullable(),
  externalRef: z.string().optional().nullable(),
  docDateUtc: z.string().optional().nullable(), // Accept any string format, API will parse it
})
export type CreateIssueDto = z.infer<typeof CreateIssueSchema>

export const AddIssueLineSchema = z.object({
  productId: z.string().uuid(),
  variantId: z.string().uuid().optional().nullable(),
  qty: z.number().positive(),
})
export type AddIssueLineDto = z.infer<typeof AddIssueLineSchema>

export const UpdateIssueHeaderSchema = z.object({
  externalRef: z.string().optional().nullable(),
  docDateUtc: z.string().optional().nullable(), // Accept any string format, API will parse it
})
export type UpdateIssueHeaderDto = z.infer<typeof UpdateIssueHeaderSchema>

export const UpdateIssueLineSchema = z.object({
  qty: z.number().positive().optional(),
})
export type UpdateIssueLineDto = z.infer<typeof UpdateIssueLineSchema>

// List types
export const IssueListItemSchema = z.object({
  id: z.string().uuid(),
  warehouseId: z.string().uuid().nullable().optional(),
  warehouseName: z.string().nullable().optional(),
  status: z.string(),
  externalRef: z.string().nullable().optional(),
  docDate: z.string(), // API returns DateTime as string, not ISO datetime
  postedAt: z.string().nullable().optional(),
  linesCount: z.number(),
  totalRequestedQty: z.number(),
  totalAllocatedQty: z.number(),
  totalRemainingQty: z.number(),
  createdAt: z.string(), // API returns DateTime as string, not ISO datetime
})
export type IssueListItem = z.infer<typeof IssueListItemSchema>

export const IssuesListResultSchema = z.object({
  items: z.array(IssueListItemSchema),
  totalCount: z.number(),
  page: z.number(),
  pageSize: z.number(),
  totalPages: z.number(),
})
export type IssuesListResult = z.infer<typeof IssuesListResultSchema>

export const IssueStatusLabels: Record<string, string> = {
  Draft: 'پیش‌نویس',
  Posted: 'ثبت شده',
  Canceled: 'لغو شده',
}

export const IssueStatusColors: Record<string, string> = {
  Draft: 'bg-yellow-100 text-yellow-800',
  Posted: 'bg-green-100 text-green-800',
  Canceled: 'bg-red-100 text-red-800',
}

export interface IssuesListFilters {
  page?: number
  pageSize?: number
  warehouseId?: string
  status?: number
  fromDate?: string
  toDate?: string
  search?: string
}



