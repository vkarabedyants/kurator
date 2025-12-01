import { render, screen, waitFor } from '@testing-library/react';
import MfaVerifyPage from '../page';
import { authApi } from '@/services/api';

jest.mock('@/services/api', () => ({
  authApi: {
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
  usePathname: () => '/mfa/verify',
}));

jest.mock('next-intl', () => ({
  useTranslations: () => (key: string, params?: any) => {
    const translations: Record<string, string> = {
      'auth.mfa_verify_title': 'Верификация MFA',
      'auth.mfa_verify_description': 'Введите код из приложения',
      'auth.mfa_verify_user': params?.user ? `Пользователь: ${params.user}` : '',
      'auth.mfa_verify_code_placeholder': 'X',
      'auth.mfa_verify_button': 'Подтвердить',
      'auth.mfa_verify_loading': 'Проверка...',
      'auth.mfa_verify_error': 'Неверный код',
      'auth.mfa_verify_back_to_login': 'Вернуться к входу',
      'auth.mfa_verify_tip_title': 'Совет',
      'auth.mfa_verify_tip_description': 'Откройте приложение',
    };
    return translations[key] || key;
  },
}));

describe('MFA Verify Page', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockRouterPush.mockClear();
    
    Storage.prototype.getItem = jest.fn((key) => {
      if (key === 'pendingMfaVerification') {
        return JSON.stringify({ userId: 1, login: 'testuser' });
      }
      return null;
    });
    Storage.prototype.setItem = jest.fn();
    Storage.prototype.removeItem = jest.fn();
  });

  it('redirects to login if no verification data', async () => {
    Storage.prototype.getItem = jest.fn().mockReturnValue(null);
    
    render(<MfaVerifyPage />);
    
    await waitFor(() => {
      expect(mockRouterPush).toHaveBeenCalledWith('/login');
    });
  });

  it('displays verification title', async () => {
    render(<MfaVerifyPage />);
    
    await waitFor(() => {
      expect(screen.getByText('Верификация MFA')).toBeInTheDocument();
    });
  });

  it('has code input fields', async () => {
    render(<MfaVerifyPage />);
    
    // Should have 6 input fields for the code
    const inputs = screen.getAllByRole('textbox');
    expect(inputs.length).toBe(6);
  });

  it('displays verify button', async () => {
    render(<MfaVerifyPage />);
    
    expect(screen.getByText('Подтвердить')).toBeInTheDocument();
  });
});
