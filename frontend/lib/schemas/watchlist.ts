/**
 * Watchlist Zod schemas
 */
import { z } from 'zod';
import { createPaginatedResponseSchema, nullableDateStringSchema } from './common';

// Risk level enum
export const riskLevelSchema = z.enum(['Low', 'Medium', 'High', 'Critical']);
export type RiskLevel = z.infer<typeof riskLevelSchema>;

// Monitoring frequency enum
export const monitoringFrequencySchema = z.enum(['Weekly', 'Monthly', 'Quarterly', 'AdHoc']);
export type MonitoringFrequency = z.infer<typeof monitoringFrequencySchema>;

// Watchlist entry schema
export const watchlistEntrySchema = z.object({
  id: z.number().int().positive(),
  fullName: z.string(),
  roleStatus: z.string().nullable().optional(),
  riskSphereId: z.number().nullable().optional(),
  threatSource: z.string().nullable().optional(),
  conflictDate: nullableDateStringSchema,
  riskLevel: riskLevelSchema.or(z.string()),
  monitoringFrequency: monitoringFrequencySchema.or(z.string()),
  lastCheckDate: nullableDateStringSchema,
  nextCheckDate: nullableDateStringSchema,
  dynamicsDescription: z.string().nullable().optional(),
  watchOwnerId: z.number().nullable().optional(),
  watchOwnerLogin: z.string().nullable().optional(),
  attachmentsJson: z.string().nullable().optional(),
  createdAt: z.string(),
  updatedAt: z.string(),
  updatedBy: z.number().int(),
  requiresCheck: z.boolean().optional(),
});

export type WatchlistEntry = z.infer<typeof watchlistEntrySchema>;

// Paginated watchlist response
export const watchlistListResponseSchema = createPaginatedResponseSchema(watchlistEntrySchema);

export type WatchlistListResponse = z.infer<typeof watchlistListResponseSchema>;

// Create watchlist entry request
export const createWatchlistEntrySchema = z.object({
  fullName: z.string().min(1, 'Full name is required'),
  roleStatus: z.string().optional(),
  riskSphereId: z.number().optional(),
  threatSource: z.string().optional(),
  conflictDate: z.string().optional(),
  riskLevel: riskLevelSchema.or(z.string()),
  monitoringFrequency: monitoringFrequencySchema.or(z.string()),
  nextCheckDate: z.string().optional(),
  dynamicsDescription: z.string().optional(),
  watchOwnerId: z.number().optional(),
  attachmentsJson: z.string().optional(),
});

export type CreateWatchlistEntry = z.infer<typeof createWatchlistEntrySchema>;

// Record check request
export const recordCheckRequestSchema = z.object({
  nextCheckDate: z.string().optional(),
  dynamicsUpdate: z.string().optional(),
  newRiskLevel: riskLevelSchema.or(z.string()).optional(),
});

export type RecordCheckRequest = z.infer<typeof recordCheckRequestSchema>;

// Watchlist statistics response
export const watchlistStatisticsSchema = z.object({
  totalEntries: z.number().int().nonnegative(),
  byRiskLevel: z.record(z.string(), z.number()),
  byMonitoringFrequency: z.record(z.string(), z.number()),
  requireingCheck: z.number().int().nonnegative(),
});

export type WatchlistStatistics = z.infer<typeof watchlistStatisticsSchema>;
