/**
 * Structured Logger for KURATOR Frontend
 *
 * This logger outputs JSON-formatted logs to stdout (console) for easy parsing
 * and integration with log aggregation systems.
 *
 * Log Format:
 * {
 *   "timestamp": "2024-01-15T10:30:45.123Z",
 *   "level": "info",
 *   "message": "User logged in",
 *   "context": { ... additional data ... }
 * }
 */

type LogLevel = 'trace' | 'debug' | 'info' | 'warn' | 'error';

interface LogEntry {
  timestamp: string;
  level: LogLevel;
  message: string;
  context?: Record<string, unknown>;
  error?: {
    name: string;
    message: string;
    stack?: string;
  };
}

interface LoggerConfig {
  minLevel: LogLevel;
  enableConsole: boolean;
  includeTimestamp: boolean;
  includeStackTrace: boolean;
  applicationName: string;
  environment: string;
}

const LOG_LEVELS: Record<LogLevel, number> = {
  trace: 0,
  debug: 1,
  info: 2,
  warn: 3,
  error: 4,
};

// Default configuration
const defaultConfig: LoggerConfig = {
  minLevel: (process.env.NODE_ENV === 'development' ? 'debug' : 'info') as LogLevel,
  enableConsole: true,
  includeTimestamp: true,
  includeStackTrace: process.env.NODE_ENV === 'development',
  applicationName: 'kurator-frontend',
  environment: process.env.NODE_ENV || 'development',
};

let config: LoggerConfig = { ...defaultConfig };

/**
 * Configure the logger
 */
export function configureLogger(newConfig: Partial<LoggerConfig>): void {
  config = { ...config, ...newConfig };
}

/**
 * Check if a log level should be output
 */
function shouldLog(level: LogLevel): boolean {
  return LOG_LEVELS[level] >= LOG_LEVELS[config.minLevel];
}

/**
 * Format error object for logging
 */
function formatError(error: unknown): LogEntry['error'] | undefined {
  if (!error) return undefined;

  if (error instanceof Error) {
    return {
      name: error.name,
      message: error.message,
      stack: config.includeStackTrace ? error.stack : undefined,
    };
  }

  return {
    name: 'UnknownError',
    message: String(error),
  };
}

/**
 * Create a log entry
 */
function createLogEntry(
  level: LogLevel,
  message: string,
  context?: Record<string, unknown>,
  error?: unknown
): LogEntry {
  const entry: LogEntry = {
    timestamp: new Date().toISOString(),
    level,
    message,
  };

  // Add application metadata
  const metadata: Record<string, unknown> = {
    application: config.applicationName,
    environment: config.environment,
  };

  // Add browser info if available
  if (typeof window !== 'undefined') {
    metadata.url = window.location.href;
    metadata.userAgent = navigator.userAgent;
  }

  // Merge context with metadata
  if (context || Object.keys(metadata).length > 0) {
    entry.context = { ...metadata, ...context };
  }

  // Add error if present
  if (error) {
    entry.error = formatError(error);
  }

  return entry;
}

/**
 * Output log entry to console
 */
function outputLog(entry: LogEntry): void {
  if (!config.enableConsole) return;

  const jsonOutput = JSON.stringify(entry);

  switch (entry.level) {
    case 'trace':
    case 'debug':
      console.debug(jsonOutput);
      break;
    case 'info':
      console.info(jsonOutput);
      break;
    case 'warn':
      console.warn(jsonOutput);
      break;
    case 'error':
      console.error(jsonOutput);
      break;
  }
}

/**
 * Main logging function
 */
function log(
  level: LogLevel,
  message: string,
  context?: Record<string, unknown>,
  error?: unknown
): void {
  if (!shouldLog(level)) return;

  const entry = createLogEntry(level, message, context, error);
  outputLog(entry);
}

/**
 * Logger interface with convenience methods
 */
