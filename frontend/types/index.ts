export interface User {
  id: number;
  login: string;
  role: 'Admin' | 'Curator' | 'ThreatAnalyst';
  createdAt: string;
  lastLoginAt?: string;
}

export interface Block {
  id: number;
  name: string;
  code: string;
  description?: string;
  status: 'Active' | 'Archived';
  primaryCuratorId?: number;
  backupCuratorId?: number;
  primaryCuratorLogin?: string;
  backupCuratorLogin?: string;
}

export interface Contact {
  id: number;
  contactId: string;
  blockId: number;
  fullName: string;
  organization?: string;
  position?: string;
  influenceStatus: 'A' | 'B' | 'C' | 'D';
  influenceType: InfluenceType;
  usefulnessDescription?: string;
  communicationChannel?: string;
  contactSource?: string;
  lastInteractionDate?: string;
  nextTouchDate?: string;
  notes?: string;
  responsibleCuratorId: number;
  createdAt: string;
  updatedAt: string;
  updatedBy: string;
}

export type InfluenceType = 
  | 'Navigational'
  | 'Interpretational'
  | 'Functional'
  | 'Reputational'
  | 'Analytical';

export interface Interaction {
  id: number;
  contactId: number;
  interactionDate: string;
  interactionType: string;
  curatorId: number;
  result: string;
  comment?: string;
  statusChangeTo?: string;
  attachmentPath?: string;
  nextTouchDate?: string;
  createdAt: string;
  updatedAt: string;
  updatedBy: string;
}

export interface InfluenceStatusHistory {
  id: number;
  contactId: number;
  previousStatus: string;
  newStatus: string;
  changedByUserId: number;
  changedAt: string;
}

export interface AuditLog {
  id: number;
  userId: number;
  actionType: string;
  entityType: string;
  entityId?: string;
  oldValue?: string;
  newValue?: string;
  timestamp: string;
}

export interface Watchlist {
  id: number;
  fullName: string;
  roleStatus: string;
  riskSphere: string;
  threatSource: string;
  conflictDate: string;
  riskLevel: 'Low' | 'Medium' | 'High' | 'Critical';
  monitoringFrequency: 'Weekly' | 'Monthly' | 'Quarterly' | 'AdHoc';
  lastCheckDate?: string;
  nextCheckDate?: string;
  dynamicsDescription?: string;
  watchOwnerId?: number;
  attachedMaterials?: string;
  createdAt: string;
  updatedAt: string;
  updatedBy: string;
}

export interface FAQ {
  id: number;
  title: string;
  content: string;
  visibility: 'All' | 'CuratorsOnly' | 'AdminOnly';
  displayOrder: number;
  updatedAt: string;
  updatedBy: string;
}

export interface ReferenceValue {
  id: string;
  category: string;
  name: string;
  description?: string;
  displayOrder: number;
  isActive: boolean;
}

export interface LoginRequest {
  login: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: {
    id: number;
    login: string;
    role: string;
  };
}
