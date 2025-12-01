import { test as base, expect } from '@playwright/test';
import { LoginPage } from '../pages/LoginPage';

/**
 * Extended test fixture with authentication helpers
 *
 * This fixture provides:
 * 1. Pre-authenticated page (when using storageState from config)
 * 2. Helper to verify authentication status
 * 3. Quick navigation helpers
 */
export const test = base.extend<{
  authenticatedPage: ReturnType<typeof base.extend>;
}>({
  // The page is already authenticated via storageState in config
  // This fixture just provides convenient helpers
});

/**
 * Helper to verify the page is authenticated
 * Can be used at the start of tests to ensure auth state is valid
 */
export async function verifyAuthenticated(page: any): Promise<boolean> {
  // Check if we're on a protected page (not login)
  const url = page.url();
  if (url.includes('/login')) {
    return false;
  }

  // Check for token in localStorage
  const token = await page.evaluate(() => localStorage.getItem('token'));
  return !!token;
}

/**
 * Quick navigation helper - navigates and waits for page to be ready
 * Uses domcontentloaded instead of networkidle for speed
 */
export async function navigateTo(page: any, path: string): Promise<void> {
  await page.goto(path);
  await page.waitForLoadState('domcontentloaded');
}

/**
 * Wait for API response - more reliable than fixed timeouts
 */
export async function waitForApiResponse(
  page: any,
  urlPattern: string | RegExp,
  timeout = 5000
): Promise<void> {
  await page.waitForResponse(
    (response: any) => {
      if (typeof urlPattern === 'string') {
        return response.url().includes(urlPattern);
      }
      return urlPattern.test(response.url());
    },
    { timeout }
  );
}

// Re-export expect for convenience
export { expect };
