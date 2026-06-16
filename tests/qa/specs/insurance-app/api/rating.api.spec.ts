import { test, expect } from '@insurance-app-fixtures';
import {
  expectedRating,
  expectedRatingForCode,
  resolveProductCode,
  RATING_CATALOG,
} from '@builders/expected/expected-rating-result';

/**
 * The rating oracle ({@link expectedRating}) is a black-box port of the server's
 * `RatingService`. These tests pin its branches (minimum-premium floor, pure
 * revenue rating, and the large-account load) and guard it against drift from the
 * live product catalog, so any change to the published rates surfaces immediately.
 */
test.describe('Rating oracle', () => {
  test('applies the minimum premium when the revenue-based premium is lower', () => {
    // LIABILITY: 100,000 / 1000 * 1.8 = 180, below the 500 minimum.
    const rating = expectedRatingForCode('LIABILITY', 100_000);

    expect(rating.revenuePremium).toBe(180);
    expect(rating.largeAccountLoad).toBe(0);
    expect(rating.technicalPremium).toBe(500);
  });

  test('uses the revenue-based premium for mid-size accounts', () => {
    // LIABILITY: 1,500,000 / 1000 * 1.8 = 2700, above the minimum, below threshold.
    const rating = expectedRatingForCode('LIABILITY', 1_500_000);

    expect(rating.revenuePremium).toBe(2700);
    expect(rating.largeAccountLoad).toBe(0);
    expect(rating.technicalPremium).toBe(2700);
  });

  test('adds the large-account load at or above the threshold', () => {
    // COMMERCIAL_PROPERTY: 20,000,000 / 1000 * 2.5 = 50000; load 5% = 2500.
    const rating = expectedRatingForCode('COMMERCIAL_PROPERTY', 20_000_000);

    expect(rating.revenuePremium).toBe(50_000);
    expect(rating.largeAccountLoad).toBe(2_500);
    expect(rating.technicalPremium).toBe(52_500);
  });

  test('applies the load exactly at the threshold boundary', () => {
    // LIABILITY at exactly 10,000,000: 18000 + 5% (900) = 18900.
    const rating = expectedRatingForCode('LIABILITY', 10_000_000);

    expect(rating.largeAccountLoad).toBe(900);
    expect(rating.technicalPremium).toBe(18_900);
  });

  test('resolves QuoteForge product lines to canonical codes', () => {
    expect(resolveProductCode('Liability')).toBe('LIABILITY');
    expect(resolveProductCode('Property')).toBe('COMMERCIAL_PROPERTY');
    expect(resolveProductCode('Cyber')).toBe('CYBER');
    expect(expectedRating('Cyber', 200_000).technicalPremium).toBe(1_500);
  });

  test('stays in sync with the live product catalog @smoke', async ({ api }) => {
    const products = await api.products();

    for (const product of products) {
      const oracle = RATING_CATALOG[product.productCode];
      expect(oracle, `oracle missing product ${product.productCode}`).toBeDefined();
      expect(product.baseRatePerThousandRevenue).toBeCloseTo(oracle.baseRatePerThousandRevenue, 5);
      expect(product.minimumPremium).toBeCloseTo(oracle.minimumPremium, 5);
      expect(product.largeAccountThreshold).toBeCloseTo(oracle.largeAccountThreshold, 5);
      expect(product.largeAccountLoad).toBeCloseTo(oracle.largeAccountLoad, 5);
    }
  });
});
