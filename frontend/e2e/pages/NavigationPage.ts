import { Page, Locator } from '@playwright/test';

export class NavigationPage {
  readonly page: Page;
  readonly contactsLink: Locator;
  readonly interactionsLink: Locator;
  readonly blocksLink: Locator;
  readonly usersLink: Locator;
  readonly watchlistLink: Locator;
  readonly logoutButton: Locator;

  constructor(page: Page) {
    this.page = page;
    // Navigation links - may vary based on user role
    this.contactsLink = page.locator('a[href*="/contacts"]').first();
    this.interactionsLink = page.locator('a[href*="/interactions"]').first();
    this.blocksLink = page.locator('a[href*="/blocks"]').first();
    this.usersLink = page.locator('a[href*="/users"]').first();
    this.watchlistLink = page.locator('a[href*="/watchlist"]').first();
    this.logoutButton = page.locator('button:has-text("Выход")');
  }

  async goToContacts() {
    await this.contactsLink.click();
    await this.page.waitForURL('**/contacts');
  }

  async goToInteractions() {
    await this.interactionsLink.click();
    await this.page.waitForURL('**/interactions');
  }

  async goToBlocks() {
    await this.blocksLink.click();
    await this.page.waitForURL('**/blocks');
  }

  async goToUsers() {
    await this.usersLink.click();
    await this.page.waitForURL('**/users');
  }

  async logout() {
    await this.logoutButton.click();
    await this.page.waitForURL('**/login');
  }
}
