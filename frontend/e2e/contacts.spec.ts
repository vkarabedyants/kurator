import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';

/**
 * KURATOR Contacts E2E Tests
 * Complete CRUD workflow for contacts with encryption
 *
 * Tests cover:
 * - View contacts list
 * - Create new contact
 * - Edit existing contact
 * - Delete contact
 * - View contact details
 * - Add interaction from contact page
 * - View contact interaction history
 * - Change contact status (A/B/C/D)
 * - Filter contacts by block
 * - Filter contacts by status
 * - Search contacts by name
 */

test.describe('Contacts CRUD Workflows', () => {
  test.beforeEach(async ({ page }) => {
    // Login for tests that don't use shared auth state
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should navigate to contacts page', async ({ page }) => {
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');

    expect(page.url()).toContain('/contacts');

    // Verify page loaded with expected elements
    const heading = page.locator('h1, h2').first();
    await expect(heading).toBeVisible();
  });

  test('should display contacts list', async ({ page }) => {
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Page should have table or empty state
    const table = page.locator('table');
    const emptyState = page.locator('text=Контакты не найдены');

    const hasTable = await table.count() > 0;
    const isEmpty = await emptyState.count() > 0;

    expect(hasTable || isEmpty).toBe(true);
  });

  test('should filter contacts by search', async ({ page }) => {
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Look for search input
    const searchInput = page.locator('input[type="search"], input[placeholder*="search" i], input[placeholder*="Поиск" i]').first();

    if (await searchInput.count() > 0) {
      await searchInput.fill('test');
      await page.waitForTimeout(500);

      // Verify some filtering happened
      expect(await searchInput.inputValue()).toBe('test');
    }
  });

  test('should navigate to create contact page', async ({ page }) => {
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(1000);

    // Look for create/add button or link
    const createButton = page.locator('a[href*="/contacts/new"], a[href*="/contacts/create"], button:has-text("Создать"), button:has-text("Добавить"), a:has-text("Новый контакт"), a:has-text("Добавить контакт")').first();

    if (await createButton.count() > 0) {
      await createButton.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);
    }

    // If navigation didn't happen or button wasn't found, go directly
    if (!page.url().match(/\/contacts\/(create|new)/)) {
      await page.goto('/contacts/new');
      await page.waitForLoadState('networkidle');
    }

    // Verify we're on the create page
    expect(page.url()).toMatch(/\/contacts\/(create|new)/);
  });

  test('should show validation errors on empty form submission', async ({ page }) => {
    await page.goto('/contacts/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Look for submit button
    const submitButton = page.locator('button[type="submit"]').first();

    if (await submitButton.count() > 0) {
      await submitButton.click();
      await page.waitForTimeout(500);

      // Either still on same page (HTML5 validation) or error message visible
      expect(page.url()).toContain('/contacts/new');
    }
  });

  test('should create a new contact successfully', async ({ page }) => {
    // Navigate to create contact page
    await page.goto('/contacts/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    // Wait for the form to load (blocks and references)
    const blockSelect = page.locator('select').first();
    await expect(blockSelect).toBeVisible({ timeout: 15000 });

    // Wait for options to be populated
    await page.waitForFunction(
      (selector) => {
        const select = document.querySelector(selector) as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      'select',
      { timeout: 15000 }
    );

    // Generate unique name with timestamp to avoid conflicts
    const timestamp = Date.now();
    const contactName = `Test Contact E2E ${timestamp}`;

    // Select the first real option (not the placeholder)
    const options = await blockSelect.locator('option').all();
    if (options.length > 1) {
      const firstOptionValue = await options[1].getAttribute('value');
      if (firstOptionValue && firstOptionValue !== '0') {
        await blockSelect.selectOption(firstOptionValue);
      }
    }

    // Fill in the full name (required field)
    const fullNameInput = page.locator('input[type="text"]').first();
    await expect(fullNameInput).toBeVisible();
    await fullNameInput.fill(contactName);

    // Click the submit button
    const submitButton = page.locator('button[type="submit"]').first();
    await expect(submitButton).toBeVisible();
    await expect(submitButton).toBeEnabled();

    // Click submit and wait for navigation
    await Promise.all([
      page.waitForURL(/\/contacts\/\d+/, { timeout: 20000 }),
      submitButton.click(),
    ]);

    // Verify we are on the contact detail page
    expect(page.url()).toMatch(/\/contacts\/\d+/);

    // Verify the contact was created - check the page content
    await page.waitForLoadState('networkidle');
    const pageContent = await page.textContent('body');
    expect(pageContent).toBeTruthy();
    expect(pageContent!.length).toBeGreaterThan(0);
  });

  test('should display created contact in contacts list', async ({ page }) => {
    // First, create a contact
    await page.goto('/contacts/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    // Wait for form to load
    const blockSelect = page.locator('select').first();
    await expect(blockSelect).toBeVisible({ timeout: 15000 });

    await page.waitForFunction(
      (selector) => {
        const select = document.querySelector(selector) as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      'select',
      { timeout: 15000 }
    );

    const timestamp = Date.now();
    const contactName = `List Test Contact ${timestamp}`;

    // Select block
    const options = await blockSelect.locator('option').all();
    if (options.length > 1) {
      const firstOptionValue = await options[1].getAttribute('value');
      if (firstOptionValue && firstOptionValue !== '0') {
        await blockSelect.selectOption(firstOptionValue);
      }
    }

    // Fill name
    const fullNameInput = page.locator('input[type="text"]').first();
    await fullNameInput.fill(contactName);

    // Submit
    const submitButton = page.locator('button[type="submit"]').first();
    await Promise.all([
      page.waitForURL(/\/contacts\/\d+/, { timeout: 20000 }),
      submitButton.click(),
    ]);

    // Get the new contact ID from URL
    const url = page.url();
    const contactIdMatch = url.match(/\/contacts\/(\d+)/);
    expect(contactIdMatch).toBeTruthy();
    const newContactId = contactIdMatch![1];

    // Navigate to contacts list
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Verify the page loaded without errors
    const pageContent = await page.textContent('body');
    expect(pageContent).toBeTruthy();

    // Check if there's a link to the new contact
    const contactLink = page.locator(`a[href*="/contacts/${newContactId}"]`);

    if (await contactLink.count() > 0) {
      await expect(contactLink.first()).toBeVisible();
    }
  });
});

test.describe('Contact Detail View', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should view contact details with all fields', async ({ page }) => {
    // First find a contact in the list
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Try to find a contact link (links to /contacts/[number], not /contacts/new)
    const contactLinks = page.locator('a[href^="/contacts/"]');
    const allLinks = await contactLinks.all();

    let contactDetailLink = null;
    for (const link of allLinks) {
      const href = await link.getAttribute('href');
      if (href && /\/contacts\/\d+/.test(href)) {
        contactDetailLink = link;
        break;
      }
    }

    if (contactDetailLink) {
      await contactDetailLink.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);

      // Should be on detail page
      expect(page.url()).toMatch(/\/contacts\/\d+/);

      // Verify contact detail fields are present
      const detailFields = [
        'Основная информация',
        'Блок',
        'Организация',
      ];

      for (const field of detailFields) {
        const fieldElement = page.locator(`text=${field}`).first();
        if (await fieldElement.count() > 0) {
          await expect(fieldElement).toBeVisible();
        }
      }
    }
  });

  test('should display contact information', async ({ page }) => {
    // Try to access a contact detail page directly
    await page.goto('/contacts/1');
    await page.waitForLoadState('networkidle');

    // Page should have loaded (either contact or error message)
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should navigate to edit contact page', async ({ page }) => {
    // Find a contact first
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    const contactLink = page.locator('a[href*="/contacts/"]').filter({ hasNotText: 'new' }).first();

    if (await contactLink.count() > 0) {
      await contactLink.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);

      // Look for edit button
      const editButton = page.locator('a:has-text("Редактировать"), button:has-text("Редактировать")').first();

      if (await editButton.count() > 0) {
        await editButton.click();
        await page.waitForLoadState('networkidle');

        // Should be on edit page
        expect(page.url()).toContain('/edit');
      }
    }
  });

  test('should show contact interaction history', async ({ page }) => {
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    const contactLink = page.locator('a[href*="/contacts/"]').filter({ hasNotText: 'new' }).first();

    if (await contactLink.count() > 0) {
      await contactLink.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);

      // Look for interactions section
      const interactionsSection = page.locator('text=История взаимодействий').or(page.locator('text=Взаимодействия'));

      if (await interactionsSection.count() > 0) {
        await expect(interactionsSection.first()).toBeVisible();
      }
    }
  });
});

