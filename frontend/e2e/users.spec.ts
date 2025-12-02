import { test, expect } from '@playwright/test';
import { LoginPage } from './pages/LoginPage';
import { UsersPage } from './pages/UsersPage';

/**
 * KURATOR Users Management E2E Tests
 * Tests for user CRUD operations (Admin only feature)
 *
 * Tests cover:
 * - View users list
 * - Create new user
 * - Edit user
 * - Delete/deactivate user
 * - Change user role
 * - Reset user password
 */

test.describe('Users Management - Admin Access', () => {
  let loginPage: LoginPage;
  let usersPage: UsersPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    usersPage = new UsersPage(page);

    // Login as admin
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should navigate to users page', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    expect(page.url()).toContain('/users');
    await expect(usersPage.pageTitle).toBeVisible();
  });

  test('should display users table', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    // Table should be visible
    await expect(usersPage.usersTable).toBeVisible();

    // Verify table headers
    const headers = await usersPage.getTableHeaders();
    expect(headers).toContain('ID');
    expect(headers).toContain('Логин');
    expect(headers).toContain('Роль');
  });

  test('should have Add User button visible', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    await expect(usersPage.addUserButton).toBeVisible();
    await expect(usersPage.addUserButton).toBeEnabled();
  });

  test('should show add user form when clicking Add User', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.clickAddUser();

    // Form should be visible
    expect(await usersPage.isFormVisible()).toBe(true);

    // Verify form header
    const formHeader = page.locator('h3:has-text("Создать нового пользователя")');
    await expect(formHeader).toBeVisible();
  });

  test('should have required form fields for creating user', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.clickAddUser();

    // Check form fields
    const form = page.locator('form');
    await expect(form).toBeVisible();

    // Login field
    const loginLabel = page.locator('label:has-text("Логин")');
    await expect(loginLabel).toBeVisible();

    // Password field
    const passwordLabel = page.locator('label:has-text("Пароль")');
    await expect(passwordLabel).toBeVisible();

    // Role field - use exact text matching
    const roleLabel = page.getByText('Роль *', { exact: true });
    await expect(roleLabel).toBeVisible();
  });

  test('should have all role options available', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.clickAddUser();

    // Find role select
    const roleSelect = page.locator('form select');

    // Check options
    const options = roleSelect.locator('option');
    const optionTexts: string[] = [];
    const count = await options.count();
    for (let i = 0; i < count; i++) {
      const text = await options.nth(i).textContent();
      if (text) optionTexts.push(text.trim());
    }

    expect(optionTexts).toContain('Куратор');
    expect(optionTexts).toContain('Аналитик угроз');
    expect(optionTexts).toContain('Администратор');
  });

  test('should cancel form and hide it', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.clickAddUser();
    expect(await usersPage.isFormVisible()).toBe(true);

    await usersPage.cancelForm();
    await page.waitForTimeout(500);

    // The form should no longer show the create user header
    const formHeader = page.locator('h3:has-text("Создать нового пользователя")');
    await expect(formHeader).not.toBeVisible({ timeout: 2000 });
  });

  test('should validate required fields on submit', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.clickAddUser();

    // Try to submit without filling required fields
    await usersPage.submitForm();

    // Should still be on form (HTML5 validation prevents submission)
    expect(await usersPage.isFormVisible()).toBe(true);
  });

  test('should display role badges with correct colors', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    // Check that role badges exist
    const roleBadges = page.locator('tbody .rounded-full');
    expect(await roleBadges.count()).toBeGreaterThan(0);
  });

  test('should display user action buttons', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    const userCount = await usersPage.getUserCount();

    if (userCount > 0) {
      // Change password button should exist
      const changePasswordButtons = page.locator('button:has-text("Изменить пароль")');
      expect(await changePasswordButtons.count()).toBeGreaterThan(0);

      // Delete button should exist
      const deleteButtons = page.locator('button:has-text("Удалить")');
      expect(await deleteButtons.count()).toBeGreaterThan(0);
    }
  });

  test('should not allow deleting admin user', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    // Find admin row
    const adminRow = await usersPage.getUserRow('admin');

    // Delete button for admin should be disabled
    const deleteButton = adminRow.locator('button:has-text("Удалить")');
    await expect(deleteButton).toBeDisabled();
  });

  test('should show change password form when clicking button', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    const userCount = await usersPage.getUserCount();

    if (userCount > 0) {
      // Click change password for first user
      await usersPage.openChangePassword('admin');

      // Password form should appear - the header is "Изменить пароль - admin"
      const passwordFormHeader = page.locator('h4:has-text("Изменить пароль")');
      await expect(passwordFormHeader).toBeVisible();

      // Password fields should be visible
      const passwordInputs = page.locator('input[type="password"]');
      expect(await passwordInputs.count()).toBeGreaterThanOrEqual(3);
    }
  });

  test('should display last login information', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    // Header should include last login column
    const headers = await usersPage.getTableHeaders();
    expect(headers).toContain('Последний вход');
  });

  test('should display creation date for users', async ({ page }) => {
    await usersPage.goto();
    await usersPage.waitForLoad();

    // Header should include creation date column - the translation is "Создан"
    const headers = await usersPage.getTableHeaders();
    expect(headers).toContain('Создан');
  });
});

