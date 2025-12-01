import { render, screen, waitFor } from '@testing-library/react';
import WatchlistPage from '../page';
import { api } from '@/services/api';

// Mock the API
jest.mock('@/services/api', () => ({
  api: {
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
    patch: jest.fn(),
  },
}));

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
    prefetch: jest.fn(),
    back: jest.fn(),
    reload: jest.fn(),
    forward: jest.fn(),
  }),
  usePathname: () => '/watchlist',
}));

// Mock MainLayout
jest.mock('@/components/layout/MainLayout', () => {
  return function MainLayout({ children }: { children: React.ReactNode }) {
    return <div data-testid="main-layout">{children}</div>;
  };
});

describe('Watchlist Page', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should render watchlist for threat analyst', async () => {
    const mockWatchlist = {
      data: [
        {
          id: 1,
          fullName: 'Test Contact',
          roleStatus: 'Test Role',
          threatSource: 'Email',
          riskSphereId: 1,
          riskLevel: 'High',
          monitoringFrequency: 'Weekly',
          conflictDate: '2024-01-01T00:00:00Z',
          lastCheckDate: '2024-01-15T00:00:00Z',
          nextCheckDate: '2024-02-01T00:00:00Z',
          watchOwnerLogin: 'Analyst',
          dynamicsDescription: 'Escalating',
        },
      ],
    };

    (api.get as jest.Mock).mockResolvedValue({ data: mockWatchlist });

    render(<WatchlistPage />);

    await waitFor(() => {
      expect(screen.getByText('Test Contact')).toBeInTheDocument();
    });
  });

  it('should show empty state when watchlist is empty', async () => {
    (api.get as jest.Mock).mockResolvedValue({
      data: { data: [] },
    });

    render(<WatchlistPage />);

    await waitFor(() => {
      expect(screen.getByText(/Угрозы не найдены/i)).toBeInTheDocument();
    });
  });

  it('should handle loading state', () => {
    (api.get as jest.Mock).mockImplementation(
      () => new Promise(() => {})
    );

    render(<WatchlistPage />);
    expect(screen.getByText(/Загрузка угроз/i)).toBeInTheDocument();
  });

  it('should handle error state', async () => {
    (api.get as jest.Mock).mockRejectedValue(
      new Error('Failed to load')
    );

    render(<WatchlistPage />);

    await waitFor(() => {
      expect(screen.getByText(/Угрозы не найдены/i)).toBeInTheDocument();
    });
  });

  it('should display threat levels with appropriate colors', async () => {
    const mockWatchlist = {
      data: [
        {
          id: 1,
          fullName: 'High Threat Contact',
          roleStatus: 'Test',
          threatSource: 'Email',
          riskSphereId: 1,
          riskLevel: 'Critical',
          monitoringFrequency: 'Daily',
          conflictDate: '2024-01-01T00:00:00Z',
          lastCheckDate: '2024-01-15T00:00:00Z',
          nextCheckDate: '2024-02-01T00:00:00Z',
          watchOwnerLogin: 'Analyst',
        },
        {
          id: 2,
          fullName: 'Medium Threat Contact',
          roleStatus: 'Test',
          threatSource: 'Email',
          riskSphereId: 2,
          riskLevel: 'Medium',
          monitoringFrequency: 'Weekly',
          conflictDate: '2024-01-02T00:00:00Z',
          lastCheckDate: '2024-01-15T00:00:00Z',
          nextCheckDate: '2024-02-01T00:00:00Z',
          watchOwnerLogin: 'Analyst',
        },
        {
          id: 3,
          fullName: 'Low Threat Contact',
          roleStatus: 'Test',
          threatSource: 'Email',
          riskSphereId: 3,
          riskLevel: 'Low',
          monitoringFrequency: 'Monthly',
          conflictDate: '2024-01-03T00:00:00Z',
          lastCheckDate: '2024-01-15T00:00:00Z',
          nextCheckDate: '2024-02-01T00:00:00Z',
          watchOwnerLogin: 'Analyst',
        },
      ],
    };

    (api.get as jest.Mock).mockResolvedValue({ data: mockWatchlist });

    render(<WatchlistPage />);

    await waitFor(() => {
      expect(screen.getByText('Критический риск')).toBeInTheDocument();
      expect(screen.getByText('Средний риск')).toBeInTheDocument();
      expect(screen.getByText('Низкий риск')).toBeInTheDocument();
    });
  });
});