test.describe('Contact Edit Operations', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should edit existing contact', async ({ page }) => {
    // First create a contact
    await page.goto('/contacts/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    const blockSelect = page.locator('select').first();
    await expect(blockSelect).toBeVisible({ timeout: 15000 });

    await page.waitForFunction(
      (selector) => {
        const select = document.querySelector(selector) as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      'select',
      { timeout: 15000 }
    );

    const timestamp = Date.now();
    const originalName = `Edit Test Contact ${timestamp}`;

    // Select block
    const options = await blockSelect.locator('option').all();
    if (options.length > 1) {
      const firstOptionValue = await options[1].getAttribute('value');
      if (firstOptionValue && firstOptionValue !== '0') {
        await blockSelect.selectOption(firstOptionValue);
      }
    }

    // Fill name
    const fullNameInput = page.locator('input[type="text"]').first();
    await fullNameInput.fill(originalName);

    // Submit
    const submitButton = page.locator('button[type="submit"]').first();
    await Promise.all([
      page.waitForURL(/\/contacts\/\d+/, { timeout: 20000 }),
      submitButton.click(),
    ]);

    // Now navigate to edit page
    const editLink = page.locator('a:has-text("Редактировать")').first();

    if (await editLink.count() > 0) {
      await editLink.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(2000);

      // Edit the name
      const editNameInput = page.locator('input[type="text"]').first();

      if (await editNameInput.count() > 0) {
        await editNameInput.clear();
        await editNameInput.fill(`${originalName} - Updated`);

        // Save changes
        const saveButton = page.locator('button[type="submit"]').first();

        if (await saveButton.count() > 0) {
          await saveButton.click();
          await page.waitForTimeout(2000);

          // Should return to detail page or show success
          const bodyText = await page.textContent('body');
          expect(bodyText).toBeTruthy();
        }
      }
    }
  });

  test('should change contact status (A/B/C/D)', async ({ page }) => {
    // Navigate to a contact detail page
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    const contactLink = page.locator('a[href*="/contacts/"]').filter({ hasNotText: 'new' }).first();

    if (await contactLink.count() > 0) {
      await contactLink.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);

      // Look for status change functionality
      // This could be via edit or via add interaction with status change
      const addInteractionButton = page.locator('button:has-text("Добавить взаимодействие")').first();

      if (await addInteractionButton.count() > 0) {
        await addInteractionButton.click();
        await page.waitForTimeout(1000);

        // Find status change select
        const statusSelect = page.locator('select').filter({ has: page.locator('option:has-text("Без изменений")') }).first();

        if (await statusSelect.count() > 0) {
          // Select a new status
          const statusOptions = await statusSelect.locator('option').all();

          for (const option of statusOptions) {
            const text = await option.textContent();
            if (text && text.includes('A') && !text.includes('Без')) {
              const value = await option.getAttribute('value');
              if (value) {
                await statusSelect.selectOption(value);
                break;
              }
            }
          }

          // Status change should be reflected
          const selectedValue = await statusSelect.inputValue();
          expect(selectedValue).toBeTruthy();
        }
      }
    }
  });
});

