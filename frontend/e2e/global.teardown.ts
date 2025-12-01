import { test as teardown } from '@playwright/test';
import fs from 'fs';
import path from 'path';

const AUTH_DIR = path.join(__dirname, '.auth');

/**
 * Global teardown for E2E tests
 *
 * This runs after all tests complete to clean up:
 * 1. Remove stored authentication state
 * 2. Clean up any temporary files
 */
teardown('cleanup auth state', async () => {
  // Clean up auth state files
  try {
    const files = fs.readdirSync(AUTH_DIR);
    for (const file of files) {
      if (file.endsWith('.json')) {
        fs.unlinkSync(path.join(AUTH_DIR, file));
        console.log('Cleaned up auth state:', file);
      }
    }
  } catch (error) {
    // Directory might not exist, which is fine
    console.log('No auth state to clean up');
  }
});
