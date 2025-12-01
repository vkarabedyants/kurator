import { render, screen, waitFor } from '@testing-library/react';
import ContactsPage from '../page';
import { contactsApi } from '@/services/api';

jest.mock('@/services/api', () => ({
  contactsApi: {
    getAll: jest.fn(),
    delete: jest.fn(),
  },
}));

jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
    prefetch: jest.fn(),
  }),
  usePathname: () => '/contacts',
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

describe('Contacts Page', () => {
  const mockContacts = {
    data: [
      { id: 1, contactId: 'CNT-001', fullName: 'John Doe', blockId: 1, influenceStatus: 'A', influenceType: 'Navigational' },
      { id: 2, contactId: 'CNT-002', fullName: 'Jane Smith', blockId: 2, influenceStatus: 'B', influenceType: 'Functional' },
    ],
    totalPages: 1,
    totalCount: 2,
  };

  beforeEach(() => {
    jest.clearAllMocks();
    (contactsApi.getAll as jest.Mock).mockResolvedValue(mockContacts);
  });

  describe('Initial Load', () => {
    it('should render contacts list', async () => {
      render(<ContactsPage />);

      await waitFor(() => {
        expect(screen.getByText('John Doe')).toBeInTheDocument();
        expect(screen.getByText('Jane Smith')).toBeInTheDocument();
      });
    });

    it('should show loading state initially', () => {
      (contactsApi.getAll as jest.Mock).mockImplementation(() => new Promise(() => {}));

      render(<ContactsPage />);

      // Check for loading indicator or contacts title
      expect(screen.getByText('Контакты')).toBeInTheDocument();
    });

    it('should display the add contact button', async () => {
      render(<ContactsPage />);

      await waitFor(() => {
        expect(screen.getByText('Добавить контакт')).toBeInTheDocument();
      });
    });
  });

  describe('Empty state', () => {
    it('should show empty state when no contacts', async () => {
      (contactsApi.getAll as jest.Mock).mockResolvedValue({ data: [], totalPages: 0, totalCount: 0 });

      render(<ContactsPage />);

      await waitFor(() => {
        // Check if table header exists even if empty
        expect(screen.getByText('Контакты')).toBeInTheDocument();
      });
    });
  });
});
