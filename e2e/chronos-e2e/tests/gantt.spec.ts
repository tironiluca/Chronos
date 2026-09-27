import { test, expect } from '@playwright/test';

// Smoke test -- extend once the Projects list / creation flow has a UI to navigate from.
test('renders the Gantt chart for a project', async ({ page }) => {
  await page.goto('/projects/00000000-0000-0000-0000-000000000001/gantt');

  await expect(page.locator('.gantt-container svg')).toBeVisible();
});
