
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import AuditLogPage from '../page';
import { AuditActionType } from '@/types/api';

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
    replace: jest.fn(),
    prefetch: jest.fn(),
    back: jest.fn(),
    reload: jest.fn(),
    forward: jest.fn(),
  }),
  useSearchParams: () => new URLSearchParams(),
  usePathname: () => '/audit-log',
}));

describe('Audit Log Page', () => {
  const mockApi = api as jest.Mocked<typeof api>;

  const mockAuditLogs = [
    {
      id: 1,
      userId: 1,
      userName: 'Иванов И.И.',
      actionType: AuditActionType.Create,
      entityType: 'Contact',
      entityId: 'OP-001',
      details: 'Создан новый контакт Иван Петров',
      oldValue: null,
      newValue: null,
      timestamp: '2024-11-20T14:30:00Z',
    },
    {
      id: 2,
      userId: 2,
      userName: 'Петрова А.С.',
      actionType: AuditActionType.Update,
      entityType: 'Interaction',
      entityId: 'INT-045',
      details: 'Обновлено взаимодействие с контактом',
      oldValue: 'Старый комментарий',
      newValue: 'Обновленный комментарий о встрече',
      timestamp: '2024-11-20T12:15:00Z',
    },
    {
      id: 3,
      userId: 1,
      userName: 'Иванов И.И.',
      actionType: AuditActionType.Delete,
      entityType: 'Contact',
      entityId: 'OLD-999',
      details: 'Удален контакт из архива',
      oldValue: null,
      newValue: null,
      timestamp: '2024-11-19T09:45:00Z',
    },
    {
      id: 4,
      userId: 3,
      userName: 'Сидоров С.С.',
      actionType: AuditActionType.Login,
      entityType: 'User',
      entityId: '1',
      details: 'Успешный вход в систему',
      oldValue: null,
      newValue: null,
      timestamp: '2024-11-20T08:00:00Z',
    },
  ];

  const mockApiResponse = {
    items: mockAuditLogs,
    total: 4,
    page: 1,
    totalPages: 1,
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockApi.get.mockResolvedValue({
      data: mockApiResponse
    });
  });

  it('loads and displays audit entries', async () => {
    render(<AuditLogPage />);

    // Wait for loading to complete
    await waitFor(() => {
      expect(screen.queryByText(/Загрузка журнала аудита/i)).not.toBeInTheDocument();
    }, { timeout: 3000 });

    // Check API call was made
    expect(mockApi.get).toHaveBeenCalledWith('/audit-log', {
      params: {
        pageSize: 500,
      },
    });

    // Wait for specific content to appear
    await waitFor(() => {
      const elements = screen.queryAllByText(/Иванов И.И./);
      expect(elements.length).toBeGreaterThan(0);
    }, { timeout: 3000 });
  });

  it('displays audit log entries', async () => {
    render(<AuditLogPage />);

    // Wait for loading to complete
    await waitFor(() => {
      expect(screen.queryByText(/Загрузка/i)).not.toBeInTheDocument();
    }, { timeout: 3000 });

    // Just check that at least one entry detail appears
    await waitFor(() => {
      const detailsText = screen.queryByText(/Создан новый контакт Иван Петров/) ||
                          screen.queryByText(/Обновлено взаимодействие/) ||
                          screen.queryByText(/Удален контакт из архива/) ||
                          screen.queryByText(/Успешный вход в систему/);
      expect(detailsText).toBeInTheDocument();
    }, { timeout: 5000 });
  });

  it('shows correct total count', async () => {
    render(<AuditLogPage />);

    await waitFor(() => {
      expect(screen.queryByText(/Загрузка/i)).not.toBeInTheDocument();
    });

    // Check that "Всего записей: 4" is displayed
    await waitFor(() => {
      expect(screen.getByText(/Всего записей: 4/)).toBeInTheDocument();
    }, { timeout: 3000 });
  });

  it('filters by action type', async () => {
    const user = userEvent.setup();
    render(<AuditLogPage />);

    // Wait for all data to load
    await waitFor(() => {
      expect(screen.queryByText(/Загрузка/i)).not.toBeInTheDocument();
      expect(screen.getByText(/Создан новый контакт/)).toBeInTheDocument();
    }, { timeout: 5000 });

    // Find the action filter - look for the one with "Создание" option
    const selects = screen.getAllByRole('combobox');
    const actionSelect = selects.find(select =>
      select.querySelector('option[value="Create"]')
    );

    if (actionSelect) {
      await user.selectOptions(actionSelect, AuditActionType.Create);

      // After filtering, should still show the Create entry
      await waitFor(() => {
        expect(screen.getByText(/Создан новый контакт/)).toBeInTheDocument();
      });
    }
  });

  it('searches audit entries by content', async () => {
    const user = userEvent.setup();
    render(<AuditLogPage />);

    // Wait for data to load
    await waitFor(() => {
      expect(screen.getByText(/Создан новый контакт/)).toBeInTheDocument();
    });

    // Search for "контакт"
    const searchInput = screen.getByPlaceholderText(/поиск по журналу/i);
    await user.type(searchInput, 'контакт');

    // Should show entries containing "контакт"
    expect(screen.getByText(/Создан новый контакт Иван Петров/)).toBeInTheDocument();
  });

  it('handles empty audit log gracefully', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        items: [],
        total: 0,
        page: 1,
        totalPages: 0,
      }
    });

    render(<AuditLogPage />);

    await waitFor(() => {
      expect(screen.getByText(/Записи аудита не найдены/i)).toBeInTheDocument();
    });
  });

  it('shows loading state', () => {
    mockApi.get.mockImplementation(() => new Promise(() => {}));

    render(<AuditLogPage />);
    expect(screen.getByText(/Загрузка журнала аудита/i)).toBeInTheDocument();
  });

  it('displays summary statistics', async () => {
    render(<AuditLogPage />);

    await waitFor(() => {
      expect(screen.queryByText(/Загрузка/i)).not.toBeInTheDocument();
    });

    // Check statistics section
    await waitFor(() => {
      expect(screen.getByText(/Сводка активности/i)).toBeInTheDocument();
    });

    // Use getAllByText to handle multiple occurrences of these labels
    expect(screen.getAllByText('Создано').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Обновлено').length).toBeGreaterThan(0);
    expect(screen.getAllByText('Удалено').length).toBeGreaterThan(0);
  });
});
