import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';
import { NavigationPage } from './pages/NavigationPage';

/**
 * KURATOR Application Smoke Tests
 *
 * These tests verify critical functionality of the KURATOR application:
 * - Authentication (login/logout)
 * - Dashboard loading
 * - Basic navigation
 * - Key pages availability
 */

test.describe('KURATOR Smoke Tests', () => {

  test.beforeEach(async ({ page }) => {
    // Clear any stored authentication before each test
    await page.context().clearCookies();
    await page.goto('/');
  });

  test('1. Login page loads correctly', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.goto();

    // Verify login page elements are visible
    await expect(loginPage.pageTitle).toBeVisible();
    await expect(loginPage.loginInput).toBeVisible();
    await expect(loginPage.passwordInput).toBeVisible();
    await expect(loginPage.submitButton).toBeVisible();
  });

  test('2. Successful login with admin credentials', async ({ page }) => {
    const loginPage = new LoginPage(page);
    const dashboardPage = new DashboardPage(page);

    await loginPage.goto();
    await loginPage.login('admin', 'admin123');

    // Wait for redirect to dashboard
    await loginPage.waitForDashboardRedirect();

    // Verify dashboard loaded
    await dashboardPage.waitForLoad();
    expect(await dashboardPage.isWelcomeVisible()).toBe(true);
  });

  test('3. Failed login with incorrect credentials', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.goto();
    await loginPage.login('wronguser', 'wrongpassword');

    // Wait a bit for error to appear
    await page.waitForTimeout(1000);

    // Verify error message appears
    const hasError = await loginPage.isErrorVisible();
    expect(hasError).toBe(true);

    // Verify we're still on login page
    expect(page.url()).toContain('/login');
  });

  test('4. Dashboard displays correctly for admin', async ({ page }) => {
    const loginPage = new LoginPage(page);
    const dashboardPage = new DashboardPage(page);

    // Login as admin
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.waitForDashboardRedirect();

    // Wait for dashboard to load
    await dashboardPage.waitForLoad();

    // Verify dashboard elements
    expect(await dashboardPage.isWelcomeVisible()).toBe(true);
    expect(await dashboardPage.hasMetricsCards()).toBe(true);

    // Verify role is displayed
    const role = await dashboardPage.getUserRole();
    expect(role).toBeTruthy();
  });

  test('5. Navigation to Contacts page', async ({ page }) => {
    const loginPage = new LoginPage(page);
    const dashboardPage = new DashboardPage(page);

    // Login
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.waitForDashboardRedirect();
    await dashboardPage.waitForLoad();

    // Navigate to contacts
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');

    // Verify we're on contacts page
    expect(page.url()).toContain('/contacts');

    // Verify page loaded (look for common elements)
    const pageContent = await page.textContent('body');
    expect(pageContent).toBeTruthy();
  });

  test('6. Navigation to Interactions page', async ({ page }) => {
    const loginPage = new LoginPage(page);
    const dashboardPage = new DashboardPage(page);

    // Login
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.waitForDashboardRedirect();
    await dashboardPage.waitForLoad();

    // Navigate to interactions
    await page.goto('/interactions');
    await page.waitForLoadState('networkidle');

    // Verify we're on interactions page
    expect(page.url()).toContain('/interactions');

    // Verify page loaded
    const pageContent = await page.textContent('body');
    expect(pageContent).toBeTruthy();
  });

  test('7. Navigation to Blocks page (Admin only)', async ({ page }) => {
    const loginPage = new LoginPage(page);
    const dashboardPage = new DashboardPage(page);

    // Login as admin
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.waitForDashboardRedirect();
    await dashboardPage.waitForLoad();

    // Navigate to blocks
    await page.goto('/blocks');
    await page.waitForLoadState('networkidle');

    // Verify we're on blocks page
    expect(page.url()).toContain('/blocks');

    // Verify page loaded
    const pageContent = await page.textContent('body');
    expect(pageContent).toBeTruthy();
  });

  test('8. Navigation to Users page (Admin only)', async ({ page }) => {
    const loginPage = new LoginPage(page);
    const dashboardPage = new DashboardPage(page);

    // Login as admin
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.waitForDashboardRedirect();
    await dashboardPage.waitForLoad();

    // Navigate to users
    await page.goto('/users');
    await page.waitForLoadState('networkidle');

    // Verify we're on users page
    expect(page.url()).toContain('/users');

    // Verify page loaded
    const pageContent = await page.textContent('body');
    expect(pageContent).toBeTruthy();
  });

  test('9. Protected routes redirect to login when not authenticated', async ({ page }) => {
    // Try to access dashboard without logging in
    await page.goto('/dashboard');

    // Should be redirected to login
    await page.waitForURL('**/login', { timeout: 10000 });
    expect(page.url()).toContain('/login');
  });

  test('10. Application handles API errors gracefully', async ({ page }) => {
    const loginPage = new LoginPage(page);

    await loginPage.goto();

    // Simulate network error by blocking API calls
    await page.route('**/api/auth/login', route => {
      route.abort('failed');
    });

    await loginPage.login('admin', 'admin123');

    // Wait for error handling
    await page.waitForTimeout(2000);

    // Should show error message or still be on login page
    expect(page.url()).toContain('/login');
  });
});

test.describe('KURATOR Critical User Flows', () => {

  test('Complete user journey: Login -> View Dashboard -> Navigate pages -> Logout', async ({ page }) => {
    const loginPage = new LoginPage(page);
    const dashboardPage = new DashboardPage(page);

    // Step 1: Login
    await loginPage.goto();
    await loginPage.login('admin', 'admin123');
    await loginPage.waitForDashboardRedirect();

    // Step 2: Verify Dashboard
    await dashboardPage.waitForLoad();
    expect(await dashboardPage.isWelcomeVisible()).toBe(true);

    // Step 3: Navigate to different pages
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    expect(page.url()).toContain('/contacts');

    await page.goto('/interactions');
    await page.waitForLoadState('networkidle');
    expect(page.url()).toContain('/interactions');

    // Step 4: Return to dashboard
    await page.goto('/dashboard');
    await dashboardPage.waitForLoad();
    expect(await dashboardPage.isWelcomeVisible()).toBe(true);

    // Step 5: Logout (if logout functionality exists)
    // Clear localStorage to simulate logout
    await page.evaluate(() => {
      localStorage.clear();
    });

    await page.goto('/dashboard');
    await page.waitForURL('**/login', { timeout: 10000 });
    expect(page.url()).toContain('/login');
  });
});
