import { redirect } from 'next/navigation';
import type { ReactNode } from 'react';

import { getSession } from '@/lib/auth/session';
import { APP_HOME } from '@/lib/routes';

/** Somebody already signed in has no business on a sign-in screen. */
export default async function AuthLayout({ children }: { children: ReactNode }) {
  if (await getSession()) {
    redirect(APP_HOME);
  }

  return children;
}
