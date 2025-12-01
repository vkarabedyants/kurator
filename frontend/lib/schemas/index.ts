/**
 * Zod Schemas - Central Export
 *
 * This module exports all Zod schemas for API validation.
 * Use these schemas for runtime type checking of API responses.
 *
 * Example usage:
 * ```typescript
 * import { contactDetailSchema, validateApiResponse } from '@/lib/schemas';
 *
 * const response = await api.get('/contacts/1');
 * const validated = validateApiResponse(response.data, contactDetailSchema, 'GET /contacts/:id');
 * ```
 */

// Common schemas
export {
  paginationMetaSchema,
  createPaginatedResponseSchema,
  apiErrorSchema,
  dateStringSchema,
  nullableDateStringSchema,
  type PaginationMeta,
  type ApiError,
} from './common';

// Auth schemas
export {
  userRoleSchema,
  userSchema,
  loginRequestSchema,
  loginResponseSchema,
  tokenResponseSchema,
  setupMfaRequestSchema,
  setupMfaResponseSchema,
  verifyMfaRequestSchema,
  verifyMfaResponseSchema,
  type UserRole,
  type User,
  type LoginRequest,
  type LoginResponse,
  type TokenResponse,
  type SetupMfaRequest,
  type SetupMfaResponse,
  type VerifyMfaRequest,
  type VerifyMfaResponse,
} from './auth';

// Contact schemas
export {
  interactionSummarySchema,
  statusHistorySchema,
  contactListItemSchema,
  contactDetailSchema,
  contactListResponseSchema,
  createContactRequestSchema,
  updateContactRequestSchema,
  type InteractionSummary,
  type StatusHistory,
  type ContactListItem,
  type ContactDetail,
  type ContactListResponse,
  type CreateContactRequest,
  type UpdateContactRequest,
} from './contact';

// Block schemas
export {
  blockStatusSchema,
  blockCuratorSchema,
  blockSchema,
  blockListResponseSchema,
  createBlockRequestSchema,
  updateBlockRequestSchema,
  assignCuratorRequestSchema,
  type BlockStatus,
  type BlockCurator,
  type Block,
  type BlockListResponse,
  type CreateBlockRequest,
  type UpdateBlockRequest,
  type AssignCuratorRequest,
} from './block';

// Interaction schemas
export {
  interactionSchema,
  interactionListResponseSchema,
  createInteractionRequestSchema,
  updateInteractionRequestSchema,
  type Interaction,
  type InteractionListResponse,
  type CreateInteractionRequest as CreateInteractionRequestZod,
  type UpdateInteractionRequest as UpdateInteractionRequestZod,
} from './interaction';

// Watchlist schemas
export {
  riskLevelSchema,
  monitoringFrequencySchema,
  watchlistEntrySchema,
  watchlistListResponseSchema,
  createWatchlistEntrySchema,
  recordCheckRequestSchema,
  watchlistStatisticsSchema,
  type RiskLevel,
  type MonitoringFrequency,
  type WatchlistEntry,
  type WatchlistListResponse,
  type CreateWatchlistEntry,
  type RecordCheckRequest,
  type WatchlistStatistics,
} from './watchlist';

// Reference schemas
export {
  referenceValueSchema,
  referenceValuesListSchema,
  referencesByCategorySchema,
  createReferenceValueSchema,
  updateReferenceValueSchema,
  type ReferenceValue,
  type ReferenceValuesList,
  type ReferencesByCategory,
  type CreateReferenceValue,
  type UpdateReferenceValue,
} from './reference';

// Dashboard schemas
export {
  recentInteractionSchema,
  attentionContactSchema,
  curatorDashboardSchema,
  auditLogSummarySchema,
  adminDashboardSchema,
  type RecentInteraction,
  type AttentionContact,
  type CuratorDashboard,
  type AuditLogSummary,
  type AdminDashboard,
} from './dashboard';

// Validator utilities
export {
  validateApiResponse,
  createValidator,
  validateStrictApiResponse,
  validateArrayItems,
  parseOrUndefined,
  type ValidationResult,
} from './validator';
