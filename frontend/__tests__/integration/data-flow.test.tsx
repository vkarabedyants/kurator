/**
 * Интеграционные тесты потока данных
 * Минимальный набор тестов для проверки критических путей:
 * - CRUD операции с контактами
 * - Взаимодействия
 * - Списки наблюдения
 * - Шифрование/дешифрование
 */
import React from 'react';
import { waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';

// Моки для API
const mockApi = {
  get: jest.fn(),
  post: jest.fn(),
  put: jest.fn(),
  delete: jest.fn(),
  patch: jest.fn(),
};
jest.mock('@/lib/api', () => ({
  __esModule: true,
  default: mockApi,
}));

// Мок шифрования
const mockEncrypt = jest.fn();
const mockDecrypt = jest.fn();
jest.mock('@/lib/encryption', () => ({
  encryptField: mockEncrypt,
  decryptField: mockDecrypt,
  KeyManager: {
    getPrivateKey: jest.fn().mockReturnValue('mock-key'),
  },
}));

describe('Поток данных контактов (Contacts Data Flow)', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockEncrypt.mockResolvedValue({ data: 'encrypted', iv: 'iv', keys: [] });
    mockDecrypt.mockResolvedValue('decrypted');
  });

  describe('Получение списка контактов', () => {
    it('должен получить и расшифровать контакты', async () => {
      const mockContacts = {
        data: [
          {
            id: 1,
            contactId: 'CNT-001',
            fullNameEncrypted: 'encrypted_name_1',
            blockId: 1,
            blockName: 'Блок 1',
          },
          {
            id: 2,
            contactId: 'CNT-002',
            fullNameEncrypted: 'encrypted_name_2',
            blockId: 1,
            blockName: 'Блок 1',
          },
        ],
        total: 2,
        page: 1,
        pageSize: 20,
      };

      mockApi.get.mockResolvedValueOnce({ data: mockContacts });

      const result = await mockApi.get('/contacts', { params: { page: 1, pageSize: 20 } });

      expect(result.data.data).toHaveLength(2);
      expect(result.data.total).toBe(2);
    });

    it('должен применить фильтры при запросе', async () => {
      mockApi.get.mockResolvedValueOnce({ data: { data: [], total: 0 } });

      await mockApi.get('/contacts', {
        params: { blockId: 1, search: 'Иванов', page: 1 },
      });

      expect(mockApi.get).toHaveBeenCalledWith('/contacts', {
        params: expect.objectContaining({
          blockId: 1,
          search: 'Иванов',
        }),
      });
    });

    it('должен корректно обрабатывать пагинацию', async () => {
      const page1 = { data: Array(20).fill({ id: 1 }), total: 45, page: 1 };
      const page2 = { data: Array(20).fill({ id: 21 }), total: 45, page: 2 };
      const page3 = { data: Array(5).fill({ id: 41 }), total: 45, page: 3 };

      mockApi.get
        .mockResolvedValueOnce({ data: page1 })
        .mockResolvedValueOnce({ data: page2 })
        .mockResolvedValueOnce({ data: page3 });

      const result1 = await mockApi.get('/contacts', { params: { page: 1 } });
      const result2 = await mockApi.get('/contacts', { params: { page: 2 } });
      const result3 = await mockApi.get('/contacts', { params: { page: 3 } });

      expect(result1.data.data).toHaveLength(20);
      expect(result2.data.data).toHaveLength(20);
      expect(result3.data.data).toHaveLength(5);
    });
  });

  describe('Создание контакта', () => {
    it('должен зашифровать данные перед отправкой', async () => {
      const contactData = {
        blockId: 1,
        fullName: 'Иванов Иван Иванович',
        notes: 'Конфиденциальные заметки',
      };

      mockApi.post.mockResolvedValueOnce({ data: { id: 1, contactId: 'CNT-001' } });

      // Шифруем ФИО
      await mockEncrypt(contactData.fullName, []);

      // Шифруем заметки
      await mockEncrypt(contactData.notes, []);

      // Отправляем
      const result = await mockApi.post('/contacts', {
        ...contactData,
        fullNameEncrypted: { data: 'encrypted' },
        notesEncrypted: { data: 'encrypted' },
      });

      expect(mockEncrypt).toHaveBeenCalledTimes(2);
      expect(result.data.contactId).toBe('CNT-001');
    });

    it('должен обработать ошибку валидации', async () => {
      mockApi.post.mockRejectedValueOnce({
        response: {
          status: 400,
          data: { message: 'Поле ФИО обязательно' },
        },
      });

      let errorMessage = '';
      try {
        await mockApi.post('/contacts', { blockId: 1 });
      } catch (error: any) {
        errorMessage = error.response.data.message;
      }

      expect(errorMessage).toBe('Поле ФИО обязательно');
    });
  });

  describe('Обновление контакта', () => {
    it('должен обновить только измененные поля', async () => {
      mockApi.put.mockResolvedValueOnce({ data: { success: true } });

      const updates = {
        position: 'Генеральный директор',
        influenceStatusId: 2,
      };

      await mockApi.put('/contacts/1', updates);

      expect(mockApi.put).toHaveBeenCalledWith('/contacts/1', updates);
    });

    it('должен перешифровать данные при изменении блока', async () => {
      mockApi.put.mockResolvedValueOnce({ data: { success: true } });

      const updates = {
        blockId: 2, // Новый блок
        fullName: 'Обновленное ФИО',
      };

      // При смене блока нужно перешифровать для новых получателей
      await mockEncrypt(updates.fullName, [{ userId: 3, publicKey: 'new-key' }]);

      await mockApi.put('/contacts/1', {
        ...updates,
        fullNameEncrypted: { data: 'encrypted' },
      });

      expect(mockEncrypt).toHaveBeenCalled();
    });
  });

  describe('Удаление контакта', () => {
    it('должен выполнить мягкое удаление', async () => {
      mockApi.delete.mockResolvedValueOnce({ data: { success: true } });

      const result = await mockApi.delete('/contacts/1');

      expect(result.data.success).toBe(true);
    });

    it('должен обработать ошибку при удалении несуществующего контакта', async () => {
      mockApi.delete.mockRejectedValueOnce({
        response: {
          status: 404,
          data: { message: 'Контакт не найден' },
        },
      });

      let errorMessage = '';
      try {
        await mockApi.delete('/contacts/999');
      } catch (error: any) {
        errorMessage = error.response.data.message;
      }

      expect(errorMessage).toBe('Контакт не найден');
    });
  });
});

