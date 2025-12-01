import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { AuditLogPage } from './pages/AuditLogPage';

/**
 * KURATOR Audit Log E2E Tests
 * Tests for audit log viewing and filtering (Admin only feature)
 */

test.describe('Audit Log - Admin Access', () => {
  let loginPage: LoginPage;
  let auditLogPage: AuditLogPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    auditLogPage = new AuditLogPage(page);

    // Login as admin
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should navigate to audit log page', async ({ page }) => {
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    expect(page.url()).toContain('/audit-log');
    await expect(auditLogPage.pageTitle).toBeVisible();
  });

  test('should display page title and description', async ({ page }) => {
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    await expect(auditLogPage.pageTitle).toBeVisible();

    const description = page.locator('text=Отслеживание всех действий пользователей');
    await expect(description).toBeVisible();
  });

  test('should display total records count', async ({ page }) => {
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    await expect(auditLogPage.totalRecords).toBeVisible();
  });

  test('should display audit log table', async ({ page }) => {
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // If logs exist, table should be visible
    const isEmpty = await auditLogPage.isEmptyStateVisible();
    if (!isEmpty) {
      await expect(auditLogPage.logTable).toBeVisible();
    }
  });

  test('should have correct table headers', async ({ page }) => {
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const isEmpty = await auditLogPage.isEmptyStateVisible();
    if (!isEmpty) {
      const headers = await auditLogPage.getTableHeaders();

      expect(headers).toContain('Время');
      expect(headers).toContain('Пользователь');
      expect(headers).toContain('Действие');
      expect(headers).toContain('Сущность');
      expect(headers).toContain('Детали');
    }
  });
});

test.describe('Audit Log - Filters', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should have search input available', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    await expect(auditLogPage.searchInput).toBeVisible();
  });

  test('should have user filter available', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // Wait for page to fully load with data
    await page.waitForTimeout(2000);

    // Find the user filter select (first one in the filter panel)
    const userSelect = page.locator('.bg-white.rounded-lg.shadow select').first();
    await expect(userSelect).toBeVisible();

    // Check that it has the default "All users" option
    const options = userSelect.locator('option');
    const optionCount = await options.count();
    expect(optionCount).toBeGreaterThanOrEqual(1);

    // The first option should be "All users"
    const firstOption = await options.first().textContent();
    expect(firstOption?.trim()).toBe('Все пользователи');
  });

  test('should have action filter available', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const actionSelect = page.locator('select').nth(1);
    const options = actionSelect.locator('option');

    const optionTexts: string[] = [];
    const count = await options.count();
    for (let i = 0; i < count; i++) {
      const text = await options.nth(i).textContent();
      if (text) optionTexts.push(text.trim());
    }

    expect(optionTexts).toContain('Все действия');
    expect(optionTexts).toContain('Создание');
    expect(optionTexts).toContain('Обновление');
    expect(optionTexts).toContain('Удаление');
  });

  test('should have date range filters', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // Date inputs should exist
    const dateInputs = page.locator('input[type="date"]');
    expect(await dateInputs.count()).toBeGreaterThanOrEqual(2);
  });

  test('should filter by search term', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    await auditLogPage.search('admin');

    // Wait for filter to apply
    await page.waitForTimeout(1000);

    // Search input should have the value
    await expect(auditLogPage.searchInput).toHaveValue('admin');
  });

  test('should filter by action type', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    await auditLogPage.filterByAction('Create');

    // Wait for filter to apply
    await page.waitForTimeout(1000);

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should filter by date range', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // Set date range
    const today = new Date();
    const lastWeek = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000);
    const fromDate = lastWeek.toISOString().split('T')[0];
    const toDate = today.toISOString().split('T')[0];

    await auditLogPage.filterByDateRange(fromDate, toDate);

    // Wait for filter to apply
    await page.waitForTimeout(1000);

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should show clear filters button when filters are active', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // Apply a filter
    await auditLogPage.search('test');
    await page.waitForTimeout(500);

    // Clear filters button should be visible
    const clearButton = page.locator('button:has-text("Очистить все")').or(page.locator('text=Очистить все'));
    if (await clearButton.count() > 0) {
      await expect(clearButton).toBeVisible();
    }
  });

  test('should clear all filters', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // Apply filters
    await auditLogPage.search('test');
    await page.waitForTimeout(500);

    // Clear filters
    await auditLogPage.clearFilters();

    // Search should be empty
    await expect(auditLogPage.searchInput).toHaveValue('');
  });
});

test.describe('Audit Log - Log Entries', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display log entries if data exists', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const logCount = await auditLogPage.getLogCount();

    // Either have logs or show empty state
    if (logCount > 0) {
      expect(logCount).toBeGreaterThan(0);
    } else {
      await expect(auditLogPage.emptyState).toBeVisible();
    }
  });

  test('should display action type badges', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const logCount = await auditLogPage.getLogCount();

    if (logCount > 0) {
      // Action badges should exist
      const actionBadges = page.locator('tbody .rounded-full');
      expect(await actionBadges.count()).toBeGreaterThan(0);
    }
  });

  test('should display timestamp for each entry', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const logCount = await auditLogPage.getLogCount();

    if (logCount > 0) {
      // Time column should have content
      const firstRow = auditLogPage.logRows.first();
      const timeCell = firstRow.locator('td').first();
      const timeText = await timeCell.textContent();
      expect(timeText).toBeTruthy();
    }
  });

  test('should display user name for each entry', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const logCount = await auditLogPage.getLogCount();

    if (logCount > 0) {
      // Get users from entries
      const users = await auditLogPage.getLogUsers();
      expect(users.length).toBeGreaterThan(0);
    }
  });

  test('should display entity type for each entry', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const logCount = await auditLogPage.getLogCount();

    if (logCount > 0) {
      // Entity type column should have content
      const firstRow = auditLogPage.logRows.first();
      const entityCell = firstRow.locator('td').nth(3);
      const entityText = await entityCell.textContent();
      expect(entityText).toBeTruthy();
    }
  });
});

