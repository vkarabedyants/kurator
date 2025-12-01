import { defineConfig, devices } from '@playwright/test';
import path from 'path';

/**
 * Playwright configuration for KURATOR E2E tests
 *
 * OPTIMIZATIONS APPLIED:
 * 1. Parallel test execution with multiple workers
 * 2. Shared authentication state to avoid re-login
 * 3. Optimized timeouts for faster failure detection
 * 4. Browser context reuse through storage state
 *
 * @see https://playwright.dev/docs/test-configuration
 */

// Storage state file for authenticated sessions
const STORAGE_STATE = path.join(__dirname, 'e2e/.auth/admin.json');

export default defineConfig({
  testDir: './e2e',

  /* Run tests in files in parallel */
  fullyParallel: true,

  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,

  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,

  /* Use multiple workers for parallel execution */
  workers: process.env.CI ? 2 : 4,

  /* Global timeout for each test */
  timeout: 30000,

  /* Expect timeout - faster assertion failures */
  expect: {
    timeout: 5000,
  },

  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['list']
  ],

  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    /* Base URL to use in actions like `await page.goto('/')`. */
    baseURL: 'http://localhost:3000',

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: 'on-first-retry',

    /* Screenshot on failure */
    screenshot: 'only-on-failure',

    /* Video - disabled by default for speed, enable only on failure in CI */
    video: process.env.CI ? 'retain-on-failure' : 'off',

    /* Maximum time each action can take - reduced for faster failure detection */
    actionTimeout: 8000,

    /* Navigation timeout - reduced from 30000 */
    navigationTimeout: 15000,

    /* Bypass CSP for faster page loads in tests */
    bypassCSP: true,

    /* Accept downloads automatically */
    acceptDownloads: true,
  },

  /* Configure projects for major browsers */
  projects: [
    // Setup project - runs first to create auth state
    {
      name: 'setup',
      testMatch: /global\.setup\.ts/,
      teardown: 'cleanup',
    },
    // Cleanup project - runs after all tests
    {
      name: 'cleanup',
      testMatch: /global\.teardown\.ts/,
    },
    // Main test project - uses authenticated state
    {
      name: 'chromium',
      // Exclude tests that need fresh sessions (login flow, role tests)
      testIgnore: /smoke\.spec\.ts|role-based-access\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1280, height: 720 },
        // Use stored authentication state
        storageState: STORAGE_STATE,
      },
      dependencies: ['setup'],
    },
    // Unauthenticated tests - for login page, auth flow tests
    // These tests need fresh sessions without pre-authenticated state
    {
      name: 'chromium-no-auth',
      testMatch: /smoke\.spec\.ts|role-based-access\.spec\.ts/,
      use: {
        ...devices['Desktop Chrome'],
        viewport: { width: 1280, height: 720 },
        // No storage state - fresh session
        storageState: undefined,
      },
    },
  ],

  /* Run your local dev server before starting the tests */
  // Uncomment if you want Playwright to start the server automatically
  // webServer: {
  //   command: 'npm run dev',
  //   url: 'http://localhost:3000',
  //   reuseExistingServer: !process.env.CI,
  //   timeout: 120 * 1000,
  // },
});
