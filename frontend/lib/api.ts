import axios, { AxiosError, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { logger } from './logger';

// Generate unique request ID
function generateRequestId(): string {
  return `req_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
}

// Mask sensitive data in request/response
function maskSensitiveData(data: unknown): unknown {
  if (!data || typeof data !== 'object') return data;

  const sensitiveFields = ['password', 'token', 'secret', 'mfaSecret', 'totpCode', 'privateKey', 'encryptionKey'];
  const masked = { ...(data as Record<string, unknown>) };

  for (const field of sensitiveFields) {
    if (field in masked) {
      masked[field] = '***MASKED***';
    }
  }

  return masked;
}

// Create axios instance
const api = axios.create({
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000, // 30 seconds timeout
});

logger.info('API client initialized', {
  baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api',
  timeout: 30000,
});

// Request interceptor to add auth token and logging
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Generate and attach request ID for tracing
    const requestId = generateRequestId();
    config.headers['X-Request-Id'] = requestId;

    // Store request start time for duration calculation
    (config as Record<string, unknown>).__startTime = performance.now();
    (config as Record<string, unknown>).__requestId = requestId;

    // Add auth token if available
    if (typeof window !== 'undefined') {
      const token = localStorage.getItem('token');
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
    }

    // Log the outgoing request
    logger.apiRequest(config.method?.toUpperCase() || 'UNKNOWN', config.url || 'unknown', {
      requestId,
      baseURL: config.baseURL,
      params: config.params,
      data: maskSensitiveData(config.data),
      headers: {
        'Content-Type': config.headers['Content-Type'],
        'X-Request-Id': requestId,
        Authorization: config.headers.Authorization ? '***PRESENT***' : '***NOT_PRESENT***',
      },
    });

    return config;
  },
  (error: AxiosError) => {
    logger.error('Request interceptor error', {
      type: 'request_interceptor_error',
    }, error);
    return Promise.reject(error);
  }
);

// Response interceptor for logging and error handling
api.interceptors.response.use(
  (response: AxiosResponse) => {
    const config = response.config as InternalAxiosRequestConfig & {
      __startTime?: number;
      __requestId?: string;
    };
    const duration = config.__startTime ? performance.now() - config.__startTime : 0;
    const requestId = config.__requestId || 'unknown';

    // Log successful response
    logger.apiResponse(
      config.method?.toUpperCase() || 'UNKNOWN',
      config.url || 'unknown',
      response.status,
      Math.round(duration),
      {
        requestId,
        statusText: response.statusText,
        responseHeaders: {
          'Content-Type': response.headers['content-type'],
          'X-Correlation-Id': response.headers['x-correlation-id'],
        },
        dataSize: JSON.stringify(response.data).length,
      }
    );

    return response;
  },
  (error: AxiosError) => {
    const config = error.config as InternalAxiosRequestConfig & {
      __startTime?: number;
      __requestId?: string;
    } | undefined;
    const duration = config?.__startTime ? performance.now() - config.__startTime : 0;
    const requestId = config?.__requestId || 'unknown';

    // Log the error details
    logger.apiError(
      config?.method?.toUpperCase() || 'UNKNOWN',
      config?.url || 'unknown',
      error,
      {
        requestId,
        status: error.response?.status,
        statusText: error.response?.statusText,
        duration: Math.round(duration),
        responseData: maskSensitiveData(error.response?.data),
        code: error.code,
        isNetworkError: !error.response,
      }
    );

    // Handle 401 Unauthorized - redirect to login
    if (error.response?.status === 401) {
      logger.auth('Unauthorized response received - session may be expired', {
        requestId,
        url: config?.url,
      });

      if (typeof window !== 'undefined') {
        // Only redirect if not already on login page
        if (!window.location.pathname.includes('/login') &&
            !window.location.pathname.includes('/mfa')) {
          logger.auth('Clearing auth state and redirecting to login', {
            currentPath: window.location.pathname,
          });
          localStorage.removeItem('token');
          localStorage.removeItem('user');
          window.location.href = '/login';
        }
      }
    }

    // Handle 403 Forbidden
    if (error.response?.status === 403) {
      logger.warn('Access denied - insufficient permissions', {
        requestId,
        url: config?.url,
        status: 403,
      });
    }

    // Handle network errors
    if (!error.response) {
      logger.error('Network error - no response received', {
        requestId,
        url: config?.url,
        code: error.code,
        message: error.message,
      }, error);
    }

    // Handle timeout
    if (error.code === 'ECONNABORTED') {
      logger.error('Request timeout', {
        requestId,
        url: config?.url,
        timeout: config?.timeout,
      }, error);
    }

    return Promise.reject(error);
  }
);

export default api;
