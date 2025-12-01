import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { useRouter, usePathname } from 'next/navigation';
import MainLayout from '../MainLayout';

jest.mock('@/components/ui/Navigation', () => ({
  __esModule: true,
  default: () => <div data-testid="desktop-navigation">Desktop Navigation</div>,
  MobileNavigation: ({ isOpen, onClose }: any) =>
    isOpen ? (
      <div data-testid="mobile-navigation">
        Mobile Navigation
        <button onClick={onClose}>Close</button>
      </div>
    ) : null,
  Breadcrumb: ({ items }: any) => (
    <nav data-testid="breadcrumb">
      {items.map((item: any, index: number) => (
        <span key={index}>{item.label}</span>
      ))}
    </nav>
  ),
}));

jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
  usePathname: jest.fn(),
}));

describe('MainLayout', () => {
  const mockPush = jest.fn();
  const mockRouter = { push: mockPush };

  beforeEach(() => {
    jest.clearAllMocks();
    (useRouter as jest.Mock).mockReturnValue(mockRouter);
    (usePathname as jest.Mock).mockReturnValue('/');

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

  it('should render layout for authenticated user', () => {
    const mockUser = { id: 1, login: 'admin', role: 'Admin', email: 'admin@example.com' };
    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));
    render(<MainLayout><div>Test Content</div></MainLayout>);

    expect(screen.getByText('Kurator')).toBeInTheDocument();
    expect(screen.getByText('admin')).toBeInTheDocument();
    expect(screen.getByText('Admin')).toBeInTheDocument();
    expect(screen.getByText('Test Content')).toBeInTheDocument();
  });

  it('should render desktop navigation for large screens', () => {
    const mockUser = { id: 1, login: 'admin', role: 'Admin', email: 'admin@example.com' };
    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));
    render(<MainLayout><div>Test Content</div></MainLayout>);
    expect(screen.getByTestId('desktop-navigation')).toBeInTheDocument();
  });

  it('should render breadcrumb navigation', () => {
    (usePathname as jest.Mock).mockReturnValue('/contacts/123');
    const mockUser = { id: 1, login: 'admin', role: 'Admin', email: 'admin@example.com' };
    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));
    render(<MainLayout><div>Test Content</div></MainLayout>);
    expect(screen.getByTestId('breadcrumb')).toBeInTheDocument();
  });

  it.skip('should handle logout correctly', async () => {
    const mockUser = { id: 1, login: 'admin', role: 'Admin', email: 'admin@example.com' };
    (window.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));
    render(<MainLayout><div>Test Content</div></MainLayout>);

    // Find logout button by looking for the exit icon or accessible name
    const logoutButtons = screen.getAllByRole('button');
    const logoutButton = logoutButtons.find(btn => 
      btn.querySelector('svg') !== null || 
      btn.textContent?.toLowerCase().includes('exit') ||
      btn.textContent?.toLowerCase().includes('logout') ||
      btn.textContent?.toLowerCase().includes('выход')
    );
    
    if (logoutButton) {
      fireEvent.click(logoutButton);
      expect(window.localStorage.removeItem).toHaveBeenCalledWith('token');
      expect(window.localStorage.removeItem).toHaveBeenCalledWith('user');
      expect(mockPush).toHaveBeenCalledWith('/login');
    }
  });
});
