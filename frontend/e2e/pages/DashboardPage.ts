import { Page, Locator, expect } from '@playwright/test';

export class DashboardPage {
  readonly page: Page;
  readonly welcomeHeading: Locator;
  readonly userRole: Locator;
  readonly metricsCards: Locator;
  readonly loadingSpinner: Locator;

  constructor(page: Page) {
    this.page = page;
    // Flexible locators that work with Russian UI
    // Russian welcome: "Добро пожаловать, {login}!"
    this.welcomeHeading = page.locator('h1').filter({ hasText: 'Добро пожаловать' });
    this.userRole = page.locator('text=Роль:').or(page.locator('text=Role:'));
    this.metricsCards = page.locator('.bg-white.rounded-lg.shadow');
    this.loadingSpinner = page.locator('.animate-spin');
  }

  async goto() {
    await this.page.goto('/dashboard');
  }

  async waitForLoad() {
    // Wait for loading spinner to disappear - shorter timeout
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 5000 }).catch(() => {
      // Spinner might not appear if data loads quickly
    });

    // Wait for welcome heading or any main content - shorter timeout
    await expect(this.welcomeHeading).toBeVisible({ timeout: 5000 });
  }

  async isWelcomeVisible() {
    return await this.welcomeHeading.isVisible();
  }

  async getUserRole() {
    const roleText = await this.userRole.textContent();
    return roleText?.split(':')[1]?.trim() || '';
  }

  async hasMetricsCards() {
    const count = await this.metricsCards.count();
    return count > 0;
  }
}
