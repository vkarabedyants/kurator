import { render, screen, waitFor } from '@testing-library/react';
import ReferencesPage from '../page';
import { referencesApi } from '@/services/api';

// Mock the API
jest.mock('@/services/api', () => ({
  referencesApi: {
    getByCategory: jest.fn(),
    create: jest.fn(),
    update: jest.fn(),
    delete: jest.fn(),
  },
}));

// Create stable router mock
const mockPush = jest.fn();
const mockRouter = {
  push: mockPush,
  replace: jest.fn(),
  prefetch: jest.fn(),
  back: jest.fn(),
  forward: jest.fn(),
};

jest.mock('next/navigation', () => ({
  useRouter: () => mockRouter,
  usePathname: () => '/references',
}));

describe('References Page', () => {
  // Store original localStorage
  const originalLocalStorage = global.localStorage;

  beforeEach(() => {
    jest.clearAllMocks();
    // Mock localStorage
    const localStorageMock: Storage = {
      length: 0,
      key: jest.fn(),
      getItem: jest.fn(),
      setItem: jest.fn(),
      removeItem: jest.fn(),
      clear: jest.fn(),
    };
    Object.defineProperty(global, 'localStorage', {
      value: localStorageMock,
      writable: true,
    });
  });

  afterEach(() => {
    Object.defineProperty(global, 'localStorage', {
      value: originalLocalStorage,
      writable: true,
    });
  });

  it('should redirect to login if no user', async () => {
    (global.localStorage.getItem as jest.Mock).mockReturnValue(null);

    render(<ReferencesPage />);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/login');
    });
  });

  it('should redirect to dashboard if not admin', async () => {
    const mockUser = { id: 1, login: 'curator', role: 'Curator' };
    (global.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    render(<ReferencesPage />);

    await waitFor(() => {
      expect(mockPush).toHaveBeenCalledWith('/dashboard');
    });
  });

  it('should render references page for admin', async () => {
    const mockUser = { id: 1, login: 'admin', role: 'Admin' };
    (global.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    const mockReferences = [
      {
        id: 1,
        value: 'Organization 1',
        description: 'Test organization',
        isActive: true,
        displayOrder: 1,
      },
    ];

    (referencesApi.getByCategory as jest.Mock).mockResolvedValue(mockReferences);

    render(<ReferencesPage />);

    await waitFor(() => {
      expect(screen.getByText('Управление справочниками')).toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByText('Organization 1')).toBeInTheDocument();
    });
  });

  it('should show category selector tabs', async () => {
    const mockUser = { id: 1, login: 'admin', role: 'Admin' };
    (global.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    (referencesApi.getByCategory as jest.Mock).mockResolvedValue([]);

    render(<ReferencesPage />);

    await waitFor(() => {
      expect(screen.getByText('Организации')).toBeInTheDocument();
      expect(screen.getByText('Статусы влияния')).toBeInTheDocument();
      expect(screen.getByText('Типы влияния')).toBeInTheDocument();
      expect(screen.getByText('Каналы коммуникации')).toBeInTheDocument();
    });
  });

  it('should show add button for admin', async () => {
    const mockUser = { id: 1, login: 'admin', role: 'Admin' };
    (global.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    (referencesApi.getByCategory as jest.Mock).mockResolvedValue([]);

    render(<ReferencesPage />);

    await waitFor(() => {
      expect(screen.getByText(/Добавить новое значение/i)).toBeInTheDocument();
    });
  });

  it('should handle error state', async () => {
    const mockUser = { id: 1, login: 'admin', role: 'Admin' };
    (global.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    (referencesApi.getByCategory as jest.Mock).mockRejectedValue(
      new Error('Failed to load')
    );

    render(<ReferencesPage />);

    await waitFor(() => {
      expect(screen.getByText(/Не удалось загрузить справочные значения/i)).toBeInTheDocument();
    });
  });

  it('should display values in a table', async () => {
    const mockUser = { id: 1, login: 'admin', role: 'Admin' };
    (global.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    const mockReferences = [
      {
        id: 1,
        value: 'Org 1',
        description: 'Organization 1',
        isActive: true,
        displayOrder: 1,
      },
      {
        id: 2,
        value: 'Org 2',
        description: 'Organization 2',
        isActive: true,
        displayOrder: 2,
      },
    ];

    (referencesApi.getByCategory as jest.Mock).mockResolvedValue(mockReferences);

    render(<ReferencesPage />);

    await waitFor(() => {
      expect(screen.getByText('Org 1')).toBeInTheDocument();
      expect(screen.getByText('Org 2')).toBeInTheDocument();
    });
  });

  it('should show empty state when no values', async () => {
    const mockUser = { id: 1, login: 'admin', role: 'Admin' };
    (global.localStorage.getItem as jest.Mock).mockReturnValue(JSON.stringify(mockUser));

    (referencesApi.getByCategory as jest.Mock).mockResolvedValue([]);

    render(<ReferencesPage />);

    await waitFor(() => {
      expect(screen.getByText(/Для этой категории значений не найдено/i)).toBeInTheDocument();
    });
  });
});
