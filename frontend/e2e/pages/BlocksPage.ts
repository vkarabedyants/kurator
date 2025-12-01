import { Page, Locator, expect } from '@playwright/test';

export class BlocksPage {
  readonly page: Page;
  readonly pageTitle: Locator;
  readonly addBlockButton: Locator;
  readonly blockNameInput: Locator;
  readonly blockCodeInput: Locator;
  readonly blockDescriptionInput: Locator;
  readonly statusSelect: Locator;
  readonly primaryCuratorSelect: Locator;
  readonly backupCuratorSelect: Locator;
  readonly submitButton: Locator;
  readonly cancelButton: Locator;
  readonly loadingSpinner: Locator;
  readonly blockCards: Locator;
  readonly editButton: Locator;
  readonly deleteButton: Locator;
  readonly formContainer: Locator;

  constructor(page: Page) {
    this.page = page;
    this.pageTitle = page.locator('h2:has-text("Управление блоками")');
    this.addBlockButton = page.locator('button:has-text("Добавить блок")');
    this.blockNameInput = page.locator('input').filter({ has: page.locator('..').filter({ hasText: 'Название' }) }).first();
    this.blockCodeInput = page.locator('input[placeholder*="напр"]').or(page.locator('input').filter({ has: page.locator('..').filter({ hasText: 'Код' }) })).first();
    this.blockDescriptionInput = page.locator('textarea');
    this.statusSelect = page.locator('select').filter({ has: page.locator('..').filter({ hasText: 'Статус' }) }).first();
    this.primaryCuratorSelect = page.locator('select').filter({ has: page.locator('..').filter({ hasText: 'Основной куратор' }) }).first();
    this.backupCuratorSelect = page.locator('select').filter({ has: page.locator('..').filter({ hasText: 'Резервный куратор' }) }).first();
    this.submitButton = page.locator('button[type="submit"]');
    this.cancelButton = page.locator('button:has-text("Отмена")');
    this.loadingSpinner = page.locator('.animate-spin');
    this.blockCards = page.locator('.border.rounded-lg.p-4');
    this.editButton = page.locator('button:has-text("Редактировать")');
    this.deleteButton = page.locator('button:has-text("Удалить")');
    this.formContainer = page.locator('.bg-gray-50');
  }

  async goto() {
    await this.page.goto('/blocks');
    // Use domcontentloaded instead of networkidle for faster navigation
    await this.page.waitForLoadState('domcontentloaded');
  }

  async waitForLoad() {
    // Wait for loading to complete - shorter timeout since page should load quickly
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout: 5000 }).catch(() => {});
    await expect(this.pageTitle).toBeVisible({ timeout: 5000 });
  }

  async clickAddBlock() {
    await this.addBlockButton.click();
    await expect(this.formContainer).toBeVisible();
  }

  async fillBlockForm(data: {
    name: string;
    code: string;
    description?: string;
    status?: 'Active' | 'Archived';
  }) {
    // Find input fields by their labels in the form
    const form = this.page.locator('form');

    // Name input - first input in the grid
    const nameInput = form.locator('input[type="text"]').first();
    await nameInput.fill(data.name);

    // Code input - second input
    const codeInput = form.locator('input[type="text"]').nth(1);
    await codeInput.fill(data.code);

    if (data.description) {
      await this.blockDescriptionInput.fill(data.description);
    }

    if (data.status) {
      const statusSelect = form.locator('select').first();
      await statusSelect.selectOption(data.status);
    }
  }

  async submitForm() {
    await this.submitButton.click();
  }

  async cancelForm() {
    await this.cancelButton.click();
  }

  async getBlockCount() {
    return await this.blockCards.count();
  }

  async getBlockByName(name: string) {
    return this.page.locator('.border.rounded-lg.p-4', { hasText: name });
  }

  async editBlock(name: string) {
    const block = await this.getBlockByName(name);
    await block.locator('button:has-text("Редактировать")').click();
  }

  async deleteBlock(name: string) {
    const block = await this.getBlockByName(name);

    // Set up dialog handler before clicking
    this.page.once('dialog', dialog => dialog.accept());

    await block.locator('button:has-text("Удалить")').click();
  }

  async isFormVisible() {
    return await this.formContainer.isVisible();
  }

  async getBlockStatus(name: string) {
    const block = await this.getBlockByName(name);
    const statusBadge = block.locator('.rounded-full');
    return await statusBadge.textContent();
  }
}
