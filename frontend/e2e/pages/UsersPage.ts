import { Page, Locator, expect } from '@playwright/test';

export class UsersPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly addUserButton: Locator;
  readonly loginInput: Locator;
  readonly passwordInput: Locator;
  readonly roleSelect: Locator;
  readonly submitButton: Locator;
  readonly cancelButton: Locator;
  readonly loadingSpinner: Locator;
  readonly usersTable: Locator;
  readonly userRows: Locator;
  readonly changePasswordButton: Locator;
  readonly deleteButton: Locator;
  readonly formContainer: Locator;
  readonly errorMessage: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator('h2:has-text("Управление пользователями")');
    this.addUserButton = page.locator('button:has-text("Добавить пользователя")');
    this.loginInput = page.locator('input[type="text"]').first();
    this.passwordInput = page.locator('input[type="password"]').first();
    this.roleSelect = page.locator('select').first();
    this.submitButton = page.locator('button[type="submit"]').first();
    this.cancelButton = page.locator('button:has-text("Отмена")').first();
    this.loadingSpinner = page.locator('.animate-spin');
    this.usersTable = page.locator('table');
    // Only count direct user rows, not nested password form rows
    this.userRows = page.locator('tbody tr').filter({ has: page.locator('td') });
    this.changePasswordButton = page.locator('button:has-text("Изменить пароль")');
    this.deleteButton = page.locator('button:has-text("Удалить")');
    // The add user form is in a section with bg-gray-50 and contains h3 "Создать нового пользователя"
    this.formContainer = page.locator('h3:has-text("Создать нового пользователя")').locator('..');
    this.errorMessage = page.locator('.text-red-600, .text-red-500');
  }

  async goto() {
    await this.page.goto('/users');
    await this.page.waitForLoadState('networkidle');
  }

  async waitForLoad() {
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 15000 }).catch(() => {});
    await expect(this.pageTitle).toBeVisible({ timeout: 10000 });
  }

  async clickAddUser() {
    await this.addUserButton.click();
    // Wait for the form header to become visible
    const formHeader = this.page.locator('h3:has-text("Создать нового пользователя")');
    await expect(formHeader).toBeVisible({ timeout: 5000 });
  }

  async fillUserForm(data: {
    login: string;
    password: string;
    role: 'Admin' | 'Curator' | 'ThreatAnalyst';
  }) {
    const form = this.page.locator('form');

    // Login input
    const loginInput = form.locator('input[type="text"]');
    await loginInput.fill(data.login);

    // Password input
    const passwordInput = form.locator('input[type="password"]');
    await passwordInput.fill(data.password);

    // Role select
    const roleSelect = form.locator('select');
    await roleSelect.selectOption(data.role);
  }

  async submitForm() {
    await this.submitButton.click();
  }

  async cancelForm() {
    await this.cancelButton.click();
  }

  async getUserCount() {
    return await this.userRows.count();
  }

  async getUserRow(login: string) {
    return this.page.locator('tbody tr', { hasText: login });
  }

  async deleteUser(login: string) {
    const row = await this.getUserRow(login);

    // Set up dialog handler before clicking
    this.page.once('dialog', dialog => dialog.accept());

    await row.locator('button:has-text("Удалить")').click();
  }

  async openChangePassword(login: string) {
    const row = await this.getUserRow(login);
    await row.locator('button:has-text("Изменить пароль")').click();
  }

  async fillChangePasswordForm(data: {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
  }) {
    const passwordInputs = this.page.locator('input[type="password"]');
    await passwordInputs.nth(0).fill(data.currentPassword);
    await passwordInputs.nth(1).fill(data.newPassword);
    await passwordInputs.nth(2).fill(data.confirmPassword);
  }

  async isFormVisible() {
    const formHeader = this.page.locator('h3:has-text("Создать нового пользователя")');
    return await formHeader.isVisible();
  }

  async getUserRole(login: string) {
    const row = await this.getUserRow(login);
    const roleBadge = row.locator('.rounded-full');
    return await roleBadge.textContent();
  }

  async getTableHeaders() {
    const headers = this.page.locator('thead th');
    const count = await headers.count();
    const texts: string[] = [];
    for (let i = 0; i < count; i++) {
      const text = await headers.nth(i).textContent();
      if (text) texts.push(text.trim());
    }
    return texts;
  }
}
