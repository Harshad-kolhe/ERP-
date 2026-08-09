import Link from 'next/link';
import type { ReactNode } from 'react';

import { BrandMark } from '@/components/shell/brand-mark';
import { ThemeToggle } from '@/components/theme-toggle';

/**
 * Shared shell for every auth screen. Login, register and forgot-password all
 * render inside this, so the three stay identical without duplicated markup.
 *
 * The theme toggle lives here too: a sign-in screen that ignores the user's chosen
 * theme and then switches the moment they sign in reads as a glitch.
 */
export function AuthFrame({
  heading,
  subheading,
  children,
  footer,
}: {
  heading: string;
  subheading: string;
  children: ReactNode;
  footer?: ReactNode;
}) {
  return (
    <div className="auth-canvas relative flex min-h-svh flex-col items-center justify-center gap-6 overflow-hidden p-6">
      <main className="relative z-10 w-full max-w-sm">
        <div className="bg-card/85 rounded-xl border p-8 shadow-lg backdrop-blur-sm">
          <header className="mb-6 space-y-1.5">
            <Link
              href="/"
              className="text-primary mb-4 flex items-center gap-2 text-sm font-medium hover:underline"
            >
              <BrandMark />
              ERP
            </Link>
            <h1 className="text-2xl font-semibold tracking-tight">{heading}</h1>
            <p className="text-muted-foreground text-sm">{subheading}</p>
          </header>

          {children}
        </div>

        {footer ? <p className="text-muted-foreground mt-6 text-center text-sm">{footer}</p> : null}
      </main>

      {/* Visually top-right, but last in the DOM on purpose. Placed first it was
          the first thing Tab reached on the sign-in screen — a theme switcher
          standing between the user and the email field. Order and position are
          allowed to disagree in this direction for a decorative control. */}
      <div className="absolute top-4 right-4 z-10">
        <ThemeToggle />
      </div>
    </div>
  );
}
