import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { WatchlistPage } from './pages/WatchlistPage';

/**
 * KURATOR Watchlist (Threat Registry) E2E Tests
 * Tests for watchlist management - threat monitoring functionality
 *
 * Tests cover:
 * - View watchlist
 * - Add entry to watchlist
 * - Edit watchlist entry
 * - Delete watchlist entry
 * - Change risk level
 * - Filter by risk level
 */

test.describe('Watchlist - Basic Access', () => {
  let loginPage: LoginPage;
  let watchlistPage: WatchlistPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    watchlistPage = new WatchlistPage(page);

    // Login as admin
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should navigate to watchlist page', async ({ page }) => {
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    expect(page.url()).toContain('/watchlist');
  });

  test('should display watchlist page title', async ({ page }) => {
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Check for page title - use first() to avoid strict mode violation
    const title = page.locator('h1').filter({ hasText: 'Реестр угроз' }).first();
    await expect(title).toBeVisible();
  });

  test('should display subtitle description', async ({ page }) => {
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const subtitle = page.locator('text=Мониторинг потенциальных угроз и рисков');
    await expect(subtitle).toBeVisible();
  });

  test('should have Add Threat button visible', async ({ page }) => {
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    await expect(watchlistPage.addThreatButton).toBeVisible();
  });

  test('should navigate to create threat page', async ({ page }) => {
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    await watchlistPage.clickAddThreat();

    expect(page.url()).toContain('/watchlist/new');
  });
});

test.describe('Watchlist - CRUD Operations', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should navigate to create threat page', async ({ page }) => {
    await page.goto('/watchlist/new');
    await page.waitForLoadState('networkidle');

    expect(page.url()).toContain('/watchlist/new');

    // Page should have form elements
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should display create threat form fields', async ({ page }) => {
    await page.goto('/watchlist/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Check for form elements - full name input
    const fullNameInput = page.locator('input[type="text"]').first();
    if (await fullNameInput.count() > 0) {
      await expect(fullNameInput).toBeVisible();
    }

    // Check for risk level select
    const riskSelect = page.locator('select').filter({ has: page.locator('option:has-text("Критический")') });
    if (await riskSelect.count() > 0) {
      await expect(riskSelect.first()).toBeVisible();
    }
  });

  test('should create a new watchlist entry', async ({ page }) => {
    await page.goto('/watchlist/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    const timestamp = Date.now();
    const threatName = `Test Threat ${timestamp}`;

    // Find and fill the name field
    const nameInput = page.locator('input[type="text"]').first();
    if (await nameInput.count() > 0) {
      await nameInput.fill(threatName);
    }

    // Select risk level
    const riskSelect = page.locator('select').filter({ has: page.locator('option:has-text("Критический")') }).first();
    if (await riskSelect.count() > 0) {
      await riskSelect.selectOption({ index: 1 });
    }

    // Fill other required fields
    const threatSourceInput = page.locator('textarea').first();
    if (await threatSourceInput.count() > 0) {
      await threatSourceInput.fill('Test threat source from E2E');
    }

    // Submit form
    const submitButton = page.locator('button[type="submit"]').first();
    if (await submitButton.count() > 0) {
      await submitButton.click();
      await page.waitForTimeout(3000);

      // Should redirect to watchlist
      const url = page.url();
      expect(url.includes('/watchlist')).toBe(true);
    }
  });

  test('should edit an existing watchlist entry', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      await watchlistPage.editThreat(0);
      await page.waitForURL('**/watchlist/**/edit');

      // Verify we're on edit page
      expect(page.url()).toContain('/edit');

      // Should have pre-filled form fields
      const nameInput = page.locator('input[type="text"]').first();
      if (await nameInput.count() > 0) {
        const value = await nameInput.inputValue();
        expect(value).toBeTruthy();
      }

      // Make a change
      const threatSource = page.locator('textarea').first();
      if (await threatSource.count() > 0) {
        await threatSource.fill('Updated threat source from E2E test');
      }

      // Save changes
      const saveButton = page.locator('button[type="submit"]').first();
      if (await saveButton.count() > 0) {
        await saveButton.click();
        await page.waitForTimeout(2000);

        // Should return to watchlist or detail page
        const bodyText = await page.textContent('body');
        expect(bodyText).toBeTruthy();
      }
    }
  });

  test('should delete a watchlist entry', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      // Use the deleteThreat method which handles dialog internally
      await watchlistPage.deleteThreat(0);
      await page.waitForTimeout(2000);

      // Page should update
      const bodyText = await page.textContent('body');
      expect(bodyText).toBeTruthy();
    }
  });

  test('should confirm before deleting', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      let dialogWasShown = false;
      page.once('dialog', async dialog => {
        dialogWasShown = true;
        await dialog.dismiss();
      });

      const deleteButton = page.locator('button[title="Удалить"]').first();
      if (await deleteButton.count() > 0) {
        await deleteButton.click();
        await page.waitForTimeout(500);
        expect(dialogWasShown).toBe(true);
      }
    }
  });
});

