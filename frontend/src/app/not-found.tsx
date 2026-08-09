import Link from 'next/link';

import { Button } from '@/components/ui/button';
import { APP_HOME } from '@/lib/routes';

export const metadata = { title: 'Not found · ERP' };

/**
 * A URL that does not resolve.
 *
 * At the app root rather than inside `(app)` because it has to answer for signed-
 * out visitors too — a mistyped public link should not meet a shell that assumes
 * a session.
 */
export default function NotFound() {
  return (
    <div className="flex min-h-svh flex-col items-center justify-center gap-3 p-8 text-center">
      <p className="text-muted-foreground font-mono text-[11px] tracking-[0.18em] uppercase">
        404
      </p>

      <h1 className="text-lg font-semibold tracking-tight">This page does not exist</h1>

      <p className="text-muted-foreground max-w-md text-[13px] leading-relaxed">
        The address may be mistyped, or the screen may not have been built yet — the sidebar marks
        the ones that are still to come.
      </p>

      <Button asChild className="mt-1">
        <Link href={APP_HOME}>Go to the dashboard</Link>
      </Button>
    </div>
  );
}
