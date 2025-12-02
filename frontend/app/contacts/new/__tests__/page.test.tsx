import { render, screen, waitFor } from '@testing-library/react';
import NewContactPage from '../page';
import { api } from '@/services/api';

jest.mock('@/services/api', () => ({
  api: {
    get: jest.fn(),
    post: jest.fn(),
  },
}));

jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
    back: jest.fn(),
  }),
  usePathname: () => '/contacts/new',
}));

describe('New Contact Page', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should render contact creation form', async () => {
    (api.get as jest.Mock).mockImplementation((url: string) => {
      if (url.includes('blocks')) {
        return Promise.resolve({ data: [{ id: 1, name: 'Test Block', code: 'TB' }] });
      }
      if (url.includes('references')) {
        return Promise.resolve({ data: {} });
      }
      return Promise.resolve({ data: {} });
    });

    render(<NewContactPage />);

    await waitFor(() => {
      expect(screen.getByText('Создать новый контакт')).toBeInTheDocument();
    });
  });

  it('should show loading state initially', () => {
    (api.get as jest.Mock).mockImplementation(() => new Promise(() => {}));

    render(<NewContactPage />);

    // Page uses tCommon('loading') = 'Загрузка...'
    expect(screen.getByText('Загрузка...')).toBeInTheDocument();
  });

  it('should show no blocks message', async () => {
    (api.get as jest.Mock).mockImplementation((url: string) => {
      if (url.includes('blocks')) {
        return Promise.resolve({ data: [] });
      }
      return Promise.resolve({ data: {} });
    });

    render(<NewContactPage />);

    await waitFor(() => {
      expect(screen.getByText('Нет доступных блоков')).toBeInTheDocument();
    });
  });
});
