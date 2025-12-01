
import React, { ReactElement } from 'react';
import { render, RenderOptions } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

// Mock implementations for common dependencies
export const mockUser = {
  id: 1,
  login: 'testuser',
  email: 'test@example.com',
  role: 'Admin',
  isActive: true,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

export const mockContact = {
  id: 1,
  contactId: 'TEST-001',
  fullName: 'Иванов Иван Иванович',
  phone: '+7 (999) 123-45-67',
  email: 'ivanov@example.com',
  blockId: 1,
  responsibleCuratorId: 1,
  status: 'Active',
  notes: 'Тестовый контакт',
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

export const mockInteraction = {
  id: 1,
  contactId: 1,
  curatorId: 1,
  type: 'Meeting',
  description: 'Встреча с клиентом',
  date: '2024-01-15T10:00:00Z',
  duration: 60,
  outcome: 'Успешно',
  notes: 'Обсудили требования',
  createdAt: '2024-01-15T10:00:00Z',
  updatedAt: '2024-01-15T10:00:00Z',
};

export const mockBlock = {
  id: 1,
  name: 'Тестовый блок',
  code: 'TEST',
  description: 'Блок для тестирования',
  status: 'Active',
  curatorAssignments: [],
  contacts: [],
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

// Create a custom render function that includes providers
const AllTheProviders: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const queryClient = new QueryClient({
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

  return (
    <QueryClientProvider client={queryClient}>
      {children}
    </QueryClientProvider>
  );
};

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>,
) => render(ui, { wrapper: AllTheProviders, ...options });

// Mock API responses
export const mockApiResponse = {
  success: (data: any) => ({
    data,
    success: true,
    message: 'Success',
  }),
  error: (message: string = 'Error occurred') => ({
    data: null,
    success: false,
    message,
    errors: [message],
  }),
  paginated: (data: any[], total: number = data.length, page: number = 1, limit: number = 10) => ({
    data,
    pagination: {
      page,
      limit,
      total,
      totalPages: Math.ceil(total / limit),
    },
    success: true,
  }),
};

// Mock fetch for API calls
export const mockFetchResponse = (data: any, status: number = 200) => {
  return Promise.resolve({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(data),
    text: () => Promise.resolve(JSON.stringify(data)),
  } as Response);
};

// Utility to wait for all promises to resolve
export const flushPromises = () => new Promise((resolve) => setTimeout(resolve, 0));

// Generate mock data arrays
export const generateMockArray = <T>(template: T, count: number): T[] => {
  return Array.from({ length: count }, (_, index) => ({
    ...template,
    id: index + 1,
  }));
};

// Mock localStorage
export const mockLocalStorage = () => {
  const store: Record<string, string> = {};

  return {
    getItem: (key: string) => store[key] || null,
    setItem: (key: string, value: string) => {
      store[key] = value;
    },
    removeItem: (key: string) => {
      delete store[key];
    },
    clear: () => {
      Object.keys(store).forEach(key => delete store[key]);
    },
  };
};

// Mock window.location
export const mockWindowLocation = (url: string = 'http://localhost:3000') => {
  delete (global as any).window.location;
  (global as any).window.location = new URL(url);
};

// Mock IntersectionObserver
export const mockIntersectionObserver = () => {
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
};

// Mock ResizeObserver
export const mockResizeObserver = () => {
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
};

// Setup all mocks at once
export const setupTestEnvironment = () => {
  mockIntersectionObserver();
  mockResizeObserver();

  // Mock localStorage
  Object.defineProperty(window, 'localStorage', {
    value: mockLocalStorage(),
    writable: true,
  });

  // Mock window.location
  mockWindowLocation();

  // Mock console methods to reduce noise in tests
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

// Test wrapper for components that need routing
export const withRouter = (component: ReactElement, pathname: string = '/') => {
  const useRouter = jest.spyOn(require('next/navigation'), 'useRouter');
  const usePathname = jest.spyOn(require('next/navigation'), 'usePathname');

  useRouter.mockImplementation(() => ({
    push: jest.fn(),
    replace: jest.fn(),
    back: jest.fn(),
    forward: jest.fn(),
    refresh: jest.fn(),
    prefetch: jest.fn(),
  }));

  usePathname.mockImplementation(() => pathname);

  return component;
};

// Utility to create mock event handlers
export const createMockHandlers = () => ({
  onClick: jest.fn(),
  onChange: jest.fn(),
  onSubmit: jest.fn(),
  onFocus: jest.fn(),
  onBlur: jest.fn(),
  onMouseEnter: jest.fn(),
  onMouseLeave: jest.fn(),
});

// Utility to test component props
export const testProps = {
  className: 'test-class',
  'data-testid': 'test-component',
  style: { color: 'red' },
};

// Common test data generators
export const createMockUser = (overrides: Partial<typeof mockUser> = {}) => ({
  ...mockUser,
  ...overrides,
});

export const createMockContact = (overrides: Partial<typeof mockContact> = {}) => ({
  ...mockContact,
  ...overrides,
});

export const createMockInteraction = (overrides: Partial<typeof mockInteraction> = {}) => ({
  ...mockInteraction,
  ...overrides,
});

export const createMockBlock = (overrides: Partial<typeof mockBlock> = {}) => ({
  ...mockBlock,
  ...overrides,
});

// Validation helpers
export const expectValidComponent = (component: ReactElement) => {
  expect(component).toBeTruthy();
  expect(typeof component.type).toBe('function');
};

export const expectValidProps = (props: Record<string, any>) => {
  expect(props).toBeDefined();
  expect(typeof props).toBe('object');
};

// Performance testing utilities
export const measureRenderTime = async (component: ReactElement) => {
  const startTime = performance.now();
  customRender(component);
  const endTime = performance.now();
  return endTime - startTime;
};

export const expectRenderTime = async (component: ReactElement, maxTime: number = 100) => {
  const renderTime = await measureRenderTime(component);
  expect(renderTime).toBeLessThan(maxTime);
};

// Memory leak detection (basic)
export const detectMemoryLeaks = (component: ReactElement) => {
  const originalWarn = console.warn;
  const warnings: string[] = [];

  console.warn = (...args) => {
    warnings.push(args.join(' '));
  };

  customRender(component);

  console.warn = originalWarn;

  const memoryWarnings = warnings.filter(w => w.includes('memory') || w.includes('leak'));
  expect(memoryWarnings.length).toBe(0);
};

export { customRender as render };
