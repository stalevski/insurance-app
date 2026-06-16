/** A product catalog entry as returned by `GET /api/v1/products`. */
export interface ProductDto {
  productCode: string;
  displayName: string;
  family: string;
  baseRatePerThousandRevenue: number;
  minimumPremium: number;
  largeAccountThreshold: number;
  largeAccountLoad: number;
}
