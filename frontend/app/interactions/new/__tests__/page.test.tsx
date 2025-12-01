import { render, screen, waitFor } from '@testing-library/react';
import NewInteractionPage from '../page';
import { api } from '@/services/api';

jest.mock('@/services/api', () => ({
  api: {
    get: jest.fn(),
    post: jest.fn(),
  },
}));

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn(), back: jest.fn() }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/interactions/new',
}));

describe('New Interaction Page', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (api.get as jest.Mock).mockResolvedValue({ data: { items: [] } });
  });

  it('should render interaction form', async () => {
    render(<NewInteractionPage />);

    await waitFor(() => {
      expect(screen.getByRole('heading', { name: /создать новое взаимодействие/i })).toBeInTheDocument();
    });
  });

  it('should show loading state initially', () => {
    (api.get as jest.Mock).mockImplementation(() => new Promise(() => {}));

    render(<NewInteractionPage />);

    expect(screen.getByText(/загрузка/i)).toBeInTheDocument();
  });
});
