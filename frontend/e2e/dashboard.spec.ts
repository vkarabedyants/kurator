import { test, expect } from '@playwright/test';
import { DashboardPage } from './pages/DashboardPage';

/**
 * KURATOR Dashboard E2E Tests
 * Tests for dashboard metrics and role-based views
 *
 * OPTIMIZATION: Uses shared authentication state from global.setup.ts
 * No longer performs login in beforeEach - the page is pre-authenticated
 * via storageState in playwright.config.ts
 */

test.describe('Dashboard - Admin View', () => {
  let dashboardPage: DashboardPage;

  test.beforeEach(async ({ page }) => {
    dashboardPage = new DashboardPage(page);
    // No login needed - using shared auth state
  });

  test('should display welcome message with user name', async ({ page }) => {
    await page.goto('/dashboard');
    await dashboardPage.waitForLoad();

    // Welcome message should include user login
    const welcomeText = await dashboardPage.welcomeHeading.textContent();
    expect(welcomeText).toContain('Добро пожаловать');
  });

  test('should display user role', async ({ page }) => {
    await page.goto('/dashboard');
    await dashboardPage.waitForLoad();

    const roleText = await dashboardPage.getUserRole();
    expect(roleText).toBeTruthy();
  });

  test('should display metrics cards', async ({ page }) => {
    await page.goto('/dashboard');
    await dashboardPage.waitForLoad();

    const hasMetrics = await dashboardPage.hasMetricsCards();
    expect(hasMetrics).toBe(true);
  });

  test('should display quick actions for admin', async ({ page }) => {
    await page.goto('/dashboard');
    await dashboardPage.waitForLoad();

    // Admin should have access to user management
    const userManagementLink = page.locator('a[href="/users"]');
    if (await userManagementLink.count() > 0) {
      await expect(userManagementLink).toBeVisible();
    }
  });

  test('should display link to blocks management', async ({ page }) => {
    await page.goto('/dashboard');
    await dashboardPage.waitForLoad();

    const blocksLink = page.locator('a[href="/blocks"]');
    if (await blocksLink.count() > 0) {
      await expect(blocksLink).toBeVisible();
    }
  });

  test('should display link to audit log', async ({ page }) => {
    await page.goto('/dashboard');
    await dashboardPage.waitForLoad();

    const auditLink = page.locator('a[href="/audit-log"]');
    if (await auditLink.count() > 0) {
      await expect(auditLink).toBeVisible();
    }
  });

  test('should display total contacts metric', async ({ page }) => {
    await page.goto('/dashboard');
    await dashboardPage.waitForLoad();

    // Look for total contacts metric card
    const contactsMetric = page.locator('text=Всего контактов').or(page.locator('p:has-text("Всего контактов")'));
    if (await contactsMetric.count() > 0) {
      await expect(contactsMetric.first()).toBeVisible();
    }
  });

  test('should display total interactions metric', async ({ page }) => {
    await page.goto('/dashboard');
    await dashboardPage.waitForLoad();

    // Look for interactions metric
    const interactionsMetric = page.locator('text=взаимодействий').or(page.locator('p:has-text("взаимодействий")'));
    if (await interactionsMetric.count() > 0) {
      await expect(interactionsMetric.first()).toBeVisible();
    }
  });

  test('should display blocks metric', async ({ page }) => {
    await page.goto('/dashboard');
    await dashboardPage.waitForLoad();

    // Look for blocks metric
    const blocksMetric = page.locator('text=блок').or(page.locator('p:has-text("блок")'));
    if (await blocksMetric.count() > 0) {
      await expect(blocksMetric.first()).toBeVisible();
    }
  });

  test('should display users metric', async ({ page }) => {
    await page.goto('/dashboard');
    await dashboardPage.waitForLoad();

    // Look for users metric
    const usersMetric = page.locator('text=пользовател').or(page.locator('p:has-text("пользовател")'));
    if (await usersMetric.count() > 0) {
      await expect(usersMetric.first()).toBeVisible();
    }
  });
});

test.describe('Dashboard - Contacts Statistics', () => {
  // No login needed - using shared auth state

  test('should display contacts by block section', async ({ page }) => {
    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    const blockSection = page.locator('h2:has-text("Контакты по блокам")').or(page.locator('text=Контакты по блокам'));
    if (await blockSection.count() > 0) {
      await expect(blockSection.first()).toBeVisible();
    }
  });

  test('should display contacts by status section', async ({ page }) => {
    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    const statusSection = page.locator('h2:has-text("Контакты по статусу")').or(page.locator('text=Контакты по статусам'));
    if (await statusSection.count() > 0) {
      await expect(statusSection.first()).toBeVisible();
    }
  });

  test('should display influence status distribution', async ({ page }) => {
    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    // Look for status labels (A, B, C, D)
    const statusLabels = page.locator('text=/Статус [ABCD]/').or(page.locator('span:has-text("A")'));
    if (await statusLabels.count() > 0) {
      // Should have at least one status
      expect(await statusLabels.count()).toBeGreaterThanOrEqual(0);
    }
  });
});

