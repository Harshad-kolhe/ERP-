import { defineConfig } from 'orval';

/**
 * Generates the TypeScript view of the API's contract from the OpenAPI document
 * the backend build emits into `contracts/openapi.json`.
 *
 * Types only — no client, no hooks. The app already has one way of talking to the
 * API (`apiFetch`, which routes every call through the BFF so the browser never
 * holds a token), and a generated client would be a second one with different
 * rules about credentials and error shapes. What was missing was never the fetch
 * code; it was a machine-checked answer to "does the server still return what the
 * client thinks it does".
 *
 * The output is committed, and CI regenerates it and fails on any diff. So a
 * change to a C# DTO that nobody reflected in the web app stops being something
 * discovered at runtime.
 */
export default defineConfig({
  erp: {
    input: {
      target: '../contracts/openapi.json',
    },
    output: {
      // orval requires a target even when the models are the point, so the client
      // it emits alongside them is a by-product. It is generated against the same
      // BFF-relative base URL the hand-written `apiFetch` uses, so adopting it
      // later is a change of import rather than a change of architecture.
      target: 'src/lib/api/generated/erp.ts',
      mode: 'single',
      client: 'fetch',
      baseUrl: '/api/v1',
      clean: true,
      prettier: false,

      // Deliberately no `schemas` folder. Pointing one at this API emits ~700
      // one-type files, and a contract gate whose failure mode is a 700-file diff
      // is a gate people learn to rubber-stamp. Everything lands in one file that
      // can actually be read in a pull request.
    },
  },
});
