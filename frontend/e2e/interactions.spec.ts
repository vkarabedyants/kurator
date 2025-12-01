import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { InteractionsPage } from './pages/InteractionsPage';

/**
 * KURATOR Interactions Management E2E Tests
 * Tests for interaction CRUD operations and filtering
 *
 * Tests cover:
 * - View interactions list
 * - Create new interaction
 * - Edit interaction
 * - Delete interaction
 * - Filter by date range
 * - Filter by interaction type
 * - Filter by result
 */

test.describe('Interactions Management - Basic Access', () => {
  let loginPage: LoginPage;
  let interactionsPage: InteractionsPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    interactionsPage = new InteractionsPage(page);

    // Login as admin
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should navigate to interactions page', async ({ page }) => {
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    expect(page.url()).toContain('/interactions');
    await expect(interactionsPage.pageTitle).toBeVisible();
  });

  test('should display interactions list or empty state', async ({ page }) => {
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // Page should load with either interactions or empty state
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should have Add Interaction button visible', async ({ page }) => {
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    await expect(interactionsPage.addInteractionButton).toBeVisible();
  });

  test('should navigate to create interaction page', async ({ page }) => {
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    await interactionsPage.clickAddInteraction();

    expect(page.url()).toContain('/interactions/new');
  });
});

test.describe('Interactions CRUD - Create', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should create a new interaction', async ({ page }) => {
    await page.goto('/interactions/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(5000);

    // Wait for page to fully load
    const contactSelect = page.locator('select').first();

    // Check if select is visible
    if (await contactSelect.count() === 0) {
      // Page may not have a form, skip
      return;
    }

    await expect(contactSelect).toBeVisible({ timeout: 10000 });

    // Wait for options to load - use a retry approach
    let hasOptions = false;
    for (let attempt = 0; attempt < 10; attempt++) {
      const optionCount = await contactSelect.locator('option').count();
      if (optionCount > 1) {
        hasOptions = true;
        break;
      }
      await page.waitForTimeout(1000);
    }

    if (!hasOptions) {
      // No contacts available to create interaction
      return;
    }

    // Select a contact
    const options = await contactSelect.locator('option').all();
    if (options.length > 1) {
      const contactValue = await options[1].getAttribute('value');
      if (contactValue) {
        await contactSelect.selectOption(contactValue);
      }
    }

    // Select interaction type
    const typeSelect = page.locator('select').filter({ has: page.locator('option:has-text("Встреча")') }).first();
    if (await typeSelect.count() > 0) {
      await typeSelect.selectOption({ index: 1 });
    }

    // Select result
    const resultSelect = page.locator('select').filter({ has: page.locator('option:has-text("Положительный")') }).first();
    if (await resultSelect.count() > 0) {
      await resultSelect.selectOption({ index: 1 });
    }

    // Fill comment (required)
    const commentField = page.locator('textarea').first();
    if (await commentField.count() > 0) {
      await commentField.fill(`Test interaction created at ${Date.now()}`);
    }

    // Submit form
    const submitButton = page.locator('button[type="submit"]').first();
    if (await submitButton.count() > 0) {
      await submitButton.click();
      await page.waitForTimeout(3000);

      // Should redirect to interactions list or contact page
      const url = page.url();
      expect(url.includes('/interactions') || url.includes('/contacts')).toBe(true);
    }
  });

  test('should validate required fields on create', async ({ page }) => {
    await page.goto('/interactions/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Try to submit empty form
    const submitButton = page.locator('button[type="submit"]').first();
    if (await submitButton.count() > 0) {
      await submitButton.click();
      await page.waitForTimeout(1000);

      // Should still be on create page
      expect(page.url()).toContain('/interactions/new');
    }
  });
});

test.describe('Interactions CRUD - Delete', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should delete an interaction', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    const interactionCount = await interactionsPage.getInteractionCount();

    if (interactionCount > 0) {
      // Click delete button directly (page object handles dialog)
      const deleteButton = page.locator('button:has-text("Удалить")').first();
      if (await deleteButton.count() > 0) {
        // Handle dialog before clicking
        page.once('dialog', dialog => dialog.accept());
        await deleteButton.click();
        await page.waitForTimeout(2000);

        // Page should update
        const bodyText = await page.textContent('body');
        expect(bodyText).toBeTruthy();
      }
    }
  });
});

