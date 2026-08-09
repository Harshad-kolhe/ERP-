'use client';

import Link from 'next/link';

import { Skeleton } from '@/components/ui/skeleton';

import { usePartCounts } from './use-dashboard';

/** Part master by lifecycle state. Every row opens the list filtered to that state. */
export function PartStatusCard() {
  const { counts, total, isLoading } = usePartCounts();

  return (
    <section className="bg-card rounded-md border">
      <header className="flex items-center gap-2 border-b px-4 py-2.5">
        <h2 className="text-muted-foreground font-mono text-[10.5px] font-semibold tracking-[0.09em] uppercase">
          Part master
        </h2>
        <span className="text-muted-foreground ml-auto font-mono text-[11px]">
          {isLoading ? '—' : `${total} total`}
        </span>
      </header>

      <div className="divide-y">
        {counts.map((entry) => (
          <Link
            key={entry.status}
            href={entry.href}
            className="hover:bg-accent/50 flex items-center gap-3 px-4 py-2.5 transition-colors"
          >
            <span className="text-[13px]">{entry.label}</span>
            {isLoading ? (
              <Skeleton className="ml-auto h-4 w-8" />
            ) : (
              <span className="ml-auto font-mono text-[13px] tabular-nums">{entry.count}</span>
            )}
          </Link>
        ))}
      </div>

      {!isLoading && total === 0 ? (
        <p className="text-muted-foreground border-t px-4 py-3 text-[13px]">
          No parts yet. They arrive with the part create screen, or the Excel import.
        </p>
      ) : null}
    </section>
  );
}
