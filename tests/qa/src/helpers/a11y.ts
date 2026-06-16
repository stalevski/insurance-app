/**
 * Accessibility test helper.
 *
 * Wraps `@axe-core/playwright` with the standard WCAG 2.0/2.1 A + AA rule sets
 * and a single assertion that fails the test if any `critical` or `serious`
 * violations are found. Lower-impact issues (`moderate`, `minor`) are surfaced
 * by Axe but not enforced - they need design or content judgement and would
 * otherwise produce noisy runs.
 *
 * Usage:
 *   import { assertNoSeriousA11yViolations } from '@helpers/a11y';
 *
 *   test('quotes page meets a11y baseline', async ({ page }) => {
 *     await page.goto('/quotes');
 *     await assertNoSeriousA11yViolations(page);
 *   });
 */
import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Page } from '@playwright/test';

const WCAG_TAGS = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'];
const BLOCKING_IMPACTS = new Set(['critical', 'serious']);

type AxeResults = Awaited<ReturnType<InstanceType<typeof AxeBuilder>['analyze']>>;
type AxeViolation = AxeResults['violations'][number];

/** Renders blocking violations as a compact, actionable summary for the failure message. */
const summarize = (violations: AxeViolation[]): string =>
  violations
    .map((violation) => {
      const targets = violation.nodes
        .slice(0, 5)
        .map((node) => node.target.map(String).join(' '))
        .join(', ');
      return `- [${violation.impact}] ${violation.id}: ${violation.help}\n    nodes (${violation.nodes.length}): ${targets}\n    ${violation.helpUrl}`;
    })
    .join('\n');

export const assertNoSeriousA11yViolations = async (page: Page): Promise<void> => {
  const results = await new AxeBuilder({ page }).withTags(WCAG_TAGS).analyze();
  const blocking = results.violations.filter((violation) =>
    BLOCKING_IMPACTS.has(violation.impact ?? ''),
  );

  // Attach the full Axe report to the Playwright report so every scan - passing or
  // failing - is auditable from the HTML report without dumping raw JSON into the
  // assertion message.
  await test.info().attach('axe-results.json', {
    body: JSON.stringify(results, null, 2),
    contentType: 'application/json',
  });

  expect(
    blocking,
    blocking.length === 0
      ? 'no blocking accessibility violations'
      : `Found ${blocking.length} blocking (critical/serious) accessibility violation(s) on ${page.url()}:\n${summarize(blocking)}`,
  ).toEqual([]);
};