export const logger = {
  /**
   * Trace level - most detailed logging
   */
  trace: (message: string, context?: Record<string, unknown>) => {
    log('trace', message, context);
  },

  /**
   * Debug level - detailed information for debugging
   */
  debug: (message: string, context?: Record<string, unknown>) => {
    log('debug', message, context);
  },

  /**
   * Info level - general operational information
   */
  info: (message: string, context?: Record<string, unknown>) => {
    log('info', message, context);
  },

  /**
   * Warn level - warning messages for potential issues
   */
  warn: (message: string, context?: Record<string, unknown>, error?: unknown) => {
    log('warn', message, context, error);
  },

  /**
   * Error level - error messages for failures
   */
  error: (message: string, context?: Record<string, unknown>, error?: unknown) => {
    log('error', message, context, error);
  },

  /**
   * Log API request
   */
  apiRequest: (
    method: string,
    url: string,
    context?: Record<string, unknown>
  ) => {
    log('debug', `API Request: ${method} ${url}`, {
      type: 'api_request',
      method,
      url,
      ...context,
    });
  },

  /**
   * Log API response
   */
  apiResponse: (
    method: string,
    url: string,
    status: number,
    duration: number,
    context?: Record<string, unknown>
  ) => {
    const level: LogLevel = status >= 400 ? (status >= 500 ? 'error' : 'warn') : 'debug';
    log(level, `API Response: ${method} ${url} - ${status}`, {
      type: 'api_response',
      method,
      url,
      status,
      duration,
      ...context,
    });
  },

  /**
   * Log API error
   */
  apiError: (
    method: string,
    url: string,
    error: unknown,
    context?: Record<string, unknown>
  ) => {
    log('error', `API Error: ${method} ${url}`, {
      type: 'api_error',
      method,
      url,
      ...context,
    }, error);
  },

  /**
   * Log user action
   */
  userAction: (action: string, context?: Record<string, unknown>) => {
    log('info', `User Action: ${action}`, {
      type: 'user_action',
      action,
      ...context,
    });
  },

  /**
   * Log navigation event
   */
  navigation: (from: string, to: string, context?: Record<string, unknown>) => {
    log('debug', `Navigation: ${from} -> ${to}`, {
      type: 'navigation',
      from,
      to,
      ...context,
    });
  },

  /**
   * Log authentication event
   */
  auth: (event: string, context?: Record<string, unknown>) => {
    log('info', `Auth: ${event}`, {
      type: 'authentication',
      event,
      ...context,
    });
  },

  /**
   * Log performance metric
   */
  performance: (metric: string, value: number, unit: string, context?: Record<string, unknown>) => {
    log('debug', `Performance: ${metric} = ${value}${unit}`, {
      type: 'performance',
      metric,
      value,
      unit,
      ...context,
    });
  },

  /**
   * Create a child logger with preset context
   */
  child: (baseContext: Record<string, unknown>) => ({
    trace: (message: string, context?: Record<string, unknown>) => {
      log('trace', message, { ...baseContext, ...context });
    },
    debug: (message: string, context?: Record<string, unknown>) => {
      log('debug', message, { ...baseContext, ...context });
    },
    info: (message: string, context?: Record<string, unknown>) => {
      log('info', message, { ...baseContext, ...context });
    },
    warn: (message: string, context?: Record<string, unknown>, error?: unknown) => {
      log('warn', message, { ...baseContext, ...context }, error);
    },
    error: (message: string, context?: Record<string, unknown>, error?: unknown) => {
      log('error', message, { ...baseContext, ...context }, error);
    },
  }),

  /**
   * Start a timer for measuring operation duration
   */
  startTimer: (operationName: string): (() => number) => {
    const start = performance.now();
    return () => {
      const duration = performance.now() - start;
      log('debug', `Timer: ${operationName} completed`, {
        type: 'timer',
        operation: operationName,
        duration,
        durationMs: Math.round(duration),
      });
      return duration;
    };
  },
};

// Export types
export type { LogLevel, LogEntry, LoggerConfig };

// Default export
export default logger;