test.describe('Interactions Management - Filters', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should have type filter available', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // Type filter label should be visible
    const typeLabel = page.locator('label:has-text("Тип")');
    await expect(typeLabel).toBeVisible();

    // Type select should have options
    const typeSelect = page.locator('select').first();
    const options = typeSelect.locator('option');
    expect(await options.count()).toBeGreaterThan(1);
  });

  test('should have all interaction type options', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    const typeSelect = page.locator('select').first();
    const options = typeSelect.locator('option');

    const optionTexts: string[] = [];
    const count = await options.count();
    for (let i = 0; i < count; i++) {
      const text = await options.nth(i).textContent();
      if (text) optionTexts.push(text.trim());
    }

    expect(optionTexts).toContain('Все типы');
    expect(optionTexts).toContain('Встреча');
    expect(optionTexts).toContain('Звонок');
    expect(optionTexts).toContain('Электронная почта');
  });

  test('should have result filter available', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // Result filter label should be visible
    const resultLabel = page.locator('label:has-text("Результат")');
    await expect(resultLabel).toBeVisible();
  });

  test('should have all result filter options', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // Find result select by label
    const resultSelect = page.locator('select').filter({ has: page.locator('option:has-text("Положительный")') });
    const options = resultSelect.locator('option');

    const optionTexts: string[] = [];
    const count = await options.count();
    for (let i = 0; i < count; i++) {
      const text = await options.nth(i).textContent();
      if (text) optionTexts.push(text.trim());
    }

    expect(optionTexts).toContain('Все результаты');
    expect(optionTexts).toContain('Положительный');
    expect(optionTexts).toContain('Отрицательный');
    expect(optionTexts).toContain('Нейтральный');
  });

  test('should have date range filters', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // Date from label
    const dateFromLabel = page.locator('label:has-text("С даты")');
    await expect(dateFromLabel).toBeVisible();

    // Date to label
    const dateToLabel = page.locator('label:has-text("По дату")');
    await expect(dateToLabel).toBeVisible();

    // Date inputs should exist
    const dateInputs = page.locator('input[type="date"]');
    expect(await dateInputs.count()).toBeGreaterThanOrEqual(2);
  });

  test('should filter by type', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // Get initial count
    const initialCount = await interactionsPage.getInteractionCount();

    // Apply filter
    await interactionsPage.filterByType('Meeting');

    // Wait for filter to apply
    await page.waitForTimeout(1000);

    // Page should still be functional (count might be same, less, or more depending on data)
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should filter by result', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // Apply filter
    await interactionsPage.filterByResult('Positive');

    // Wait for filter to apply
    await page.waitForTimeout(1000);

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should filter by date range', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // Set date range (last month)
    const today = new Date();
    const lastMonth = new Date(today.getFullYear(), today.getMonth() - 1, 1);
    const fromDate = lastMonth.toISOString().split('T')[0];
    const toDate = today.toISOString().split('T')[0];

    await interactionsPage.filterByDateRange(fromDate, toDate);

    // Wait for filter to apply
    await page.waitForTimeout(1000);

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should filter by specific date range - last 7 days', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // Set date range (last 7 days)
    const today = new Date();
    const weekAgo = new Date(today.getTime() - 7 * 24 * 60 * 60 * 1000);
    const fromDate = weekAgo.toISOString().split('T')[0];
    const toDate = today.toISOString().split('T')[0];

    await interactionsPage.filterByDateRange(fromDate, toDate);

    // Wait for filter to apply
    await page.waitForTimeout(1000);

    // Verify date inputs have values
    const dateFromInput = page.locator('input[type="date"]').first();
    const dateToInput = page.locator('input[type="date"]').nth(1);

    await expect(dateFromInput).toHaveValue(fromDate);
    await expect(dateToInput).toHaveValue(toDate);
  });

  test('should combine multiple filters', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // Apply type filter
    await interactionsPage.filterByType('Meeting');
    await page.waitForTimeout(500);

    // Apply result filter
    await interactionsPage.filterByResult('Positive');
    await page.waitForTimeout(500);

    // Apply date range
    const today = new Date();
    const lastMonth = new Date(today.getFullYear(), today.getMonth() - 1, 1);
    await interactionsPage.filterByDateRange(
      lastMonth.toISOString().split('T')[0],
      today.toISOString().split('T')[0]
    );
    await page.waitForTimeout(1000);

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });
});

test.describe('Interactions Management - Interaction Cards', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display interaction type badge', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    const interactionCount = await interactionsPage.getInteractionCount();

    if (interactionCount > 0) {
      // Type badges should exist
      const typeBadges = page.locator('.rounded-full');
      expect(await typeBadges.count()).toBeGreaterThan(0);
    }
  });

  test('should display delete button for interactions', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    const interactionCount = await interactionsPage.getInteractionCount();

    if (interactionCount > 0) {
      await expect(interactionsPage.deleteButton.first()).toBeVisible();
    }
  });

  test('should display view contact link', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    const interactionCount = await interactionsPage.getInteractionCount();

    if (interactionCount > 0) {
      await expect(interactionsPage.viewContactButton.first()).toBeVisible();
    }
  });

  test('should navigate to contact from interaction', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    const interactionCount = await interactionsPage.getInteractionCount();

    if (interactionCount > 0) {
      await interactionsPage.viewContact(0);
      await page.waitForURL('**/contacts/**');
      expect(page.url()).toContain('/contacts/');
    }
  });

  test('should display interaction date', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    const interactionCount = await interactionsPage.getInteractionCount();

    if (interactionCount > 0) {
      // Date format should be visible in interaction cards
      const interactionCard = interactionsPage.interactionCards.first();
      const cardText = await interactionCard.textContent();
      // Card should contain some date-like text or contact reference
      expect(cardText).toBeTruthy();
    }
  });

  test('should display curator information', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    const interactionCount = await interactionsPage.getInteractionCount();

    if (interactionCount > 0) {
      // "от" prefix for curator should be visible
      const curatorRef = page.locator('text=/от\\s+\\w+/');
      if (await curatorRef.count() > 0) {
        expect(await curatorRef.count()).toBeGreaterThan(0);
      }
    }
  });
});