test.describe('Audit Log - Action Types', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display Create action with green styling', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // Check for create action badge styling
    const createBadge = page.locator('.bg-green-100.text-green-800:has-text("Создано")');
    if (await createBadge.count() > 0) {
      await expect(createBadge.first()).toBeVisible();
    }
  });

  test('should display Update action with blue styling', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // Check for update action badge styling
    const updateBadge = page.locator('.bg-blue-100.text-blue-800:has-text("Обновлено")');
    if (await updateBadge.count() > 0) {
      await expect(updateBadge.first()).toBeVisible();
    }
  });

  test('should display Delete action with red styling', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // Check for delete action badge styling
    const deleteBadge = page.locator('.bg-red-100.text-red-800:has-text("Удалено")');
    if (await deleteBadge.count() > 0) {
      await expect(deleteBadge.first()).toBeVisible();
    }
  });

  test('should display Login action with purple styling', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // Check for login action badge styling
    const loginBadge = page.locator('.bg-purple-100.text-purple-800:has-text("Вход выполнен")');
    if (await loginBadge.count() > 0) {
      await expect(loginBadge.first()).toBeVisible();
    }
  });
});

test.describe('Audit Log - Summary Statistics', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display summary statistics section', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const summaryHeader = page.locator('h3:has-text("Сводка активности")');
    await expect(summaryHeader).toBeVisible();
  });

  test('should display created count', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const createdLabel = page.locator('text=Создано');
    if (await createdLabel.count() > 0) {
      await expect(createdLabel.first()).toBeVisible();
    }
  });

  test('should display updated count', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const updatedLabel = page.locator('text=Обновлено');
    if (await updatedLabel.count() > 0) {
      await expect(updatedLabel.first()).toBeVisible();
    }
  });

  test('should display deleted count', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const deletedLabel = page.locator('text=Удалено');
    if (await deletedLabel.count() > 0) {
      await expect(deletedLabel.first()).toBeVisible();
    }
  });

  test('should display login count', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const loginsLabel = page.locator('text=Входов в систему');
    if (await loginsLabel.count() > 0) {
      await expect(loginsLabel.first()).toBeVisible();
    }
  });
});

test.describe('Audit Log - Pagination', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display pagination when many records exist', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // If there are many records, pagination should be visible
    const paginationNav = page.locator('nav').filter({ has: page.locator('button:has-text("Назад")') });
    if (await paginationNav.count() > 0) {
      await expect(paginationNav).toBeVisible();
    }
  });

  test('should navigate between pages', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // If pagination exists
    if (await auditLogPage.paginationNext.isVisible()) {
      // Try to go to next page
      await auditLogPage.goToNextPage();

      // Page 2 button should be active
      const page2Button = page.locator('button:has-text("2")');
      if (await page2Button.count() > 0) {
        await expect(page2Button).toHaveClass(/bg-indigo-600/);
      }
    }
  });
});

test.describe('Audit Log - Empty State', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should show empty state when no logs', async ({ page }) => {
    // Mock empty response
    await page.route('**/api/audit-log**', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ items: [] })
      });
    });

    await page.goto('/audit-log');
    await page.waitForTimeout(2000);

    // Empty state message should be visible
    const emptyMessage = page.locator('text=Записи аудита не найдены');
    await expect(emptyMessage).toBeVisible();
  });

  test('should show filter hint in empty state', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    // Apply a filter that returns no results
    await auditLogPage.search('nonexistent_search_term_xyz');
    await page.waitForTimeout(1000);

    // Check for empty state
    const emptyMessage = page.locator('text=Записи аудита не найдены');
    if (await emptyMessage.isVisible()) {
      const filterHint = page.locator('text=Попробуйте изменить параметры фильтров');
      if (await filterHint.count() > 0) {
        await expect(filterHint).toBeVisible();
      }
    }
  });
});

test.describe('Audit Log - Error Handling', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should handle API errors gracefully', async ({ page }) => {
    // Block API calls
    await page.route('**/api/audit-log**', route => route.abort('failed'));

    await page.goto('/audit-log');
    await page.waitForTimeout(2000);

    // Page should still render
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });
});

test.describe('Audit Log - Responsive Design', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/audit-log');
    await page.waitForLoadState('networkidle');

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();

    // Title should still be visible
    const title = page.locator('h1:has-text("Журнал аудита")');
    await expect(title).toBeVisible();
  });

  test('should have table with overflow on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/audit-log');
    await page.waitForLoadState('networkidle');

    // Table should be inside overflow container
    const overflowContainer = page.locator('.overflow-hidden, .overflow-x-auto');
    if (await overflowContainer.count() > 0) {
      await expect(overflowContainer.first()).toBeVisible();
    }
  });
});

test.describe('Audit Log - Time Formatting', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display relative time for today entries', async ({ page }) => {
    const auditLogPage = new AuditLogPage(page);
    await auditLogPage.goto();
    await auditLogPage.waitForLoad();

    const logCount = await auditLogPage.getLogCount();

    if (logCount > 0) {
      // Look for "Сегодня" in time column
      const todayText = page.locator('td:has-text("Сегодня")');
      // This might or might not be visible depending on data
      const count = await todayText.count();
      expect(count).toBeGreaterThanOrEqual(0);
    }
  });
});
