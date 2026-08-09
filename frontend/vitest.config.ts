import { defineConfig } from 'vitest/config';

// Node, not jsdom: what is tested here is the query-string translation, which is
// pure. A DOM environment would be setup for components nothing tests yet.
export default defineConfig({
  test: { environment: 'node', include: ['src/**/*.test.ts'] },
});
