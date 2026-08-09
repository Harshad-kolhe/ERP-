'use client';

/**
 * The last resort: the root layout itself failed.
 *
 * This file replaces the whole document, which means it gets **none** of the
 * app's context — not `globals.css`, not the font variables, and not the `dark`
 * class next-themes writes on `<html>`. So everything here is inline and the
 * colour scheme comes from the OS via `prefers-color-scheme`. Reaching for a
 * token or a utility class would produce an unstyled page at exactly the moment
 * the user most needs a legible one.
 *
 * `metadata` is unavailable in an error boundary, so the tab title is a `<title>`
 * element instead.
 */
export default function GlobalError({
  error,
  retry,
}: {
  error: Error & { digest?: string };
  retry: () => void;
}) {
  return (
    <html lang="en-IN">
      <body
        style={{
          margin: 0,
          minHeight: '100svh',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: '0.75rem',
          padding: '2rem',
          textAlign: 'center',
          fontFamily: 'system-ui, -apple-system, Segoe UI, sans-serif',
          background: 'Canvas',
          color: 'CanvasText',
        }}
      >
        <title>Something went wrong · ERP</title>
        {/* `color-scheme` is what makes the CSS system colours above follow the OS
            setting instead of defaulting to light. */}
        <style>{':root { color-scheme: light dark; }'}</style>

        <h1 style={{ fontSize: '1.125rem', fontWeight: 600, margin: 0 }}>Something went wrong</h1>

        <p style={{ maxWidth: '32rem', fontSize: '0.875rem', lineHeight: 1.6, opacity: 0.75 }}>
          The application failed to start. This is not something you did, and no data has been
          changed.
        </p>

        {error.digest ? (
          <p style={{ fontFamily: 'ui-monospace, monospace', fontSize: '0.6875rem', opacity: 0.6 }}>
            Reference: {error.digest}
          </p>
        ) : null}

        <button
          type="button"
          onClick={() => retry()}
          style={{
            marginTop: '0.25rem',
            padding: '0.5rem 1rem',
            fontSize: '0.875rem',
            fontWeight: 500,
            borderRadius: '0.375rem',
            border: '1px solid currentColor',
            background: 'transparent',
            color: 'inherit',
            cursor: 'pointer',
          }}
        >
          Try again
        </button>
      </body>
    </html>
  );
}
