import api from '@/lib/api';
import { logger } from '@/lib/logger';
import {
  AuthResponse,
  LoginRequest,
  LoginResponse,
  SetupMfaRequest,
  SetupMfaResponse,
  VerifyMfaRequest,
  VerifyMfaResponse,
  PaginatedResponse,
  ContactListItem,
  ContactDetail,
  CreateContactRequest,
  UpdateContactRequest,
  Interaction,
  CreateInteractionRequest,
  Block,
  User,
  WatchlistEntry,
  FAQ,
  ReferenceValue,
  AuditLogEntry,
  CuratorDashboard,
  AdminDashboard
} from '@/types/api';

// Import Zod schemas and validator
import {
  validateApiResponse,
  loginResponseSchema,
  userSchema,
  contactListResponseSchema,
  contactDetailSchema,
  contactListItemSchema,
  blockSchema,
  interactionSchema,
  interactionListResponseSchema,
  watchlistEntrySchema,
  watchlistListResponseSchema,
  referenceValueSchema,
  curatorDashboardSchema,
  adminDashboardSchema,
} from '@/lib/schemas';
import { z } from 'zod';

// Create service logger
const serviceLogger = logger.child({ service: 'api-service' });

// Helper for validated API calls with logging
function withValidation<T>(
  promise: Promise<T>,
  schema: z.ZodType<T>,
  context: string
): Promise<T> {
  const timer = logger.startTimer(`validation:${context}`);
  return promise.then(data => {
    serviceLogger.debug(`Validating response for: ${context}`, { context });
    const result = validateApiResponse(data, schema, context);
    timer();
    return data;
  }).catch(error => {
    serviceLogger.error(`Validation failed for: ${context}`, { context }, error);
    throw error;
  });
}

// Auth API with detailed logging
export const authApi = {
  login: (data: LoginRequest) => {
    logger.auth('Login attempt started', { login: data.login });
    return withValidation(
      api.post<LoginResponse>('/auth/login', data).then(res => {
        logger.auth('Login response received', {
          login: data.login,
          requireMfaSetup: res.data.requireMfaSetup,
          requireMfaVerification: res.data.requireMfaVerification,
          hasToken: !!res.data.token,
        });
        return res.data;
      }),
      loginResponseSchema,
      'POST /auth/login'
    );
  },

  register: (data: { login: string; password: string; role: string }) => {
    logger.auth('Registration attempt started', { login: data.login, role: data.role });
    return api.post('/auth/register', data).then(res => {
      logger.auth('Registration successful', { login: data.login });
      return res.data;
    });
  },

  setupMfa: (data: SetupMfaRequest) => {
    logger.auth('MFA setup initiated', { userId: data.userId });
    return api.post<SetupMfaResponse>('/auth/setup-mfa', data).then(res => {
      logger.auth('MFA setup response received', { userId: data.userId, hasQrCode: !!res.data.qrCodeUrl });
      return res.data;
    });
  },

  verifyMfa: (data: VerifyMfaRequest) => {
    logger.auth('MFA verification attempt', { userId: data.userId });
    return api.post<VerifyMfaResponse>('/auth/verify-mfa', data).then(res => {
      logger.auth('MFA verification successful', { userId: data.userId, hasToken: !!res.data.token });
      return res.data;
    });
  },

  logout: () => {
    logger.auth('Logout initiated');
    return api.post('/auth/logout').then(res => {
      logger.auth('Logout completed');
      return res.data;
    });
  },
};

