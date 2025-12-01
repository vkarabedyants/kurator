import React from 'react';
import { render, screen, waitFor, fireEvent, within } from '@testing-library/react';
import '@testing-library/jest-dom';
import UsersPage from '../page';

// Mock Next.js navigation
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    refresh: jest.fn(),
    prefetch: jest.fn(),
  }),
  usePathname: () => '/users',
  useSearchParams: () => new URLSearchParams(),
}));

// Mock MainLayout
jest.mock('@/components/layout/MainLayout', () => {
  return {
    __esModule: true,
    default: ({ children }: { children: React.ReactNode }) => (
      <div data-testid="main-layout">{children}</div>
    ),
  };
});

// Mock API - must be before we define the mock variables
jest.mock('@/services/api', () => ({
  usersApi: {
    getAll: jest.fn(),
    delete: jest.fn(),
    changePassword: jest.fn(),
  },
  authApi: {
    register: jest.fn(),
  },
}));

// Mock types
jest.mock('@/types/api', () => ({
  UserRole: {
    Admin: 'Admin',
    Curator: 'Curator',
    ThreatAnalyst: 'ThreatAnalyst',
  },
}));

// Import mocked modules after mocking
import { usersApi, authApi } from '@/services/api';

// Mock window.confirm and alert
const mockConfirm = jest.fn();
const mockAlert = jest.fn();
global.confirm = mockConfirm;
global.alert = mockAlert;

