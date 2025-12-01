import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { usePathname } from 'next/navigation';
import Navigation, { MobileNavigation, Breadcrumb } from '../Navigation';

jest.mock('next/navigation', () => ({
  usePathname: jest.fn(),
  useRouter: jest.fn(() => ({
    push: jest.fn(),
    replace: jest.fn(),
    prefetch: jest.fn(),
  })),
}));

jest.mock('next/link', () => {
  return ({ children, href, onClick, ...props }: any) => (
    <a href={href} onClick={onClick} {...props}>
      {children}
    </a>
  );
});

jest.mock('next-intl', () => ({
  useTranslations: () => (key: string) => {
    const translations: Record<string, string> = {
      'navigation.home': 'Главная',
      'navigation.contacts': 'Контакты',
      'navigation.contacts_all': 'Все контакты',
      'navigation.contacts_new': 'Новый контакт',
      'navigation.interactions': 'Взаимодействия',
      'navigation.interactions_all': 'Все взаимодействия',
      'navigation.interactions_new': 'Новое взаимодействие',
      'navigation.blocks': 'Блоки',
      'navigation.users': 'Пользователи',
      'navigation.analytics': 'Аналитика',
      'navigation.audit': 'Аудит',
      'common.settings': 'Настройки',
      'common.logout': 'Выйти',
    };
    return translations[key] || key;
  },
}));

jest.mock('../LanguageSwitcher', () => {
  return function MockLanguageSwitcher() {
    return <div data-testid="language-switcher">Language</div>;
  };
});

const mockUsePathname = usePathname as jest.MockedFunction<typeof usePathname>;

describe('Navigation Component', () => {
  beforeEach(() => {
    mockUsePathname.mockReturnValue('/');
  });

  it('renders navigation with logo and menu items', () => {
    render(<Navigation />);

    expect(screen.getByText('Kurator')).toBeInTheDocument();
    expect(screen.getByText('Главная')).toBeInTheDocument();
    expect(screen.getByText('Контакты')).toBeInTheDocument();
    expect(screen.getByText('Взаимодействия')).toBeInTheDocument();
    expect(screen.getByText('Блоки')).toBeInTheDocument();
    expect(screen.getByText('Аналитика')).toBeInTheDocument();
    expect(screen.getByText('Аудит')).toBeInTheDocument();
  });

  it('applies custom className', () => {
    render(<Navigation className="custom-nav" />);

    const nav = screen.getByRole('navigation');
    expect(nav).toHaveClass('custom-nav');
  });
});

describe('MobileNavigation Component', () => {
  const mockUser = {
    name: 'Иван Иванов',
    email: 'ivan@example.com',
    role: 'Администратор',
  };

  beforeEach(() => {
    mockUsePathname.mockReturnValue('/');
  });

  it('does not render when isOpen is false', () => {
    render(<MobileNavigation isOpen={false} onClose={() => {}} />);

    expect(screen.queryByText('Kurator')).not.toBeInTheDocument();
  });

  it('renders mobile navigation when isOpen is true', () => {
    render(
      <MobileNavigation
        isOpen={true}
        onClose={() => {}}
        user={mockUser}
      />
    );

    expect(screen.getByText('Kurator')).toBeInTheDocument();
    expect(screen.getByText('Главная')).toBeInTheDocument();
    expect(screen.getByText('Иван Иванов')).toBeInTheDocument();
  });

  it('displays user information', () => {
    render(
      <MobileNavigation
        isOpen={true}
        onClose={() => {}}
        user={mockUser}
      />
    );

    expect(screen.getByText('Иван Иванов')).toBeInTheDocument();
    expect(screen.getByText('ivan@example.com')).toBeInTheDocument();
    expect(screen.getByText('Администратор')).toBeInTheDocument();
  });
});

describe('Breadcrumb Component', () => {
  it('renders breadcrumb navigation', () => {
    const items = [
      { label: 'Главная', href: '/' },
      { label: 'Контакты', href: '/contacts' },
      { label: 'Иван Иванов' },
    ];

    render(<Breadcrumb items={items} />);

    expect(screen.getByText('Главная')).toBeInTheDocument();
    expect(screen.getByText('Контакты')).toBeInTheDocument();
    expect(screen.getByText('Иван Иванов')).toBeInTheDocument();
  });

  it('renders separators between items', () => {
    const items = [
      { label: 'Главная', href: '/' },
      { label: 'Контакты' },
    ];

    render(<Breadcrumb items={items} />);

    const separators = screen.getAllByTestId('breadcrumb-separator');
    expect(separators).toHaveLength(1);
  });

  it('applies custom className', () => {
    const items = [{ label: 'Главная' }];

    render(<Breadcrumb items={items} className="custom-breadcrumb" />);

    const nav = screen.getByRole('navigation');
    expect(nav).toHaveClass('custom-breadcrumb');
  });
});
