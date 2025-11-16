import { Page, Locator, expect } from '@playwright/test';

export class DashboardPage {
  readonly page: Page;
  readonly welcomeHeading: Locator;
  readonly userRole: Locator;
  readonly metricsCards: Locator;
  readonly loadingSpinner: Locator;

  constructor(page: Page) {
    this.page = page;
    this.welcomeHeading = page.locator('h1:has-text("Добро пожаловать")');
    this.userRole = page.locator('text=Роль:');
    this.metricsCards = page.locator('.bg-white.rounded-lg.shadow');
    this.loadingSpinner = page.locator('.animate-spin');
  }

  async goto() {
    await this.page.goto('/dashboard');
  }

  async waitForLoad() {
    // Wait for loading spinner to disappear
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 15000 }).catch(() => {
      // Spinner might not appear if data loads quickly
    });

    // Wait for welcome heading
    await expect(this.welcomeHeading).toBeVisible({ timeout: 10000 });
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