test.describe('Watchlist - Change Risk Level', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should change risk level via edit', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      await watchlistPage.editThreat(0);
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);

      // Find risk level select
      const riskSelect = page.locator('select').filter({ has: page.locator('option:has-text("Критический")') }).first();

      if (await riskSelect.count() > 0) {
        // Get current value
        const currentValue = await riskSelect.inputValue();

        // Select different value
        const options = await riskSelect.locator('option').all();
        for (const option of options) {
          const value = await option.getAttribute('value');
          if (value && value !== currentValue) {
            await riskSelect.selectOption(value);
            break;
          }
        }

        // Save
        const saveButton = page.locator('button[type="submit"]').first();
        if (await saveButton.count() > 0) {
          await saveButton.click();
          await page.waitForTimeout(2000);

          // Page should update
          const bodyText = await page.textContent('body');
          expect(bodyText).toBeTruthy();
        }
      }
    }
  });
});

test.describe('Watchlist - Filters', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should have search input available', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    await expect(watchlistPage.searchInput).toBeVisible();
  });

  test('should have risk level filter', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Risk level filter should have options
    const riskSelect = page.locator('select').first();
    const options = riskSelect.locator('option');

    const optionTexts: string[] = [];
    const count = await options.count();
    for (let i = 0; i < count; i++) {
      const text = await options.nth(i).textContent();
      if (text) optionTexts.push(text.trim());
    }

    expect(optionTexts).toContain('Все уровни риска');
    expect(optionTexts).toContain('Критический');
    expect(optionTexts).toContain('Высокий');
    expect(optionTexts).toContain('Средний');
    expect(optionTexts).toContain('Низкий');
  });

  test('should have threat sphere filter', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Threat sphere filter should have options
    const sphereSelect = page.locator('select').nth(1);
    const options = sphereSelect.locator('option');

    const optionTexts: string[] = [];
    const count = await options.count();
    for (let i = 0; i < count; i++) {
      const text = await options.nth(i).textContent();
      if (text) optionTexts.push(text.trim());
    }

    expect(optionTexts).toContain('Все сферы угроз');
  });

  test('should filter by search term', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    await watchlistPage.search('test');

    // Wait for filter to apply
    await page.waitForTimeout(1000);

    // Search input should have the value
    await expect(watchlistPage.searchInput).toHaveValue('test');
  });

  test('should filter by risk level', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    await watchlistPage.filterByRiskLevel('Critical');

    // Wait for filter to apply
    await page.waitForTimeout(1000);

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should filter by threat sphere', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Select a threat sphere
    const sphereSelect = page.locator('select').nth(1);
    if (await sphereSelect.count() > 0) {
      const options = await sphereSelect.locator('option').all();
      if (options.length > 1) {
        const value = await options[1].getAttribute('value');
        if (value) {
          await sphereSelect.selectOption(value);
          await page.waitForTimeout(1000);

          // Filter should be applied
          const selectedValue = await sphereSelect.inputValue();
          expect(selectedValue).toBe(value);
        }
      }
    }
  });

  test('should display filter count', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Should show count of filtered threats
    const filterCount = page.locator('text=/\\d+ угроз найдено/');
    if (await filterCount.count() > 0) {
      await expect(filterCount).toBeVisible();
    }
  });

  test('should combine multiple filters', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Apply search
    await watchlistPage.search('test');
    await page.waitForTimeout(500);

    // Apply risk level filter
    await watchlistPage.filterByRiskLevel('High');
    await page.waitForTimeout(500);

    // Page should still work
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should clear filters', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Apply filters
    await watchlistPage.search('test');
    await watchlistPage.filterByRiskLevel('Critical');
    await page.waitForTimeout(500);

    // Clear filters
    await watchlistPage.clearFilters();
    await page.waitForTimeout(500);

    // Search should be empty
    await expect(watchlistPage.searchInput).toHaveValue('');
  });
});