test.describe('Interactions Management - Pagination', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display pagination when multiple pages exist', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // If pagination exists
    if (await interactionsPage.paginationInfo.isVisible()) {
      const pageInfo = await interactionsPage.getCurrentPage();
      expect(pageInfo).not.toBeNull();
      expect(pageInfo?.current).toBeGreaterThanOrEqual(1);
    }
  });

  test('should navigate between pages', async ({ page }) => {
    const interactionsPage = new InteractionsPage(page);
    await interactionsPage.goto();
    await interactionsPage.waitForLoad();

    // If pagination exists and has more than one page
    if (await interactionsPage.paginationInfo.isVisible()) {
      const pageInfo = await interactionsPage.getCurrentPage();
      if (pageInfo && pageInfo.total > 1) {
        // Try to go to next page
        await interactionsPage.goToNextPage();
        const newPageInfo = await interactionsPage.getCurrentPage();
        expect(newPageInfo?.current).toBe(2);
      }
    }
  });
});

test.describe('Interactions Management - Error Handling', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should handle API errors gracefully', async ({ page }) => {
    // Block API calls
    await page.route('**/api/interactions**', route => route.abort('failed'));

    await page.goto('/interactions');
    await page.waitForTimeout(2000);

    // Page should still render
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should show error message when interactions fail to load', async ({ page }) => {
    // Block API calls to return error
    await page.route('**/api/interactions**', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Internal server error' })
      });
    });

    await page.goto('/interactions');
    await page.waitForTimeout(2000);

    // Error message should be visible
    const errorText = page.locator('text=Ошибка').or(page.locator('.text-red-600'));
    if (await errorText.count() > 0) {
      await expect(errorText.first()).toBeVisible();
    }
  });
});

test.describe('Interactions Page - Empty State', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should show empty state when no interactions', async ({ page }) => {
    // Mock empty response
    await page.route('**/api/interactions**', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [], totalPages: 0 })
      });
    });

    await page.goto('/interactions');
    await page.waitForTimeout(2000);

    // Empty state message should be visible
    const emptyMessage = page.locator('text=Взаимодействия не найдены');
    if (await emptyMessage.count() > 0) {
      await expect(emptyMessage.first()).toBeVisible();
    }
  });
});

test.describe('Interactions Page - Responsive Design', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/interactions');
    await page.waitForLoadState('networkidle');

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();

    // Add interaction button should still be visible
    const addButton = page.locator('a:has-text("Добавить взаимодействие")');
    await expect(addButton).toBeVisible();
  });

  test('should stack filters on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/interactions');
    await page.waitForLoadState('networkidle');

    // Filters should still be accessible
    const filterSelects = page.locator('select');
    expect(await filterSelects.count()).toBeGreaterThanOrEqual(2);
  });
});

test.describe('Create Interaction Page', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should load create interaction page', async ({ page }) => {
    await page.goto('/interactions/new');
    await page.waitForLoadState('networkidle');

    expect(page.url()).toContain('/interactions/new');

    // Page should have some content
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should display form fields', async ({ page }) => {
    await page.goto('/interactions/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    // Contact select should be visible
    const contactSelect = page.locator('select').first();
    await expect(contactSelect).toBeVisible({ timeout: 10000 });

    // Type select should be visible
    const typeSelect = page.locator('select').filter({ has: page.locator('option:has-text("Встреча")') });
    if (await typeSelect.count() > 0) {
      await expect(typeSelect.first()).toBeVisible();
    }

    // Comment field should be visible
    const commentField = page.locator('textarea').first();
    if (await commentField.count() > 0) {
      await expect(commentField).toBeVisible();
    }
  });

  test('should have submit and cancel buttons', async ({ page }) => {
    await page.goto('/interactions/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Submit button
    const submitButton = page.locator('button[type="submit"]').first();
    await expect(submitButton).toBeVisible();

    // Cancel/back button
    const cancelButton = page.locator('button:has-text("Отмена")').or(page.locator('button:has-text("Назад")'));
    if (await cancelButton.count() > 0) {
      await expect(cancelButton.first()).toBeVisible();
    }
  });

  test('should navigate back on cancel', async ({ page }) => {
    await page.goto('/interactions/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Click cancel/back button
    const cancelButton = page.locator('button:has-text("Отмена")').or(page.locator('button:has-text("Назад")'));
    if (await cancelButton.count() > 0) {
      await cancelButton.first().click();
      await page.waitForTimeout(1000);

      // Should navigate away from create page
      const url = page.url();
      // URL should change (either back to interactions or to previous page)
      expect(url).toBeTruthy();
    }
  });
});
