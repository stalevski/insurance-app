import { test, expect } from '@insurance-app-fixtures';
import { RATING_CATALOG } from '@builders/expected/expected-rating-result';

test.describe('Products catalog @smoke', () => {
  test('returns the four seeded products with rating parameters', async ({ api }) => {
    const products = await api.products();

    const codes = products.map((product) => product.productCode).sort();
    expect(codes).toEqual(['COMMERCIAL_PROPERTY', 'CYBER', 'LIABILITY', 'MOTOR']);
  });

  test('exposes rating parameters that match the rating oracle', async ({ api }) => {
    const products = await api.products();

    for (const product of products) {
      const expected = RATING_CATALOG[product.productCode];
      expect(expected, `unexpected product code ${product.productCode}`).toBeDefined();
      expect(product.baseRatePerThousandRevenue).toBeCloseTo(expected.baseRatePerThousandRevenue, 5);
      expect(product.minimumPremium).toBeCloseTo(expected.minimumPremium, 5);
      expect(product.largeAccountThreshold).toBeCloseTo(expected.largeAccountThreshold, 5);
      expect(product.largeAccountLoad).toBeCloseTo(expected.largeAccountLoad, 5);
    }
  });

  test('every product has a non-empty display name and family', async ({ api }) => {
    const products = await api.products();

    expect(products.length).toBeGreaterThan(0);
    for (const product of products) {
      expect(product.displayName.trim().length).toBeGreaterThan(0);
      expect(product.family.trim().length).toBeGreaterThan(0);
    }
  });
});