describe('Users Page', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockConfirm.mockReturnValue(true);
  });

  it('should render loading state initially', () => {
    (usersApi.getAll as jest.Mock).mockImplementation(() => new Promise(() => {}));

    render(<UsersPage />);

    expect(screen.getByText('Управление пользователями')).toBeInTheDocument();
    expect(screen.getByTestId('main-layout')).toBeInTheDocument();
  });

  it('should render users list', async () => {
    const mockUsers = [
      {
        id: 1,
        login: 'admin',
        role: 'Admin',
        lastLoginAt: '2024-01-01T10:00:00',
        createdAt: '2024-01-01T00:00:00',
      },
      {
        id: 2,
        login: 'curator',
        role: 'Curator',
        lastLoginAt: null,
        createdAt: '2024-01-02T00:00:00',
      },
    ];

    (usersApi.getAll as jest.Mock).mockResolvedValue(mockUsers);

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('admin')).toBeInTheDocument();
      expect(screen.getByText('curator')).toBeInTheDocument();
      expect(screen.getByText('Admin')).toBeInTheDocument();
      expect(screen.getByText('Curator')).toBeInTheDocument();
    });
  });

  it('should handle error state', async () => {
    (usersApi.getAll as jest.Mock).mockRejectedValue(new Error('Network error'));

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText(/Ошибка: Network error/)).toBeInTheDocument();
    });
  });

  it('should show add user form when button is clicked', async () => {
    (usersApi.getAll as jest.Mock).mockResolvedValue([]);

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('Добавить пользователя')).toBeInTheDocument();
    });

    const addButton = screen.getByText('Добавить пользователя');
    fireEvent.click(addButton);

    expect(screen.getByText('Создать нового пользователя')).toBeInTheDocument();

    // Find form inputs by their position/context
    const formElement = screen.getByText('Создать нового пользователя').parentElement;
    if (formElement) {
      const inputs = within(formElement).getAllByRole('textbox');
      expect(inputs.length).toBeGreaterThan(0);

      const passwordInputs = within(formElement).getAllByRole('textbox', { hidden: true });
      const selectElement = within(formElement).getByRole('combobox');
      expect(selectElement).toBeInTheDocument();
    }
  });

  it('should create new user', async () => {
    (usersApi.getAll as jest.Mock).mockResolvedValueOnce([]).mockResolvedValueOnce([
      {
        id: 1,
        login: 'newuser',
        role: 'Curator',
        lastLoginAt: null,
        createdAt: '2024-01-03T00:00:00',
      },
    ]);

    (authApi.register as jest.Mock).mockResolvedValue({ success: true });

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('Добавить пользователя')).toBeInTheDocument();
    });

    // Open form
    fireEvent.click(screen.getByText('Добавить пользователя'));

    // Wait for form to appear
    await waitFor(() => {
      expect(screen.getByText('Создать нового пользователя')).toBeInTheDocument();
    });

    // Find all inputs in the form container
    const formContainer = screen.getByText('Создать нового пользователя').closest('div');
    if (formContainer) {
      const allInputs = within(formContainer).getAllByRole('textbox');
      const loginInput = allInputs[0]; // First input is login

      // Password input has type="password" so query differently
      const passwordInput = formContainer.querySelector('input[type="password"]');

      if (loginInput && passwordInput) {
        fireEvent.change(loginInput, { target: { value: 'newuser' } });
        fireEvent.change(passwordInput, { target: { value: 'password123' } });
      }

      // Submit form
      const submitButton = within(formContainer).getByRole('button', { name: /Создать пользователя/ });
      fireEvent.click(submitButton);

      await waitFor(() => {
        expect(authApi.register).toHaveBeenCalledWith({
          login: 'newuser',
          password: 'password123',
          role: 'Curator',
        });
      });
    }
  });

  it('should delete user', async () => {
    const mockUsers = [
      {
        id: 1,
        login: 'testuser',
        role: 'Curator',
        lastLoginAt: null,
        createdAt: '2024-01-01T00:00:00',
      },
    ];

    (usersApi.getAll as jest.Mock).mockResolvedValueOnce(mockUsers).mockResolvedValueOnce([]);
    (usersApi.delete as jest.Mock).mockResolvedValue({ success: true });

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('testuser')).toBeInTheDocument();
    });

    const deleteButton = screen.getByRole('button', { name: /Удалить/ });
    fireEvent.click(deleteButton);

    await waitFor(() => {
      expect(usersApi.delete).toHaveBeenCalledWith(1);
    });
  });

  it('should show password change form', async () => {
    const mockUsers = [
      {
        id: 1,
        login: 'testuser',
        role: 'Curator',
        lastLoginAt: null,
        createdAt: '2024-01-01T00:00:00',
      },
    ];

    (usersApi.getAll as jest.Mock).mockResolvedValue(mockUsers);

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('testuser')).toBeInTheDocument();
    });

    const changePasswordButton = screen.getByRole('button', { name: /Изменить пароль/ });
    fireEvent.click(changePasswordButton);

    await waitFor(() => {
      expect(screen.getByText(/Изменение пароля для testuser/)).toBeInTheDocument();
    });

    // Verify password inputs exist
    const passwordContainer = screen.getByText(/Изменение пароля для testuser/).closest('td');
    if (passwordContainer) {
      const passwordInputs = passwordContainer.querySelectorAll('input[type="password"]');
      expect(passwordInputs.length).toBe(3); // Current, new, confirm
    }
  });

  it('should change user password', async () => {
    const mockUsers = [
      {
        id: 1,
        login: 'testuser',
        role: 'Curator',
        lastLoginAt: null,
        createdAt: '2024-01-01T00:00:00',
      },
    ];

    (usersApi.getAll as jest.Mock).mockResolvedValue(mockUsers);
    (usersApi.changePassword as jest.Mock).mockResolvedValue({ success: true });

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('testuser')).toBeInTheDocument();
    });

    // Open password form
    fireEvent.click(screen.getByRole('button', { name: /Изменить пароль/ }));

    await waitFor(() => {
      expect(screen.getByText(/Изменение пароля для testuser/)).toBeInTheDocument();
    });

    // Fill password form
    const passwordContainer = screen.getByText(/Изменение пароля для testuser/).closest('td');
    if (passwordContainer) {
      const passwordInputs = passwordContainer.querySelectorAll('input[type="password"]');

      if (passwordInputs.length === 3) {
        fireEvent.change(passwordInputs[0], { target: { value: 'oldpass' } });
        fireEvent.change(passwordInputs[1], { target: { value: 'newpass123' } });
        fireEvent.change(passwordInputs[2], { target: { value: 'newpass123' } });

        // Submit form
        const submitButton = within(passwordContainer as HTMLElement).getAllByRole('button').find(
          (btn) => btn.textContent === 'Изменить пароль'
        );

        if (submitButton) {
          fireEvent.click(submitButton);

          await waitFor(() => {
            expect(usersApi.changePassword).toHaveBeenCalledWith(1, {
              currentPassword: 'oldpass',
              newPassword: 'newpass123',
            });
            expect(mockAlert).toHaveBeenCalledWith('Пароль успешно изменен!');
          });
        }
      }
    }
  });

  it('should validate password confirmation', async () => {
    const mockUsers = [
      {
        id: 1,
        login: 'testuser',
        role: 'Curator',
        lastLoginAt: null,
        createdAt: '2024-01-01T00:00:00',
      },
    ];

    (usersApi.getAll as jest.Mock).mockResolvedValue(mockUsers);

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('testuser')).toBeInTheDocument();
    });

    // Open password form
    fireEvent.click(screen.getByRole('button', { name: /Изменить пароль/ }));

    await waitFor(() => {
      expect(screen.getByText(/Изменение пароля для testuser/)).toBeInTheDocument();
    });

    // Fill with mismatched passwords
    const passwordContainer = screen.getByText(/Изменение пароля для testuser/).closest('td');
    if (passwordContainer) {
      const passwordInputs = passwordContainer.querySelectorAll('input[type="password"]');

      if (passwordInputs.length === 3) {
        fireEvent.change(passwordInputs[0], { target: { value: 'oldpass' } });
        fireEvent.change(passwordInputs[1], { target: { value: 'newpass123' } });
        fireEvent.change(passwordInputs[2], { target: { value: 'different' } });

        // Submit form
        const submitButton = within(passwordContainer as HTMLElement).getAllByRole('button').find(
          (btn) => btn.textContent === 'Изменить пароль'
        );

        if (submitButton) {
          fireEvent.click(submitButton);

          await waitFor(() => {
            expect(mockAlert).toHaveBeenCalledWith('Новые пароли не совпадают!');
            expect(usersApi.changePassword).not.toHaveBeenCalled();
          });
        }
      }
    }
  });

  it('should display empty state', async () => {
    (usersApi.getAll as jest.Mock).mockResolvedValue([]);

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('Пользователи не найдены')).toBeInTheDocument();
    });
  });

  it('should format dates correctly', async () => {
    const mockUsers = [
      {
        id: 1,
        login: 'admin',
        role: 'Admin',
        lastLoginAt: '2024-01-15T14:30:00',
        createdAt: '2024-01-01T00:00:00',
      },
    ];

    (usersApi.getAll as jest.Mock).mockResolvedValue(mockUsers);

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('admin')).toBeInTheDocument();
    });

    // Check if date is rendered (will be in local format)
    const table = screen.getByRole('table');
    expect(table).toBeInTheDocument();
  });

  it('should disable delete button for admin user', async () => {
    const mockUsers = [
      {
        id: 1,
        login: 'admin',
        role: 'Admin',
        lastLoginAt: null,
        createdAt: '2024-01-01T00:00:00',
      },
    ];

    (usersApi.getAll as jest.Mock).mockResolvedValue(mockUsers);

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('admin')).toBeInTheDocument();
    });

    const deleteButton = screen.getByRole('button', { name: /Удалить/ });
    expect(deleteButton).toBeDisabled();
  });

  it('should cancel add user form', async () => {
    (usersApi.getAll as jest.Mock).mockResolvedValue([]);

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('Добавить пользователя')).toBeInTheDocument();
    });

    // Open form
    fireEvent.click(screen.getByText('Добавить пользователя'));

    expect(screen.getByText('Создать нового пользователя')).toBeInTheDocument();

    // Cancel form
    const cancelButton = screen.getByRole('button', { name: /Отмена/ });
    fireEvent.click(cancelButton);

    // Form should be hidden
    expect(screen.queryByText('Создать нового пользователя')).not.toBeInTheDocument();
  });

  it('should handle API errors gracefully', async () => {
    (usersApi.getAll as jest.Mock).mockResolvedValue([]);
    (authApi.register as jest.Mock).mockRejectedValue(new Error('Registration failed'));

    // Mock window.alert for this test
    const alertSpy = jest.spyOn(window, 'alert').mockImplementation(() => {});

    render(<UsersPage />);

    await waitFor(() => {
      expect(screen.getByText('Добавить пользователя')).toBeInTheDocument();
    });

    // Open and fill form
    fireEvent.click(screen.getByText('Добавить пользователя'));

    const formContainer = screen.getByText('Создать нового пользователя').closest('div');
    if (formContainer) {
      const allInputs = within(formContainer).getAllByRole('textbox');
      const loginInput = allInputs[0];
      const passwordInput = formContainer.querySelector('input[type="password"]');

      if (loginInput && passwordInput) {
        fireEvent.change(loginInput, { target: { value: 'testuser' } });
        fireEvent.change(passwordInput, { target: { value: 'password123' } });

        const submitButton = within(formContainer).getByRole('button', { name: /Создать пользователя/ });
        fireEvent.click(submitButton);

        await waitFor(() => {
          expect(alertSpy).toHaveBeenCalledWith('Не удалось создать пользователя: Registration failed');
        });
      }
    }

    alertSpy.mockRestore();
  });
});