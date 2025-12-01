import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { useRouter } from 'next/navigation';
import ContactForm from '../ContactForm';
import { contactsApi, blocksApi, usersApi, referencesApi } from '@/services/api';
import { encryptField, getBlockRecipients, KeyManager } from '@/lib/encryption';

jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
  usePathname: () => '/contacts/new',
}));

jest.mock('@/services/api', () => ({
  contactsApi: {
    create: jest.fn(),
    update: jest.fn(),
    getById: jest.fn(),
  },
  blocksApi: {
    getAll: jest.fn(),
  },
  usersApi: {
    getAll: jest.fn(),
  },
  referencesApi: {
    getByCategory: jest.fn(),
  },
}));

jest.mock('@/lib/encryption', () => ({
  encryptField: jest.fn(),
  getBlockRecipients: jest.fn(),
  KeyManager: {
    getPrivateKey: jest.fn(),
    loadPrivateKeyFromFile: jest.fn(),
  },
}));

describe('ContactForm Component', () => {
  const mockPush = jest.fn();
  const mockRouter = { push: mockPush, refresh: jest.fn() };

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue(mockRouter);

    (blocksApi.getAll as jest.Mock).mockResolvedValue([
      { id: 1, name: 'Block 1', code: 'BLK1' },
      { id: 2, name: 'Block 2', code: 'BLK2' },
    ]);

    (usersApi.getAll as jest.Mock).mockResolvedValue([
      { id: 1, login: 'curator1', role: 'Curator' },
      { id: 2, login: 'curator2', role: 'Curator' },
    ]);

    (referencesApi.getByCategory as jest.Mock).mockImplementation((category: string) => {
      const mockData: Record<string, any[]> = {
        organization: [{ id: 1, name: 'Org 1' }],
        influence_status: [{ id: 1, name: 'High', description: 'High influence' }],
        influence_type: [{ id: 1, name: 'Political' }],
        communication_channel: [{ id: 1, name: 'Email' }],
        contact_source: [{ id: 1, name: 'LinkedIn' }],
      };
      return Promise.resolve(mockData[category] || []);
    });

    (KeyManager.getPrivateKey as jest.Mock).mockReturnValue('mock-private-key');
    (getBlockRecipients as jest.Mock).mockResolvedValue([
      { userId: 1, publicKey: 'public-key-1' },
    ]);
    (encryptField as jest.Mock).mockResolvedValue({
      data: 'encrypted-data',
      iv: 'iv',
      keys: [{ userId: 1, encryptedKey: 'key1' }],
    });
  });

  describe('Create Mode', () => {
    it('should render form in create mode', async () => {
      render(<ContactForm mode="create" />);

      await waitFor(() => {
        expect(screen.getByText('Создать контакт')).toBeInTheDocument();
      });

      expect(screen.getByRole('button', { name: 'Создать' })).toBeInTheDocument();
    });

    it('should show warning when private key is not loaded', async () => {
      (KeyManager.getPrivateKey as jest.Mock).mockReturnValue(null);

      render(<ContactForm mode="create" />);

      await waitFor(() => {
        expect(screen.getByText(/Для шифрования данных необходимо загрузить ваш приватный ключ/)).toBeInTheDocument();
      });
    });

    it('should have required fields', async () => {
      render(<ContactForm mode="create" />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Создать' })).toBeInTheDocument();
      });

      const fullNameInput = screen.getByPlaceholderText(/Введите ФИО контакта/) as HTMLInputElement;
      expect(fullNameInput.required).toBe(true);
    });

    it('should allow canceling', async () => {
      render(<ContactForm mode="create" />);

      await waitFor(() => {
        expect(screen.getByRole('button', { name: 'Отмена' })).toBeInTheDocument();
      });

      fireEvent.click(screen.getByRole('button', { name: 'Отмена' }));
      expect(mockPush).toHaveBeenCalledWith('/contacts');
    });
  });

  describe('Edit Mode', () => {
    it('should load existing contact', async () => {
      const mockContact = {
        id: 1,
        blockId: 1,
        fullName: 'John Doe',
        position: 'Manager',
      };

      (contactsApi.getById as jest.Mock).mockResolvedValue(mockContact);

      render(<ContactForm mode="edit" contactId={1} />);

      await waitFor(() => {
        expect(contactsApi.getById).toHaveBeenCalledWith(1);
      });

      await waitFor(() => {
        const fullNameInput = screen.getByPlaceholderText(/Введите ФИО контакта/) as HTMLInputElement;
        expect(fullNameInput.value).toBe('John Doe');
      });
    });

    it('should handle load error', async () => {
      (contactsApi.getById as jest.Mock).mockRejectedValue(new Error('Not found'));

      render(<ContactForm mode="edit" contactId={999} />);

      await waitFor(() => {
        expect(screen.getByText(/Не удалось загрузить контакт/)).toBeInTheDocument();
      });
    });
  });
});
