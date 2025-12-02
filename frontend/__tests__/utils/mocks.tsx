
import React from 'react';
import { QueryClient } from '@tanstack/react-query';

// Mock Next.js router
export const mockRouter = {
  push: jest.fn(),
  replace: jest.fn(),
  back: jest.fn(),
  forward: jest.fn(),
  refresh: jest.fn(),
  prefetch: jest.fn(),
};

// Mock Next.js pathname hook
export const mockUsePathname = jest.fn();

// Mock Next.js router hook
export const mockUseRouter = jest.fn(() => mockRouter);

// Mock Next.js search params hook
export const mockUseSearchParams = jest.fn(() => new URLSearchParams());

// Setup Next.js mocks
export const setupNextJSMocks = () => {
  jest.doMock('next/navigation', () => ({
    useRouter: mockUseRouter,
    usePathname: mockUsePathname,
    useSearchParams: mockUseSearchParams,
  }));

  jest.doMock('next/link', () => {
    return ({ children, href, ...props }: any) => (
      <a href={href} {...props}>
        {children}
      </a>
    );
  });
};

// Mock React Query
export const createMockQueryClient = () => {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        cacheTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
  });
};

// Mock API service responses
export const mockApiService = {
  auth: {
    login: jest.fn(),
    logout: jest.fn(),
    refresh: jest.fn(),
    getCurrentUser: jest.fn(),
  },
  contacts: {
    getAll: jest.fn(),
    getById: jest.fn(),
    create: jest.fn(),
    update: jest.fn(),
    delete: jest.fn(),
    getOverdue: jest.fn(),
  },
  interactions: {
    getAll: jest.fn(),
    getById: jest.fn(),
    create: jest.fn(),
    update: jest.fn(),
    delete: jest.fn(),
    getByContact: jest.fn(),
  },
  blocks: {
    getAll: jest.fn(),
    getById: jest.fn(),
    create: jest.fn(),
    update: jest.fn(),
    assignCurator: jest.fn(),
    removeCurator: jest.fn(),
  },
  users: {
    getAll: jest.fn(),
    getById: jest.fn(),
    create: jest.fn(),
    update: jest.fn(),
    delete: jest.fn(),
    changePassword: jest.fn(),
    getCurators: jest.fn(),
    getStatistics: jest.fn(),
  },
  analytics: {
    getDashboard: jest.fn(),
    getInteractionsReport: jest.fn(),
    getBlocksReport: jest.fn(),
    getUserActivity: jest.fn(),
  },
  audit: {
    getLogs: jest.fn(),
    getLogById: jest.fn(),
    exportLogs: jest.fn(),
  },
};

// Mock fetch for API calls
export const mockFetch = (responseData: any = {}, status: number = 200) => {
  global.fetch = jest.fn(() =>
    Promise.resolve({
      ok: status >= 200 && status < 300,
      status,
      json: () => Promise.resolve(responseData),
      text: () => Promise.resolve(JSON.stringify(responseData)),
      headers: new Headers({ 'content-type': 'application/json' }),
    } as Response)
  );
};

// Mock successful API responses
export const mockSuccessfulApiResponse = (data: any) => ({
  data,
  success: true,
  message: 'Success',
});

// Mock error API responses
export const mockErrorApiResponse = (message: string = 'Error occurred') => ({
  data: null,
  success: false,
  message,
  errors: [message],
});

// Mock loading states
export const mockLoadingState = {
  isLoading: true,
  isError: false,
  data: undefined,
  error: null,
};

export const mockErrorState = {
  isLoading: false,
  isError: true,
  data: undefined,
  error: new Error('Test error'),
};

export const mockSuccessState = <T,>(data: T) => ({
  isLoading: false,
  isError: false,
  data,
  error: null,
});

// Mock form event
export const mockFormEvent = (values: Record<string, string> = {}) => ({
  preventDefault: jest.fn(),
  target: {
    elements: Object.keys(values).reduce((acc, key) => {
      acc[key] = { value: values[key] };
      return acc;
    }, {} as Record<string, { value: any }>),
  },
});

// Mock user event handlers
export const mockEventHandlers = {
  onClick: jest.fn(),
  onChange: jest.fn((e) => e),
  onSubmit: jest.fn((e) => e.preventDefault()),
  onFocus: jest.fn(),
  onBlur: jest.fn(),
  onMouseEnter: jest.fn(),
  onMouseLeave: jest.fn(),
  onKeyDown: jest.fn(),
  onKeyUp: jest.fn(),
};

// Mock component props
export const mockComponentProps = {
  className: 'test-class',
  style: { color: 'red' },
  'data-testid': 'test-component',
  children: 'Test content',
};

// Mock table props
export const mockTableProps = {
  data: [
    { id: 1, name: 'Test 1', status: 'Active' },
    { id: 2, name: 'Test 2', status: 'Inactive' },
  ],
  columns: [
    { key: 'name' as const, header: 'Name' },
    { key: 'status' as const, header: 'Status' },
  ],
  loading: false,
  onRowClick: jest.fn(),
};

