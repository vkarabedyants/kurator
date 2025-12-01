/**
 * Block Zod schemas
 */
import { z } from 'zod';

// Block status enum
export const blockStatusSchema = z.enum(['Active', 'Archived']);
export type BlockStatus = z.infer<typeof blockStatusSchema>;

// Block curator schema
export const blockCuratorSchema = z.object({
  userId: z.number().int().positive(),
  userLogin: z.string(),
  curatorType: z.string(),
  assignedAt: z.string(),
});

export type BlockCurator = z.infer<typeof blockCuratorSchema>;

// Block schema
export const blockSchema = z.object({
  id: z.number().int().positive(),
  name: z.string(),
  description: z.string().nullable().optional(),
  code: z.string(),
  status: blockStatusSchema.or(z.string()),
  curators: z.array(blockCuratorSchema).optional(),
  createdAt: z.string(),
  updatedAt: z.string(),
  // Legacy fields for backwards compatibility
  primaryCuratorId: z.number().optional(),
  backupCuratorId: z.number().optional(),
  primaryCurator: z.any().optional(),
  backupCurator: z.any().optional(),
});

export type Block = z.infer<typeof blockSchema>;

// Block list response (array)
export const blockListResponseSchema = z.array(blockSchema);

export type BlockListResponse = z.infer<typeof blockListResponseSchema>;

// Create block request
export const createBlockRequestSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  code: z.string().min(1, 'Code is required'),
  description: z.string().optional(),
  status: blockStatusSchema.or(z.string()),
});

export type CreateBlockRequest = z.infer<typeof createBlockRequestSchema>;

// Update block request
export const updateBlockRequestSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  description: z.string().optional(),
  status: blockStatusSchema.or(z.string()),
});

export type UpdateBlockRequest = z.infer<typeof updateBlockRequestSchema>;

// Assign curator request
export const assignCuratorRequestSchema = z.object({
  userId: z.number().int().positive(),
  curatorType: z.string(),
});

export type AssignCuratorRequest = z.infer<typeof assignCuratorRequestSchema>;
