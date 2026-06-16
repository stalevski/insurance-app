import { test } from '@insurance-app-fixtures';
import { assertNoSeriousA11yViolations } from '@helpers/a11y';

/**
 * Accessibility baseline for the primary surfaces. Each route is checked against
 * WCAG 2.0/2.1 A+AA and fails only on `critical` or `serious` violations.
 */
const ROUTES: { name: string; path: string }[] = [
  { name: 'Dashboard', path: '/' },
  { name: 'Quotes', path: '/quotes' },
  { name: 'Policies', path: '/policies' },
  { name: 'Domain events', path: '/events' },
  { name: 'Ingest', path: '/ingest' },
];

test.describe('Accessibility @a11y', () => {
  for (const route of ROUTES) {
    test(`${route.name} has no serious accessibility violations`, async ({ page }) => {
      await page.goto(route.path, { waitUntil: 'domcontentloaded' });
      await assertNoSeriousA11yViolations(page);
    });
  }
});
