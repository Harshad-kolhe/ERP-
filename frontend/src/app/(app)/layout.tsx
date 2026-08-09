import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';
import type { ReactNode } from 'react';

import { SessionProvider } from '@/components/permission/session-provider';
import { Sidebar } from '@/components/shell/sidebar';
import { SIDEBAR_COOKIE } from '@/components/shell/sidebar-preference';
import { Topbar } from '@/components/shell/topbar';
import { getSession } from '@/lib/auth/session';

/**
 * The app shell, and the gate in front of it.
 *
 * The session is resolved once here and handed down, so the dozens of permission
 * checks a screen performs cost nothing extra. Note this is a convenience gate,
 * not the security boundary — the API rejects an unauthenticated or unauthorised
 * request on its own, and would do so even if this check were deleted.
 */
export default async function AppLayout({ children }: { children: ReactNode }) {
  const user = await getSession();

  if (!user) {
    redirect('/login');
  }

  // Read here rather than inside the sidebar so the first HTML already has the
  // chosen width; a client-side read would paint wide and then snap narrow.
  const collapsed = (await cookies()).get(SIDEBAR_COOKIE)?.value === '1';

  return (
    <SessionProvider user={user}>
      {/* Past ~20 nav links and a top bar before reaching the page. Visually hidden
          until focused, which is the first thing a keyboard user meets. */}
      <a
        href="#main"
        className="bg-card focus:ring-ring sr-only focus:not-sr-only focus:fixed focus:top-2 focus:left-2 focus:z-50 focus:rounded-md focus:px-3 focus:py-2 focus:text-sm focus:font-medium focus:shadow-md focus:ring-2 focus:outline-none"
      >
        Skip to content
      </a>

      <div className="flex h-svh overflow-hidden">
        <Sidebar defaultCollapsed={collapsed} />
        <div className="flex min-w-0 flex-1 flex-col">
          <Topbar />
          <main id="main" className="min-h-0 flex-1 overflow-y-auto">
            {children}
          </main>
        </div>
      </div>
    </SessionProvider>
  );
}
