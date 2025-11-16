// API Response Types
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
  message?: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

// User Types
export enum UserRole {
  Admin = 'Admin',
  Curator = 'Curator',
  BackupCurator = 'BackupCurator',
  ThreatAnalyst = 'ThreatAnalyst'
}

export interface User {
  id: number;
  login: string;
  role: UserRole;
  lastLoginAt?: string;
  createdAt: string;
}

export interface LoginRequest {
  login: string;
  password: string;
}

export interface LoginResponse {
  token?: string;
  user?: {
    id: number;
    login: string;
    role: string;
    isFirstLogin?: boolean;
    mfaEnabled?: boolean;
  };
  requireMfaSetup?: boolean;
  requireMfaVerification?: boolean;
  userId?: number;
  login?: string;
  message?: string;
}

export interface AuthResponse {
  token: string;
  user: {
    id: number;
    login: string;
    role: string;
  };
}

export interface SetupMfaRequest {
  userId: number;
  password: string;
  publicKey?: string;
}

export interface SetupMfaResponse {
  mfaSecret: string;
  qrCodeUrl: string;
  message: string;
}

export interface VerifyMfaRequest {
  userId: number;
  totpCode: string;
}

export interface VerifyMfaResponse {
  token: string;
  user: {
    id: number;
    login: string;
    role: string;
    isFirstLogin: boolean;
    mfaEnabled: boolean;
  };
  message: string;
}

// Block Types
export enum BlockStatus {
  Active = 'Active',
  Archived = 'Archived'
}

export interface Block {
  id: number;
  name: string;
  description?: string;
  code: string;
  status: BlockStatus;
  primaryCuratorId?: number;
  backupCuratorId?: number;
  createdAt: string;
  updatedAt: string;
  primaryCurator?: User;
  backupCurator?: User;
}

// Contact Types
export enum InfluenceStatus {
  A = 'A',
  B = 'B',
  C = 'C',
  D = 'D'
}

export enum InfluenceType {
  Navigational = 'Navigational',
  Interpretational = 'Interpretational',
  Functional = 'Functional',
  Reputational = 'Reputational',
  Analytical = 'Analytical'
}

export enum CommunicationChannel {
  Official = 'Official',
  ThroughIntermediary = 'ThroughIntermediary',
  ThroughAssociation = 'ThroughAssociation',
  Personal = 'Personal',
  Legal = 'Legal'
}

export enum ContactSource {
  PersonalAcquaintance = 'PersonalAcquaintance',
  Association = 'Association',
  Recommendation = 'Recommendation',
  Event = 'Event',
  Media = 'Media',
  Other = 'Other'
}

export interface Contact {
  id: number;
  contactId: string;
  fullName: string;
  blockId: number;
  blockName: string;
  blockCode: string;
  organizationId?: string;
  position?: string;
  influenceStatus: InfluenceStatus;
  influenceType: InfluenceType;
  usefulnessDescription?: string;
  communicationChannelId?: string;
  contactSourceId?: string;
  lastInteractionDate?: string;
  nextTouchDate?: string;
  notes?: string;
  responsibleCuratorId: number;
  responsibleCuratorLogin: string;
  createdAt: string;
  updatedAt: string;
  updatedBy: string;
  interactionCount?: number;
  lastInteractionDaysAgo?: number;
  isOverdue: boolean;
}

export interface ContactListItem {
  id: number;
  contactId: string;
  fullName: string;
  blockId: number;
  blockName: string;
  blockCode: string;
  organizationId?: string;
  position?: string;
  influenceStatus: string;
  influenceType: string;
  lastInteractionDate?: string;
  nextTouchDate?: string;
  responsibleCuratorId: number;
  responsibleCuratorLogin: string;
  updatedAt: string;
  updatedBy: string;
  isOverdue: boolean;
}

export interface ContactDetail extends ContactListItem {
  usefulnessDescription?: string;
  communicationChannelId?: string;
  contactSourceId?: string;
  notes?: string;
  createdAt: string;
  interactionCount: number;
  lastInteractionDaysAgo?: number;
  interactions: InteractionSummary[];
  statusHistory: StatusHistoryItem[];
}

