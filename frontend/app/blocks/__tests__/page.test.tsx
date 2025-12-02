
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import BlocksPage from '../page';
import { blocksApi, usersApi } from '@/services/api';

jest.mock('@/services/api', () => ({
  blocksApi: {
    getAll: jest.fn(),
    create: jest.fn(),
    update: jest.fn(),
    delete: jest.fn(),
  },
  usersApi: {
    getAll: jest.fn(),
  },
}));

jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
    prefetch: jest.fn(),
  }),
  usePathname: () => '/blocks',
}));

jest.mock('@/components/layout/MainLayout', () => {
  return function MainLayout({ children }: { children: React.ReactNode }) {
    return <div data-testid="main-layout">{children}</div>;
  };
});

describe('Blocks Page', () => {
  const mockUsers = [
    { id: 1, login: 'curator1', role: 'Curator', createdAt: '2024-01-01T00:00:00Z' },
    { id: 2, login: 'curator2', role: 'Curator', createdAt: '2024-01-01T00:00:00Z' },
  ];

  const mockBlocks = [
    {
      id: 1,
      code: 'BLK-001',
      name: 'Test Block',
      status: 'Active',
      description: 'Test description',
      curators: [
        { userId: 1, userLogin: 'curator1', curatorType: 'Primary', assignedAt: '2024-01-01T00:00:00Z' },
        { userId: 2, userLogin: 'curator2', curatorType: 'Backup', assignedAt: '2024-01-01T00:00:00Z' },
      ],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    },
    {
      id: 2,
      code: 'BLK-002',
      name: 'Another Block',
      status: 'Active',
      description: 'Another description',
      curators: [],
      createdAt: '2024-01-02T00:00:00Z',
      updatedAt: '2024-01-02T00:00:00Z',
    },
  ];

  beforeEach(() => {
    jest.clearAllMocks();
    (usersApi.getAll as jest.Mock).mockResolvedValue(mockUsers);
    (blocksApi.getAll as jest.Mock).mockResolvedValue(mockBlocks);
  });

  describe('Initial Load', () => {
    it('should render blocks list correctly', async () => {
      render(<BlocksPage />);
      await waitFor(() => {
        expect(screen.getByText('Test Block')).toBeInTheDocument();
        expect(screen.getByText('Another Block')).toBeInTheDocument();
      });
    });

    it('should display curator names', async () => {
      render(<BlocksPage />);
      await waitFor(() => {
        expect(screen.getByText('Test Block')).toBeInTheDocument();
      });
      expect(screen.getByText('curator1')).toBeInTheDocument();
    });

    it('should show loading spinner initially', () => {
      (blocksApi.getAll as jest.Mock).mockImplementation(() => new Promise(() => {}));
      (usersApi.getAll as jest.Mock).mockImplementation(() => new Promise(() => {}));
      const { container } = render(<BlocksPage />);
      expect(container.querySelector('.animate-spin')).toBeInTheDocument();
    });
  });

  describe('Empty State', () => {
    it('should display empty state when no blocks', async () => {
      (blocksApi.getAll as jest.Mock).mockResolvedValue([]);
      render(<BlocksPage />);
      await waitFor(() => {
        expect(screen.getByText(/Блоки не найдены/i)).toBeInTheDocument();
      });
    });
  });

  describe('Error Handling', () => {
    it('should display error on API failure', async () => {
      (blocksApi.getAll as jest.Mock).mockRejectedValue(new Error('Network error'));
      render(<BlocksPage />);
      await waitFor(() => {
        expect(screen.getByText(/Network error/i)).toBeInTheDocument();
      });
    });
  });

  describe('Block Actions', () => {
    it('should show add form when clicking add button', async () => {
      render(<BlocksPage />);
      await waitFor(() => {
        expect(screen.getByText('Test Block')).toBeInTheDocument();
      });
      const addButton = screen.getByRole('button', { name: /Добавить блок/i });
      fireEvent.click(addButton);
      expect(screen.getByText(/Добавить новый блок/i)).toBeInTheDocument();
    });

    it('should handle block deletion', async () => {
      (blocksApi.delete as jest.Mock).mockResolvedValue({});
      jest.spyOn(window, 'confirm').mockReturnValue(true);
      render(<BlocksPage />);
      await waitFor(() => {
        expect(screen.getByText('Test Block')).toBeInTheDocument();
      });
      const deleteButtons = screen.getAllByText('Удалить');
      fireEvent.click(deleteButtons[0]);
      await waitFor(() => {
        expect(blocksApi.delete).toHaveBeenCalledWith(1);
      });
    });
  });
});
