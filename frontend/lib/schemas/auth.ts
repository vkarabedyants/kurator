/**
 * Authentication Zod schemas
 */
import { z } from 'zod';

// User roles enum
export const userRoleSchema = z.enum(['Admin', 'Curator', 'ThreatAnalyst']);
export type UserRole = z.infer<typeof userRoleSchema>;

// User schema
export const userSchema = z.object({
  id: z.number().int().positive(),
  login: z.string().min(1),
  role: userRoleSchema.or(z.string()), // Allow string for backwards compatibility
  lastLoginAt: z.string().optional().nullable(),
  createdAt: z.string(),
  isActive: z.boolean().optional(),
  isFirstLogin: z.boolean().optional(),
  mfaEnabled: z.boolean().optional(),
  primaryBlockIds: z.array(z.number()).optional(),
  primaryBlockNames: z.array(z.string()).optional(),
  backupBlockIds: z.array(z.number()).optional(),
  backupBlockNames: z.array(z.string()).optional(),
});

export type User = z.infer<typeof userSchema>;

// Login request schema
export const loginRequestSchema = z.object({
  login: z.string().min(1, 'Login is required'),
  password: z.string().min(1, 'Password is required'),
});

export type LoginRequest = z.infer<typeof loginRequestSchema>;

// Login response schema
export const loginResponseSchema = z.object({
  token: z.string().optional(),
  user: z.object({
    id: z.number().int().positive(),
    login: z.string(),
    role: z.string(),
    isFirstLogin: z.boolean().optional(),
    mfaEnabled: z.boolean().optional(),
  }).optional(),
  requireMfaSetup: z.boolean().optional(),
  requireMfaVerification: z.boolean().optional(),
  userId: z.number().optional(),
  login: z.string().optional(),
  message: z.string().optional(),
});

export type LoginResponse = z.infer<typeof loginResponseSchema>;

// Token response schema (for refresh token)
export const tokenResponseSchema = z.object({
  token: z.string(),
  user: z.object({
    id: z.number().int().positive(),
    login: z.string(),
    role: z.string(),
  }).optional(),
});

export type TokenResponse = z.infer<typeof tokenResponseSchema>;

// Setup MFA request
export const setupMfaRequestSchema = z.object({
  userId: z.number().int().positive(),
  password: z.string().min(1),
  publicKey: z.string().optional(),
});

export type SetupMfaRequest = z.infer<typeof setupMfaRequestSchema>;

// Setup MFA response
export const setupMfaResponseSchema = z.object({
  mfaSecret: z.string(),
  qrCodeUrl: z.string(),
  message: z.string(),
});

export type SetupMfaResponse = z.infer<typeof setupMfaResponseSchema>;

// Verify MFA request
export const verifyMfaRequestSchema = z.object({
  userId: z.number().int().positive(),
  totpCode: z.string().length(6),
});

export type VerifyMfaRequest = z.infer<typeof verifyMfaRequestSchema>;

// Verify MFA response
export const verifyMfaResponseSchema = z.object({
  token: z.string(),
  user: z.object({
    id: z.number().int().positive(),
    login: z.string(),
    role: z.string(),
    isFirstLogin: z.boolean(),
    mfaEnabled: z.boolean(),
  }),
  message: z.string(),
});

export type VerifyMfaResponse = z.infer<typeof verifyMfaResponseSchema>;