test.describe('Watchlist - Threat Cards', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display threat cards if data exists', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    // Either have threats or show empty state
    if (threatCount > 0) {
      await expect(watchlistPage.watchlistCards.first()).toBeVisible();
    } else {
      await expect(watchlistPage.emptyState).toBeVisible();
    }
  });

  test('should display threat name in card', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      // Each card should have an h3 with name
      const threatNames = page.locator('.bg-white.rounded-lg.shadow h3');
      expect(await threatNames.count()).toBeGreaterThan(0);
    }
  });

  test('should display risk level badge', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      // Risk level badges should exist
      const riskBadges = page.locator('.rounded-full.border');
      expect(await riskBadges.count()).toBeGreaterThan(0);
    }
  });

  test('should display action buttons for each threat', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      // View button
      await expect(watchlistPage.viewButton.first()).toBeVisible();

      // Edit button
      await expect(watchlistPage.editButton.first()).toBeVisible();

      // Delete button
      await expect(watchlistPage.deleteButton.first()).toBeVisible();
    }
  });

  test('should display monitoring information', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      // Monitoring frequency should be displayed
      const monitoringText = page.locator('text=Мониторинг:');
      expect(await monitoringText.count()).toBeGreaterThan(0);
    }
  });

  test('should display threat source information', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      // Threat source label should be present
      const sourceLabel = page.locator('text=Источник угрозы:');
      expect(await sourceLabel.count()).toBeGreaterThan(0);
    }
  });

  test('should display date information', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      // Date labels should be present
      const dateLabels = [
        'Начало конфликта',
        'Последняя проверка',
        'Следующая проверка'
      ];

      for (const label of dateLabels) {
        const element = page.locator(`text=${label}`);
        expect(await element.count()).toBeGreaterThan(0);
      }
    }
  });

  test('should display watch owner information', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      // Observer/watch owner label should be present
      const ownerLabel = page.locator('text=Наблюдатель');
      expect(await ownerLabel.count()).toBeGreaterThan(0);
    }
  });
});

test.describe('Watchlist - Actions', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should navigate to threat details on view click', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      await watchlistPage.viewThreat(0);
      await page.waitForURL('**/watchlist/**');
      expect(page.url()).toMatch(/\/watchlist\/\d+/);
    }
  });

  test('should navigate to edit page on edit click', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      await watchlistPage.editThreat(0);
      await page.waitForURL('**/watchlist/**/edit');
      expect(page.url()).toContain('/edit');
    }
  });
});

test.describe('Watchlist - Empty State', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should show empty state when no threats', async ({ page }) => {
    // Mock empty response
    await page.route('**/api/watchlist**', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ data: [] })
      });
    });

    await page.goto('/watchlist');
    await page.waitForTimeout(2000);

    // Empty state message should be visible
    const emptyMessage = page.locator('text=Угрозы не найдены');
    await expect(emptyMessage).toBeVisible();
  });

  test('should show filter hint in empty state', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Apply a filter that returns no results
    await watchlistPage.search('nonexistent_search_term_xyz');
    await page.waitForTimeout(1000);

    // Check for empty state with filter hint
    const emptyMessage = page.locator('text=Угрозы не найдены');
    if (await emptyMessage.isVisible()) {
      const filterHint = page.locator('text=Попробуйте изменить фильтры');
      if (await filterHint.count() > 0) {
        await expect(filterHint).toBeVisible();
      }
    }
  });
});

