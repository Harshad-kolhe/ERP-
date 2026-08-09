'use client';

import { AlertTriangle } from 'lucide-react';
import { useEffect } from 'react';

import { Button } from '@/components/ui/button';

/**
 * What a signed-in screen shows when it throws.
 *
 * Inside `(app)` rather than at the app root on purpose: the boundary replaces
 * only what it wraps, so the sidebar and the top bar survive and the user is
 * still somewhere rather than staring at a bare page. Without this file the
 * whole shell was replaced by Next's default, which in production is a blank
 * screen.
 *
 * One boundary for every screen rather than one per route. A per-route file is
 * scaffolding for a difference that does not exist yet — every screen here fails
 * the same way, by a request throwing.
 */
export default function AppError({
  error,
  retry,
}: {
  error: Error & { digest?: string };
  retry: () => void;
}) {
  useEffect(() => {
    // The digest is the only handle on the server-side stack, which is withheld
    // from the browser in production. Without logging it here a support request
    // has nothing to correlate against the server log.
    console.error('Screen failed to render', error);
  }, [error]);

  return (
    <div className="flex h-full flex-col items-center justify-center gap-3 p-8 text-center">
      <span className="bg-destructive/10 text-destructive flex size-11 items-center justify-center rounded-full">
        <AlertTriangle className="size-5" aria-hidden />
      </span>

      <h1 className="text-base font-semibold">This screen could not be loaded</h1>

      <p className="text-muted-foreground max-w-md text-[13px] leading-relaxed">
        Something failed while building the page. Nothing you were viewing has been changed. Trying
        again is safe.
      </p>

      {error.digest ? (
        <p className="text-ink-faint font-mono text-[11px]">Reference: {error.digest}</p>
      ) : null}

      <Button className="mt-1" onClick={() => retry()}>
        Try again
      </Button>
    </div>
  );
}
