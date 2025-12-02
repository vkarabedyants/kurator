// Jest setup file for testing library and custom configurations
import '@testing-library/jest-dom';

// Mock Next.js router with comprehensive functionality
jest.mock('next/navigation', () => ({
  useRouter() {
    return {
      push: jest.fn(),
      replace: jest.fn(),
      prefetch: jest.fn(),
      back: jest.fn(),
      forward: jest.fn(),
      reload: jest.fn(),
      pathname: '/',
      query: {},
      asPath: '/',
    };
  },
  usePathname() {
    return '/';
  },
  useSearchParams() {
    return new URLSearchParams();
  },
}));

// Mock window.matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: jest.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: jest.fn(),
    removeListener: jest.fn(),
    addEventListener: jest.fn(),
    removeEventListener: jest.fn(),
    dispatchEvent: jest.fn(),
  })),
});

// Mock localStorage with proper implementation
const localStorageMock = (() => {
  const store = new Map();
  return {
    getItem: jest.fn((key) => store.get(key) || null),
    setItem: jest.fn((key, value) => store.set(key, String(value))),
    removeItem: jest.fn((key) => store.delete(key)),
    clear: jest.fn(() => store.clear()),
    get length() {
      return store.size;
    },
    key: jest.fn((index) => Array.from(store.keys())[index] || null),
  };
})();
global.localStorage = localStorageMock;

// Mock axios with proper interceptors
jest.mock('axios', () => ({
  create: jest.fn(() => ({
    get: jest.fn(),
    post: jest.fn(),
    put: jest.fn(),
    delete: jest.fn(),
    patch: jest.fn(),
    interceptors: {
      request: { use: jest.fn(), eject: jest.fn() },
      response: { use: jest.fn(), eject: jest.fn() }
    }
  }))
}));

// Mock TextEncoder/TextDecoder for Node.js environment
global.TextEncoder = require('util').TextEncoder;
global.TextDecoder = require('util').TextDecoder;

// Mock crypto for encryption tests
global.crypto = {
  getRandomValues: (arr) => {
    for (let i = 0; i < arr.length; i++) {
      arr[i] = Math.floor(Math.random() * 256);
    }
    return arr;
  },
  subtle: {
    generateKey: jest.fn().mockResolvedValue({
      publicKey: 'mock-public-key',
      privateKey: 'mock-private-key',
    }),
    encrypt: jest.fn().mockResolvedValue(new ArrayBuffer(32)),
    decrypt: jest.fn().mockResolvedValue(new ArrayBuffer(32)),
    importKey: jest.fn().mockResolvedValue('mock-imported-key'),
    exportKey: jest.fn().mockResolvedValue(new ArrayBuffer(32)),
  },
};

// Mock next-intl for internationalization with Russian translations
const translations = {
  common: {
    loading: 'Загрузка...',
    save: 'Сохранить',
    cancel: 'Отмена',
    delete: 'Удалить',
    edit: 'Редактировать',
    add: 'Добавить',
    search: 'Поиск',
    status: 'Статус',
  },
  users: {
    management: 'Управление пользователями',
    add_new: 'Добавить пользователя',
    create_new: 'Создать нового пользователя',
    no_users: 'Пользователи не найдены',
    login: 'Логин',
    role: 'Роль',
    last_login: 'Последний вход',
    created_at: 'Дата создания',
    change_password: 'Изменить пароль',
    current_password: 'Текущий пароль',
    new_password: 'Новый пароль',
    confirm_password: 'Подтвердите пароль',
    passwords_not_match: 'Новые пароли не совпадают!',
    password_change_success: 'Пароль успешно изменен!',
    password_change_error: 'Ошибка изменения пароля',
    create_error: 'Не удалось создать пользователя',
    delete_error: 'Ошибка удаления',
    load_error: 'Ошибка',
    delete_confirm: 'Удалить пользователя?',
    login_required: 'Логин',
    password_required: 'Пароль',
    role_required: 'Роль',
  },
  contacts: {
    title: 'Контакты',
    add_new: 'Добавить контакт',
    create_new: 'Создать новый контакт',
    no_contacts: 'Контакты не найдены',
    loading: 'Загрузка данных...',
    no_blocks: 'Нет доступных блоков',
    no_blocks_available: 'Нет доступных блоков',
    no_blocks_message: 'У вас нет доступа к блокам для создания контактов.',
    full_name: 'ФИО',
    position: 'Должность',
    block: 'Блок',
    influence_status: 'Статус влияния',
  },
  blocks: {
    management: 'Управление блоками',
    add_new: 'Добавить блок',
    add_block: 'Добавить новый блок',
    edit_block: 'Редактировать блок',
    no_blocks: 'Блоки не найдены',
    load_error: 'Ошибка загрузки',
    save_error: 'Ошибка сохранения',
    delete_error: 'Ошибка удаления',
    delete_confirm: 'Удалить блок?',
    name_required: 'Название',
    code_required: 'Код',
    description: 'Описание',
    primary_curator: 'Основной куратор',
    backup_curator: 'Резервный куратор',
    not_assigned: 'Не назначен',
    status_active: 'Активный',
    status_archived: 'Архивный',
    code: 'Код',
    created_at: 'Дата создания',
    create: 'Создать',
    update: 'Обновить',
  },
  roles: {
    admin: 'Admin',
    curator: 'Curator',
    threat_analyst: 'ThreatAnalyst',
  },
};

jest.mock('next-intl', () => ({
  useTranslations: (namespace) => {
    return (key) => {
      // Get translation from namespace
      const ns = translations[namespace];
      if (ns && ns[key]) {
        return ns[key];
      }
      return key;
    };
  },
  useLocale: () => 'ru',
}));

// Suppress console errors for specific React warnings
const originalError = console.error;
beforeAll(() => {
  console.error = (...args) => {
    if (
      typeof args[0] === 'string' &&
      (args[0].includes('Warning: useLayoutEffect') ||
        args[0].includes('Warning: ReactDOM.render'))
    ) {
      return;
    }
    originalError.call(console, ...args);
  };
});

afterAll(() => {
  console.error = originalError;
});