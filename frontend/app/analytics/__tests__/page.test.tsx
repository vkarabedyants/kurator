import { render, screen, waitFor } from '@testing-library/react';
import AnalyticsPage from '../page';
import { api } from '@/services/api';

jest.mock('@/services/api', () => ({
  api: { get: jest.fn() },
}));

describe('Analytics Page', () => {
  const mockAnalytics = {
    contactsCount: 100,
    newContactsThisMonth: 10,
    interactionsCount: 500,
    interactionsThisMonth: 50,
    blockStatistics: [],
    influenceStatusDistribution: [],
    influenceTypeDistribution: [],
    topCurators: [],
    statusChanges: [],
    monthlyTrends: [],
  };

  beforeEach(() => {
    jest.clearAllMocks();
    (api.get as jest.Mock).mockResolvedValue({ data: mockAnalytics });
  });

  it('should render analytics dashboard', async () => {
    render(<AnalyticsPage />);

    await waitFor(() => {
      expect(screen.getByText('Панель аналитики')).toBeInTheDocument();
    });
  });

  it('should show loading state', () => {
    (api.get as jest.Mock).mockImplementation(() => new Promise(() => {}));

    render(<AnalyticsPage />);

    expect(screen.getByText('Загрузка аналитики...')).toBeInTheDocument();
  });

  it('should show empty state when no data', async () => {
    (api.get as jest.Mock).mockResolvedValue({ data: null });

    render(<AnalyticsPage />);

    await waitFor(() => {
      expect(screen.getByText('Данные аналитики недоступны')).toBeInTheDocument();
    });
  });
});
