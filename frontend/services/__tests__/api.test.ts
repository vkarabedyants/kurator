// Mock the api module before imports
jest.mock('@/lib/api', () => {
  const mockApi = {
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
    patch: jest.fn(),
  };
  return {
    __esModule: true,
    default: mockApi,
  };
});

import api from '@/lib/api';
import {
  authApi,
  contactsApi,
  interactionsApi,
  blocksApi,
  usersApi,
  dashboardApi,
  referencesApi,
  auditLogApi,
} from '../api';

const mockApi = api as jest.Mocked<typeof api>;

describe('API Services', () => {

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Auth API', () => {
    it('should login with credentials', async () => {
      const loginData = { login: 'admin', password: 'admin123' };
      const mockResponse = {
        data: {
          token: 'jwt-token',
          user: { id: 1, login: 'admin', role: 'Admin' },
        },
      };

      mockApi.post.mockResolvedValue(mockResponse);

      const result = await authApi.login(loginData);

      expect(mockApi.post).toHaveBeenCalledWith('/auth/login', loginData);
      expect(result).toEqual(mockResponse.data);
    });

    it('should setup MFA', async () => {
      const mfaData = { userId: 1, password: 'password' };
      const mockResponse = {
        data: {
          qrCodeUrl: 'data:image/png;base64,...',
          mfaSecret: 'ABCD1234',
          message: 'MFA setup successful',
        },
      };

      mockApi.post.mockResolvedValue(mockResponse);

      const result = await authApi.setupMfa(mfaData);

      expect(mockApi.post).toHaveBeenCalledWith('/auth/setup-mfa', mfaData);
      expect(result).toEqual(mockResponse.data);
    });

    it('should verify MFA', async () => {
      const verifyData = { userId: 1, totpCode: '123456' };
      const mockResponse = {
        data: {
          token: 'jwt-token',
          user: { id: 1, login: 'admin', role: 'Admin', isFirstLogin: false, mfaEnabled: true },
          message: 'MFA verified',
        },
      };

      mockApi.post.mockResolvedValue(mockResponse);

      const result = await authApi.verifyMfa(verifyData);

      expect(mockApi.post).toHaveBeenCalledWith('/auth/verify-mfa', verifyData);
      expect(result).toEqual(mockResponse.data);
    });

    it('should logout', async () => {
      const mockResponse = { data: { message: 'Logged out' } };

      mockApi.post.mockResolvedValue(mockResponse);

      const result = await authApi.logout();

      expect(mockApi.post).toHaveBeenCalledWith('/auth/logout');
      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('Contacts API', () => {
    it('should get all contacts with pagination', async () => {
      const mockResponse = {
        data: {
          data: [
            { id: 1, contactId: 'CNT-001', fullName: 'John Doe' },
          ],
          total: 1,
          page: 1,
          pageSize: 20,
          totalPages: 1,
        },
      };

      mockApi.get.mockResolvedValue(mockResponse);

      const result = await contactsApi.getAll({ page: 1, pageSize: 20 });

      expect(mockApi.get).toHaveBeenCalledWith('/contacts', {
        params: { page: 1, pageSize: 20 },
      });
      expect(result).toEqual(mockResponse.data);
    });

    it('should get contact by id', async () => {
      const mockResponse = {
        data: {
          id: 1,
          contactId: 'CNT-001',
          fullName: 'John Doe',
          blockId: 1,
          blockName: 'Block 1',
          blockCode: 'BLK1',
          organizationId: 'ORG-001',
          position: 'Director',
          influenceStatus: 'A',
          influenceType: 'Navigational',
          responsibleCuratorId: 1,
          responsibleCuratorLogin: 'curator',
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
          updatedBy: 'curator',
          isOverdue: false,
          interactionCount: 5,
          interactions: [],
          statusHistory: [],
        },
      };

      mockApi.get.mockResolvedValue(mockResponse);

      const result = await contactsApi.getById(1);

      expect(mockApi.get).toHaveBeenCalledWith('/contacts/1');
      expect(result).toEqual(mockResponse.data);
    });

    it('should create new contact', async () => {
      const contactData = {
        blockId: 1,
        fullName: 'John Doe',
        organizationId: 'ORG-001',
        position: 'Director',
        influenceStatus: 'A' as const,
        influenceType: 'Navigational' as const,
      };
      const mockResponse = { data: { id: 1, contactId: 'CNT-001' } };

      mockApi.post.mockResolvedValue(mockResponse);

      const result = await contactsApi.create(contactData);

      expect(mockApi.post).toHaveBeenCalledWith('/contacts', contactData);
      expect(result).toEqual(mockResponse.data);
    });

    it('should update contact', async () => {
      const updateData = {
        organizationId: 'ORG-002',
        position: 'CEO',
        influenceStatus: 'A' as const,
        influenceType: 'Navigational' as const,
      };
      const mockResponse = { data: { success: true } };

      mockApi.put.mockResolvedValue(mockResponse);

      const result = await contactsApi.update(1, updateData);

      expect(mockApi.put).toHaveBeenCalledWith('/contacts/1', updateData);
      expect(result).toEqual(mockResponse.data);
    });

    it('should delete contact', async () => {
      const mockResponse = { data: { success: true } };

      mockApi.delete.mockResolvedValue(mockResponse);

      const result = await contactsApi.delete(1);

      expect(mockApi.delete).toHaveBeenCalledWith('/contacts/1');
      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('Interactions API', () => {
    it('should get all interactions', async () => {
      const mockResponse = {
        data: {
          data: [
            {
              id: 1,
              contactId: 1,
              interactionDate: '2024-01-01',
              interactionTypeId: 'Meeting',
              curatorId: 1,
              curatorLogin: 'curator',
              resultId: 'Positive',
              createdAt: '2024-01-01T00:00:00Z',
              updatedAt: '2024-01-01T00:00:00Z',
              updatedBy: 'curator',
            },
          ],
          total: 1,
          page: 1,
          pageSize: 20,
          totalPages: 1,
        },
      };

      mockApi.get.mockResolvedValue(mockResponse);

      const result = await interactionsApi.getAll({ page: 1, pageSize: 20 });

      expect(mockApi.get).toHaveBeenCalledWith('/interactions', {
        params: { page: 1, pageSize: 20 },
      });
      expect(result).toEqual(mockResponse.data);
    });

    it('should create interaction', async () => {
      const interactionData = {
        contactId: 1,
        interactionDate: '2024-01-01',
        interactionTypeId: 'Meeting',
        resultId: 'Positive',
      };
      const mockResponse = { data: { id: 1 } };

      mockApi.post.mockResolvedValue(mockResponse);

      const result = await interactionsApi.create(interactionData);

      expect(mockApi.post).toHaveBeenCalledWith('/interactions', interactionData);
      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('Blocks API', () => {
    it('should get all blocks', async () => {
      const mockResponse = {
        data: [
          {
            id: 1,
            name: 'Block 1',
            code: 'BLK1',
            status: 'Active',
            createdAt: '2024-01-01T00:00:00Z',
            updatedAt: '2024-01-01T00:00:00Z',
          },
        ],
      };

      mockApi.get.mockResolvedValue(mockResponse);

      const result = await blocksApi.getAll();

      expect(mockApi.get).toHaveBeenCalledWith('/blocks');
      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('Users API', () => {
    it('should get all users', async () => {
      const mockResponse = {
        data: [
          {
            id: 1,
            login: 'admin',
            role: 'Admin',
            createdAt: '2024-01-01T00:00:00Z',
          },
        ],
      };

      mockApi.get.mockResolvedValue(mockResponse);

      const result = await usersApi.getAll();

      expect(mockApi.get).toHaveBeenCalledWith('/users');
      expect(result).toEqual(mockResponse.data);
    });

    it('should create user', async () => {
      const userData = {
        login: 'newuser',
        password: 'password123',
        role: 'Curator',
      };
      const mockResponse = { data: { id: 2 } };

      mockApi.post.mockResolvedValue(mockResponse);

      const result = await usersApi.create(userData);

      expect(mockApi.post).toHaveBeenCalledWith('/users', userData);
      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('Dashboard API', () => {
    it('should get curator dashboard', async () => {
      const mockResponse = {
        data: {
          totalContacts: 10,
          interactionsLastMonth: 5,
          averageInteractionInterval: 30,
          overdueContacts: 2,
          recentInteractions: [],
          contactsRequiringAttention: [],
          contactsByInfluenceStatus: {},
          interactionsByType: {},
        },
      };

      mockApi.get.mockResolvedValue(mockResponse);

      const result = await dashboardApi.getCuratorDashboard();

      expect(mockApi.get).toHaveBeenCalledWith('/dashboard/curator');
      expect(result).toEqual(mockResponse.data);
    });

    it('should get admin dashboard', async () => {
      const mockResponse = {
        data: {
          totalContacts: 100,
          totalInteractions: 50,
          totalBlocks: 5,
          totalUsers: 10,
          newContactsLastMonth: 10,
          interactionsLastMonth: 20,
          contactsByBlock: {},
          contactsByInfluenceStatus: {},
          contactsByInfluenceType: {},
          interactionsByBlock: {},
          topCuratorsByActivity: {},
          statusChangeDynamics: {},
          recentAuditLogs: [],
        },
      };

      mockApi.get.mockResolvedValue(mockResponse);

      const result = await dashboardApi.getAdminDashboard();

      expect(mockApi.get).toHaveBeenCalledWith('/dashboard/admin');
      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('References API', () => {
    it('should get references by category', async () => {
      const mockResponse = {
        data: [
          {
            id: 1,
            category: 'InteractionType',
            value: 'Meeting',
            displayName: 'Встреча',
            isActive: true,
          },
        ],
      };

      mockApi.get.mockResolvedValue(mockResponse);

      const result = await referencesApi.getByCategory();

      expect(mockApi.get).toHaveBeenCalledWith('/references/by-category');
      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('Audit Log API', () => {
    it('should get all audit logs', async () => {
      const mockResponse = {
        data: {
          data: [
            {
              id: 1,
              userId: 1,
              userLogin: 'admin',
              actionType: 'Create',
              entityType: 'Contact',
              entityId: 'CNT-001',
              timestamp: '2024-01-01T00:00:00Z',
            },
          ],
          total: 1,
          page: 1,
          pageSize: 20,
          totalPages: 1,
        },
      };

      mockApi.get.mockResolvedValue(mockResponse);

      const result = await auditLogApi.getAll({ page: 1, pageSize: 20 });

      expect(mockApi.get).toHaveBeenCalledWith('/audit-log', {
        params: { page: 1, pageSize: 20 },
      });
      expect(result).toEqual(mockResponse.data);
    });
  });
});