export interface CreateContactRequest {
  blockId: number;
  fullName: string;
  organizationId?: string;
  position?: string;
  influenceStatus: InfluenceStatus;
  influenceType: InfluenceType;
  usefulnessDescription?: string;
  communicationChannelId?: string;
  contactSourceId?: string;
  nextTouchDate?: string;
  notes?: string;
}

export interface UpdateContactRequest {
  organizationId?: string;
  position?: string;
  influenceStatus: InfluenceStatus;
  influenceType: InfluenceType;
  usefulnessDescription?: string;
  communicationChannelId?: string;
  contactSourceId?: string;
  nextTouchDate?: string;
  notes?: string;
}

// Interaction Types
export enum InteractionType {
  Meeting = 'Meeting',
  Call = 'Call',
  Correspondence = 'Correspondence',
  Event = 'Event',
  Other = 'Other'
}

export enum InteractionResult {
  Positive = 'Positive',
  Neutral = 'Neutral',
  Negative = 'Negative',
  Postponed = 'Postponed',
  NoResult = 'NoResult'
}

export interface Interaction {
  id: number;
  contactId: number;
  interactionDate: string;
  interactionTypeId: string;
  curatorId: number;
  curatorLogin: string;
  resultId: string;
  comment?: string;
  statusChangeFrom?: string;
  statusChangeTo?: string;
  nextTouchDate?: string;
  attachmentUrl?: string;
  createdAt: string;
  updatedAt: string;
  updatedBy: string;
}

export interface InteractionSummary {
  id: number;
  interactionDate: string;
  interactionTypeId: string;
  resultId: string;
  comment?: string;
  statusChangeTo?: string;
  curatorLogin: string;
}

export interface CreateInteractionRequest {
  contactId: number;
  interactionDate?: string;
  interactionTypeId: string;
  resultId: string;
  comment?: string;
  statusChangeTo?: InfluenceStatus;
  nextTouchDate?: string;
}

// Status History Types
export interface StatusHistoryItem {
  id: number;
  oldStatus: string;
  newStatus: string;
  changedAt: string;
  changedBy: string;
}