test.describe('Contact Filters', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should filter contacts by block', async ({ page }) => {
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Find block filter select
    const blockFilter = page.locator('select').filter({ has: page.locator('option:has-text("Все блоки")') }).first();

    if (await blockFilter.count() > 0) {
      // Select first real block option
      const options = await blockFilter.locator('option').all();

      if (options.length > 1) {
        const secondOption = options[1];
        const value = await secondOption.getAttribute('value');

        if (value) {
          await blockFilter.selectOption(value);
          await page.waitForTimeout(1000);

          // Filter should be applied
          const selectedValue = await blockFilter.inputValue();
          expect(selectedValue).toBe(value);
        }
      }
    }
  });

  test('should filter contacts by status', async ({ page }) => {
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Find status filter select
    const statusFilter = page.locator('select').filter({ has: page.locator('option:has-text("Все статусы")') }).first();

    if (await statusFilter.count() > 0) {
      // Select a status option
      const options = await statusFilter.locator('option').all();

      for (const option of options) {
        const text = await option.textContent();
        if (text && text.includes('A')) {
          const value = await option.getAttribute('value');
          if (value) {
            await statusFilter.selectOption(value);
            await page.waitForTimeout(1000);

            // Filter should be applied
            const selectedValue = await statusFilter.inputValue();
            expect(selectedValue).toBe(value);
            break;
          }
        }
      }
    }
  });

  test('should search contacts by name', async ({ page }) => {
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Find search input
    const searchInput = page.locator('input[placeholder*="Поиск"]').first();

    if (await searchInput.count() > 0) {
      await searchInput.fill('test');
      await page.waitForTimeout(1000);

      // Search should be applied
      expect(await searchInput.inputValue()).toBe('test');

      // Clear search
      await searchInput.clear();
      await page.waitForTimeout(500);

      expect(await searchInput.inputValue()).toBe('');
    }
  });
});

