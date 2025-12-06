import { z } from 'zod'

export const ReceiptStatusSchema = z.enum(['Draft', 'Received', 'Approved', 'Canceled'])
export type ReceiptStatus = z.infer<typeof ReceiptStatusSchema>

export const ReceiptReasonSchema = z.enum(['Purchase', 'ReturnIn', 'Production', 'Other'])
export type ReceiptReason = z.infer<typeof ReceiptReasonSchema>

export const ReceiptLineSchema = z.object({
  id: z.string().uuid(),
  lineNo: z.number(),
  productId: z.string().uuid(),
  variantId: z.string().uuid().nullable().optional(),
  qty: z.number(),
  lotNumber: z.string().nullable().optional(),
  expiryDateUtc: z.string().datetime().nullable().optional(),
  unitCost: z.number().nullable().optional(),
})
export type ReceiptLine = z.infer<typeof ReceiptLineSchema>

export const ReceiptDetailSchema = z.object({
  id: z.string().uuid(),
  warehouseId: z.string().uuid(),
  status: ReceiptStatusSchema,
  reason: ReceiptReasonSchema,
  externalRef: z.string().nullable().optional(),
  docDate: z.string().datetime(),
  receivedAt: z.string().datetime().nullable().optional(),
  approvedAt: z.string().datetime().nullable().optional(),
  lines: z.array(ReceiptLineSchema),
})
export type ReceiptDetail = z.infer<typeof ReceiptDetailSchema>

export const CreateReceiptSchema = z.object({
  warehouseId: z.string().uuid(),
  reason: z.number().min(1).max(99),
  externalRef: z.string().optional().nullable(),
  docDateUtc: z.string().datetime().optional().nullable(),
})
export type CreateReceiptDto = z.infer<typeof CreateReceiptSchema>

export const AddReceiptLineSchema = z.object({
  productId: z.string().uuid(),
  variantId: z.string().uuid().optional().nullable(),
  qty: z.number().positive(),
  lotNumber: z.string().optional().nullable(),
  expiryDateUtc: z.string().datetime().optional().nullable(),
  unitCost: z.number().positive().optional().nullable(),
})
export type AddReceiptLineDto = z.infer<typeof AddReceiptLineSchema>

export const UpdateReceiptHeaderSchema = z.object({
  externalRef: z.string().optional().nullable(),
  docDateUtc: z.string().datetime().optional().nullable(),
})
export type UpdateReceiptHeaderDto = z.infer<typeof UpdateReceiptHeaderSchema>

export const UpdateReceiptLineSchema = z.object({
  qty: z.number().positive().optional(),
  lotNumber: z.string().optional().nullable(),
  expiryDateUtc: z.string().datetime().optional().nullable(),
  unitCost: z.number().positive().optional().nullable(),
})
export type UpdateReceiptLineDto = z.infer<typeof UpdateReceiptLineSchema>


