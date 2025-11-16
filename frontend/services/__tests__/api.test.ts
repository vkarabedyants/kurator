import axios from 'axios';
import { authApi, contactsApi, interactionsApi, blocksApi, usersApi } from '../api';

// Mock the entire lib/api module
jest.mock('@/lib/api', () => ({
  __esModule: true,
  default: {
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
  }
}));

import api from '@/lib/api';
const mockedApi = api as jest.Mocked<typeof api>;

describe('API Services', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('authApi', () => {
    it('should call login endpoint with correct data', async () => {
      const mockResponse = {
        data: {
          token: 'test-token',
          user: { id: 1, login: 'testuser', role: 'Curator' }
        }
      };

      mockedApi.post.mockResolvedValue(mockResponse);

      const loginData = { login: 'testuser', password: 'password123' };
      const result = await authApi.login(loginData);

      expect(result).toEqual(mockResponse.data);
    });

    it('should call register endpoint with correct data', async () => {
      const mockResponse = { data: { message: 'User registered successfully' } };
      mockedApi.post.mockResolvedValue(mockResponse);

      const registerData = { login: 'newuser', password: 'password123', role: 'Curator' };
      const result = await authApi.register(registerData);

      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('contactsApi', () => {
    it('should fetch all contacts with pagination', async () => {
      const mockResponse = {
        data: {
          items: [
            { id: 1, contactId: 'OP-001', fullName: 'Test User' }
          ],
          total: 1,
          page: 1,
          pageSize: 50
        }
      };

      mockedApi.get.mockResolvedValue(mockResponse);

      const result = await contactsApi.getAll({ page: 1, pageSize: 50 });

      expect(result).toEqual(mockResponse.data);
    });

    it('should create a contact with correct data', async () => {
      const mockResponse = { data: { id: 1, contactId: 'OP-001' } };
      mockedApi.post.mockResolvedValue(mockResponse);

      const contactData = {
        blockId: 1,
        fullName: 'Test User',
        position: 'Director'
      };
      const result = await contactsApi.create(contactData as any);

      expect(result).toEqual(mockResponse.data);
    });

    it('should update a contact', async () => {
      const mockResponse = { data: { success: true } };
      mockedApi.put.mockResolvedValue(mockResponse);

      const updateData = { position: 'Senior Director' };
      const result = await contactsApi.update(1, updateData as any);

      expect(result).toEqual(mockResponse.data);
    });

    it('should delete a contact', async () => {
      const mockResponse = { data: { success: true } };
      mockedApi.delete.mockResolvedValue(mockResponse);

      const result = await contactsApi.delete(1);

      expect(result).toEqual(mockResponse.data);
    });

    it('should fetch overdue contacts', async () => {
      const mockResponse = {
        data: [
          { id: 1, contactId: 'OP-001', nextTouchDate: '2023-01-01' }
        ]
      };
      mockedApi.get.mockResolvedValue(mockResponse);

      const result = await contactsApi.getOverdue();

      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('interactionsApi', () => {
    it('should fetch all interactions', async () => {
      const mockResponse = {
        data: {
          items: [
            { id: 1, contactId: 1, interactionDate: '2023-01-01' }
          ],
          total: 1
        }
      };
      mockedApi.get.mockResolvedValue(mockResponse);

      const result = await interactionsApi.getAll({ page: 1 });

      expect(result).toEqual(mockResponse.data);
    });

    it('should create an interaction', async () => {
      const mockResponse = { data: { id: 1 } };
      mockedApi.post.mockResolvedValue(mockResponse);

      const interactionData = {
        contactId: 1,
        interactionDate: new Date().toISOString(),
        interactionTypeId: 1
      };
      const result = await interactionsApi.create(interactionData as any);

      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('blocksApi', () => {
    it('should fetch all blocks', async () => {
      const mockResponse = {
        data: [
          { id: 1, name: 'Operations', code: 'OP', status: 'Active' }
        ]
      };
      mockedApi.get.mockResolvedValue(mockResponse);

      const result = await blocksApi.getAll();

      expect(result).toEqual(mockResponse.data);
    });

    it('should create a block', async () => {
      const mockResponse = { data: { id: 1 } };
      mockedApi.post.mockResolvedValue(mockResponse);

      const blockData = { name: 'New Block', code: 'NB', status: 'Active' };
      const result = await blocksApi.create(blockData);

      expect(result).toEqual(mockResponse.data);
    });
  });

  describe('usersApi', () => {
    it('should fetch all users', async () => {
      const mockResponse = {
        data: [
          { id: 1, login: 'admin', role: 'Admin' }
        ]
      };
      mockedApi.get.mockResolvedValue(mockResponse);

      const result = await usersApi.getAll();

      expect(result).toEqual(mockResponse.data);
    });

    it('should create a user', async () => {
      const mockResponse = { data: { id: 1 } };
      mockedApi.post.mockResolvedValue(mockResponse);

      const userData = { login: 'newuser', password: 'password123', role: 'Curator' };
      const result = await usersApi.create(userData);

      expect(result).toEqual(mockResponse.data);
    });

    it('should change password', async () => {
      const mockResponse = { data: { success: true } };
      mockedApi.post.mockResolvedValue(mockResponse);

      const passwordData = { currentPassword: 'old', newPassword: 'new' };
      const result = await usersApi.changePassword(1, passwordData);

      expect(result).toEqual(mockResponse.data);
    });
  });
});
