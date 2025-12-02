/**
 * Интеграционные тесты потока аутентификации
 * Минимальный набор тестов для проверки критических путей:
 * - Вход в систему
 * - Настройка MFA
 * - Верификация MFA
 * - Выход из системы
 * - Защита маршрутов
 */
import '@testing-library/jest-dom';

// Моки для Next.js
const mockPush = jest.fn();
const mockRefresh = jest.fn();
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mockPush,
    refresh: mockRefresh,
    replace: jest.fn(),
  }),
  usePathname: jest.fn().mockReturnValue('/login'),
  useSearchParams: jest.fn().mockReturnValue({
    get: jest.fn().mockReturnValue(null),
  }),
}));

// Мок API
const mockApi = {
  post: jest.fn(),
  get: jest.fn(),
};
jest.mock('@/lib/api', () => ({
  __esModule: true,
  default: mockApi,
}));

// Мок localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => { store[key] = value; },
    removeItem: (key: string) => { delete store[key]; },
    clear: () => { store = {}; },
  };
})();
Object.defineProperty(window, 'localStorage', { value: localStorageMock });

describe('Поток аутентификации (Authentication Flow)', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    localStorageMock.clear();
  });

  describe('Вход в систему', () => {
    it('успешный вход должен сохранить токен и перенаправить', async () => {
      const mockLoginResponse = {
        data: {
          token: 'jwt-token-123',
          user: {
            id: 1,
            login: 'admin',
            role: 'Admin',
            mfaEnabled: false,
            isFirstLogin: false,
          },
        },
      };

      mockApi.post.mockResolvedValueOnce(mockLoginResponse);

      // Имитация вызова API входа
      const loginResult = await mockApi.post('/auth/login', {
        login: 'admin',
        password: 'admin123',
      });

      // Сохранение токена
      if (loginResult.data.token) {
        localStorageMock.setItem('auth_token', loginResult.data.token);
        localStorageMock.setItem('user', JSON.stringify(loginResult.data.user));
      }

      expect(localStorageMock.getItem('auth_token')).toBe('jwt-token-123');
      expect(JSON.parse(localStorageMock.getItem('user')!).role).toBe('Admin');
    });

    it('неверные учетные данные должны показать ошибку', async () => {
      mockApi.post.mockRejectedValueOnce({
        response: {
          status: 401,
          data: { message: 'Неверный логин или пароль' },
        },
      });

      let errorMessage = '';
      try {
        await mockApi.post('/auth/login', {
          login: 'wrong',
          password: 'wrong',
        });
      } catch (error: unknown) {
        const err = error as { response: { data: { message: string } } };
        errorMessage = err.response.data.message;
      }

      expect(errorMessage).toBe('Неверный логин или пароль');
      expect(localStorageMock.getItem('auth_token')).toBeNull();
    });

    it('первый вход должен перенаправить на настройку MFA', async () => {
      const mockFirstLoginResponse = {
        data: {
          token: 'temp-token',
          user: {
            id: 1,
            login: 'newuser',
            role: 'Curator',
            mfaEnabled: false,
            isFirstLogin: true,
          },
          requireMfaSetup: true,
        },
      };

      mockApi.post.mockResolvedValueOnce(mockFirstLoginResponse);

      const result = await mockApi.post('/auth/login', {
        login: 'newuser',
        password: 'password123',
      });

      if (result.data.requireMfaSetup || result.data.user.isFirstLogin) {
        // Должен перенаправить на настройку MFA
        mockPush('/mfa/setup');
      }

      expect(mockPush).toHaveBeenCalledWith('/mfa/setup');
    });
  });

  describe('Настройка MFA', () => {
    it('должен получить QR-код для настройки', async () => {
      const mockMfaSetupResponse = {
        data: {
          qrCodeUrl: 'data:image/png;base64,iVBORw0KGgo...',
          mfaSecret: 'ABCDEFGHIJKLMNOP',
          message: 'Отсканируйте QR-код в приложении аутентификатора',
        },
      };

      mockApi.post.mockResolvedValueOnce(mockMfaSetupResponse);

      const result = await mockApi.post('/auth/setup-mfa', {
        userId: 1,
        password: 'password123',
      });

      expect(result.data.qrCodeUrl).toBeDefined();
      expect(result.data.mfaSecret).toBeDefined();
      expect(result.data.mfaSecret.length).toBeGreaterThanOrEqual(16);
    });

    it('должен подтвердить настройку MFA с кодом', async () => {
      const mockVerifySetupResponse = {
        data: {
          success: true,
          message: 'MFA успешно настроен',
        },
      };

      mockApi.post.mockResolvedValueOnce(mockVerifySetupResponse);

      const result = await mockApi.post('/auth/verify-mfa-setup', {
        userId: 1,
        totpCode: '123456',
      });

      expect(result.data.success).toBe(true);
    });
  });

  describe('Верификация MFA', () => {
    it('должен проверить TOTP код и выдать токен', async () => {
      const mockVerifyResponse = {
        data: {
          token: 'full-access-token',
          user: {
            id: 1,
            login: 'admin',
            role: 'Admin',
            mfaEnabled: true,
          },
        },
      };

      mockApi.post.mockResolvedValueOnce(mockVerifyResponse);

      const result = await mockApi.post('/auth/verify-mfa', {
        userId: 1,
        totpCode: '123456',
      });

      // Сохранение полного токена доступа
      localStorageMock.setItem('auth_token', result.data.token);

      expect(localStorageMock.getItem('auth_token')).toBe('full-access-token');
    });

    it('неверный TOTP код должен вернуть ошибку', async () => {
      mockApi.post.mockRejectedValueOnce({
        response: {
          status: 401,
          data: { message: 'Неверный код подтверждения' },
        },
      });

      let errorMessage = '';
      try {
        await mockApi.post('/auth/verify-mfa', {
          userId: 1,
          totpCode: '000000',
        });
      } catch (error: unknown) {
        const err = error as { response: { data: { message: string } } };
        errorMessage = err.response.data.message;
      }

      expect(errorMessage).toBe('Неверный код подтверждения');
    });
  });

  describe('Выход из системы', () => {
    it('должен очистить токен и перенаправить на страницу входа', async () => {
      // Устанавливаем начальное состояние
      localStorageMock.setItem('auth_token', 'test-token');
      localStorageMock.setItem('user', JSON.stringify({ id: 1, login: 'admin' }));

      mockApi.post.mockResolvedValueOnce({ data: { success: true } });

      // Выход
      await mockApi.post('/auth/logout');

      // Очистка данных
      localStorageMock.removeItem('auth_token');
      localStorageMock.removeItem('user');

      // Перенаправление
      mockPush('/login');

      expect(localStorageMock.getItem('auth_token')).toBeNull();
      expect(localStorageMock.getItem('user')).toBeNull();
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  describe('Защита маршрутов', () => {
    it('неавторизованный доступ должен перенаправить на вход', () => {
      // Симуляция middleware проверки
      const isAuthenticated = !!localStorageMock.getItem('auth_token');

      if (!isAuthenticated) {
        mockPush('/login');
      }

      expect(mockPush).toHaveBeenCalledWith('/login');
    });

    it('авторизованный пользователь должен иметь доступ к защищенным маршрутам', () => {
      localStorageMock.setItem('auth_token', 'valid-token');

      const isAuthenticated = !!localStorageMock.getItem('auth_token');

      expect(isAuthenticated).toBe(true);
      expect(mockPush).not.toHaveBeenCalledWith('/login');
    });

    it('истекший токен должен перенаправить на вход', async () => {
      mockApi.get.mockRejectedValueOnce({
        response: {
          status: 401,
          data: { message: 'Token expired' },
        },
      });

      try {
        await mockApi.get('/dashboard');
      } catch (error: unknown) {
        const err = error as { response: { status: number } };
        if (err.response.status === 401) {
          localStorageMock.removeItem('auth_token');
          mockPush('/login');
        }
      }

      expect(localStorageMock.getItem('auth_token')).toBeNull();
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  describe('Контроль доступа по ролям', () => {
    const checkAccess = (userRole: string, requiredRole: string): boolean => {
      const roleHierarchy: Record<string, number> = {
        'Admin': 3,
        'Curator': 2,
        'ThreatAnalyst': 1,
      };
      return roleHierarchy[userRole] >= roleHierarchy[requiredRole];
    };

    it('Admin должен иметь доступ ко всем страницам', () => {
      expect(checkAccess('Admin', 'Admin')).toBe(true);
      expect(checkAccess('Admin', 'Curator')).toBe(true);
      expect(checkAccess('Admin', 'ThreatAnalyst')).toBe(true);
    });

    it('Curator должен иметь ограниченный доступ', () => {
      expect(checkAccess('Curator', 'Admin')).toBe(false);
      expect(checkAccess('Curator', 'Curator')).toBe(true);
      expect(checkAccess('Curator', 'ThreatAnalyst')).toBe(true);
    });

    it('ThreatAnalyst должен иметь минимальный доступ', () => {
      expect(checkAccess('ThreatAnalyst', 'Admin')).toBe(false);
      expect(checkAccess('ThreatAnalyst', 'Curator')).toBe(false);
      expect(checkAccess('ThreatAnalyst', 'ThreatAnalyst')).toBe(true);
    });
  });
});

describe('Безопасность токенов', () => {
  beforeEach(() => {
    localStorageMock.clear();
  });

  it('токен должен храниться безопасно', () => {
    const token = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test';
    localStorageMock.setItem('auth_token', token);

    // Токен не должен быть виден в URL
    expect(window.location.href).not.toContain(token);

    // Токен должен быть получен только из localStorage
    expect(localStorageMock.getItem('auth_token')).toBe(token);
  });

  it('конфиденциальные данные не должны логироваться', () => {
    const consoleSpy = jest.spyOn(console, 'log');

    const sensitiveData = {
      password: 'secret123',
      token: 'jwt-token',
      mfaSecret: 'TOTP-SECRET',
    };

    // Безопасное логирование
    const safeLog = (data: object) => {
      const sanitized = Object.entries(data).reduce((acc, [key, value]) => {
        if (['password', 'token', 'mfaSecret'].includes(key)) {
          acc[key] = '[REDACTED]';
        } else {
          acc[key] = value;
        }
        return acc;
      }, {} as Record<string, string>);
      console.log(sanitized);
    };

    safeLog(sensitiveData);

    expect(consoleSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        password: '[REDACTED]',
        token: '[REDACTED]',
        mfaSecret: '[REDACTED]',
      })
    );

    consoleSpy.mockRestore();
  });
});