// Contacts API with logging
export const contactsApi = {
  getAll: (params?: {
    blockId?: number;
    search?: string;
    influenceStatus?: string;
    influenceType?: string;
    organizationId?: string;
    page?: number;
    pageSize?: number;
  }) => {
    serviceLogger.debug('Fetching contacts list', { params });
    return withValidation(
      api.get<PaginatedResponse<ContactListItem>>('/contacts', { params }).then(res => {
        serviceLogger.debug('Contacts fetched', { count: res.data.items?.length, totalCount: res.data.totalCount });
        return res.data;
      }),
      contactListResponseSchema as z.ZodType<PaginatedResponse<ContactListItem>>,
      'GET /contacts'
    );
  },

  getById: (id: number) => {
    serviceLogger.debug('Fetching contact by ID', { contactId: id });
    return withValidation(
      api.get<ContactDetail>(`/contacts/${id}`).then(res => {
        serviceLogger.debug('Contact fetched', { contactId: id });
        return res.data;
      }),
      contactDetailSchema as z.ZodType<ContactDetail>,
      `GET /contacts/${id}`
    );
  },

  create: (data: CreateContactRequest) => {
    serviceLogger.info('Creating new contact', { blockId: data.blockId });
    return api.post<{ id: number; contactId: string }>('/contacts', data).then(res => {
      serviceLogger.info('Contact created', { id: res.data.id, contactId: res.data.contactId });
      return res.data;
    });
  },

  update: (id: number, data: UpdateContactRequest) => {
    serviceLogger.info('Updating contact', { contactId: id });
    return api.put(`/contacts/${id}`, data).then(res => {
      serviceLogger.info('Contact updated', { contactId: id });
      return res.data;
    });
  },

  delete: (id: number) => {
    serviceLogger.info('Deleting contact', { contactId: id });
    return api.delete(`/contacts/${id}`).then(res => {
      serviceLogger.info('Contact deleted', { contactId: id });
      return res.data;
    });
  },

  getOverdue: () => {
    serviceLogger.debug('Fetching overdue contacts');
    return api.get<ContactListItem[]>('/contacts/overdue').then(res => {
      serviceLogger.debug('Overdue contacts fetched', { count: res.data.length });
      const result = validateApiResponse(
        res.data,
        z.array(contactListItemSchema),
        'GET /contacts/overdue'
      );
      return result.data as ContactListItem[];
    });
  },
};

// Interactions API
export const interactionsApi = {
  getAll: (params?: {
    contactId?: number;
    blockId?: number;
    fromDate?: string;
    toDate?: string;
    interactionTypeId?: number;
    resultId?: number;
    page?: number;
    pageSize?: number;
  }) =>
    withValidation(
      api.get<PaginatedResponse<Interaction>>('/interactions', { params }).then(res => res.data),
      interactionListResponseSchema as z.ZodType<PaginatedResponse<Interaction>>,
      'GET /interactions'
    ),

  getById: (id: number) =>
    withValidation(
      api.get<Interaction>(`/interactions/${id}`).then(res => res.data),
      interactionSchema as z.ZodType<Interaction>,
      `GET /interactions/${id}`
    ),

  create: (data: CreateInteractionRequest) =>
    api.post<{ id: number }>('/interactions', data).then(res => res.data),

  update: (id: number, data: Partial<CreateInteractionRequest>) =>
    api.put(`/interactions/${id}`, data).then(res => res.data),

  delete: (id: number) =>
    api.delete(`/interactions/${id}`).then(res => res.data),

  deactivate: (id: number) =>
    api.put(`/interactions/${id}/deactivate`).then(res => res.data),
};

