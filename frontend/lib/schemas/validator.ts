/**
 * API Response Validator Utility
 *
 * Provides runtime validation of API responses using Zod schemas.
 * Logs warnings for validation failures but does not break the application.
 */
import { z } from 'zod';

export interface ValidationResult<T> {
  success: boolean;
  data: T;
  errors?: z.ZodError;
}

/**
 * Validates API response data against a Zod schema.
 * Returns the data regardless of validation result, but logs warnings on failure.
 *
 * @param data - The data to validate
 * @param schema - The Zod schema to validate against
 * @param context - Optional context string for logging (e.g., endpoint name)
 * @returns ValidationResult with the original data and validation status
 */
export function validateApiResponse<T>(
  data: unknown,
  schema: z.ZodType<T>,
  context?: string
): ValidationResult<T> {
  const result = schema.safeParse(data);

  if (!result.success) {
    const contextStr = context ? ` [${context}]` : '';
    console.warn(
      `API Response Validation Warning${contextStr}:`,
      '\nErrors:',
      ((result.error?.errors ?? result.error?.issues ?? [])).map(e => ({
        path: e.path.join('.'),
        message: e.message,
        received: e.code === 'invalid_type' ? (e as { received?: unknown }).received : undefined,
      })),
      '\nData received:',
      JSON.stringify(data, null, 2).substring(0, 500)
    );

    return {
      success: false,
      data: data as T, // Return original data even on failure
      errors: result.error,
    };
  }

  return {
    success: true,
    data: result.data,
  };
}

/**
 * Creates a validated API response handler for use with axios interceptors
 * or custom API wrappers.
 *
 * @param schema - The Zod schema to validate against
 * @param context - Optional context for logging
 * @returns A function that validates and returns the data
 */
export function createValidator<T>(
  schema: z.ZodType<T>,
  context?: string
): (data: unknown) => T {
  return (data: unknown) => {
    const result = validateApiResponse(data, schema, context);
    return result.data;
  };
}

/**
 * Strict validation that throws on failure.
 * Use this when you want to fail fast on invalid data.
 *
 * @param data - The data to validate
 * @param schema - The Zod schema to validate against
 * @param context - Optional context for error message
 * @throws Error if validation fails
 */
export function validateStrictApiResponse<T>(
  data: unknown,
  schema: z.ZodType<T>,
  context?: string
): T {
  const result = schema.safeParse(data);

  if (!result.success) {
    const contextStr = context ? ` (${context})` : '';
    throw new Error(
      `API Response Validation Failed${contextStr}: ${result.error.message}`
    );
  }

  return result.data;
}

/**
 * Validates an array of items against a schema.
 * Filters out invalid items and logs warnings.
 *
 * @param data - Array of items to validate
 * @param itemSchema - Schema for individual items
 * @param context - Optional context for logging
 * @returns Array of valid items only
 */
export function validateArrayItems<T>(
  data: unknown[],
  itemSchema: z.ZodType<T>,
  context?: string
): T[] {
  const validItems: T[] = [];
  const invalidIndices: number[] = [];

  data.forEach((item, index) => {
    const result = itemSchema.safeParse(item);
    if (result.success) {
      validItems.push(result.data);
    } else {
      invalidIndices.push(index);
    }
  });

  if (invalidIndices.length > 0) {
    const contextStr = context ? ` [${context}]` : '';
    console.warn(
      `API Response Validation Warning${contextStr}: ${invalidIndices.length} invalid items at indices: ${invalidIndices.join(', ')}`
    );
  }

  return validItems;
}

/**
 * Type guard helper - validates data and returns typed data if valid.
 *
 * @param data - The data to validate
 * @param schema - The Zod schema to validate against
 * @returns The typed data if valid, undefined otherwise
 */
export function parseOrUndefined<T>(
  data: unknown,
  schema: z.ZodType<T>
): T | undefined {
  const result = schema.safeParse(data);
  return result.success ? result.data : undefined;
}
