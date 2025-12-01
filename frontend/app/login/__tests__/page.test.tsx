
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import LoginPage from '../page';
import { authApi } from '@/services/api';

// Mock the API
jest.mock('@/services/api', () => ({
  authApi: {
    login: jest.fn(),
  },
}));

// Mock Next.js router
const mockPush = jest.fn();
const mockReplace = jest.fn();
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(() => ({
    push: mockPush,
    replace: mockReplace,
  })),
  usePathname: () => '/login',
}));

// Mock localStorage
const localStorageMock = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
};
Object.defineProperty(window, 'localStorage', {
  value: localStorageMock,
  writable: true,
});

describe('Login Page', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockPush.mockClear();
    mockReplace.mockClear();
    localStorageMock.setItem.mockClear();
  });

  // Helper to find elements - i18n mock returns key names
  const findLoginInput = () => screen.getByPlaceholderText(/login_placeholder/i);
  const findPasswordInput = () => screen.getByPlaceholderText(/password_placeholder/i);
  const findSubmitButton = () => screen.getByRole('button', { name: /login_button/i });

  it('should render login form', () => {
    render(<LoginPage />);
    expect(findLoginInput()).toBeInTheDocument();
    expect(findPasswordInput()).toBeInTheDocument();
    expect(findSubmitButton()).toBeInTheDocument();
  });

  it('should handle successful login', async () => {
    const mockResponse = {
      token: 'jwt-token',
      user: { id: 1, login: 'admin', role: 'Admin' },
      requireMfaSetup: false,
      requireMfaVerification: false,
    };

    (authApi.login as jest.Mock).mockResolvedValue(mockResponse);

    render(<LoginPage />);

    fireEvent.change(findLoginInput(), { target: { value: 'admin' } });
    fireEvent.change(findPasswordInput(), { target: { value: 'password123' } });
    fireEvent.click(findSubmitButton());

    await waitFor(() => {
      expect(authApi.login).toHaveBeenCalledWith({
        login: 'admin',
        password: 'password123',
      });
      expect(localStorageMock.setItem).toHaveBeenCalledWith('token', 'jwt-token');
      expect(mockPush).toHaveBeenCalledWith('/dashboard');
    });
  });

  it('should display error on failed login', async () => {
    const errorResponse = {
      response: { data: { message: 'Invalid credentials' } }
    };
    (authApi.login as jest.Mock).mockRejectedValue(errorResponse);

    render(<LoginPage />);

    fireEvent.change(findLoginInput(), { target: { value: 'admin' } });
    fireEvent.change(findPasswordInput(), { target: { value: 'wrong' } });
    fireEvent.click(findSubmitButton());

    await waitFor(() => {
      // Error is displayed in div with red border - i18n returns key name
      const errorDiv = document.querySelector('.bg-red-50');
      expect(errorDiv).toBeInTheDocument();
    });
  });

  it('should show loading state while submitting', () => {
    (authApi.login as jest.Mock).mockImplementation(
      () => new Promise(resolve => setTimeout(resolve, 100))
    );

    render(<LoginPage />);

    fireEvent.change(findLoginInput(), { target: { value: 'admin' } });
    fireEvent.change(findPasswordInput(), { target: { value: 'password' } });
    fireEvent.click(findSubmitButton());

    expect(findSubmitButton()).toBeDisabled();
  });

  it('should redirect to MFA setup if required', async () => {
    const mockResponse = { requireMfaSetup: true, userId: 1, login: 'admin' };
    (authApi.login as jest.Mock).mockResolvedValue(mockResponse);

    render(<LoginPage />);

    fireEvent.change(findLoginInput(), { target: { value: 'admin' } });
    fireEvent.change(findPasswordInput(), { target: { value: 'password123' } });
    fireEvent.click(findSubmitButton());

    await waitFor(() => {
      expect(localStorageMock.setItem).toHaveBeenCalledWith('pendingMfaSetup', expect.any(String));
      expect(mockPush).toHaveBeenCalledWith('/mfa/setup');
    });
  });

  it('should redirect to MFA verification if required', async () => {
    const mockResponse = { requireMfaVerification: true, userId: 1, login: 'admin' };
    (authApi.login as jest.Mock).mockResolvedValue(mockResponse);

    render(<LoginPage />);

    fireEvent.change(findLoginInput(), { target: { value: 'admin' } });
    fireEvent.change(findPasswordInput(), { target: { value: 'password123' } });
    fireEvent.click(findSubmitButton());

    await waitFor(() => {
      expect(localStorageMock.setItem).toHaveBeenCalledWith('pendingMfaVerification', expect.any(String));
      expect(mockPush).toHaveBeenCalledWith('/mfa/verify');
    });
  });
});
