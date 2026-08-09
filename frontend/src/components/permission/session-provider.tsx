'use client';

import { createContext, useContext, useMemo, type ReactNode } from 'react';

import type { CurrentUser } from '@/lib/api/types';

const SessionContext = createContext<CurrentUser | null>(null);

/**
 * Carries the signed-in user from the server layout into client components.
 *
 * Fetched once per navigation on the server rather than by every component that
 * needs it, so a screen with a dozen permission checks makes zero extra requests.
 */
export function SessionProvider({ user, children }: { user: CurrentUser; children: ReactNode }) {
  return <SessionContext.Provider value={user}>{children}</SessionContext.Provider>;
}

/** Throws outside the provider, which can only mean a component escaped the app shell. */
export function useSession(): CurrentUser {
  const user = useContext(SessionContext);

  if (!user) {
    throw new Error('useSession must be used inside the app shell, which supplies SessionProvider.');
  }

  return user;
}

/**
 * Permission checks for deciding what to render.
 *
 * Emphatically not an authorization mechanism. Hiding a button stops an honest
 * user from clicking something that would fail; it stops nobody who opens the
 * developer tools. The server re-checks every endpoint against its declared
 * permission, and that is the only check that counts. The legacy system had this
 * exactly backwards — the JavaScript check was the *only* one, so the entire
 * permission model could be removed from the browser console.
 */
export function usePermissions() {
  const user = useSession();

  const granted = useMemo(
    () => new Set(user.permissions),
    [user.permissions],
  );

  const isSuperAdministrator = user.isSuperAdministrator;

  return useMemo(
    () => ({
      /** True when the user holds the permission. Undefined means "no permission needed". */
      can: (permission?: string) =>
        permission === undefined || isSuperAdministrator || granted.has(permission),
      canAny: (permissions: string[]) =>
        isSuperAdministrator || permissions.some((permission) => granted.has(permission)),
      all: granted,
      /**
       * Checked here as well as relying on the expanded claim list, so a screen for a
       * module whose permissions were added after this session started still renders
       * for a super administrator rather than silently disappearing from their menu.
       */
      isSuperAdministrator,
    }),
    [granted, isSuperAdministrator],
  );
}