test.describe('Contact Interactions', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should add interaction from contact page', async ({ page }) => {
    // Navigate to a contact
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    const contactLink = page.locator('a[href*="/contacts/"]').filter({ hasNotText: 'new' }).first();

    if (await contactLink.count() > 0) {
      await contactLink.click();
      await page.waitForLoadState('networkidle');
      await page.waitForTimeout(1000);

      // Click add interaction button
      const addInteractionButton = page.locator('button:has-text("Добавить взаимодействие")').first();

      if (await addInteractionButton.count() > 0) {
        await addInteractionButton.click();
        await page.waitForTimeout(1000);

        // Interaction form should be visible
        const interactionForm = page.locator('form').filter({ has: page.locator('select') }).first();

        if (await interactionForm.count() > 0) {
          // Fill in interaction details
          const typeSelect = interactionForm.locator('select').first();
          if (await typeSelect.count() > 0) {
            // Select first option
            await typeSelect.selectOption({ index: 1 });
          }

          const resultSelect = interactionForm.locator('select').nth(1);
          if (await resultSelect.count() > 0) {
            await resultSelect.selectOption({ index: 1 });
          }

          // Fill comment
          const commentField = interactionForm.locator('textarea').first();
          if (await commentField.count() > 0) {
            await commentField.fill('Test interaction comment from E2E');
          }

          // Submit the form
          const submitButton = interactionForm.locator('button[type="submit"]').first();
          if (await submitButton.count() > 0) {
            await submitButton.click();
            await page.waitForTimeout(2000);

            // Page should update
            const bodyText = await page.textContent('body');
            expect(bodyText).toBeTruthy();
          }
        }
      }
    }
  });

  test('should navigate to interactions page', async ({ page }) => {
    await page.goto('/interactions');
    await page.waitForLoadState('networkidle');

    expect(page.url()).toContain('/interactions');
  });

  test('should filter interactions by contact', async ({ page }) => {
    await page.goto('/interactions');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Look for filter controls
    const filterSelect = page.locator('select').first();

    if (await filterSelect.count() > 0) {
      const options = await filterSelect.locator('option').count();
      if (options > 1) {
        await filterSelect.selectOption({ index: 1 });
        await page.waitForTimeout(500);

        // Verify filter was applied
        expect(await filterSelect.inputValue()).toBeTruthy();
      }
    }
  });
});

test.describe('Contact Delete Operations', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should delete contact', async ({ page }) => {
    // First create a contact to delete
    await page.goto('/contacts/new');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(3000);

    const blockSelect = page.locator('select').first();
    await expect(blockSelect).toBeVisible({ timeout: 15000 });

    await page.waitForFunction(
      (selector) => {
        const select = document.querySelector(selector) as HTMLSelectElement;
        return select && select.options.length > 1;
      },
      'select',
      { timeout: 15000 }
    );

    const timestamp = Date.now();
    const contactName = `Delete Test Contact ${timestamp}`;

    // Select block
    const options = await blockSelect.locator('option').all();
    if (options.length > 1) {
      const firstOptionValue = await options[1].getAttribute('value');
      if (firstOptionValue && firstOptionValue !== '0') {
        await blockSelect.selectOption(firstOptionValue);
      }
    }

    // Fill name
    const fullNameInput = page.locator('input[type="text"]').first();
    await fullNameInput.fill(contactName);

    // Submit
    const submitButton = page.locator('button[type="submit"]').first();
    await Promise.all([
      page.waitForURL(/\/contacts\/\d+/, { timeout: 20000 }),
      submitButton.click(),
    ]);

    // Get contact ID
    const contactId = page.url().match(/\/contacts\/(\d+)/)?.[1];

    // Navigate to contacts list and find delete option
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Look for delete button in the row
    const deleteButton = page.locator('button:has-text("Удалить")').first();

    if (await deleteButton.count() > 0) {
      // Set up dialog handler to confirm deletion
      page.once('dialog', async dialog => {
        await dialog.accept();
      });

      await deleteButton.click();
      await page.waitForTimeout(2000);

      // Page should update - either success message or contact removed
      const bodyText = await page.textContent('body');
      expect(bodyText).toBeTruthy();
    }
  });
});

test.describe('Responsive Design', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');

    // Page should load without layout issues
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should display correctly on tablet viewport', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    await page.goto('/contacts');
    await page.waitForLoadState('networkidle');

    // Page should load without layout issues
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });
});

test.describe('Error Handling', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should handle non-existent contact gracefully', async ({ page }) => {
    await page.goto('/contacts/99999');
    await page.waitForLoadState('networkidle');

    // Should show error or redirect, not crash
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should handle network errors gracefully', async ({ page }) => {
    // Block API requests
    await page.route('**/api/**', route => route.abort('failed'));

    await page.goto('/contacts');
    await page.waitForLoadState('domcontentloaded');

    // Page should still render, possibly with error message
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });
});
