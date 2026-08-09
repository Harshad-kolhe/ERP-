import type { ReactNode } from 'react';

import type { Availability } from '@/config/project';
import { cn } from '@/lib/utils';

/**
 * Layout pieces for the landing page.
 *
 * They exist so the page reads as content rather than as markup: five sections
 * each setting their own spacing and heading styles would drift apart within a
 * week. Nothing here holds copy — the words live in `@/config/project`.
 */

/** One numbered section, with a ruled heading and an optional lede. */
export function Section({
  id,
  index,
  title,
  lede,
  children,
}: {
  id: string;
  index: string;
  title: string;
  lede?: ReactNode;
  children: ReactNode;
}) {
  return (
    <section id={id} className="scroll-mt-16 border-t py-12 sm:py-16">
      <header className="mb-7">
        <p className="text-muted-foreground mb-2 font-mono text-[11px] tracking-[0.18em] uppercase">
          {index}
        </p>
        <h2 className="text-xl font-semibold tracking-tight sm:text-2xl">{title}</h2>
        {lede ? (
          <p className="text-muted-foreground mt-2.5 max-w-3xl text-[13.5px] leading-relaxed">
            {lede}
          </p>
        ) : null}
      </header>
      {children}
    </section>
  );
}

/** A titled prose card — the shape most of this page's explanatory content takes. */
export function Card({
  eyebrow,
  title,
  children,
}: {
  eyebrow?: string;
  title: string;
  children: ReactNode;
}) {
  return (
    <div className="bg-card rounded-md border p-5">
      {eyebrow ? (
        <p className="text-muted-foreground mb-1.5 font-mono text-[10.5px] tracking-[0.12em] uppercase">
          {eyebrow}
        </p>
      ) : null}
      <h3 className="text-[13.5px] font-semibold">{title}</h3>
      <p className="text-muted-foreground mt-1.5 text-[13px] leading-relaxed">{children}</p>
    </div>
  );
}

const PILL_TONES: Record<Availability, string> = {
  available: 'border-primary/30 bg-primary/10 text-primary',
  building: 'border-chart-3/40 bg-chart-3/15 text-foreground',
  planned: 'border-border/70 bg-transparent text-muted-foreground',
};

const PILL_LABELS: Record<Availability, string> = {
  available: 'ready now',
  building: 'building',
  planned: 'planned',
};

/**
 * Whether a capability exists yet.
 *
 * Wording is in the reader's terms — "ready now", not "phase 1" — and matches the
 * `soon` marker the sidebar puts on a screen that is not built, so the two
 * surfaces never disagree about what is available.
 */
export function StatusPill({ status }: { status: Availability }) {
  return (
    <span
      className={cn(
        'rounded border px-1.5 py-0.5 font-mono text-[10px] tracking-wide whitespace-nowrap',
        PILL_TONES[status],
      )}
    >
      {PILL_LABELS[status]}
    </span>
  );
}
