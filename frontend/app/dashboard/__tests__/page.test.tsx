import { render, screen, waitFor } from '@testing-library/react';
import DashboardPage from '../page';
import { dashboardApi } from '@/services/api';
import { useRouter } from 'next/navigation';

// Mock the API
jest.mock('@/services/api', () => ({
  dashboardApi: {
    getCuratorDashboard: jest.fn(),
    getAdminDashboard: jest.fn(),
  },
}));

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
  usePathname: () => '/dashboard',
}));

// Mock localStorage
const localStorageMock = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
};

describe('Dashboard Page', () => {
  const mockPush = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
    Object.defineProperty(window, 'localStorage', {
      value: localStorageMock,
      writable: true,
    });
    (useRouter as jest.Mock).mockReturnValue({
      push: mockPush,
    });
  });

  it('should redirect to login when no user in localStorage', () => {
    localStorageMock.getItem.mockReturnValue(null);

    render(<DashboardPage />);

    expect(mockPush).toHaveBeenCalledWith('/login');
  });

  it('should load curator dashboard when user is curator', async () => {
    const mockUser = { id: 1, login: 'curator', role: 'Curator' };
    localStorageMock.getItem.mockReturnValue(JSON.stringify(mockUser));

    const mockData = {
      totalContacts: 100,
      interactionsLastMonth: 250,
      averageInteractionInterval: 7,
      overdueContacts: 5,
      contactsRequiringAttention: [],
      recentInteractions: [],
      contactsByInfluenceStatus: {},
      upcomingTasks: [],
      recentActivity: [],
    };

    (dashboardApi.getCuratorDashboard as jest.Mock).mockResolvedValue(mockData);

    render(<DashboardPage />);

    await waitFor(() => {
      expect(dashboardApi.getCuratorDashboard).toHaveBeenCalled();
    });
  });

  it('should handle error state', async () => {
    const mockUser = { id: 1, login: 'curator', role: 'Curator' };
    localStorageMock.getItem.mockReturnValue(JSON.stringify(mockUser));

    (dashboardApi.getCuratorDashboard as jest.Mock).mockRejectedValue(
      new Error('Network error')
    );

    render(<DashboardPage />);

    await waitFor(() => {
      expect(dashboardApi.getCuratorDashboard).toHaveBeenCalled();
    });
  });
});