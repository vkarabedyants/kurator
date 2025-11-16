import { Page, Locator, expect } from '@playwright/test';

export class LoginPage {
  readonly page: Page;
  readonly loginInput: Locator;
  readonly passwordInput: Locator;
  readonly submitButton: Locator;
  readonly errorMessage: Locator;
  readonly pageTitle: Locator;

  constructor(page: Page) {
    this.page = page;
    // More flexible locators to match the actual page
    this.loginInput = page.locator('input[placeholder*="ogin" i]').first();
    this.passwordInput = page.locator('input[placeholder*="assword" i]').first();
    this.submitButton = page.locator('button[type="submit"]').or(page.locator('button:has-text("Sign in")')).first();
    this.errorMessage = page.locator('.text-red-500, .text-red-600, .text-red-700, [class*="error"]');
    this.pageTitle = page.locator('text=KURATOR').or(page.locator('text=КУРАТОР')).first();
  }

  async goto() {
    await this.page.goto('/login');
    await this.page.waitForLoadState('networkidle');
    await expect(this.pageTitle).toBeVisible({ timeout: 10000 });
  }

  async login(username: string, password: string) {
    await this.loginInput.fill(username);
    await this.passwordInput.fill(password);
    await this.submitButton.click();
  }

  async waitForDashboardRedirect() {
    await this.page.waitForURL('**/dashboard', { timeout: 10000 });
  }

  async isErrorVisible() {
    return await this.errorMessage.isVisible();
  }
}