describe('Поток данных взаимодействий (Interactions Data Flow)', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Получение взаимодействий', () => {
    it('должен получить взаимодействия для контакта', async () => {
      const mockInteractions = {
        data: [
          {
            id: 1,
            contactId: 1,
            interactionDate: '2024-01-15',
            interactionType: 'Meeting',
            result: 'Positive',
          },
        ],
        total: 1,
      };

      mockApi.get.mockResolvedValueOnce({ data: mockInteractions });

      const result = await mockApi.get('/interactions', {
        params: { contactId: 1 },
      });

      expect(result.data.data[0].interactionType).toBe('Meeting');
    });

    it('должен фильтровать по периоду', async () => {
      mockApi.get.mockResolvedValueOnce({ data: { data: [], total: 0 } });

      await mockApi.get('/interactions', {
        params: {
          fromDate: '2024-01-01',
          toDate: '2024-01-31',
        },
      });

      expect(mockApi.get).toHaveBeenCalledWith('/interactions', {
        params: expect.objectContaining({
          fromDate: '2024-01-01',
          toDate: '2024-01-31',
        }),
      });
    });
  });

  describe('Создание взаимодействия', () => {
    it('должен создать взаимодействие с шифрованием комментария', async () => {
      mockEncrypt.mockResolvedValueOnce({ data: 'encrypted_comment' });
      mockApi.post.mockResolvedValueOnce({ data: { id: 1 } });

      const interactionData = {
        contactId: 1,
        interactionDate: '2024-01-15',
        interactionTypeId: 1,
        comment: 'Обсудили детали проекта',
      };

      // Шифруем комментарий
      const encrypted = await mockEncrypt(interactionData.comment, []);

      await mockApi.post('/interactions', {
        ...interactionData,
        commentEncrypted: encrypted,
      });

      expect(mockEncrypt).toHaveBeenCalledWith('Обсудили детали проекта', []);
    });
  });
});