// Mock modal props
export const mockModalProps = {
  isOpen: true,
  onClose: jest.fn(),
  title: 'Test Modal',
  children: <div>Modal content</div>,
};

// Mock form props
export const mockFormProps = {
  initialValues: { name: '', email: '' },
  onSubmit: jest.fn(),
  validationSchema: {},
  isSubmitting: false,
  errors: {},
  touched: {},
};

// Mock chart data
export const mockChartData = {
  labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May'],
  datasets: [
    {
      label: 'Interactions',
      data: [12, 19, 3, 5, 2],
      backgroundColor: 'rgba(75, 192, 192, 0.2)',
      borderColor: 'rgba(75, 192, 192, 1)',
      borderWidth: 1,
    },
  ],
};

// Mock audit log entry
export const mockAuditLogEntry = {
  id: 1,
  timestamp: '2024-01-15T10:00:00Z',
  userId: 1,
  userName: 'Admin User',
  action: 'CREATE',
  entityType: 'Contact',
  entityId: 123,
  changes: {
    oldValues: {},
    newValues: { name: 'New Contact' },
  },
  ipAddress: '192.168.1.1',
  userAgent: 'Mozilla/5.0...',
};

// Mock notification system
export const mockNotification = {
  success: jest.fn(),
  error: jest.fn(),
  warning: jest.fn(),
  info: jest.fn(),
};

// Mock theme provider
export const mockTheme = {
  colors: {
    primary: '#007bff',
    secondary: '#6c757d',
    success: '#28a745',
    danger: '#dc3545',
    warning: '#ffc107',
    info: '#17a2b8',
  },
  breakpoints: {
    sm: '576px',
    md: '768px',
    lg: '992px',
    xl: '1200px',
  },
};

// Mock i18n (internationalization)
export const mockI18n = {
  t: jest.fn((key: string) => key),
  language: 'ru',
  changeLanguage: jest.fn(),
};

// Setup all mocks
export const setupAllMocks = () => {
  setupNextJSMocks();
  mockFetch();

  // Mock localStorage
  Object.defineProperty(window, 'localStorage', {
    value: {
      getItem: jest.fn(),
      setItem: jest.fn(),
      removeItem: jest.fn(),
      clear: jest.fn(),
    },
    writable: true,
  });

  // Mock IntersectionObserver
  global.IntersectionObserver = class IntersectionObserver {
    constructor() {}
    observe() {
      return null;
    }
    disconnect() {
      return null;
    }
    unobserve() {
      return null;
    }
  };

  // Mock ResizeObserver
  global.ResizeObserver = class ResizeObserver {
    constructor() {}
    observe() {
      return null;
    }
    disconnect() {
      return null;
    }
    unobserve() {
      return null;
    }
  };

  // Mock console methods
  const originalConsoleError = console.error;
  const originalConsoleWarn = console.warn;

  beforeAll(() => {
    console.error = jest.fn();
    console.warn = jest.fn();
  });

  afterAll(() => {
    console.error = originalConsoleError;
    console.warn = originalConsoleWarn;
  });
};

// Cleanup mocks after each test
export const cleanupMocks = () => {
  jest.clearAllMocks();
};

// Utility to wait for async operations
export const waitForAsync = () => new Promise((resolve) => setTimeout(resolve, 0));

// Mock server responses for testing
export const mockServerResponses = {
  contacts: {
    list: {
      data: [
        { id: 1, name: 'Contact 1', email: 'contact1@example.com' },
        { id: 2, name: 'Contact 2', email: 'contact2@example.com' },
      ],
      pagination: { page: 1, limit: 10, total: 2, totalPages: 1 },
    },
    detail: {
      id: 1,
      name: 'Contact 1',
      email: 'contact1@example.com',
      phone: '+1234567890',
    },
  },
  interactions: {
    list: {
      data: [
        { id: 1, type: 'Meeting', date: '2024-01-15', description: 'Test meeting' },
        { id: 2, type: 'Call', date: '2024-01-16', description: 'Test call' },
      ],
      pagination: { page: 1, limit: 10, total: 2, totalPages: 1 },
    },
  },
  blocks: {
    list: {
      data: [
        { id: 1, name: 'Block 1', code: 'B1', status: 'Active' },
        { id: 2, name: 'Block 2', code: 'B2', status: 'Inactive' },
      ],
    },
  },
  users: {
    list: {
      data: [
        { id: 1, login: 'user1', role: 'Admin', isActive: true },
        { id: 2, login: 'user2', role: 'Curator', isActive: true },
      ],
    },
  },
  analytics: {
    dashboard: {
      totalContacts: 150,
      totalInteractions: 450,
      activeBlocks: 12,
      overdueContacts: 5,
    },
  },
  audit: {
    logs: {
      data: [mockAuditLogEntry],
      pagination: { page: 1, limit: 10, total: 1, totalPages: 1 },
    },
  },
};
