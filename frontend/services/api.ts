import api from '@/lib/api';
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

// Auth API
export const authApi = {
  login: (data: LoginRequest) =>
    api.post<LoginResponse>('/auth/login', data).then(res => res.data),

  register: (data: { login: string; password: string; role: string }) =>
    api.post('/auth/register', data).then(res => res.data),

  setupMfa: (data: SetupMfaRequest) =>
    api.post<SetupMfaResponse>('/auth/setup-mfa', data).then(res => res.data),

  verifyMfa: (data: VerifyMfaRequest) =>
    api.post<VerifyMfaResponse>('/auth/verify-mfa', data).then(res => res.data),

  logout: () =>
    api.post('/auth/logout').then(res => res.data),
};

// Contacts API
export const contactsApi = {
  getAll: (params?: {
    blockId?: number;
    search?: string;
    influenceStatus?: string;
    influenceType?: string;
    organizationId?: string;
    page?: number;
    pageSize?: number;
  }) =>
    api.get<PaginatedResponse<ContactListItem>>('/contacts', { params }).then(res => res.data),

  getById: (id: number) =>
    api.get<ContactDetail>(`/contacts/${id}`).then(res => res.data),

  create: (data: CreateContactRequest) =>
    api.post<{ id: number; contactId: string }>('/contacts', data).then(res => res.data),

  update: (id: number, data: UpdateContactRequest) =>
    api.put(`/contacts/${id}`, data).then(res => res.data),

  delete: (id: number) =>
    api.delete(`/contacts/${id}`).then(res => res.data),

  getOverdue: () =>
    api.get<ContactListItem[]>('/contacts/overdue').then(res => res.data),
};

// Interactions API
export const interactionsApi = {
  getAll: (params?: {
    contactId?: number;
    blockId?: number;
    fromDate?: string;
    toDate?: string;
    interactionTypeId?: string;
    resultId?: string;
    page?: number;
    pageSize?: number;
  }) =>
    api.get<PaginatedResponse<Interaction>>('/interactions', { params }).then(res => res.data),

  getById: (id: number) =>
    api.get<Interaction>(`/interactions/${id}`).then(res => res.data),

  create: (data: CreateInteractionRequest) =>
    api.post<{ id: number }>('/interactions', data).then(res => res.data),

  update: (id: number, data: Partial<CreateInteractionRequest>) =>
    api.put(`/interactions/${id}`, data).then(res => res.data),

  delete: (id: number) =>
    api.delete(`/interactions/${id}`).then(res => res.data),
};

// Blocks API
export const blocksApi = {
  getAll: () =>
    api.get<Block[]>('/blocks').then(res => res.data),

  getById: (id: number) =>
    api.get<Block>(`/blocks/${id}`).then(res => res.data),

  create: (data: Partial<Block>) =>
    api.post<{ id: number }>('/blocks', data).then(res => res.data),

  update: (id: number, data: Partial<Block>) =>
    api.put(`/blocks/${id}`, data).then(res => res.data),

  delete: (id: number) =>
    api.delete(`/blocks/${id}`).then(res => res.data),
};

// Users API
export const usersApi = {
  getAll: () =>
    api.get<User[]>('/users').then(res => res.data),

  getById: (id: number) =>
    api.get<User>(`/users/${id}`).then(res => res.data),

  create: (data: { login: string; password: string; role: string }) =>
    api.post<{ id: number }>('/users', data).then(res => res.data),

  update: (id: number, data: Partial<User>) =>
    api.put(`/users/${id}`, data).then(res => res.data),

  delete: (id: number) =>
    api.delete(`/users/${id}`).then(res => res.data),

  changePassword: (id: number, data: { currentPassword: string; newPassword: string }) =>
    api.post(`/users/${id}/change-password`, data).then(res => res.data),
};

// Watchlist API
export const watchlistApi = {
  getAll: (params?: {
    riskLevel?: string;
    monitoringFrequency?: string;
    page?: number;
    pageSize?: number;
  }) =>
    api.get<PaginatedResponse<WatchlistEntry>>('/watchlist', { params }).then(res => res.data),

  getById: (id: number) =>
    api.get<WatchlistEntry>(`/watchlist/${id}`).then(res => res.data),

  create: (data: Partial<WatchlistEntry>) =>
    api.post<{ id: number }>('/watchlist', data).then(res => res.data),

  update: (id: number, data: Partial<WatchlistEntry>) =>
    api.put(`/watchlist/${id}`, data).then(res => res.data),

  delete: (id: number) =>
    api.delete(`/watchlist/${id}`).then(res => res.data),

  getOverdue: () =>
    api.get<WatchlistEntry[]>('/watchlist/overdue').then(res => res.data),
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
  getByCategory: (category: string) =>
    api.get<ReferenceValue[]>(`/references/${category}`).then(res => res.data),

  create: (data: Partial<ReferenceValue>) =>
    api.post<{ id: number }>('/references', data).then(res => res.data),

  update: (id: number, data: Partial<ReferenceValue>) =>
    api.put(`/references/${id}`, data).then(res => res.data),

  delete: (id: number) =>
    api.delete(`/references/${id}`).then(res => res.data),
};

// Audit Log API
export const auditLogApi = {
  getAll: (params?: {
    userId?: number;
    actionType?: string;
    entityType?: string;
    fromDate?: string;
    toDate?: string;
    page?: number;
    pageSize?: number;
  }) =>
    api.get<PaginatedResponse<AuditLogEntry>>('/audit-log', { params }).then(res => res.data),
};

// Dashboard API
export const dashboardApi = {
  getCuratorDashboard: () =>
    api.get<CuratorDashboard>('/dashboard/curator').then(res => res.data),

  getAdminDashboard: () =>
    api.get<AdminDashboard>('/dashboard/admin').then(res => res.data),
};

// Export the default api instance for direct usage
export { default as api } from '@/lib/api';