// Watchlist Types
export enum RiskLevel {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

export enum MonitoringFrequency {
  Weekly = 'Weekly',
  Monthly = 'Monthly',
  Quarterly = 'Quarterly',
  AdHoc = 'AdHoc'
}

export enum ThreatSphere {
  Media = 'Media',
  Legal = 'Legal',
  Political = 'Political',
  Economic = 'Economic',
  Security = 'Security',
  Communication = 'Communication',
  Other = 'Other'
}

export interface WatchlistEntry {
  id: number;
  nameOrAlias: string;
  roleOrStatus?: string;
  riskSphere?: string;
  threatSource?: string;
  conflictStartDate?: string;
  riskLevel: RiskLevel;
  monitoringFrequency: MonitoringFrequency;
  lastCheckDate?: string;
  nextCheckDate?: string;
  progressDynamics?: string;
  watchOwnerId?: number;
  watchOwnerLogin?: string;
  attachedMaterials?: string;
  createdAt: string;
  updatedAt: string;
  updatedBy: string;
}

// FAQ Types
export enum FAQVisibility {
  All = 'All',
  CuratorsOnly = 'CuratorsOnly',
  AdminOnly = 'AdminOnly'
}

export interface FAQ {
  id: number;
  title: string;
  content: string;
  visibility: FAQVisibility;
  order: number;
  createdAt: string;
  updatedAt: string;
}

// Reference Value Types
export interface ReferenceValue {
  id: number;
  category: string;
  value: string;
  displayName?: string;
  description?: string;
  isActive: boolean;
  displayOrder?: number;
}

// Audit Log Types
export enum AuditActionType {
  Create = 'Create',
  Update = 'Update',
  Delete = 'Delete',
  View = 'View',
  Login = 'Login',
  ChangeStatus = 'ChangeStatus',
  CreateContact = 'CreateContact',
  UpdateContact = 'UpdateContact',
  DeleteContact = 'DeleteContact',
  CreateInteraction = 'CreateInteraction',
  UpdateInteraction = 'UpdateInteraction',
  DeleteInteraction = 'DeleteInteraction',
  ChangeInfluenceStatus = 'ChangeInfluenceStatus',
  CreateBlock = 'CreateBlock',
  UpdateBlock = 'UpdateBlock',
  DeleteBlock = 'DeleteBlock',
  CreateUser = 'CreateUser',
  UpdateUser = 'UpdateUser',
  DeleteUser = 'DeleteUser',
  Logout = 'Logout'
}

export interface AuditLogEntry {
  id: number;
  userId: number;
  userLogin: string;
  actionType: AuditActionType;
  entityType: string;
  entityId?: string;
  timestamp: string;
  oldValue?: string;
  newValue?: string;
}

// Dashboard Types
export interface RecentInteraction {
  id: number;
  contactName: string;
  contactId: string;
  interactionDate: string;
  interactionTypeId: string;
  resultId: string;
}

export interface AttentionContact {
  id: number;
  contactId: string;
  fullName: string;
  nextTouchDate?: string;
  daysOverdue: number;
  influenceStatus: string;
}

export interface CuratorDashboard {
  totalContacts: number;
  interactionsLastMonth: number;
  averageInteractionInterval: number;
  overdueContacts: number;
  recentInteractions: RecentInteraction[];
  contactsRequiringAttention: AttentionContact[];
  contactsByInfluenceStatus: Record<string, number>;
  interactionsByType: Record<string, number>;
}

export interface AuditLogSummary {
  id: number;
  userLogin: string;
  actionType: string;
  entityType: string;
  timestamp: string;
}

export interface AdminDashboard {
  totalContacts: number;
  totalInteractions: number;
  totalBlocks: number;
  totalUsers: number;
  newContactsLastMonth: number;
  interactionsLastMonth: number;
  contactsByBlock: Record<string, number>;
  contactsByInfluenceStatus: Record<string, number>;
  contactsByInfluenceType: Record<string, number>;
  interactionsByBlock: Record<string, number>;
  topCuratorsByActivity: Record<string, number>;
  statusChangeDynamics: Record<string, number>;
  recentAuditLogs: AuditLogSummary[];
}

// Additional DTOs for Create/Update operations
export interface CreateContactDto {
  fullName: string;
  organization: string;
  position: string;
  influenceStatus: InfluenceStatus;
  influenceType: InfluenceType;
  howCanHelp?: string;
  communicationChannel: CommunicationChannel;
  contactSource: ContactSource;
  nextTouchDate?: string;
  notes?: string;
}

export interface UpdateContactDto extends CreateContactDto {}

export interface ContactDto extends ContactDetail {
  organization: string;
}

export interface CreateInteractionDto {
  contactId: number;
  interactionDate: string;
  interactionType: InteractionType;
  result: InteractionResult;
  comment: string;
  nextTouchDate?: string;
  influenceStatusChange?: InfluenceStatus | null;
}

export interface UpdateInteractionDto {
  interactionDate: string;
  interactionType: InteractionType;
  result: InteractionResult;
  comment: string;
  nextTouchDate?: string;
  influenceStatusChange?: InfluenceStatus | null;
}

export interface InteractionDto extends Interaction {
  curatorName: string;
}

export interface WatchlistDto extends WatchlistEntry {
  watchOwnerName?: string;
  fullNameOrAlias: string;
  threatSource: string;
  threatSphere: ThreatSphere;
  progressDynamics?: string;
  conflictStartDate: string;
  lastCheckDate?: string;
  nextCheckDate: string;
}

export interface FAQDto extends FAQ {
  category?: string;
}

export interface AuditLogDto extends AuditLogEntry {
  details?: string;
  userName: string;
  actionType: AuditActionType;
  timestamp: string;
  entityType: string;
  oldValue?: string;
  newValue?: string;
}