/**
 * Server-only constants shared by the BFF proxy and the session reader.
 *
 * `ERP_API_BASE_URL` is deliberately not prefixed `NEXT_PUBLIC_`: the browser
 * never addresses the API directly, so its location is not something the client
 * needs — or should — know.
 */
export const API_BASE_URL = process.env.ERP_API_BASE_URL ?? 'http://localhost:5080';

/** Must match `options.Cookie.Name` in the API's `AddErpAuthentication`. */
export const SESSION_COOKIE = 'erp.session';
