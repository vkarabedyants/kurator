/**
 * Contact Zod schemas
 */
import { z } from 'zod';
import { createPaginatedResponseSchema, nullableDateStringSchema } from './common';

// Interaction summary schema (nested in contact)
export const interactionSummarySchema = z.object({
  id: z.number().int().positive(),
  interactionDate: z.string(),
  interactionTypeId: z.number().nullable().optional(),
  resultId: z.number().nullable().optional(),
  comment: z.string().nullable().optional(),
  statusChangeJson: z.string().nullable().optional(),
  curatorLogin: z.string(),
});

export type InteractionSummary = z.infer<typeof interactionSummarySchema>;

// Status history schema
export const statusHistorySchema = z.object({
  id: z.number().int().positive(),
  oldStatus: z.string(),
  newStatus: z.string(),
  changedAt: z.string(),
  changedBy: z.string(),
});

export type StatusHistory = z.infer<typeof statusHistorySchema>;

// Contact list item schema
export const contactListItemSchema = z.object({
  id: z.number().int().positive(),
  contactId: z.string(),
  fullName: z.string(),
  blockId: z.number().int().positive(),
  blockName: z.string(),
  blockCode: z.string(),
  organizationId: z.number().nullable().optional(),
  position: z.string().nullable().optional(),
  influenceStatusId: z.number().nullable().optional(),
  influenceTypeId: z.number().nullable().optional(),
  lastInteractionDate: nullableDateStringSchema,
  nextTouchDate: nullableDateStringSchema,
  responsibleCuratorId: z.number().int().positive(),
  responsibleCuratorLogin: z.string(),
  updatedAt: z.string(),
  updatedBy: z.number().int(),
  isOverdue: z.boolean(),
});

export type ContactListItem = z.infer<typeof contactListItemSchema>;

// Contact detail schema (extends list item)
export const contactDetailSchema = contactListItemSchema.extend({
  usefulnessDescription: z.string().nullable().optional(),
  communicationChannelId: z.number().nullable().optional(),
  contactSourceId: z.number().nullable().optional(),
  notes: z.string().nullable().optional(),
  createdAt: z.string(),
  interactionCount: z.number().int().nonnegative(),
  lastInteractionDaysAgo: z.number().nullable().optional(),
  interactions: z.array(interactionSummarySchema),
  statusHistory: z.array(statusHistorySchema),
});

export type ContactDetail = z.infer<typeof contactDetailSchema>;

// Paginated contact list response
export const contactListResponseSchema = createPaginatedResponseSchema(contactListItemSchema);

export type ContactListResponse = z.infer<typeof contactListResponseSchema>;

// Create contact request schema
export const createContactRequestSchema = z.object({
  blockId: z.number().int().positive(),
  fullName: z.string().min(1, 'Full name is required'),
  organizationId: z.number().nullable().optional(),
  position: z.string().nullable().optional(),
  influenceStatusId: z.number().nullable().optional(),
  influenceTypeId: z.number().nullable().optional(),
  usefulnessDescription: z.string().nullable().optional(),
  communicationChannelId: z.number().nullable().optional(),
  contactSourceId: z.number().nullable().optional(),
  nextTouchDate: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
});

export type CreateContactRequest = z.infer<typeof createContactRequestSchema>;

// Update contact request schema
export const updateContactRequestSchema = z.object({
  organizationId: z.number().nullable().optional(),
  position: z.string().nullable().optional(),
  influenceStatusId: z.number().nullable().optional(),
  influenceTypeId: z.number().nullable().optional(),
  usefulnessDescription: z.string().nullable().optional(),
  communicationChannelId: z.number().nullable().optional(),
  contactSourceId: z.number().nullable().optional(),
  nextTouchDate: z.string().nullable().optional(),
  notes: z.string().nullable().optional(),
});

export type UpdateContactRequest = z.infer<typeof updateContactRequestSchema>;
