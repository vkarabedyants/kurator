import { render, screen, waitFor } from '@testing-library/react';
import InteractionsPage from '../page';
import { interactionsApi } from '@/services/api';

jest.mock('@/services/api', () => ({
  interactionsApi: {
    getAll: jest.fn(),
    delete: jest.fn(),
  },
}));

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn() }),
  usePathname: () => '/interactions',
}));

jest.mock('next/link', () => {
  return function Link({ children, href }: any) {
    return <a href={href}>{children}</a>;
  };
});

jest.mock('@/components/layout/MainLayout', () => {
  return function MainLayout({ children }: { children: React.ReactNode }) {
    return <div data-testid="main-layout">{children}</div>;
  };
});

describe('Interactions Page', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    (interactionsApi.getAll as jest.Mock).mockResolvedValue({ 
      data: [
        { id: 1, description: 'Meeting with John', date: '2024-01-01', contactId: 1, interactionType: 'Meeting' }
      ],
      totalPages: 1,
      totalCount: 1 
    });
  });

  it('should render interactions list', async () => {
    render(<InteractionsPage />);

    await waitFor(() => {
      expect(screen.getByText('Журнал взаимодействий')).toBeInTheDocument();
    });
  });

  it('should show loading state', () => {
    (interactionsApi.getAll as jest.Mock).mockImplementation(() => new Promise(() => {}));

    render(<InteractionsPage />);

    expect(screen.getByText('Журнал взаимодействий')).toBeInTheDocument();
  });

  it('should display add button', async () => {
    render(<InteractionsPage />);

    await waitFor(() => {
      expect(screen.getByText('Добавить взаимодействие')).toBeInTheDocument();
    });
  });
});
