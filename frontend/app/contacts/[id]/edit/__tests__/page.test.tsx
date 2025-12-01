import { render, screen, waitFor } from '@testing-library/react';
import EditContactPage from '../page';
import { api } from '@/services/api';

jest.mock('@/services/api', () => ({
  api: {
    get: jest.fn(),
    put: jest.fn(),
  },
}));

jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: jest.fn(), replace: jest.fn(), back: jest.fn() }),
  useParams: () => ({ id: '1' }),
  usePathname: () => '/contacts/1/edit',
}));

describe('Edit Contact Page', () => {
  const mockContact = {
    id: 1,
    fullName: 'John Doe',
    blockId: 1,
    position: 'Manager',
    influenceStatus: 'C',
    influenceType: 'Functional',
    communicationChannel: 'Personal',
    contactSource: 'PersonalAcquaintance',
  };

  beforeEach(() => {
    jest.clearAllMocks();
    (api.get as jest.Mock).mockResolvedValue({ data: mockContact });
  });

  it('should render edit contact form', async () => {
    render(<EditContactPage />);

    await waitFor(() => {
      expect(screen.getByText('Редактировать контакт')).toBeInTheDocument();
    });
  });

  it('should show loading state', () => {
    (api.get as jest.Mock).mockImplementation(() => new Promise(() => {}));

    render(<EditContactPage />);

    expect(screen.getByText('Загрузка контакта...')).toBeInTheDocument();
  });
});