// Blocks API
export const blocksApi = {
  getAll: () =>
    api.get<Block[]>('/blocks').then(res => {
      const result = validateApiResponse(
        res.data,
        z.array(blockSchema),
        'GET /blocks'
      );
      return result.data as Block[];
    }),

  getMyBlocks: () =>
    api.get<Block[]>('/blocks/my-blocks').then(res => {
      const result = validateApiResponse(
        res.data,
        z.array(blockSchema),
        'GET /blocks/my-blocks'
      );
      return result.data as Block[];
    }),

  getById: (id: number) =>
    withValidation(
      api.get<Block>(`/blocks/${id}`).then(res => res.data),
      blockSchema as z.ZodType<Block>,
      `GET /blocks/${id}`
    ),

  create: (data: { name: string; code: string; description?: string; status: string }) =>
    api.post<{ id: number }>('/blocks', data).then(res => res.data),

  update: (id: number, data: { name: string; description?: string; status: string }) =>
    api.put(`/blocks/${id}`, data).then(res => res.data),

  archive: (id: number) =>
    api.put(`/blocks/${id}/archive`).then(res => res.data),

  assignCurator: (id: number, data: { userId: number; curatorType: string }) =>
    api.post<{ id: number }>(`/blocks/${id}/curators`, data).then(res => res.data),

  removeCurator: (id: number, curatorAssignmentId: number) =>
    api.delete(`/blocks/${id}/curators/${curatorAssignmentId}`).then(res => res.data),

  delete: (id: number) =>
    api.delete(`/blocks/${id}`).then(res => res.data),
};

// Users API
export const usersApi = {
  getAll: () =>
    api.get<User[]>('/users').then(res => {
      const result = validateApiResponse(
        res.data,
        z.array(userSchema),
        'GET /users'
      );
      return result.data as User[];
    }),

  getById: (id: number) =>
    withValidation(
      api.get<User>(`/users/${id}`).then(res => res.data),
      userSchema as z.ZodType<User>,
      `GET /users/${id}`
    ),

  getCurators: () =>
    api.get<{ id: number; login: string; role: string }[]>('/users/curators').then(res => res.data),

  getMe: () =>
    withValidation(
      api.get<User>('/users/me').then(res => res.data),
      userSchema as z.ZodType<User>,
      'GET /users/me'
    ),

  getStatistics: () =>
    api.get('/users/statistics').then(res => res.data),

  create: (data: { login: string; password: string; role: string }) =>
    api.post<{ id: number; login: string }>('/users', data).then(res => res.data),

  update: (id: number, data: { role: string; newPassword?: string }) =>
    api.put(`/users/${id}`, data).then(res => res.data),

  delete: (id: number) =>
    api.delete(`/users/${id}`).then(res => res.data),

  changePassword: (id: number, data: { newPassword: string }) =>
    api.post(`/users/${id}/change-password`, data).then(res => res.data),

  toggleActive: (id: number) =>
    api.post<{ isActive: boolean }>(`/users/${id}/toggle-active`).then(res => res.data),
};

// Watchlist API
export const watchlistApi = {
  getAll: (params?: {
    riskLevel?: string;
    riskSphereId?: number;
    monitoringFrequency?: string;
    watchOwnerId?: number;
    requiresCheck?: boolean;
    page?: number;
    pageSize?: number;
  }) =>
    withValidation(
      api.get<PaginatedResponse<WatchlistEntry>>('/watchlist', { params }).then(res => res.data),
      watchlistListResponseSchema as z.ZodType<PaginatedResponse<WatchlistEntry>>,
      'GET /watchlist'
    ),

  getById: (id: number) =>
    withValidation(
      api.get<WatchlistEntry>(`/watchlist/${id}`).then(res => res.data),
      watchlistEntrySchema as z.ZodType<WatchlistEntry>,
      `GET /watchlist/${id}`
    ),

  create: (data: Partial<WatchlistEntry>) =>
    api.post<{ id: number }>('/watchlist', data).then(res => res.data),

  update: (id: number, data: Partial<WatchlistEntry>) =>
    api.put(`/watchlist/${id}`, data).then(res => res.data),

  delete: (id: number) =>
    api.delete(`/watchlist/${id}`).then(res => res.data),

  recordCheck: (id: number, data: { nextCheckDate?: string; dynamicsUpdate?: string; newRiskLevel?: string }) =>
    api.post(`/watchlist/${id}/check`, data).then(res => res.data),

  getRequiringCheck: () =>
    api.get<WatchlistEntry[]>('/watchlist/requiring-check').then(res => {
      const result = validateApiResponse(
        res.data,
        z.array(watchlistEntrySchema),
        'GET /watchlist/requiring-check'
      );
      return result.data as WatchlistEntry[];
    }),

  getStatistics: () =>
    api.get('/watchlist/statistics').then(res => res.data),

  getHistory: (id: number) =>
    api.get<{ id: number; oldRiskLevel: string | null; newRiskLevel: string | null; changedBy: number; changedByLogin: string; changedAt: string; comment: string | null }[]>(`/watchlist/${id}/history`).then(res => res.data),
};