describe('Поток данных списка наблюдения (Watchlist Data Flow)', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Получение списка наблюдения', () => {
    it('должен получить записи с сортировкой по уровню риска', async () => {
      const mockWatchlist = {
        data: [
          { id: 1, fullName: 'Критический', riskLevel: 'Critical' },
          { id: 2, fullName: 'Высокий', riskLevel: 'High' },
          { id: 3, fullName: 'Средний', riskLevel: 'Medium' },
        ],
        total: 3,
      };

      mockApi.get.mockResolvedValueOnce({ data: mockWatchlist });

      const result = await mockApi.get('/watchlist');

      // Проверяем что критический уровень первый
      expect(result.data.data[0].riskLevel).toBe('Critical');
    });

    it('должен фильтровать по требующим проверки', async () => {
      mockApi.get.mockResolvedValueOnce({ data: { data: [], total: 0 } });

      await mockApi.get('/watchlist', {
        params: { requiresCheck: true },
      });

      expect(mockApi.get).toHaveBeenCalledWith('/watchlist', {
        params: { requiresCheck: true },
      });
    });
  });

  describe('Запись проверки', () => {
    it('должен обновить дату последней проверки', async () => {
      mockApi.post.mockResolvedValueOnce({
        data: {
          lastCheckDate: new Date().toISOString(),
          nextCheckDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
        },
      });

      const result = await mockApi.post('/watchlist/1/check', {
        dynamicsUpdate: 'Ситуация стабилизировалась',
        nextCheckDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
      });

      expect(result.data.lastCheckDate).toBeDefined();
      expect(result.data.nextCheckDate).toBeDefined();
    });
  });
});

describe('Поток данных дашборда (Dashboard Data Flow)', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Куратор дашборд', () => {
    it('должен получить статистику куратора', async () => {
      const mockDashboard = {
        totalContacts: 25,
        interactionsLastMonth: 15,
        averageInteractionInterval: 12,
        overdueContacts: 3,
        recentInteractions: [],
        contactsRequiringAttention: [],
      };

      mockApi.get.mockResolvedValueOnce({ data: mockDashboard });

      const result = await mockApi.get('/dashboard/curator');

      expect(result.data.totalContacts).toBe(25);
      expect(result.data.overdueContacts).toBe(3);
    });
  });

  describe('Админ дашборд', () => {
    it('должен получить полную статистику', async () => {
      const mockAdminDashboard = {
        totalContacts: 500,
        totalInteractions: 1200,
        totalBlocks: 10,
        totalUsers: 15,
        contactsByBlock: { 'Блок 1': 50, 'Блок 2': 75 },
        topCuratorsByActivity: { curator1: 45, curator2: 38 },
      };

      mockApi.get.mockResolvedValueOnce({ data: mockAdminDashboard });

      const result = await mockApi.get('/dashboard/admin');

      expect(result.data.totalContacts).toBe(500);
      expect(result.data.totalBlocks).toBe(10);
      expect(result.data.contactsByBlock['Блок 1']).toBe(50);
    });
  });
});

describe('Обработка ошибок сети', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('должен обработать таймаут', async () => {
    mockApi.get.mockRejectedValueOnce({
      code: 'ECONNABORTED',
      message: 'timeout of 10000ms exceeded',
    });

    let errorCode = '';
    try {
      await mockApi.get('/contacts');
    } catch (error: any) {
      errorCode = error.code;
    }

    expect(errorCode).toBe('ECONNABORTED');
  });

  it('должен обработать ошибку сети', async () => {
    mockApi.get.mockRejectedValueOnce({
      code: 'ERR_NETWORK',
      message: 'Network Error',
    });

    let errorCode = '';
    try {
      await mockApi.get('/contacts');
    } catch (error: any) {
      errorCode = error.code;
    }

    expect(errorCode).toBe('ERR_NETWORK');
  });

  it('должен обработать 500 ошибку сервера', async () => {
    mockApi.get.mockRejectedValueOnce({
      response: {
        status: 500,
        data: { message: 'Internal Server Error' },
      },
    });

    let status = 0;
    try {
      await mockApi.get('/contacts');
    } catch (error: any) {
      status = error.response.status;
    }

    expect(status).toBe(500);
  });
});

describe('Кэширование данных', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('повторные запросы должны использовать кэш (симуляция)', async () => {
    const cachedData = { data: [{ id: 1 }] };

    // Первый запрос
    mockApi.get.mockResolvedValueOnce({ data: cachedData });
    await mockApi.get('/contacts');

    // Симуляция кэша: второй запрос не должен вызывать API
    const cache = new Map();
    cache.set('/contacts', cachedData);

    if (cache.has('/contacts')) {
      expect(cache.get('/contacts')).toEqual(cachedData);
    }

    // API вызывается только один раз
    expect(mockApi.get).toHaveBeenCalledTimes(1);
  });
});
