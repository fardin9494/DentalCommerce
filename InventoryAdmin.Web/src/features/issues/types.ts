import { z } from 'zod'

export const IssueStatusSchema = z.enum(['Draft', 'Posted', 'Canceled'])
export type IssueStatus = z.infer<typeof IssueStatusSchema>

export const IssueAllocationSchema = z.object({
  id: z.string().uuid(),
  stockItemId: z.string().uuid(),
  qty: z.number(),
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
  warehouseId: z.string().uuid(),
  status: IssueStatusSchema,
  externalRef: z.string().nullable().optional(),
  docDate: z.string().datetime(),
  postedAt: z.string().datetime().nullable().optional(),
  lines: z.array(IssueLineSchema),
})
export type IssueDetail = z.infer<typeof IssueDetailSchema>

export const CreateIssueSchema = z.object({
  warehouseId: z.string().uuid(),
  externalRef: z.string().optional().nullable(),
  docDateUtc: z.string().datetime().optional().nullable(),
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
  docDateUtc: z.string().datetime().optional().nullable(),
})
export type UpdateIssueHeaderDto = z.infer<typeof UpdateIssueHeaderSchema>

export const UpdateIssueLineSchema = z.object({
  qty: z.number().positive().optional(),
})
export type UpdateIssueLineDto = z.infer<typeof UpdateIssueLineSchema>


