import { test as setup, expect } from '@playwright/test';
import path from 'path';

const STORAGE_STATE = path.join(__dirname, '.auth/admin.json');

/**
 * Global setup for E2E tests
 *
 * This setup runs once before all tests to:
 * 1. Authenticate as admin user
 * 2. Save the authentication state (cookies, localStorage)
 * 3. Subsequent tests reuse this state, avoiding repeated logins
 *
 * This is a major performance optimization - login is done once instead of
 * once per test (previously 200+ logins for the full test suite).
 */
setup('authenticate as admin', async ({ page }) => {
  // Navigate to login page
  await page.goto('/login');

  // Wait for the login form to be ready
  await page.waitForLoadState('domcontentloaded');

  // Fill in admin credentials
  const loginInput = page.locator('input#login').or(page.locator('input[name="login"]')).first();
  const passwordInput = page.locator('input#password').or(page.locator('input[name="password"]')).first();
  const submitButton = page.locator('button[type="submit"]').first();

  await expect(loginInput).toBeVisible({ timeout: 10000 });
  await loginInput.fill('admin');
  await passwordInput.fill('Admin123!');
  await submitButton.click();

  // Wait for successful redirect to dashboard
  await page.waitForURL('**/dashboard', { timeout: 15000 });

  // Wait for dashboard to be fully loaded
  await page.waitForLoadState('domcontentloaded');

  // Verify we're authenticated by checking for dashboard content
  await expect(page.locator('h1, h2').first()).toBeVisible({ timeout: 5000 });

  // Save the authentication state
  await page.context().storageState({ path: STORAGE_STATE });

  console.log('Authentication state saved to:', STORAGE_STATE);
});
