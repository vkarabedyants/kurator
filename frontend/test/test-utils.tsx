import React, { ReactElement } from 'react';
import { render, RenderOptions } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

// Create a custom render function that includes providers
const AllTheProviders = ({ children }: { children: React.ReactNode }) => {
  return <>{children}</>;
};

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>
) => {
  const user = userEvent.setup();
  return {
    user,
    ...render(ui, { wrapper: AllTheProviders, ...options }),
  };
};

// Mock API response builders
export const createMockUser = (overrides = {}) => ({
  id: 1,
  login: 'testuser',
  role: 'Curator',
  ...overrides,
});

export const createMockBlock = (overrides = {}) => ({
  id: 1,
  code: 'TEST',
  name: 'Test Block',
  status: 'Active',
  description: 'Test description',
  primaryCuratorId: 1,
  backupCuratorId: 2,
  primaryCurator: createMockUser({ id: 1, login: 'curator1' }),
  backupCurator: createMockUser({ id: 2, login: 'curator2' }),
  createdAt: '2024-01-01T00:00:00Z',
  ...overrides,
});

export const createMockContact = (overrides = {}) => ({
  id: 1,
  contactId: 'CONT-001',
  fullName: 'Test Contact',
  blockId: 1,
  blockName: 'Test Block',
  position: 'Manager',
  organization: 'Test Organization',
  influenceStatus: 'High',
  createdAt: '2024-01-01T00:00:00Z',
  ...overrides,
});

export const createMockInteraction = (overrides = {}) => ({
  id: 1,
  contactId: 1,
  contactName: 'Test Contact',
  interactionDate: '2024-01-01',
  interactionType: 'Call',
  result: 'Positive',
  notes: 'Test notes',
  createdAt: '2024-01-01T00:00:00Z',
  ...overrides,
});

export const createMockWatchlistEntry = (overrides = {}) => ({
  id: 1,
  contactId: 1,
  contactName: 'Test Contact',
  riskLevel: 'High',
  monitoringFrequency: 'Weekly',
  lastMonitoringDate: '2024-01-01',
  nextMonitoringDate: '2024-01-08',
  notes: 'Test notes',
  ...overrides,
});

export const createMockDashboardData = (overrides = {}) => ({
  totalContacts: 100,
  activeInteractions: 25,
  watchlistEntries: 10,
  overdueInteractions: 5,
  recentActivity: [],
  ...overrides,
});

// API mock helpers
export const setupApiMocks = () => {
  const api = require('@/lib/api').default;
  return {
    get: jest.spyOn(api, 'get'),
    post: jest.spyOn(api, 'post'),
    put: jest.spyOn(api, 'put'),
    delete: jest.spyOn(api, 'delete'),
    patch: jest.spyOn(api, 'patch'),
  };
};

// Router mock helper
export const createMockRouter = (overrides = {}) => ({
  push: jest.fn(),
  replace: jest.fn(),
  back: jest.fn(),
  forward: jest.fn(),
  reload: jest.fn(),
  prefetch: jest.fn(),
  pathname: '/',
  query: {},
  asPath: '/',
  ...overrides,
});

// Local storage mock helper
export const setupLocalStorageMock = () => {
  const store = new Map<string, string>();

  return {
    getItem: (key: string) => store.get(key) || null,
    setItem: (key: string, value: string) => store.set(key, value),
    removeItem: (key: string) => store.delete(key),
    clear: () => store.clear(),
    get length() {
      return store.size;
    },
    key: (index: number) => Array.from(store.keys())[index] || null,
  };
};

// Component render helpers
export const waitForLoadingToFinish = async () => {
  const { waitFor, screen } = await import('@testing-library/react');
  // Wait for loading spinner to be removed
  await waitFor(() => {
    const spinner = document.querySelector('.animate-spin');
    if (spinner) {
      throw new Error('Still loading');
    }
  });
};

// Export everything from React Testing Library
export * from '@testing-library/react';
export { userEvent };
export { customRender as render };