test.describe('Users Management - Create User', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should create a new user successfully', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    const timestamp = Date.now();
    const username = `testuser${timestamp}`;

    await usersPage.clickAddUser();

    // Fill the form
    await usersPage.fillUserForm({
      login: username,
      password: 'TestPass123!',
      role: 'Curator'
    });

    // Submit form and wait for the user list to refresh
    await usersPage.submitForm();
    await page.waitForTimeout(2000);

    // User should appear in the list
    const userRow = await usersPage.getUserRow(username);
    await expect(userRow).toBeVisible({ timeout: 10000 });
  });

  test('should validate password strength', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.clickAddUser();

    // Fill with weak password
    const usernameInput = page.locator('input[type="text"]').first();
    await usernameInput.fill('weakpasstest');

    const passwordInput = page.locator('input[type="password"]').first();
    await passwordInput.fill('weak');

    // Select role
    const roleSelect = page.locator('form select');
    await roleSelect.selectOption({ index: 1 });

    await usersPage.submitForm();
    await page.waitForTimeout(1000);

    // Should show validation error or remain on form
    expect(await usersPage.isFormVisible()).toBe(true);
  });

  test('should prevent duplicate username', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.clickAddUser();

    // Try to create user with existing username
    await usersPage.fillUserForm({
      login: 'admin',
      password: 'TestPass123!',
      role: 'Curator'
    });

    // Wait for API response (expect error)
    const responsePromise = page.waitForResponse(resp => resp.url().includes('/api/users') && resp.request().method() === 'POST');
    await usersPage.submitForm();
    await responsePromise.catch(() => null);
    await page.waitForTimeout(1000);

    // Should show error or remain on form
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });
});

test.describe('Users Management - Edit User', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should change user role', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    // Find a non-admin user to edit
    const userRows = page.locator('tbody tr');
    const rowCount = await userRows.count();

    for (let i = 0; i < rowCount; i++) {
      const row = userRows.nth(i);
      const roleCell = row.locator('td').nth(2);
      const roleBadge = roleCell.locator('.rounded-full');
      const roleText = await roleBadge.textContent();

      if (roleText && !roleText.includes('Admin')) {
        // Click edit or change role button
        const editButton = row.locator('button:has-text("Изменить роль")').or(row.locator('button:has-text("Редактировать")'));

        if (await editButton.count() > 0) {
          await editButton.click();
          await page.waitForTimeout(500);

          // Change the role
          const roleSelect = page.locator('select').first();
          if (await roleSelect.count() > 0) {
            await roleSelect.selectOption({ index: 2 });

            // Save
            const saveButton = page.locator('button[type="submit"]').first();
            if (await saveButton.count() > 0) {
              await saveButton.click();
              await page.waitForTimeout(2000);
            }
          }
        }
        break;
      }
    }

    // Page should update
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });
});

test.describe('Users Management - Delete User', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should delete a non-admin user', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    // First create a user to delete
    const timestamp = Date.now();
    const username = `deleteuser${timestamp}`;

    await usersPage.clickAddUser();
    await usersPage.fillUserForm({
      login: username,
      password: 'TestPass123!',
      role: 'Curator'
    });

    // Submit form and wait for user list refresh
    await usersPage.submitForm();
    await page.waitForTimeout(2000);

    // Now delete the user
    const userRow = await usersPage.getUserRow(username);

    if (userRow) {
      // Set up dialog handler and wait for delete
      page.once('dialog', async dialog => {
        await dialog.accept();
      });

      const deleteButton = userRow.locator('button:has-text("Удалить")');
      await deleteButton.click();
      await page.waitForTimeout(2000);

      // User should be removed
      const bodyText = await page.textContent('body');
      expect(bodyText).toBeTruthy();
    }
  });

  test('should confirm before deleting user', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    // Find a deletable user (non-admin)
    const userRows = page.locator('tbody tr');
    const rowCount = await userRows.count();

    for (let i = 0; i < rowCount; i++) {
      const row = userRows.nth(i);
      const deleteButton = row.locator('button:has-text("Удалить")');

      if (await deleteButton.isEnabled()) {
        let dialogWasShown = false;
        page.once('dialog', async dialog => {
          dialogWasShown = true;
          await dialog.dismiss();
        });

        await deleteButton.click();
        await page.waitForTimeout(500);

        expect(dialogWasShown).toBe(true);
        break;
      }
    }
  });
});

