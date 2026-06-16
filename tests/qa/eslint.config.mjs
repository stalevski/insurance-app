import js from '@eslint/js';
import tseslint from 'typescript-eslint';
import playwright from 'eslint-plugin-playwright';
import prettier from 'eslint-config-prettier';
import globals from 'globals';

export default tseslint.config(
  {
    ignores: ['node_modules', 'playwright-report', 'test-results', 'blob-report', 'coverage', 'dist', '.tmp'],
  },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    languageOptions: {
      ecmaVersion: 2022,
      sourceType: 'module',
      globals: {
        ...globals.node,
      },
    },
    rules: {
      '@typescript-eslint/no-explicit-any': 'warn',
      '@typescript-eslint/no-unused-vars': ['warn', { argsIgnorePattern: '^_', varsIgnorePattern: '^_' }],
      'no-console': ['warn', { allow: ['warn', 'error', 'info'] }],
    },
  },
  {
    files: ['specs/**/*.ts', 'src/fixtures/**/*.ts'],
    ...playwright.configs['flat/recommended'],
    rules: {
      ...playwright.configs['flat/recommended'].rules,
      // Every test must assert. The a11y specs assert via a custom wrapper, so it is
      // whitelisted here; otherwise the rule would flag them as assertion-less.
      'playwright/expect-expect': ['warn', { assertFunctionNames: ['assertNoSeriousA11yViolations'] }],
      'playwright/require-top-level-describe': 'warn',
      'playwright/no-skipped-test': 'warn',
      'playwright/no-force-option': 'error',
      'playwright/no-wait-for-timeout': 'error',
    },
  },
  prettier,
);
