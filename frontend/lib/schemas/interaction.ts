/**
 * Interaction Zod schemas
 */
import { z } from 'zod';
import { createPaginatedResponseSchema, nullableDateStringSchema } from './common';

// Interaction schema
export const interactionSchema = z.object({
  id: z.number().int().positive(),
  contactId: z.number().int().positive(),
  contactName: z.string().optional(),
  contactDisplayId: z.string().optional(),
  blockName: z.string().optional(),
  interactionDate: z.string(),
  interactionTypeId: z.number().nullable().optional(),
  curatorId: z.number().int().positive(),
  curatorLogin: z.string(),
  resultId: z.number().nullable().optional(),
  comment: z.string().nullable().optional(),
  statusChangeJson: z.string().nullable().optional(),
  attachmentsJson: z.string().nullable().optional(),
  nextTouchDate: nullableDateStringSchema,
  createdAt: z.string(),
  updatedAt: z.string(),
  updatedBy: z.number().int(),
});

export type Interaction = z.infer<typeof interactionSchema>;

// Paginated interaction list response
export const interactionListResponseSchema = createPaginatedResponseSchema(interactionSchema);

export type InteractionListResponse = z.infer<typeof interactionListResponseSchema>;

// Create interaction request
export const createInteractionRequestSchema = z.object({
  contactId: z.number().int().positive(),
  interactionDate: z.string().optional(),
  interactionTypeId: z.number().nullable().optional(),
  resultId: z.number().nullable().optional(),
  comment: z.string().optional(),
  statusChangeJson: z.string().optional(),
  attachmentsJson: z.string().optional(),
  nextTouchDate: z.string().nullable().optional(),
});

export type CreateInteractionRequest = z.infer<typeof createInteractionRequestSchema>;

// Update interaction request
export const updateInteractionRequestSchema = createInteractionRequestSchema.partial().omit({ contactId: true });

export type UpdateInteractionRequest = z.infer<typeof updateInteractionRequestSchema>;
