/**
 * Dashboard Zod schemas
 */
import { z } from 'zod';

// Recent interaction schema (for dashboard)
export const recentInteractionSchema = z.object({
  id: z.number().int().positive(),
  contactName: z.string(),
  contactId: z.string(),
  interactionDate: z.string(),
  interactionTypeId: z.string().or(z.number()),
  resultId: z.string().or(z.number()),
});

export type RecentInteraction = z.infer<typeof recentInteractionSchema>;

// Attention contact schema (overdue contacts)
export const attentionContactSchema = z.object({
  id: z.number().int().positive(),
  contactId: z.string(),
  fullName: z.string(),
  nextTouchDate: z.string().nullable().optional(),
  daysOverdue: z.number().int(),
  influenceStatus: z.string(),
});

export type AttentionContact = z.infer<typeof attentionContactSchema>;

// Curator dashboard schema
export const curatorDashboardSchema = z.object({
  totalContacts: z.number().int().nonnegative(),
  interactionsLastMonth: z.number().int().nonnegative(),
  averageInteractionInterval: z.number().nonnegative(),
  overdueContacts: z.number().int().nonnegative(),
  recentInteractions: z.array(recentInteractionSchema),
  contactsRequiringAttention: z.array(attentionContactSchema),
  contactsByInfluenceStatus: z.record(z.string(), z.number()),
  interactionsByType: z.record(z.string(), z.number()),
});

export type CuratorDashboard = z.infer<typeof curatorDashboardSchema>;

// Audit log summary schema (for admin dashboard)
export const auditLogSummarySchema = z.object({
  id: z.number().int().positive(),
  userLogin: z.string(),
  actionType: z.string(),
  entityType: z.string(),
  timestamp: z.string(),
});

export type AuditLogSummary = z.infer<typeof auditLogSummarySchema>;

// Admin dashboard schema
export const adminDashboardSchema = z.object({
  totalContacts: z.number().int().nonnegative(),
  totalInteractions: z.number().int().nonnegative(),
  totalBlocks: z.number().int().nonnegative(),
  totalUsers: z.number().int().nonnegative(),
  newContactsLastMonth: z.number().int().nonnegative(),
  interactionsLastMonth: z.number().int().nonnegative(),
  contactsByBlock: z.record(z.string(), z.number()),
  contactsByInfluenceStatus: z.record(z.string(), z.number()),
  contactsByInfluenceType: z.record(z.string(), z.number()),
  interactionsByBlock: z.record(z.string(), z.number()),
  topCuratorsByActivity: z.record(z.string(), z.number()),
  statusChangeDynamics: z.record(z.string(), z.number()),
  recentAuditLogs: z.array(auditLogSummarySchema),
});

export type AdminDashboard = z.infer<typeof adminDashboardSchema>;
