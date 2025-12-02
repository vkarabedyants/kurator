
import { render, screen } from '@testing-library/react';
import {
  mockRouter,
  mockUsePathname,
  mockUseRouter,
  mockApiService,
  mockSuccessfulApiResponse,
  mockErrorApiResponse,
  mockLoadingState,
  mockErrorState,
  mockSuccessState,
  mockFormEvent,
  mockEventHandlers,
  mockComponentProps,
  mockTableProps,
  mockModalProps,
  mockFormProps,
  mockChartData,
  mockAuditLogEntry,
  mockNotification,
  mockTheme,
  mockI18n,
  mockServerResponses,
} from './mocks';

describe('Mock Utilities', () => {
  it('should provide mock router', () => {
    expect(mockRouter).toHaveProperty('push');
    expect(mockRouter).toHaveProperty('replace');
    expect(mockRouter).toHaveProperty('back');
    expect(typeof mockRouter.push).toBe('function');

    mockRouter.push('/test');
    expect(mockRouter.push).toHaveBeenCalledWith('/test');
  });

  it('should provide mock hooks', () => {
    expect(typeof mockUsePathname).toBe('function');
    expect(typeof mockUseRouter).toBe('function');

    mockUsePathname.mockReturnValue('/test');
    expect(mockUsePathname()).toBe('/test');

    mockUseRouter.mockReturnValue({ ...mockRouter, customProp: 'test' });
    expect(mockUseRouter()).toHaveProperty('customProp', 'test');
  });

  it('should provide mock API service', () => {
    expect(mockApiService).toHaveProperty('auth');
    expect(mockApiService).toHaveProperty('contacts');
    expect(mockApiService).toHaveProperty('interactions');
    expect(mockApiService).toHaveProperty('blocks');
    expect(mockApiService).toHaveProperty('users');
    expect(mockApiService).toHaveProperty('analytics');
    expect(mockApiService).toHaveProperty('audit');

    // Test auth methods
    expect(typeof mockApiService.auth.login).toBe('function');
    expect(typeof mockApiService.auth.logout).toBe('function');

    // Test contacts methods
    expect(typeof mockApiService.contacts.getAll).toBe('function');
    expect(typeof mockApiService.contacts.create).toBe('function');
  });

  it('should create mock API responses', () => {
    const success = mockSuccessfulApiResponse({ test: 'data' });
    expect(success).toEqual({
      data: { test: 'data' },
      success: true,
      message: 'Success',
    });

    const error = mockErrorApiResponse('Test error');
    expect(error).toEqual({
      data: null,
      success: false,
      message: 'Test error',
      errors: ['Test error'],
    });
  });

  it('should provide mock state objects', () => {
    expect(mockLoadingState).toEqual({
      isLoading: true,
      isError: false,
      data: undefined,
      error: null,
    });

    expect(mockErrorState).toEqual({
      isLoading: false,
      isError: true,
      data: undefined,
      error: expect.any(Error),
    });

    const success = mockSuccessState({ test: 'data' });
    expect(success).toEqual({
      isLoading: false,
      isError: false,
      data: { test: 'data' },
      error: null,
    });
  });

  it('should create mock form events', () => {
    const event = mockFormEvent({ name: 'John', email: 'john@example.com' });

    expect(event.preventDefault).toBeDefined();
    expect(event.target.elements.name.value).toBe('John');
    expect(event.target.elements.email.value).toBe('john@example.com');
  });

  it('should provide mock event handlers', () => {
    expect(typeof mockEventHandlers.onClick).toBe('function');
    expect(typeof mockEventHandlers.onChange).toBe('function');
    expect(typeof mockEventHandlers.onSubmit).toBe('function');

    mockEventHandlers.onClick();
    expect(mockEventHandlers.onClick).toHaveBeenCalledTimes(1);
  });

  it('should provide mock component props', () => {
    expect(mockComponentProps).toHaveProperty('className', 'test-class');
    expect(mockComponentProps).toHaveProperty('style', { color: 'red' });
    expect(mockComponentProps).toHaveProperty('data-testid', 'test-component');
    expect(mockComponentProps).toHaveProperty('children', 'Test content');
  });

  it('should provide mock table props', () => {
    expect(mockTableProps).toHaveProperty('data');
    expect(mockTableProps).toHaveProperty('columns');
    expect(mockTableProps).toHaveProperty('loading', false);
    expect(mockTableProps.data).toHaveLength(2);
    expect(mockTableProps.columns).toHaveLength(2);
  });

  it('should provide mock modal props', () => {
    expect(mockModalProps).toHaveProperty('isOpen', true);
    expect(mockModalProps).toHaveProperty('onClose');
    expect(mockModalProps).toHaveProperty('title', 'Test Modal');
    expect(mockModalProps).toHaveProperty('children');
  });

  it('should provide mock form props', () => {
    expect(mockFormProps).toHaveProperty('initialValues');
    expect(mockFormProps).toHaveProperty('onSubmit');
    expect(mockFormProps).toHaveProperty('isSubmitting', false);
    expect(mockFormProps.errors).toEqual({});
    expect(mockFormProps.touched).toEqual({});
  });

  it('should provide mock chart data', () => {
    expect(mockChartData).toHaveProperty('labels');
    expect(mockChartData).toHaveProperty('datasets');
    expect(mockChartData.labels).toHaveLength(5);
    expect(mockChartData.datasets).toHaveLength(1);
    expect(mockChartData.datasets[0]).toHaveProperty('label', 'Interactions');
  });

  it('should provide mock audit log entry', () => {
    expect(mockAuditLogEntry).toHaveProperty('id', 1);
    expect(mockAuditLogEntry).toHaveProperty('action', 'CREATE');
    expect(mockAuditLogEntry).toHaveProperty('entityType', 'Contact');
    expect(mockAuditLogEntry).toHaveProperty('changes');
    expect(mockAuditLogEntry.changes).toHaveProperty('oldValues');
    expect(mockAuditLogEntry.changes).toHaveProperty('newValues');
  });

  it('should provide mock notification system', () => {
    expect(typeof mockNotification.success).toBe('function');
    expect(typeof mockNotification.error).toBe('function');
    expect(typeof mockNotification.warning).toBe('function');
    expect(typeof mockNotification.info).toBe('function');

    mockNotification.success('Test message');
    expect(mockNotification.success).toHaveBeenCalledWith('Test message');
  });

  it('should provide mock theme', () => {
    expect(mockTheme).toHaveProperty('colors');
    expect(mockTheme).toHaveProperty('breakpoints');
    expect(mockTheme.colors).toHaveProperty('primary', '#007bff');
    expect(mockTheme.breakpoints).toHaveProperty('md', '768px');
  });

  it('should provide mock i18n', () => {
    expect(typeof mockI18n.t).toBe('function');
    expect(mockI18n).toHaveProperty('language', 'ru');
    expect(typeof mockI18n.changeLanguage).toBe('function');

    expect(mockI18n.t('test.key')).toBe('test.key');
  });

  it('should provide mock server responses', () => {
    expect(mockServerResponses).toHaveProperty('contacts');
    expect(mockServerResponses).toHaveProperty('interactions');
    expect(mockServerResponses).toHaveProperty('blocks');
    expect(mockServerResponses).toHaveProperty('users');
    expect(mockServerResponses).toHaveProperty('analytics');
    expect(mockServerResponses).toHaveProperty('audit');

    expect(mockServerResponses.contacts.list.data).toHaveLength(2);
    expect(mockServerResponses.analytics.dashboard).toHaveProperty('totalContacts', 150);
  });

  it('should handle complex mock interactions', () => {
    // Test API service mocking
    mockApiService.contacts.getAll.mockResolvedValue(mockSuccessfulApiResponse(mockServerResponses.contacts.list));

    expect(mockApiService.contacts.getAll).not.toHaveBeenCalled();

    mockApiService.contacts.getAll();

    expect(mockApiService.contacts.getAll).toHaveBeenCalledTimes(1);
  });

  it('should work with React components', () => {
    const TestComponent = ({ onClick, className, children }: { onClick: () => void; className: string; children: React.ReactNode }) => (
      <button onClick={onClick} className={className}>
        {children}
      </button>
    );

    render(
      <TestComponent
        onClick={mockEventHandlers.onClick}
        className={mockComponentProps.className}
      >
        {mockComponentProps.children}
      </TestComponent>
    );

    const button = screen.getByText('Test content');
    expect(button).toBeInTheDocument();
    expect(button).toHaveClass('test-class');

    button.click();
    expect(mockEventHandlers.onClick).toHaveBeenCalledTimes(1);
  });

  it('should handle async mock operations', async () => {
    mockApiService.contacts.getById.mockResolvedValue(mockSuccessfulApiResponse(mockServerResponses.contacts.detail));

    const result = await mockApiService.contacts.getById(1);

    expect(result).toEqual(mockSuccessfulApiResponse(mockServerResponses.contacts.detail));
    expect(mockApiService.contacts.getById).toHaveBeenCalledWith(1);
  });

  it('should support mock data transformation', () => {
    const transformedData = mockServerResponses.contacts.list.data.map(contact => ({
      ...contact,
      displayName: contact.name.toUpperCase(),
    }));

    expect(transformedData).toHaveLength(2);
    expect(transformedData[0].displayName).toBe('CONTACT 1');
    expect(transformedData[1].displayName).toBe('CONTACT 2');
  });

  it('should handle mock error scenarios', () => {
    const errorResponse = mockErrorApiResponse('Validation failed');
    const errorState = mockErrorState;

    expect(errorResponse.success).toBe(false);
    expect(errorResponse.message).toBe('Validation failed');
    expect(errorState.isError).toBe(true);
    expect(errorState.error).toBeInstanceOf(Error);
  });

  it('should provide consistent mock data structures', () => {
    const contact = mockServerResponses.contacts.detail;
    const interaction = mockServerResponses.interactions.list.data[0];
    const auditEntry = mockServerResponses.audit.logs.data[0];

    // Test that all required fields are present
    expect(contact).toHaveProperty('id');
    expect(contact).toHaveProperty('name');
    expect(contact).toHaveProperty('email');

    expect(interaction).toHaveProperty('id');
    expect(interaction).toHaveProperty('type');
    expect(interaction).toHaveProperty('date');

    expect(auditEntry).toHaveProperty('id');
    expect(auditEntry).toHaveProperty('action');
    expect(auditEntry).toHaveProperty('entityType');
    expect(auditEntry).toHaveProperty('changes');
  });

  it('should support mock customization', () => {
    const customContact = {
      ...mockServerResponses.contacts.detail,
      customField: 'Custom Value',
      tags: ['tag1', 'tag2'],
    };

    expect(customContact.customField).toBe('Custom Value');
    expect(customContact.tags).toEqual(['tag1', 'tag2']);
    expect(customContact.id).toBe(mockServerResponses.contacts.detail.id);
  });
});
