import type { NextRequest } from 'next/server';

import { API_BASE_URL, SESSION_COOKIE } from '@/lib/api/server';

/**
 * Backend-for-frontend proxy. Every browser call to the API passes through here.
 *
 * Why this exists rather than letting the browser call the API directly:
 *
 *  - The session cookie is HttpOnly and scoped to this origin. No token is ever
 *    readable from JavaScript, so an XSS bug cannot exfiltrate a session.
 *  - Browser and API share an origin, so there is no CORS configuration to get
 *    wrong. The legacy system shipped a wildcard `localhost:4200` CORS policy
 *    that was applied unconditionally in production.
 *  - Request headers are allow-listed, not forwarded wholesale, so nothing the
 *    browser can set leaks into an upstream trust decision.
 *
 * The extra hop costs a few milliseconds on a LAN, which is a good trade for
 * removing a whole class of vulnerability from an internal application.
 */

/** Request headers permitted upstream. Everything else is dropped. */
const FORWARDED_REQUEST_HEADERS = ['content-type', 'accept', 'accept-language', 'idempotency-key'];

/** Response headers permitted back to the browser. */
const FORWARDED_RESPONSE_HEADERS = ['content-type', 'cache-control', 'location'];

type RouteContext = { params: Promise<{ path: string[] }> };

async function proxy(request: NextRequest, context: RouteContext): Promise<Response> {
  const { path } = await context.params;

  const target = new URL(`/api/${path.join('/')}`, API_BASE_URL);
  target.search = request.nextUrl.search;

  const headers = new Headers();

  for (const name of FORWARDED_REQUEST_HEADERS) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }

  const session = request.cookies.get(SESSION_COOKIE);
  if (session) {
    headers.set('cookie', `${SESSION_COOKIE}=${session.value}`);
  }

  const hasBody = request.method !== 'GET' && request.method !== 'HEAD';

  let upstream: Response;
  try {
    upstream = await fetch(target, {
      method: request.method,
      headers,
      body: hasBody ? await request.text() : undefined,
      // Never follow a redirect on the server: an upstream 302 is a signal for
      // the client to act on, not something to resolve invisibly.
      redirect: 'manual',
      cache: 'no-store',
    });
  } catch {
    // The API being unreachable is a gateway problem, and it is reported as one.
    // Returning 200 with an error payload is exactly what made the legacy system
    // impossible to monitor.
    return Response.json(
      {
        type: 'https://problems.erp/unexpected',
        title: 'Service unavailable',
        detail: 'The API could not be reached.',
        status: 502,
      },
      { status: 502, headers: { 'content-type': 'application/problem+json' } },
    );
  }

  const responseHeaders = new Headers();

  for (const name of FORWARDED_RESPONSE_HEADERS) {
    const value = upstream.headers.get(name);
    if (value) responseHeaders.set(name, value);
  }

  // Pass the API's own Set-Cookie through on sign-in and sign-out so the browser
  // stores the session under this origin, still HttpOnly.
  for (const cookie of upstream.headers.getSetCookie()) {
    responseHeaders.append('set-cookie', cookie);
  }

  return new Response(upstream.body, {
    status: upstream.status,
    statusText: upstream.statusText,
    headers: responseHeaders,
  });
}

export const GET = proxy;
export const POST = proxy;
export const PUT = proxy;
export const PATCH = proxy;
export const DELETE = proxy;

/** Session state is per-request; nothing here may be statically rendered. */
export const dynamic = 'force-dynamic';