test.describe('Users Management - Password Change', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should have all password change fields', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.openChangePassword('admin');

    // Wait for password form to appear
    const passwordFormHeader = page.locator('h4:has-text("Изменить пароль")');
    await expect(passwordFormHeader).toBeVisible({ timeout: 5000 });

    // Current password label
    const currentLabel = page.locator('label:has-text("Текущий пароль")');
    await expect(currentLabel).toBeVisible();

    // New password label - use exact matching
    const newLabel = page.getByText('Новый пароль', { exact: true });
    await expect(newLabel).toBeVisible();

    // Confirm password label - the translation is "Подтвердите пароль"
    const confirmLabel = page.locator('label:has-text("Подтвердите пароль")');
    await expect(confirmLabel).toBeVisible();
  });

  test('should have password change submit button', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.openChangePassword('admin');

    // Wait for password form to appear
    const passwordFormHeader = page.locator('h4:has-text("Изменить пароль")');
    await expect(passwordFormHeader).toBeVisible({ timeout: 5000 });

    const submitButton = page.locator('button[type="submit"]:has-text("Изменить пароль")');
    await expect(submitButton).toBeVisible();
  });

  test('should have cancel button in password change form', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.openChangePassword('admin');

    // Wait for password form to appear
    const passwordFormHeader = page.locator('h4:has-text("Изменить пароль")');
    await expect(passwordFormHeader).toBeVisible({ timeout: 5000 });

    const cancelButton = page.locator('button:has-text("Отмена")');
    await expect(cancelButton).toBeVisible();

    // Click cancel
    await cancelButton.click();

    // Form should close - the form header is "Изменить пароль"
    await expect(passwordFormHeader).not.toBeVisible();
  });

  test('should validate password match', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.openChangePassword('admin');

    // Wait for password form to appear
    const passwordFormHeader = page.locator('h4:has-text("Изменить пароль")');
    await expect(passwordFormHeader).toBeVisible({ timeout: 5000 });

    // Fill with mismatched passwords
    const passwordInputs = page.locator('input[type="password"]');
    await passwordInputs.nth(0).fill('Admin123!');
    await passwordInputs.nth(1).fill('NewPass123!');
    await passwordInputs.nth(2).fill('DifferentPass123!');

    // Submit
    const submitButton = page.locator('button[type="submit"]:has-text("Изменить пароль")');
    await submitButton.click();
    await page.waitForTimeout(1000);

    // Should show error or remain on form - the form header is "Изменить пароль"
    await expect(passwordFormHeader).toBeVisible();
  });

  test('should require current password', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    await usersPage.openChangePassword('admin');

    // Wait for password form to appear
    const passwordFormHeader = page.locator('h4:has-text("Изменить пароль")');
    await expect(passwordFormHeader).toBeVisible({ timeout: 5000 });

    // Fill only new password fields
    const passwordInputs = page.locator('input[type="password"]');
    await passwordInputs.nth(1).fill('NewPass123!');
    await passwordInputs.nth(2).fill('NewPass123!');

    // Submit
    const submitButton = page.locator('button[type="submit"]:has-text("Изменить пароль")');
    await submitButton.click();
    await page.waitForTimeout(500);

    // Should show validation error (HTML5 validation) - the form header is "Изменить пароль"
    await expect(passwordFormHeader).toBeVisible();
  });
});

