import { Page, Locator, expect } from '@playwright/test';

export class WatchlistPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly addThreatButton: Locator;
  readonly searchInput: Locator;
  readonly riskLevelFilter: Locator;
  readonly threatSphereFilter: Locator;
  readonly loadingSpinner: Locator;
  readonly watchlistCards: Locator;
  readonly viewButton: Locator;
  readonly editButton: Locator;
  readonly deleteButton: Locator;
  readonly emptyState: Locator;
  readonly filterCount: Locator;

  constructor(page: Page) {
    this.page = page;
    // Use first() to avoid strict mode violation when multiple elements match
    this.pageTitle = page.locator('h1').filter({ hasText: 'Реестр угроз' }).first();
    this.addThreatButton = page.locator('button:has-text("Добавить угрозу")');
    this.searchInput = page.locator('input[placeholder*="Поиск угроз"]');
    this.riskLevelFilter = page.locator('select').first();
    this.threatSphereFilter = page.locator('select').nth(1);
    this.loadingSpinner = page.locator('text=Загрузка угроз...');
    // Watchlist cards - only real threat cards that contain action buttons, not the empty state
    this.watchlistCards = page.locator('.bg-white.rounded-lg.shadow').filter({
      has: page.locator('button[title="Просмотр деталей"]')
    });
    this.viewButton = page.locator('button[title="Просмотр деталей"]');
    this.editButton = page.locator('button[title="Редактировать"]');
    this.deleteButton = page.locator('button[title="Удалить"]');
    // Use h3 specifically for empty state - it's in h3 element
    this.emptyState = page.locator('h3:has-text("Угрозы не найдены")');
    this.filterCount = page.locator('text=/\\d+ угроз найдено/');
  }

  async goto() {
    await this.page.goto('/watchlist');
    await this.page.waitForLoadState('networkidle');
  }

  async waitForLoad() {
    // Wait for loading state to clear
    await this.page.waitForTimeout(1000);
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 15000 }).catch(() => {});
    // Page should be loaded - check body has content
    await this.page.waitForTimeout(500);
  }

  async clickAddThreat() {
    await this.addThreatButton.click();
    await this.page.waitForURL('**/watchlist/new');
  }

  async search(term: string) {
    await this.searchInput.fill(term);
    await this.page.waitForTimeout(500);
  }

  async filterByRiskLevel(level: string) {
    await this.riskLevelFilter.selectOption(level);
    await this.page.waitForTimeout(500);
  }

  async filterByThreatSphere(sphere: string) {
    await this.threatSphereFilter.selectOption(sphere);
    await this.page.waitForTimeout(500);
  }

  async clearFilters() {
    await this.searchInput.clear();
    await this.riskLevelFilter.selectOption('');
    await this.threatSphereFilter.selectOption('');
    await this.page.waitForTimeout(500);
  }

  async getThreatCount() {
    return await this.watchlistCards.count();
  }

  async getFilteredCount() {
    const text = await this.filterCount.textContent();
    const match = text?.match(/(\d+) угроз найдено/);
    return match ? parseInt(match[1]) : 0;
  }

  async viewThreat(index: number = 0) {
    await this.watchlistCards.nth(index).locator('button[title="Просмотр деталей"]').click();
  }

  async editThreat(index: number = 0) {
    await this.watchlistCards.nth(index).locator('button[title="Редактировать"]').click();
  }

  async deleteThreat(index: number = 0) {
    // Set up dialog handler before clicking
    this.page.once('dialog', dialog => dialog.accept());

    await this.watchlistCards.nth(index).locator('button[title="Удалить"]').click();
  }

  async isEmptyStateVisible() {
    return await this.emptyState.isVisible();
  }

  async getThreatRiskLevels() {
    const badges = this.page.locator('.rounded-full.border');
    const levels = new Set<string>();
    const count = await badges.count();
    for (let i = 0; i < count; i++) {
      const text = await badges.nth(i).textContent();
      if (text) levels.add(text);
    }
    return Array.from(levels);
  }

  async getThreatNames() {
    const names: string[] = [];
    const cards = await this.watchlistCards.count();
    for (let i = 0; i < cards; i++) {
      const nameElement = this.watchlistCards.nth(i).locator('h3');
      const name = await nameElement.textContent();
      if (name) names.push(name);
    }
    return names;
  }
}
