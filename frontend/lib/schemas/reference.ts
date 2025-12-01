/**
 * Reference Value Zod schemas
 */
import { z } from 'zod';

// Reference value schema
export const referenceValueSchema = z.object({
  id: z.number().int().positive(),
  category: z.string(),
  code: z.string(),
  value: z.string(),
  description: z.string().nullable().optional(),
  order: z.number().int().nonnegative(),
  isActive: z.boolean(),
});

export type ReferenceValue = z.infer<typeof referenceValueSchema>;

// Reference values list (array)
export const referenceValuesListSchema = z.array(referenceValueSchema);

export type ReferenceValuesList = z.infer<typeof referenceValuesListSchema>;

// Reference values by category (record)
export const referencesByCategorySchema = z.record(z.string(), z.array(referenceValueSchema));

export type ReferencesByCategory = z.infer<typeof referencesByCategorySchema>;

// Create reference value request
export const createReferenceValueSchema = z.object({
  category: z.string().min(1, 'Category is required'),
  code: z.string().min(1, 'Code is required'),
  value: z.string().min(1, 'Value is required'),
  description: z.string().optional(),
  order: z.number().int().nonnegative(),
});

export type CreateReferenceValue = z.infer<typeof createReferenceValueSchema>;

// Update reference value request
export const updateReferenceValueSchema = z.object({
  value: z.string().min(1, 'Value is required'),
  description: z.string().optional(),
  order: z.number().int().nonnegative(),
  isActive: z.boolean(),
});

export type UpdateReferenceValue = z.infer<typeof updateReferenceValueSchema>;