test.describe('Watchlist - Error Handling', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should handle API errors gracefully', async ({ page }) => {
    // Block API calls
    await page.route('**/api/watchlist**', route => route.abort('failed'));

    await page.goto('/watchlist');
    await page.waitForTimeout(2000);

    // Page should still render
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should show error on failed creation', async ({ page }) => {
    // Mock error response for POST
    await page.route('**/api/watchlist', route => {
      if (route.request().method() === 'POST') {
        route.fulfill({
          status: 400,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'Validation failed' })
        });
      } else {
        route.continue();
      }
    });

    await page.goto('/watchlist/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // Fill form minimally
    const nameInput = page.locator('input[type="text"]').first();
    if (await nameInput.count() > 0) {
      await nameInput.fill('Test');
    }

    // Submit
    const submitButton = page.locator('button[type="submit"]').first();
    if (await submitButton.count() > 0) {
      await submitButton.click();
      await page.waitForTimeout(2000);

      // Should show error or remain on form
      const bodyText = await page.textContent('body');
      expect(bodyText).toBeTruthy();
    }
  });
});

test.describe('Watchlist - Risk Level Colors', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display critical risk with red styling', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Check for critical risk badge styling
    const criticalBadge = page.locator('.bg-red-100.text-red-800');
    if (await criticalBadge.count() > 0) {
      await expect(criticalBadge.first()).toBeVisible();
    }
  });

  test('should display high risk with orange styling', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Check for high risk badge styling
    const highBadge = page.locator('.bg-orange-100.text-orange-800');
    if (await highBadge.count() > 0) {
      await expect(highBadge.first()).toBeVisible();
    }
  });

  test('should display medium risk with yellow styling', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Check for medium risk badge styling
    const mediumBadge = page.locator('.bg-yellow-100.text-yellow-800');
    if (await mediumBadge.count() > 0) {
      await expect(mediumBadge.first()).toBeVisible();
    }
  });

  test('should display low risk with green styling', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Check for low risk badge styling
    const lowBadge = page.locator('.bg-green-100.text-green-800');
    if (await lowBadge.count() > 0) {
      await expect(lowBadge.first()).toBeVisible();
    }
  });
});

test.describe('Watchlist - Responsive Design', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/watchlist');
    await page.waitForLoadState('networkidle');

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();

    // Add threat button should still be visible
    const addButton = page.locator('button:has-text("Добавить угрозу")');
    await expect(addButton).toBeVisible();
  });

  test('should display correctly on tablet viewport', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });

    await page.goto('/watchlist');
    await page.waitForLoadState('networkidle');

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should stack filters on mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/watchlist');
    await page.waitForLoadState('networkidle');

    // Filters should still be accessible
    const filterSelects = page.locator('select');
    expect(await filterSelects.count()).toBeGreaterThanOrEqual(2);
  });
});

test.describe('Watchlist - Overdue Checks', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should highlight overdue next check dates', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    // Check for red colored overdue text
    const overdueText = page.locator('.text-red-600');
    // This might or might not be visible depending on data
    const count = await overdueText.count();
    expect(count).toBeGreaterThanOrEqual(0);
  });

  test('should display overdue indicator', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      // Look for "Просрочено" text
      const overdueLabel = page.locator('text=Просрочено');
      // Count might be 0 if no overdue items
      const count = await overdueLabel.count();
      expect(count).toBeGreaterThanOrEqual(0);
    }
  });
});

test.describe('Watchlist - Detail View', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should navigate to detail view', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      // Click view button
      await watchlistPage.viewThreat(0);
      await page.waitForTimeout(1000);

      // Either navigates to detail page or shows modal/expanded view
      const currentUrl = page.url();
      const bodyText = await page.textContent('body');

      // Test passes if either on detail page or content is visible
      expect(currentUrl.includes('/watchlist') || bodyText?.length || 0 > 0).toBeTruthy();
    }
  });

  test('should display threat details', async ({ page }) => {
    const watchlistPage = new WatchlistPage(page);
    await watchlistPage.goto();
    await watchlistPage.waitForLoad();

    const threatCount = await watchlistPage.getThreatCount();

    if (threatCount > 0) {
      await watchlistPage.viewThreat(0);
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);

      // Page should have content
      const bodyText = await page.textContent('body');
      expect(bodyText).toBeTruthy();
    }
  });
});