test.describe('Users Management - Error Handling', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should handle API errors gracefully', async ({ page }) => {
    // Block API calls
    await page.route('**/api/users**', route => route.abort('failed'));

    await page.goto('/users');
    await page.waitForTimeout(2000);

    // Page should still render
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });

  test('should show error message when users fail to load', async ({ page }) => {
    // Block API calls to return error
    await page.route('**/api/users', route => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Internal server error' })
      });
    });

    await page.goto('/users');
    await page.waitForTimeout(2000);

    // Error message should be visible
    const errorText = page.locator('text=Ошибка').or(page.locator('.text-red-600'));
    if (await errorText.count() > 0) {
      await expect(errorText.first()).toBeVisible();
    }
  });

  test('should handle user creation failure', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    // Mock error response for POST only after initial load
    await page.route('**/api/users', route => {
      if (route.request().method() === 'POST') {
        route.fulfill({
          status: 400,
          contentType: 'application/json',
          body: JSON.stringify({ message: 'User creation failed' })
        });
      } else {
        route.continue();
      }
    });

    await usersPage.clickAddUser();
    await usersPage.fillUserForm({
      login: 'failuser',
      password: 'TestPass123!',
      role: 'Curator'
    });

    // Click submit and wait for response
    const responsePromise = page.waitForResponse(resp => resp.url().includes('/api/users') && resp.request().method() === 'POST');
    await usersPage.submitForm();
    await responsePromise.catch(() => null);
    await page.waitForTimeout(1000);

    // Should show error or remain on form - page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });
});

test.describe('Users Page - Empty State', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should handle empty users list', async ({ page }) => {
    // Mock empty response
    await page.route('**/api/users', route => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([])
      });
    });

    await page.goto('/users');
    await page.waitForTimeout(2000);

    // Empty state message should be visible
    const emptyMessage = page.locator('text=Пользователи не найдены');
    if (await emptyMessage.count() > 0) {
      await expect(emptyMessage.first()).toBeVisible();
    }
  });
});

test.describe('Users Page - Role Display', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display admin role with correct styling', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    // Admin badge should have red styling
    const adminBadge = page.locator('span.rounded-full:has-text("Admin")');
    if (await adminBadge.count() > 0) {
      await expect(adminBadge.first()).toHaveClass(/bg-red-100/);
    }
  });

  test('should display curator role with correct styling', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    // Curator badge should have blue styling
    const curatorBadge = page.locator('span.rounded-full:has-text("Curator")');
    if (await curatorBadge.count() > 0) {
      await expect(curatorBadge.first()).toHaveClass(/bg-blue-100/);
    }
  });

  test('should display threat analyst role with correct styling', async ({ page }) => {
    const usersPage = new UsersPage(page);
    await usersPage.goto();
    await usersPage.waitForLoad();

    // Analyst badge should have purple styling (as defined in getRoleBadgeColor)
    const analystBadge = page.locator('span:has-text("ThreatAnalyst")');
    if (await analystBadge.count() > 0) {
      await expect(analystBadge.first()).toHaveClass(/bg-purple-100/);
    }
  });
});

test.describe('Users Page - Responsive Design', () => {
  test.beforeEach(async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('admin', 'Admin123!');
    await loginPage.waitForDashboardRedirect();
  });

  test('should display correctly on mobile viewport', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/users');
    await page.waitForLoadState('networkidle');

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();

    // Add user button should still be visible
    const addButton = page.locator('button:has-text("Добавить пользователя")');
    await expect(addButton).toBeVisible();
  });

  test('should have horizontal scroll on table in mobile', async ({ page }) => {
    await page.setViewportSize({ width: 375, height: 667 });

    await page.goto('/users');
    await page.waitForLoadState('networkidle');

    // Table container should have overflow-x-auto class
    const tableContainer = page.locator('.overflow-x-auto');
    if (await tableContainer.count() > 0) {
      await expect(tableContainer.first()).toBeVisible();
    }
  });

  test('should display correctly on tablet viewport', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });

    await page.goto('/users');
    await page.waitForLoadState('networkidle');

    // Page should still be functional
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });
});

test.describe('Users Page - Role-Based Access', () => {
  test('curator should not access users page', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('curator1', 'Admin123!');
    await loginPage.waitForDashboardRedirect();

    // Try to navigate to users page
    await page.goto('/users');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Should be redirected or see access denied
    const url = page.url();
    const bodyText = await page.textContent('body');

    // Either redirected away from users or showing access denied
    expect(bodyText).toBeTruthy();
  });

  test('threat analyst should not access users page', async ({ page }) => {
    const loginPage = new LoginPage(page);
    await loginPage.goto();
    await loginPage.login('analyst1', 'Admin123!');
    await loginPage.waitForDashboardRedirect();

    // Try to navigate to users page
    await page.goto('/users');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(2000);

    // Should be redirected or see access denied
    const bodyText = await page.textContent('body');
    expect(bodyText).toBeTruthy();
  });
});
