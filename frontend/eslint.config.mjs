import { defineConfig, globalIgnores } from 'eslint/config';
import nextVitals from 'eslint-config-next/core-web-vitals';
import nextTypeScript from 'eslint-config-next/typescript';

/**
 * ESLint flat config.
 *
 * This file exists because Next 16 **removed** `next lint`. The `lint` script
 * still said `next lint`, which Next 16 reads as a request to lint a directory
 * called `lint` — so it failed with "Invalid project directory provided, no such
 * directory: frontend/lint". The CI Lint step had never run before the repository
 * got its first commit, so the breakage was invisible until then.
 *
 * Flat config rather than `.eslintrc`: `@next/eslint-plugin-next` defaults to it
 * now, and ESLint v10 drops legacy config support entirely.
 *
 * `core-web-vitals` promotes the rules that affect Core Web Vitals from warnings
 * to errors, and `typescript` adds the typescript-eslint rules. Both are the
 * configurations Next ships; nothing is hand-rolled here, so upgrading Next
 * upgrades the rule set.
 */
const eslintConfig = defineConfig([
  ...nextVitals,
  ...nextTypeScript,

  // Overrides eslint-config-next's own defaults, which do not cover build output
  // from a workspace root or the generated client.
  globalIgnores([
    '.next/**',
    'out/**',
    'build/**',
    'next-env.d.ts',
    'node_modules/**',

    // Written by orval from the API's OpenAPI document. Linting generated code
    // reports problems nobody can fix in the file they appear in.
    'src/lib/api/generated/**',
  ]),
]);

export default eslintConfig;