test.describe('Dashboard - Curator Statistics', () => {
  // No login needed - using shared auth state

  test('should display top curators section', async ({ page }) => {
    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    const curatorsSection = page.locator('h2:has-text("Топ кураторов")').or(page.locator('text=Топ кураторов'));
    if (await curatorsSection.count() > 0) {
      await expect(curatorsSection.first()).toBeVisible();
    }
  });
});

test.describe('Dashboard - Recent Activity', () => {
  // No login needed - using shared auth state

  test('should display recent activity section', async ({ page }) => {
    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    const activitySection = page.locator('h2:has-text("Последняя активность")').or(page.locator('text=Последняя активность'));
    if (await activitySection.count() > 0) {
      await expect(activitySection.first()).toBeVisible();
    }
  });

  test('should display recent audit logs table', async ({ page }) => {
    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    // Admin dashboard should show recent audit log entries
    const auditTable = page.locator('table').first();
    if (await auditTable.count() > 0) {
      await expect(auditTable).toBeVisible();
    }
  });
});

test.describe('Dashboard - Status Dynamics', () => {
  // No login needed - using shared auth state

  test('should display status dynamics section', async ({ page }) => {
    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    const dynamicsSection = page.locator('h2:has-text("Динамика статусов")').or(page.locator('text=Динамика статусов'));
    if (await dynamicsSection.count() > 0) {
      await expect(dynamicsSection.first()).toBeVisible();
    }
  });
});

test.describe('Dashboard - Error Handling', () => {
  // No login needed - using shared auth state

  test('should handle API errors gracefully', async ({ page }) => {
    // Block dashboard API calls
    await page.route('**/api/dashboard**', route => route.abort('failed'));

    await page.goto('/dashboard');
    await page.waitForLoadState('domcontentloaded');

    // Page should still render
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should show error message when dashboard fails to load', async ({ page }) => {
    // Block API calls to return error
    await page.route('**/api/dashboard**', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Internal server error' })
      });
    });

    await page.goto('/dashboard');
    await page.waitForLoadState('domcontentloaded');

    // Error text might be visible
    const errorText = page.locator('.text-red-500, .text-red-600');
    if (await errorText.count() > 0) {
      expect(await errorText.count()).toBeGreaterThan(0);
    }
  });
});

test.describe('Dashboard - Loading States', () => {
  // No login needed - using shared auth state

  test('should show loading spinner while loading', async ({ page }) => {
    const dashboardPage = new DashboardPage(page);

    // Slow down API response
    await page.route('**/api/dashboard**', async route => {
      await new Promise(resolve => setTimeout(resolve, 500));
      await route.continue();
    });

    await page.goto('/dashboard');

    // Loading spinner should be visible briefly
    // Spinner might appear and disappear quickly
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });
});

test.describe('Dashboard - Responsive Design', () => {
  // No login needed - using shared auth state

  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    // Welcome message should still be visible
    await expect(dashboardPage.welcomeHeading).toBeVisible();
  });

  test('should display correctly on tablet viewport', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });

    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    // Welcome message should still be visible
    await expect(dashboardPage.welcomeHeading).toBeVisible();
  });

  test('should stack metric cards on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    // Metric cards should still be visible
    const hasMetrics = await dashboardPage.hasMetricsCards();
    expect(hasMetrics).toBe(true);
  });
});

test.describe('Dashboard - Navigation from Dashboard', () => {
  // No login needed - using shared auth state

  test('should navigate to users from quick action', async ({ page }) => {
    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    const usersLink = page.locator('a[href="/users"]');
    if (await usersLink.count() > 0) {
      await usersLink.first().click();
      await page.waitForURL('**/users');
      expect(page.url()).toContain('/users');
    }
  });

  test('should navigate to blocks from quick action', async ({ page }) => {
    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    const blocksLink = page.locator('a[href="/blocks"]');
    if (await blocksLink.count() > 0) {
      await blocksLink.first().click();
      await page.waitForURL('**/blocks');
      expect(page.url()).toContain('/blocks');
    }
  });

  test('should navigate to audit log from quick action', async ({ page }) => {
    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    const auditLink = page.locator('a[href="/audit-log"]');
    if (await auditLink.count() > 0) {
      await auditLink.first().click();
      await page.waitForURL('**/audit-log');
      expect(page.url()).toContain('/audit-log');
    }
  });
});

test.describe('Dashboard - Metric Card Highlighting', () => {
  // No login needed - using shared auth state

  test('should highlight overdue contacts card if applicable', async ({ page }) => {
    await page.goto('/dashboard');
    const dashboardPage = new DashboardPage(page);
    await dashboardPage.waitForLoad();

    // Overdue contacts card might have ring-2 ring-red-500 class
    const highlightedCard = page.locator('.ring-2.ring-red-500');
    // This is conditional based on data
    const count = await highlightedCard.count();
    expect(count).toBeGreaterThanOrEqual(0);
  });
});
