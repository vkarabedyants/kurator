import { Page, Locator, expect } from '@playwright/test';

export class InteractionsPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly addInteractionButton: Locator;
  readonly typeFilter: Locator;
  readonly resultFilter: Locator;
  readonly dateFromInput: Locator;
  readonly dateToInput: Locator;
  readonly loadingSpinner: Locator;
  readonly interactionCards: Locator;
  readonly deleteButton: Locator;
  readonly viewContactButton: Locator;
  readonly paginationPrev: Locator;
  readonly paginationNext: Locator;
  readonly paginationInfo: Locator;
  readonly emptyState: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator('h2:has-text("Журнал взаимодействий")');
    this.addInteractionButton = page.locator('a:has-text("Добавить взаимодействие")');
    this.typeFilter = page.locator('select').filter({ has: page.locator('..').filter({ hasText: 'Тип' }) }).first();
    this.resultFilter = page.locator('select').filter({ has: page.locator('..').filter({ hasText: 'Результат' }) }).first();
    this.dateFromInput = page.locator('input[type="date"]').first();
    this.dateToInput = page.locator('input[type="date"]').last();
    this.loadingSpinner = page.locator('.animate-spin');
    this.interactionCards = page.locator('.border.rounded-lg.p-4');
    this.deleteButton = page.locator('button:has-text("Удалить")');
    this.viewContactButton = page.locator('a:has-text("Просмотр контакта")');
    this.paginationPrev = page.locator('button:has-text("Предыдущая")');
    this.paginationNext = page.locator('button:has-text("Следующая")');
    this.paginationInfo = page.locator('text=Страница');
    this.emptyState = page.locator('text=Взаимодействия не найдены');
  }

  async goto() {
    await this.page.goto('/interactions');
    await this.page.waitForLoadState('networkidle');
  }

  async waitForLoad() {
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 15000 }).catch(() => {});
    await expect(this.pageTitle).toBeVisible({ timeout: 10000 });
  }

  async clickAddInteraction() {
    await this.addInteractionButton.click();
    await this.page.waitForURL('**/interactions/new');
  }

  async filterByType(type: string) {
    await this.typeFilter.selectOption(type);
    await this.page.waitForTimeout(500);
  }

  async filterByResult(result: string) {
    await this.resultFilter.selectOption(result);
    await this.page.waitForTimeout(500);
  }

  async filterByDateRange(from: string, to: string) {
    await this.dateFromInput.fill(from);
    await this.dateToInput.fill(to);
    await this.page.waitForTimeout(500);
  }

  async getInteractionCount() {
    const count = await this.interactionCards.count();
    return count;
  }

  async deleteInteraction(index: number = 0) {
    // Set up dialog handler before clicking
    this.page.once('dialog', dialog => dialog.accept());

    await this.interactionCards.nth(index).locator('button:has-text("Удалить")').click();
  }

  async viewContact(index: number = 0) {
    await this.interactionCards.nth(index).locator('a:has-text("Просмотр контакта")').click();
  }

  async goToNextPage() {
    await this.paginationNext.click();
    await this.page.waitForLoadState('networkidle');
  }

  async goToPrevPage() {
    await this.paginationPrev.click();
    await this.page.waitForLoadState('networkidle');
  }

  async getCurrentPage() {
    const pageText = await this.paginationInfo.textContent();
    const match = pageText?.match(/Страница (\d+) из (\d+)/);
    if (match) {
      return { current: parseInt(match[1]), total: parseInt(match[2]) };
    }
    return null;
  }

  async isEmptyStateVisible() {
    return await this.emptyState.isVisible();
  }

  async getInteractionTypes() {
    const badges = this.page.locator('.rounded-full');
    const types = new Set<string>();
    const count = await badges.count();
    for (let i = 0; i < count; i++) {
      const text = await badges.nth(i).textContent();
      if (text) types.add(text);
    }
    return Array.from(types);
  }
}
