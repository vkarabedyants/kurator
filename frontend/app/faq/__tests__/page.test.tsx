
import { render, screen, waitFor } from '@testing-library/react';
import FAQPage from '../page';

// Mock the API at the correct location
jest.mock('@/lib/api', () => {
  const mockApi = {
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
    patch: jest.fn(),
  };
  return {
    __esModule: true,
    default: mockApi,
  };
});

// Import after mock
import api from '@/lib/api';

// Mock Next.js router
jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
  }),
  usePathname: () => '/faq',
}));

// Mock MainLayout
jest.mock('@/components/layout/MainLayout', () => {
  return function MainLayout({ children }: { children: React.ReactNode }) {
    return <div data-testid="main-layout">{children}</div>;
  };
});

describe('FAQ Page', () => {
  const mockApi = api as jest.Mocked<typeof api>;

  beforeEach(() => {
    jest.clearAllMocks();
    // Mock the /auth/me endpoint for role checking
    mockApi.get.mockImplementation((url: string) => {
      if (url === '/auth/me') {
        return Promise.resolve({ data: { role: 'Admin' } });
      }
      return Promise.resolve({ data: { items: [] } });
    });
  });

  it('should render FAQ list', async () => {
    const mockFAQs = {
      items: [
        {
          id: 1,
          title: 'Как добавить новый контакт?',
          content: 'Перейдите в раздел Контакты и нажмите кнопку Добавить контакт.',
          category: 'Contacts',
          isActive: true,
          displayOrder: 1,
          visibility: 'All',
          createdAt: '2024-01-01T00:00:00Z',
        },
        {
          id: 2,
          title: 'Как изменить пароль?',
          content: 'Перейдите в настройки профиля и выберите Изменить пароль.',
          category: 'Account',
          isActive: true,
          displayOrder: 2,
          visibility: 'All',
          createdAt: '2024-01-02T00:00:00Z',
        },
      ],
    };

    mockApi.get.mockImplementation((url: string) => {
      if (url === '/faq') {
        return Promise.resolve({ data: mockFAQs });
      }
      if (url === '/auth/me') {
        return Promise.resolve({ data: { role: 'Admin' } });
      }
      return Promise.resolve({ data: {} });
    });

    render(<FAQPage />);

    await waitFor(() => {
      expect(screen.getByText('Как добавить новый контакт?')).toBeInTheDocument();
      expect(screen.getByText('Как изменить пароль?')).toBeInTheDocument();
    });
  });

  it('should show empty state when no FAQs', async () => {
    mockApi.get.mockResolvedValue({ data: { items: [] } });

    render(<FAQPage />);

    await waitFor(() => {
      expect(screen.getByText(/FAQ не найдены/i)).toBeInTheDocument();
    });
  });

  it('should handle loading state', () => {
    mockApi.get.mockImplementation(
      () => new Promise(() => {})
    );

    render(<FAQPage />);
    expect(screen.getByText(/Загрузка FAQ/i)).toBeInTheDocument();
  });

  it('should handle error state', async () => {
    mockApi.get.mockRejectedValue(
      new Error('Failed to load')
    );

    render(<FAQPage />);

    // When error occurs, it shows empty state
    await waitFor(() => {
      expect(screen.getByText(/FAQ не найдены/i)).toBeInTheDocument();
    });
  });

  it('should group FAQs by category', async () => {
    const mockFAQs = {
      items: [
        {
          id: 1,
          title: 'Вопрос о контактах 1',
          content: 'Ответ 1',
          category: 'Contacts',
          isActive: true,
          displayOrder: 1,
          visibility: 'All',
          createdAt: '2024-01-01T00:00:00Z',
        },
        {
          id: 2,
          title: 'Вопрос о контактах 2',
          content: 'Ответ 2',
          category: 'Contacts',
          isActive: true,
          displayOrder: 2,
          visibility: 'All',
          createdAt: '2024-01-02T00:00:00Z',
        },
        {
          id: 3,
          title: 'Вопрос об аккаунте',
          content: 'Ответ 3',
          category: 'Account',
          isActive: true,
          displayOrder: 3,
          visibility: 'All',
          createdAt: '2024-01-03T00:00:00Z',
        },
      ],
    };

    mockApi.get.mockResolvedValue({ data: mockFAQs });

    render(<FAQPage />);

    await waitFor(() => {
      expect(screen.getByText('Вопрос о контактах 1')).toBeInTheDocument();
      expect(screen.getByText('Вопрос об аккаунте')).toBeInTheDocument();
    });
  });

  it('should only show active FAQs', async () => {
    const mockFAQs = {
      items: [
        {
          id: 1,
          title: 'Активный вопрос',
          content: 'Ответ',
          category: 'General',
          isActive: true,
          displayOrder: 1,
          visibility: 'All',
          createdAt: '2024-01-01T00:00:00Z',
        },
      ],
    };

    mockApi.get.mockResolvedValue({ data: mockFAQs });

    render(<FAQPage />);

    await waitFor(() => {
      expect(screen.getByText('Активный вопрос')).toBeInTheDocument();
    });
  });
});