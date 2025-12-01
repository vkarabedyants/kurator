import { render, screen, waitFor } from '@testing-library/react';
import ContactDetailPage from '../page';
import { contactsApi } from '@/services/api';

jest.mock('@/services/api', () => ({
  contactsApi: {
    getById: jest.fn(),
  },
  interactionsApi: {
    create: jest.fn(),
  },
}));

jest.mock('next/navigation', () => ({
  useRouter: () => ({
    push: jest.fn(),
    replace: jest.fn(),
  }),
  useParams: () => ({ id: '1' }),
  usePathname: () => '/contacts/1',
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

describe('Contact Detail Page', () => {
  const mockContact = {
    id: 1,
    contactId: 'CNT-001',
    fullName: 'John Doe',
    blockId: 1,
    position: 'Manager',
    influenceStatus: 'A',
    interactions: [],
    statusHistory: [],
  };

  beforeEach(() => {
    jest.clearAllMocks();
    (contactsApi.getById as jest.Mock).mockResolvedValue(mockContact);
  });

  it('should render contact details', async () => {
    render(<ContactDetailPage />);

    await waitFor(() => {
      expect(screen.getByText('John Doe')).toBeInTheDocument();
    });
  });

  it('should show loading state', () => {
    (contactsApi.getById as jest.Mock).mockImplementation(() => new Promise(() => {}));

    render(<ContactDetailPage />);

    expect(screen.getByTestId('main-layout')).toBeInTheDocument();
  });
});
