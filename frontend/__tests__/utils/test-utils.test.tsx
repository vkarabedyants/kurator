import React from 'react';
import { render, screen } from '@testing-library/react';
import {
  mockUser,
  mockContact,
  mockInteraction,
  mockBlock,
  mockApiResponse,
  generateMockArray,
  createMockUser,
  expectValidComponent,
  expectValidProps,
  testProps,
  mockLocalStorage,
} from './test-utils';

describe('Test Utilities', () => {
  it('should provide mock user data', () => {
    expect(mockUser).toHaveProperty('id', 1);
    expect(mockUser).toHaveProperty('login', 'testuser');
    expect(mockUser).toHaveProperty('role', 'Admin');
    expect(mockUser).toHaveProperty('email', 'test@example.com');
  });

  it('should provide mock contact data', () => {
    expect(mockContact).toHaveProperty('id', 1);
    expect(mockContact).toHaveProperty('contactId', 'TEST-001');
  });

  it('should provide mock interaction data', () => {
    expect(mockInteraction).toHaveProperty('id', 1);
    expect(mockInteraction).toHaveProperty('contactId', 1);
    expect(mockInteraction).toHaveProperty('type', 'Meeting');
  });

  it('should provide mock block data', () => {
    expect(mockBlock).toHaveProperty('id', 1);
    expect(mockBlock).toHaveProperty('code', 'TEST');
    expect(mockBlock).toHaveProperty('status', 'Active');
  });

  it('should create mock API responses', () => {
    const successResponse = mockApiResponse.success({ data: 'test' });
    expect(successResponse.success).toBe(true);
    expect(successResponse.message).toBe('Success');

    const errorResponse = mockApiResponse.error('Test error');
    expect(errorResponse.success).toBe(false);
    expect(errorResponse.message).toBe('Test error');

    const paginatedResponse = mockApiResponse.paginated([1, 2, 3], 10);
    expect(paginatedResponse.data).toEqual([1, 2, 3]);
    expect(paginatedResponse.success).toBe(true);
  });

  it('should generate mock arrays', () => {
    const template = { name: 'Test', value: 1 };
    const array = generateMockArray(template, 3);

    expect(array).toHaveLength(3);
    expect(array[0].id).toBe(1);
    expect(array[1].id).toBe(2);
    expect(array[2].id).toBe(3);
  });

  it('should create custom mock objects', () => {
    const customUser = createMockUser({ login: 'customuser', role: 'Curator' });
    expect(customUser.login).toBe('customuser');
    expect(customUser.role).toBe('Curator');
    expect(customUser.id).toBe(1);
  });

  it('should validate components and props', () => {
    const TestComponent = () => <div>Test</div>;
    const component = <TestComponent />;

    expect(() => expectValidComponent(component)).not.toThrow();
    expect(() => expectValidProps({ test: 'value' })).not.toThrow();
  });

  it('should handle custom render with QueryClient', () => {
    const TestComponent = () => <div>Test Component</div>;
    render(<TestComponent />);
    expect(screen.getByText('Test Component')).toBeInTheDocument();
  });

  it('should provide test props', () => {
    expect(testProps).toHaveProperty('className', 'test-class');
  });

  it('should provide localStorage mock', () => {
    const ls = mockLocalStorage();

    ls.setItem('test', 'value');
    expect(ls.getItem('test')).toBe('value');

    ls.removeItem('test');
    expect(ls.getItem('test')).toBe(null);
  });
});
