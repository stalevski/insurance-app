let counter = 0;

/**
 * Generates a short, unique, URL-safe suffix for test data (e.g. envelope ids and
 * quote references). Combines a coarse timestamp, a per-process counter, and a
 * random tail so parallel workers and fast repeated calls within the same
 * millisecond do not collide.
 */
export const uniqueSuffix = (): string => {
  counter = (counter + 1) % 1000;
  const timePart = Math.floor(Date.now() / 1000).toString(36);
  const counterPart = counter.toString(36).padStart(2, '0');
  const randomPart = Math.floor(Math.random() * 1296)
    .toString(36)
    .padStart(2, '0');
  return `${timePart}${counterPart}${randomPart}`;
};
