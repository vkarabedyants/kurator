import { Page, Locator, expect } from '@playwright/test';

export class AuditLogPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly searchInput: Locator;
  readonly userFilter: Locator;
  readonly actionFilter: Locator;
  readonly dateFromInput: Locator;
  readonly dateToInput: Locator;
  readonly clearFiltersButton: Locator;
  readonly loadingSpinner: Locator;
  readonly logTable: Locator;
  readonly logRows: Locator;
  readonly paginationPrev: Locator;
  readonly paginationNext: Locator;
  readonly emptyState: Locator;
  readonly totalRecords: Locator;
  readonly summaryStats: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator('h1').filter({ hasText: 'Журнал аудита' }).first();
    this.searchInput = page.locator('input[placeholder*="Поиск по журналу"]');
    // Filters in the grid - user is first select, action is second
    this.userFilter = page.locator('.bg-white.rounded-lg.shadow select').first();
    this.actionFilter = page.locator('.bg-white.rounded-lg.shadow select').nth(1);
    this.dateFromInput = page.locator('input[type="date"]').first();
    this.dateToInput = page.locator('input[type="date"]').nth(1);
    this.clearFiltersButton = page.locator('text=Очистить все');
    this.loadingSpinner = page.locator('text=Загрузка журнала аудита...');
    this.logTable = page.locator('table').first();
    this.logRows = page.locator('tbody tr');
    this.paginationPrev = page.locator('button:has-text("Назад")');
    this.paginationNext = page.locator('button:has-text("Далее")');
    this.emptyState = page.locator('h3:has-text("Записи аудита не найдены")');
    this.totalRecords = page.locator('text=Всего записей:');
    this.summaryStats = page.locator('h3:has-text("Сводка активности")').locator('..');
  }

  async goto() {
    await this.page.goto('/audit-log');
    await this.page.waitForLoadState('networkidle');
  }

  async waitForLoad() {
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 15000 }).catch(() => {});
    await expect(this.pageTitle).toBeVisible({ timeout: 10000 });
  }

  async search(term: string) {
    await this.searchInput.fill(term);
    await this.page.waitForTimeout(500);
  }

  async filterByUser(user: string) {
    await this.userFilter.selectOption(user);
    await this.page.waitForTimeout(500);
  }

  async filterByAction(action: string) {
    await this.actionFilter.selectOption(action);
    await this.page.waitForTimeout(500);
  }

  async filterByDateRange(from: string, to: string) {
    await this.dateFromInput.fill(from);
    await this.dateToInput.fill(to);
    await this.page.waitForTimeout(500);
  }

  async clearFilters() {
    if (await this.clearFiltersButton.isVisible()) {
      await this.clearFiltersButton.click();
      await this.page.waitForTimeout(500);
    }
  }

  async getLogCount() {
    return await this.logRows.count();
  }

  async getTotalRecordsCount() {
    const text = await this.totalRecords.textContent();
    const match = text?.match(/Всего записей: (\d+)/);
    return match ? parseInt(match[1]) : 0;
  }

  async goToNextPage() {
    await this.paginationNext.click();
    await this.page.waitForLoadState('networkidle');
  }

  async goToPrevPage() {
    await this.paginationPrev.click();
    await this.page.waitForLoadState('networkidle');
  }

  async isEmptyStateVisible() {
    return await this.emptyState.isVisible();
  }

  async getLogActions() {
    const actions = new Set<string>();
    const actionBadges = this.page.locator('tbody .rounded-full');
    const count = await actionBadges.count();
    for (let i = 0; i < count; i++) {
      const text = await actionBadges.nth(i).textContent();
      if (text) actions.add(text.trim());
    }
    return Array.from(actions);
  }

  async getLogUsers() {
    const users = new Set<string>();
    const count = await this.logRows.count();
    for (let i = 0; i < count; i++) {
      // User is in the second column
      const userCell = this.logRows.nth(i).locator('td').nth(1);
      const text = await userCell.textContent();
      if (text) users.add(text.trim());
    }
    return Array.from(users);
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

  async getSummaryStatistics() {
    const stats: Record<string, number> = {};
    const summarySection = this.page.locator('text=Сводка активности').locator('..').locator('..');

    const created = await summarySection.locator('text=Создано').locator('..').locator('p.font-bold').textContent();
    const updated = await summarySection.locator('text=Обновлено').locator('..').locator('p.font-bold').textContent();
    const deleted = await summarySection.locator('text=Удалено').locator('..').locator('p.font-bold').textContent();
    const logins = await summarySection.locator('text=Входов в систему').locator('..').locator('p.font-bold').textContent();

    if (created) stats['created'] = parseInt(created);
    if (updated) stats['updated'] = parseInt(updated);
    if (deleted) stats['deleted'] = parseInt(deleted);
    if (logins) stats['logins'] = parseInt(logins);

    return stats;
  }
}
