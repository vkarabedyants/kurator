import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { useRouter, usePathname } from 'next/navigation';
import MainLayout from '../MainLayout';
import { UserRole } from '@/types/api';

// Mock Next.js navigation hooks
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
  usePathname: jest.fn(),
}));

// Mock Next.js Link component
jest.mock('next/link', () => {
  return ({ children, href }: any) => {
    return <a href={href}>{children}</a>;
  };
});

describe('MainLayout', () => {
  const mockPush = jest.fn();
  const mockRouter = { push: mockPush };

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue(mockRouter);
    (usePathname as jest.Mock).mockReturnValue('/dashboard');

    // Mock localStorage
    Object.defineProperty(window, 'localStorage', {
      value: {
        getItem: jest.fn(),
        setItem: jest.fn(),
        removeItem: jest.fn(),
        clear: jest.fn(),
      },
      writable: true,
    });
  });

  it('should redirect to login if user is not authenticated', () => {
    (window.localStorage.getItem as jest.Mock).mockReturnValue(null);

    render(<MainLayout><div>Test Content</div></MainLayout>);

    expect(mockPush).toHaveBeenCalledWith('/login');
  });

  it('should render loading spinner while checking authentication', () => {
    (window.localStorage.getItem as jest.Mock).mockReturnValue(null);

    const { container } = render(<MainLayout><div>Test Content</div></MainLayout>);

    const spinner = container.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();
  });

  it('should render layout for authenticated admin user', () => {
    const mockUser = {
      id: 1,
      login: 'admin',
      role: UserRole.Admin,
    };

    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    render(<MainLayout><div>Test Content</div></MainLayout>);

    expect(screen.getByText('admin (Admin)')).toBeInTheDocument();
    expect(screen.getByText('Test Content')).toBeInTheDocument();
  });

  it('should render layout for authenticated curator user', () => {
    const mockUser = {
      id: 2,
      login: 'curator',
      role: UserRole.Curator,
    };

    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    render(<MainLayout><div>Test Content</div></MainLayout>);

    expect(screen.getByText('curator (Curator)')).toBeInTheDocument();
  });

  it('should show admin-only menu items for admin users', () => {
    const mockUser = {
      id: 1,
      login: 'admin',
      role: UserRole.Admin,
    };

    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    render(<MainLayout><div>Test Content</div></MainLayout>);

    expect(screen.getByText('Блоки')).toBeInTheDocument();
    expect(screen.getByText('Пользователи')).toBeInTheDocument();
    expect(screen.getByText('Аналитика')).toBeInTheDocument();
    expect(screen.getByText('Журнал аудита')).toBeInTheDocument();
    expect(screen.getByText('Справочники')).toBeInTheDocument();
  });

  it('should not show admin-only menu items for curator users', () => {
    const mockUser = {
      id: 2,
      login: 'curator',
      role: UserRole.Curator,
    };

    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    render(<MainLayout><div>Test Content</div></MainLayout>);

    expect(screen.queryByText('Блоки')).not.toBeInTheDocument();
    expect(screen.queryByText('Пользователи')).not.toBeInTheDocument();
    expect(screen.queryByText('Справочники')).not.toBeInTheDocument();
  });

  it('should show common menu items for all users', () => {
    const mockUser = {
      id: 2,
      login: 'curator',
      role: UserRole.Curator,
    };

    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    render(<MainLayout><div>Test Content</div></MainLayout>);

    expect(screen.getByText('Панель управления')).toBeInTheDocument();
    expect(screen.getByText('Контакты')).toBeInTheDocument();
    expect(screen.getByText('Взаимодействия')).toBeInTheDocument();
    expect(screen.getByText('ЧаВо')).toBeInTheDocument();
  });

  it('should toggle sidebar when menu button is clicked', () => {
    const mockUser = {
      id: 1,
      login: 'admin',
      role: UserRole.Admin,
    };

    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    const { container } = render(<MainLayout><div>Test Content</div></MainLayout>);

    const sidebar = container.querySelector('aside');
    expect(sidebar).toHaveClass('w-64');

    const menuButton = container.querySelector('button');
    fireEvent.click(menuButton!);

    waitFor(() => {
      expect(sidebar).toHaveClass('w-16');
    });
  });

  it('should handle logout and clear localStorage', () => {
    const mockUser = {
      id: 1,
      login: 'admin',
      role: UserRole.Admin,
    };

    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    render(<MainLayout><div>Test Content</div></MainLayout>);

    const logoutButton = screen.getByText('Выход');
    fireEvent.click(logoutButton);

    expect(window.localStorage.removeItem).toHaveBeenCalledWith('token');
    expect(window.localStorage.removeItem).toHaveBeenCalledWith('user');
    expect(mockPush).toHaveBeenCalledWith('/login');
  });

  it('should render active navigation item correctly', () => {
    (usePathname as jest.Mock).mockReturnValue('/contacts');

    const mockUser = {
      id: 2,
      login: 'curator',
      role: UserRole.Curator,
    };

    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    render(<MainLayout><div>Test Content</div></MainLayout>);

    const contactsLink = screen.getByText('Контакты').closest('a');
    expect(contactsLink).toBeInTheDocument();
    expect(contactsLink).toHaveAttribute('href', '/contacts');
  });

  it('should show watchlist for threat analyst', () => {
    const mockUser = {
      id: 3,
      login: 'analyst',
      role: UserRole.ThreatAnalyst,
    };

    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    render(<MainLayout><div>Test Content</div></MainLayout>);

    expect(screen.getByText('Список наблюдения')).toBeInTheDocument();
  });

  it('should not show watchlist for curator', () => {
    const mockUser = {
      id: 2,
      login: 'curator',
      role: UserRole.Curator,
    };

    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    render(<MainLayout><div>Test Content</div></MainLayout>);

    expect(screen.queryByText('Список наблюдения')).not.toBeInTheDocument();
  });
});