// FAQ API
export const faqApi = {
  getAll: () =>
    api.get<FAQ[]>('/faq').then(res => res.data),

  getById: (id: number) =>
    api.get<FAQ>(`/faq/${id}`).then(res => res.data),

  create: (data: Partial<FAQ>) =>
    api.post<{ id: number }>('/faq', data).then(res => res.data),

  update: (id: number, data: Partial<FAQ>) =>
    api.put(`/faq/${id}`, data).then(res => res.data),

  delete: (id: number) =>
    api.delete(`/faq/${id}`).then(res => res.data),
};

// References API
export const referencesApi = {
  getAll: (category?: string) =>
    api.get<ReferenceValue[]>('/references', { params: { category } }).then(res => {
      const result = validateApiResponse(
        res.data,
        z.array(referenceValueSchema),
        'GET /references'
      );
      return result.data as ReferenceValue[];
    }),

  getCategories: () =>
    api.get<string[]>('/references/categories').then(res => res.data),

  getByCategory: () =>
    api.get<Record<string, ReferenceValue[]>>('/references/by-category').then(res => res.data),

  create: (data: { category: string; code: string; value: string; description?: string; order: number }) =>
    api.post<ReferenceValue>('/references', data).then(res => res.data),

  update: (id: number, data: { value: string; description?: string; order: number; isActive: boolean }) =>
    api.put(`/references/${id}`, data).then(res => res.data),

  deactivate: (id: number) =>
    api.put(`/references/${id}/deactivate`).then(res => res.data),

  toggleActive: (id: number) =>
    api.post<{ isActive: boolean }>(`/references/${id}/toggle`).then(res => res.data),
};

// Audit Log API
export const auditLogApi = {
  getAll: (params?: {
    userId?: number;
    blockId?: number;
    actionType?: string;
    entityType?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }) =>
    api.get<PaginatedResponse<AuditLogEntry>>('/audit-log', { params }).then(res => res.data),

  getStatistics: (params?: { fromDate?: string; toDate?: string }) =>
    api.get('/audit-log/statistics', { params }).then(res => res.data),

  getByEntity: (entityType: string, entityId: string) =>
    api.get<AuditLogEntry[]>(`/audit-log/entity/${entityType}/${entityId}`).then(res => res.data),

  getByUser: (userId: number, params?: { page?: number; pageSize?: number }) =>
    api.get<PaginatedResponse<AuditLogEntry>>(`/audit-log/user/${userId}`, { params }).then(res => res.data),

  getRecent: (count?: number) =>
    api.get<AuditLogEntry[]>('/audit-log/recent', { params: { count } }).then(res => res.data),
};

// Dashboard API
export const dashboardApi = {
  getCuratorDashboard: () =>
    withValidation(
      api.get<CuratorDashboard>('/dashboard/curator').then(res => res.data),
      curatorDashboardSchema as z.ZodType<CuratorDashboard>,
      'GET /dashboard/curator'
    ),

  getAdminDashboard: () =>
    withValidation(
      api.get<AdminDashboard>('/dashboard/admin').then(res => res.data),
      adminDashboardSchema as z.ZodType<AdminDashboard>,
      'GET /dashboard/admin'
    ),

  getStatistics: (params?: { fromDate?: string; toDate?: string; blockId?: number }) =>
    api.get('/dashboard/statistics', { params }).then(res => res.data),
};

// Export the api instance for direct usage
export { api };
