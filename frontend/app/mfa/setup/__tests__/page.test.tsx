import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import MfaSetupPage from '../page';
import { authApi } from '@/services/api';

jest.mock('@/services/api', () => ({
  authApi: {
    setupMfa: jest.fn(),
    verifyMfa: jest.fn(),
  },
}));

const mockRouterPush = jest.fn();
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: mockRouterPush,
    replace: jest.fn(),
    prefetch: jest.fn(),
  }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/mfa/setup',
}));

jest.mock('next-intl', () => ({
  useTranslations: () => (key: string) => {
    const translations: Record<string, string> = {
      'auth.mfa_setup_title': 'Настройка двухфакторной аутентификации',
      'auth.mfa_setup_description': 'Скачайте приложение для MFA',
      'auth.mfa_setup_loading': 'Загрузка...',
      'auth.mfa_setup_step1_title': 'Шаг 1',
      'auth.mfa_setup_step1_description': 'Установите приложение',
      'auth.mfa_setup_step2_title': 'Шаг 2',
      'auth.mfa_setup_step2_description': 'Отсканируйте QR код',
      'auth.mfa_setup_manual_title': 'Ручной ввод',
      'auth.mfa_setup_manual_description': 'Введите секрет',
      'auth.mfa_setup_security_note': 'Предупреждение о безопасности',
      'auth.mfa_setup_button': 'Продолжить',
      'mfa.setup_error': 'Ошибка настройки MFA',
      'errors.not_found': 'Не найдено',
      'auth.login_error_invalid_credentials': 'Неверные учетные данные',
    };
    return translations[key] || key;
  },
}));

describe('MFA Setup Page', () => {
  const mockMfaSetupResponse = {
    mfaSecret: 'TEST_SECRET_123456789',
    qrCodeUrl: 'otpauth://totp/KURATOR:test@example.com?secret=TEST_SECRET_123456789',
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockRouterPush.mockClear();
    
    // Setup localStorage mock
    Storage.prototype.getItem = jest.fn((key) => {
      if (key === 'pendingMfaSetup') {
        return JSON.stringify({ userId: 1, password: 'test', login: 'testuser' });
      }
      return null;
    });
    Storage.prototype.setItem = jest.fn();
    Storage.prototype.removeItem = jest.fn();
    
    (authApi.setupMfa as jest.Mock).mockResolvedValue(mockMfaSetupResponse);
  });

  it('redirects to login if no setup data in localStorage', async () => {
    Storage.prototype.getItem = jest.fn().mockReturnValue(null);
    
    render(<MfaSetupPage />);
    
    await waitFor(() => {
      expect(mockRouterPush).toHaveBeenCalledWith('/login');
    });
  });

  it('displays title', async () => {
    render(<MfaSetupPage />);
    
    expect(screen.getByText('Настройка двухфакторной аутентификации')).toBeInTheDocument();
  });

  it('shows loading state', async () => {
    (authApi.setupMfa as jest.Mock).mockImplementation(() => new Promise(() => {}));
    
    render(<MfaSetupPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Загрузка...')).toBeInTheDocument();
    });
  });

  it('displays QR code and secret after loading', async () => {
    render(<MfaSetupPage />);
    
    await waitFor(() => {
      expect(screen.getByText('TEST_SECRET_123456789')).toBeInTheDocument();
    });
  });

  it('shows error on API failure', async () => {
    (authApi.setupMfa as jest.Mock).mockRejectedValue({
      response: {
        status: 500,
        data: { message: 'Ошибка настройки MFA' }
      }
    });
    
    render(<MfaSetupPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Ошибка настройки MFA')).toBeInTheDocument();
    });
  });

  it('navigates to verification page on continue', async () => {
    const user = userEvent.setup();
    
    render(<MfaSetupPage />);
    
    await waitFor(() => {
      expect(screen.getByText('TEST_SECRET_123456789')).toBeInTheDocument();
    });
    
    const continueButton = screen.getByText('Продолжить');
    await user.click(continueButton);
    
    expect(mockRouterPush).toHaveBeenCalledWith('/mfa/verify');
  });
});
