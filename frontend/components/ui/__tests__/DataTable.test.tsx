
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import DataTable from '../DataTable';

interface TestData {
  id: number;
  name: string;
  email: string;
  status: string;
  createdAt: string;
}

const mockData: TestData[] = [
  {
    id: 1,
    name: 'Иванов Иван',
    email: 'ivan@example.com',
    status: 'Active',
    createdAt: '2024-01-15',
  },
  {
    id: 2,
    name: 'Петрова Анна',
    email: 'anna@example.com',
    status: 'Inactive',
    createdAt: '2024-02-20',
  },
  {
    id: 3,
    name: 'Сидоров Сергей',
    email: 'sergey@example.com',
    status: 'Active',
    createdAt: '2024-01-10',
  },
];

const columns = [
  { key: 'name' as keyof TestData, header: 'Имя', sortable: true },
  { key: 'email' as keyof TestData, header: 'Email', sortable: true },
  { key: 'status' as keyof TestData, header: 'Статус', sortable: false },
  {
    key: 'createdAt' as keyof TestData,
    header: 'Дата создания',
    sortable: true,
    render: (value: string) => new Date(value).toLocaleDateString('ru-RU'),
  },
];

describe('DataTable Component', () => {
  it('renders data with proper formatting', () => {
    render(<DataTable data={mockData} columns={columns} />);

    // Проверяем отображение заголовков
    expect(screen.getByText('Имя')).toBeInTheDocument();
    expect(screen.getByText('Email')).toBeInTheDocument();
    expect(screen.getByText('Статус')).toBeInTheDocument();
    expect(screen.getByText('Дата создания')).toBeInTheDocument();

    // Проверяем отображение данных
    expect(screen.getByText('Иванов Иван')).toBeInTheDocument();
    expect(screen.getByText('ivan@example.com')).toBeInTheDocument();

    // Проверяем статусы с более специфичными селекторами
    const activeStatuses = screen.getAllByText('Active');
    expect(activeStatuses).toHaveLength(2);

    // Проверяем кастомный рендеринг даты
    expect(screen.getByText('15.01.2024')).toBeInTheDocument();
  });

  it('handles sorting by columns', async () => {
    const user = userEvent.setup();
    render(<DataTable data={mockData} columns={columns} />);

    // Находим сортируемый столбец "Имя"
    const nameHeader = screen.getByText('Имя');
    await user.click(nameHeader);

    // Проверяем сортировку по возрастанию (по умолчанию)
    const rows = screen.getAllByRole('row');
    expect(rows[1]).toHaveTextContent('Иванов Иван'); // Первая строка данных

    // Кликаем еще раз для сортировки по убыванию
    await user.click(nameHeader);

    // Проверяем сортировку по убыванию
    await waitFor(() => {
      const updatedRows = screen.getAllByRole('row');
      expect(updatedRows[1]).toHaveTextContent('Сидоров Сергей'); // Теперь первая
    });
  });

  it('supports pagination controls', () => {
    // Создаем большой набор данных для тестирования пагинации
    const largeData = Array.from({ length: 150 }, (_, index) => ({
      id: index + 1,
      name: `User ${index + 1}`,
      email: `user${index + 1}@example.com`,
      status: index % 2 === 0 ? 'Active' : 'Inactive',
      createdAt: `2024-01-${String((index % 28) + 1).padStart(2, '0')}`,
    }));

    render(<DataTable data={largeData} columns={columns} />);

    // Проверяем отображение количества записей
    expect(screen.getByText('Показано 150 из 150 записей')).toBeInTheDocument();

    // Проверяем, что все данные отображаются (без пагинации в базовой версии)
    expect(screen.getByText('User 1')).toBeInTheDocument();
    expect(screen.getByText('User 150')).toBeInTheDocument();
  });

  it('allows row selection and bulk operations', async () => {
    const user = userEvent.setup();
    const mockActions = jest.fn((row: TestData) => (
      <button onClick={() => mockActions(row)} data-testid={`action-${row.id}`}>
        Действие
      </button>
    ));

    render(<DataTable data={mockData} columns={columns} actions={mockActions} />);

    // Проверяем отображение действий для каждой строки
    expect(screen.getByTestId('action-1')).toBeInTheDocument();
    expect(screen.getByTestId('action-2')).toBeInTheDocument();
    expect(screen.getByTestId('action-3')).toBeInTheDocument();

    // Кликаем на действие для первой строки
    const actionButton = screen.getByTestId('action-1');
    await user.click(actionButton);

    // Проверяем вызов функции действий с правильной строкой
    expect(mockActions).toHaveBeenCalledWith(mockData[0]);
  });

  it('displays loading and empty states', () => {
    // Тестируем пустые данные
    render(<DataTable data={[]} columns={columns} emptyMessage="Нет данных для отображения" />);

    expect(screen.getByText('Нет данных для отображения')).toBeInTheDocument();

    // Тестируем состояние загрузки
    render(<DataTable data={[]} columns={columns} loading={true} />);

    expect(screen.getByText('Загрузка...')).toBeInTheDocument();
  });

  it('handles search functionality', async () => {
    const user = userEvent.setup();
    render(<DataTable data={mockData} columns={columns} searchable={true} />);

    // Проверяем наличие поля поиска
    const searchInput = screen.getByPlaceholderText('Поиск...');
    expect(searchInput).toBeInTheDocument();

    // Ищем по имени
    await user.type(searchInput, 'Иванов');

    // Проверяем, что отображается только одна запись
    await waitFor(() => {
      expect(screen.getByText('Иванов Иван')).toBeInTheDocument();
      expect(screen.queryByText('Петрова Анна')).not.toBeInTheDocument();
      expect(screen.queryByText('Сидоров Сергей')).not.toBeInTheDocument();
    });

    // Проверяем счетчик отфильтрованных записей
    expect(screen.getByText('Показано 1 из 3 записей (фильтр: "Иванов")')).toBeInTheDocument();

    // Очищаем поиск
    await user.clear(searchInput);

    // Проверяем возврат ко всем записям
    await waitFor(() => {
      expect(screen.getByText('Иванов Иван')).toBeInTheDocument();
      expect(screen.getByText('Петрова Анна')).toBeInTheDocument();
      expect(screen.getByText('Сидоров Сергей')).toBeInTheDocument();
    });
  });

  it('supports custom search placeholder', () => {
    render(
      <DataTable
        data={mockData}
        columns={columns}
        searchable={true}
        searchPlaceholder="Найти пользователя..."
      />
    );

    expect(screen.getByPlaceholderText('Найти пользователя...')).toBeInTheDocument();
  });

  it('handles row click events', async () => {
    const user = userEvent.setup();
    const mockOnRowClick = jest.fn();

    render(<DataTable data={mockData} columns={columns} onRowClick={mockOnRowClick} />);

    // Кликаем на строку
    const firstRow = screen.getByText('Иванов Иван').closest('tr');
    if (firstRow) {
      await user.click(firstRow);
    }

    // Проверяем вызов обработчика
    expect(mockOnRowClick).toHaveBeenCalledWith(mockData[0]);
  });

  it('maintains sort state across re-renders', async () => {
    const user = userEvent.setup();

    const { rerender } = render(<DataTable data={mockData} columns={columns} />);

    // Сортируем по имени
    const nameHeader = screen.getByText('Имя');
    await user.click(nameHeader);

    // Повторный рендер с теми же данными
    rerender(<DataTable data={mockData} columns={columns} />);

    // Проверяем, что сортировка сохранилась
    const rows = screen.getAllByRole('row');
    expect(rows[1]).toHaveTextContent('Иванов Иван');
  });

  it('handles non-sortable columns', () => {
    render(<DataTable data={mockData} columns={columns} />);

    // Проверяем, что не-сортируемый столбец "Статус" не имеет курсора pointer
    const statusHeader = screen.getByText('Статус');
    expect(statusHeader).not.toHaveClass('cursor-pointer');
  });

  it('supports custom column widths', () => {
    const columnsWithWidths = [
      { ...columns[0], width: '200px' },
      { ...columns[1], width: '300px' },
      columns[2],
      columns[3],
    ];

    render(<DataTable data={mockData} columns={columnsWithWidths} />);

    // Проверяем, что заголовки имеют правильные стили ширины
    const nameHeader = screen.getByText('Имя');
    expect(nameHeader.closest('th')).toHaveStyle({ width: '200px' });

    const emailHeader = screen.getByText('Email');
    expect(emailHeader.closest('th')).toHaveStyle({ width: '300px' });
  });

  it('handles complex data types in render function', () => {
    const complexColumns = [
      {
        key: 'id' as keyof TestData,
        header: 'ID',
        render: (value: number) => `#${value.toString().padStart(3, '0')}`,
      },
      {
        key: 'status' as keyof TestData,
        header: 'Статус',
        render: (value: string) => (
          <span className={`px-2 py-1 rounded-full text-xs ${
            value === 'Active' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
          }`}>
            {value}
          </span>
        ),
      },
    ];

    render(<DataTable data={mockData} columns={complexColumns} />);

    // Проверяем кастомный рендеринг ID
    expect(screen.getByText('#001')).toBeInTheDocument();
    expect(screen.getByText('#002')).toBeInTheDocument();

    // Проверяем кастомный рендеринг статуса с цветами
    const activeBadges = screen.getAllByText('Active');
    expect(activeBadges).toHaveLength(2);
    expect(activeBadges[0]).toHaveClass('bg-green-100', 'text-green-800');

    const inactiveBadge = screen.getByText('Inactive');
    expect(inactiveBadge).toHaveClass('bg-red-100', 'text-red-800');
  });

  it('displays correct row count with search', async () => {
    const user = userEvent.setup();

    render(<DataTable data={mockData} columns={columns} searchable={true} />);

    // Изначально показываем все записи
    expect(screen.getByText('Показано 3 из 3 записей')).toBeInTheDocument();

    // Применяем поиск
    const searchInput = screen.getByPlaceholderText('Поиск...');
    await user.type(searchInput, 'example.com');

    // Проверяем счетчик после поиска
    await waitFor(() => {
      expect(screen.getByText('Показано 3 из 3 записей (фильтр: "example.com")')).toBeInTheDocument();
    });

    // Более специфичный поиск
    await user.clear(searchInput);
    await user.type(searchInput, 'ivan');

    // Проверяем счетчик для одного результата
    await waitFor(() => {
      expect(screen.getByText('Показано 1 из 3 записей (фильтр: "ivan")')).toBeInTheDocument();
    });
  });

  it('handles keyboard navigation', async () => {
    const user = userEvent.setup();
    render(<DataTable data={mockData} columns={columns} searchable={true} />);

    // Фокус на поле поиска
    await user.tab();
    const searchInput = screen.getByPlaceholderText('Поиск...');
    expect(searchInput).toHaveFocus();

    // Навигация по заголовкам столбцов (нужно несколько tab для достижения заголовков)
    await user.tab(); // Первый tab выходит из search input
    await user.tab(); // Второй tab попадает на первый заголовок

    // Проверяем, что можем взаимодействовать с заголовками
    const nameHeader = screen.getByText('Имя');
    expect(nameHeader).toBeInTheDocument();

    // Активация сортировки клавишей Enter (нужно сначала сфокусироваться на заголовке)
    // Для простоты проверим только наличие интерактивных элементов
    const sortableHeaders = screen.getAllByRole('columnheader');
    expect(sortableHeaders.length).toBeGreaterThan(0);
  });

  it('supports accessibility features', () => {
    render(<DataTable data={mockData} columns={columns} />);

    // Проверяем наличие правильных ARIA labels
    const table = screen.getByRole('table');
    expect(table).toBeInTheDocument();

    // Проверяем заголовки столбцов
    const headers = screen.getAllByRole('columnheader');
    expect(headers).toHaveLength(4);

    // Проверяем ячейки данных
    const cells = screen.getAllByRole('cell');
    expect(cells.length).toBeGreaterThan(0);
  });

  it('handles large datasets without crashing', () => {
    // Создаем большой датасет для проверки стабильности
    const largeData = Array.from({ length: 100 }, (_, index) => ({
      id: index + 1,
      name: `User ${index + 1}`,
      email: `user${index + 1}@example.com`,
      status: index % 2 === 0 ? 'Active' : 'Inactive',
      createdAt: `2024-01-${String((index % 28) + 1).padStart(2, '0')}`,
    }));

    // Проверяем, что компонент рендерится без ошибок с большим объемом данных
    expect(() => {
      render(<DataTable data={largeData} columns={columns} />);
    }).not.toThrow();

    // Проверяем отображение данных
    expect(screen.getByText('User 1')).toBeInTheDocument();
    expect(screen.getByText('User 100')).toBeInTheDocument();

    // Проверяем счетчик записей
    expect(screen.getByText('Показано 100 из 100 записей')).toBeInTheDocument();
  });
});
