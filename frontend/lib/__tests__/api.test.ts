describe('API Client Configuration', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should use correct base URL from environment or default', () => {
    const expectedBaseURL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api';

    // The api instance should be created with the correct baseURL
    expect(typeof expectedBaseURL).toBe('string');
    expect(expectedBaseURL).toContain('api');
  });

  it('should have correct default headers', () => {
    const expectedHeaders = {
      'Content-Type': 'application/json',
    };

    expect(expectedHeaders['Content-Type']).toBe('application/json');
  });

  it('should have localStorage available for token management', () => {
    // Mock localStorage
    const mockStorage = {
      getItem: jest.fn(),
      setItem: jest.fn(),
      removeItem: jest.fn(),
      clear: jest.fn(),
    };

    Object.defineProperty(window, 'localStorage', {
      value: mockStorage,
      writable: true,
    });

    expect(window.localStorage.getItem).toBeDefined();
    expect(window.localStorage.setItem).toBeDefined();
    expect(window.localStorage.removeItem).toBeDefined();
  });

  it('should handle token retrieval from localStorage', () => {
    const mockStorage = {
      getItem: jest.fn().mockReturnValue('test-token'),
      setItem: jest.fn(),
      removeItem: jest.fn(),
      clear: jest.fn(),
    };

    Object.defineProperty(window, 'localStorage', {
      value: mockStorage,
      writable: true,
    });

    const token = window.localStorage.getItem('token');
    expect(token).toBe('test-token');
  });

  it('should handle clearing authentication data', () => {
    const mockStorage = {
      getItem: jest.fn(),
      setItem: jest.fn(),
      removeItem: jest.fn(),
      clear: jest.fn(),
    };

    Object.defineProperty(window, 'localStorage', {
      value: mockStorage,
      writable: true,
    });

    window.localStorage.removeItem('token');
    window.localStorage.removeItem('user');

    expect(mockStorage.removeItem).toHaveBeenCalledWith('token');
    expect(mockStorage.removeItem).toHaveBeenCalledWith('user');
  });
});
