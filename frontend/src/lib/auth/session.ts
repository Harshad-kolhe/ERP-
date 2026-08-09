import { cookies } from 'next/headers';

import { API_BASE_URL, SESSION_COOKIE } from '@/lib/api/server';
import type { CurrentUser } from '@/lib/api/types';

/**
 * Reads the signed-in user on the server, or `null`.
 *
 * The cookie's presence is checked first only to skip a pointless round trip; it
 * is never treated as proof of anything. The API is what decides whether the
 * session is valid, because the cookie is encrypted and this process cannot — and
 * must not be able to — read it.
 */
export async function getSession(): Promise<CurrentUser | null> {
  const jar = await cookies();
  const session = jar.get(SESSION_COOKIE);

  if (!session) {
    return null;
  }

  try {
    const response = await fetch(new URL('/api/v1/auth/me', API_BASE_URL), {
      headers: {
        cookie: `${SESSION_COOKIE}=${session.value}`,
        accept: 'application/json',
      },
      cache: 'no-store',
    });

    return response.ok ? ((await response.json()) as CurrentUser) : null;
  } catch {
    // The API being unreachable is not the same as being signed out, but from a
    // page's point of view there is nothing to render either way.
    return null;
  }
}
