/**
 * Independent rating oracle — a black-box port of the API's premium calculation
 * (mirrors the `ExpectedRatingResult` test oracle on the .NET side). Tests use this
 * to predict premiums from inputs rather than echoing the server's own output, so a
 * regression in the rating engine is actually caught instead of being rubber-stamped.
 *
 * Rating rules (per product):
 *   revenuePremium  = round2(revenue / 1000 * baseRatePerThousand)
 *   largeAccountLoad = revenue >= largeAccountThreshold
 *                        ? round2(revenuePremium * largeAccountLoad)
 *                        : 0
 *   technicalPremium = max(revenuePremium + largeAccountLoad, minimumPremium)
 *
 * Rounding is half-away-from-zero at 2 decimal places to match the server's
 * decimal arithmetic for the (always non-negative) premium values involved.
 */

export interface ProductRatingParameters {
  baseRatePerThousandRevenue: number;
  minimumPremium: number;
  largeAccountThreshold: number;
  largeAccountLoad: number;
}

export interface ExpectedRating {
  revenuePremium: number;
  largeAccountLoad: number;
  technicalPremium: number;
}

/** The seeded product catalog rating parameters, keyed by canonical product code. */
export const RATING_CATALOG: Readonly<Record<string, ProductRatingParameters>> = {
  COMMERCIAL_PROPERTY: {
    baseRatePerThousandRevenue: 2.5,
    minimumPremium: 750,
    largeAccountThreshold: 10_000_000,
    largeAccountLoad: 0.05,
  },
  LIABILITY: {
    baseRatePerThousandRevenue: 1.8,
    minimumPremium: 500,
    largeAccountThreshold: 10_000_000,
    largeAccountLoad: 0.05,
  },
  CYBER: {
    baseRatePerThousandRevenue: 4.0,
    minimumPremium: 1_500,
    largeAccountThreshold: 10_000_000,
    largeAccountLoad: 0.05,
  },
  MOTOR: {
    baseRatePerThousandRevenue: 3.2,
    minimumPremium: 900,
    largeAccountThreshold: 10_000_000,
    largeAccountLoad: 0.05,
  },
};

/** Maps a QuoteForge `productLine` to the canonical product code used for rating. */
export const PRODUCT_LINE_TO_CODE: Readonly<Record<string, string>> = {
  property: 'COMMERCIAL_PROPERTY',
  'commercial property': 'COMMERCIAL_PROPERTY',
  liability: 'LIABILITY',
  cyber: 'CYBER',
  motor: 'MOTOR',
};

const round2 = (value: number): number => {
  const sign = value < 0 ? -1 : 1;
  const abs = Math.abs(value);
  return (sign * Math.round((abs + Number.EPSILON) * 100)) / 100;
};

export const resolveProductCode = (productLineOrCode: string): string => {
  const normalized = productLineOrCode.trim();
  const upper = normalized.toUpperCase();
  if (upper in RATING_CATALOG) {
    return upper;
  }
  const mapped = PRODUCT_LINE_TO_CODE[normalized.toLowerCase()];
  if (!mapped) {
    throw new Error(`Unknown product line or code: "${productLineOrCode}"`);
  }
  return mapped;
};

/** Computes the expected rating for a canonical product code and insured revenue. */
export const expectedRatingForCode = (productCode: string, insuredRevenue: number): ExpectedRating => {
  const params = RATING_CATALOG[productCode];
  if (!params) {
    throw new Error(`Unknown product code: "${productCode}"`);
  }

  const revenuePremium = round2((insuredRevenue / 1000) * params.baseRatePerThousandRevenue);
  const largeAccountLoad =
    insuredRevenue >= params.largeAccountThreshold ? round2(revenuePremium * params.largeAccountLoad) : 0;
  const technicalPremium = Math.max(round2(revenuePremium + largeAccountLoad), params.minimumPremium);

  return { revenuePremium, largeAccountLoad, technicalPremium };
};

/** Computes the expected rating from a QuoteForge `productLine` (or code) and revenue. */
export const expectedRating = (productLineOrCode: string, insuredRevenue: number): ExpectedRating =>
  expectedRatingForCode(resolveProductCode(productLineOrCode), insuredRevenue);
