'use client';

import Link from 'next/link';

import { Skeleton } from '@/components/ui/skeleton';
import { cn } from '@/lib/utils';

import { usePartCounts } from './use-dashboard';

/**
 * "Awaiting you" — the first thing on the dashboard, because it is the only block
 * about the viewer rather than about the business.
 *
 * Every tile is a link into the filtered list that produced the number. A figure
 * you cannot act on is a figure nobody reads after week two.
 */
export function QueueStrip() {
  const { counts, isLoading, isError } = usePartCounts();

  const pending = counts.find((entry) => entry.status === 'PendingApproval');
  const drafts = counts.find((entry) => entry.status === 'Draft');

  if (isError) {
    return (
      <p className="text-muted-foreground rounded-md border border-dashed p-4 text-sm">
        Could not read the work queue. The API may be unavailable.
      </p>
    );
  }

  return (
    <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
      <QueueTile
        label="Parts awaiting approval"
        count={pending?.count ?? 0}
        href={pending?.href ?? '#'}
        tone={pending && pending.count > 0 ? 'warn' : 'calm'}
        isLoading={isLoading}
      />
      <QueueTile
        label="Parts in draft"
        count={drafts?.count ?? 0}
        href={drafts?.href ?? '#'}
        tone="calm"
        isLoading={isLoading}
      />
      {/*
        Stated rather than shown as zero. A tile reading "0 purchase orders" implies
        there are none; the truth is that purchasing does not exist yet, and those
        are very different things to put in front of a buyer.
      */}
      <PendingTile label="Purchase orders overdue" module="Procurement" />
      <PendingTile label="Receipts awaiting QC" module="Stores" />
    </div>
  );
}

function QueueTile({
  label,
  count,
  href,
  tone,
  isLoading,
}: {
  label: string;
  count: number;
  href: string;
  tone: 'calm' | 'warn';
  isLoading: boolean;
}) {
  return (
    <Link
      href={href}
      className={cn(
        'bg-card rounded-md border border-l-[3px] p-3.5 transition-colors',
        // The hover colour is per-tone rather than one blanket rule. It used to be
        // an unconditional `hover:border-l-primary`, which meant pointing at a
        // queue that wanted clearing turned its amber warning blue — the hover
        // state erased the one thing the tile was there to say.
        tone === 'warn' && count > 0
          ? 'border-l-warning hover:border-l-warning'
          : 'border-l-primary/60 hover:border-l-primary',
      )}
    >
      <span className="text-muted-foreground block font-mono text-[10px] tracking-[0.09em] uppercase">
        {label}
      </span>
      {isLoading ? (
        <Skeleton className="mt-2 h-7 w-12" />
      ) : (
        <span className="mt-1.5 block font-mono text-2xl leading-none font-semibold tabular-nums">
          {count}
        </span>
      )}
    </Link>
  );
}

function PendingTile({ label, module }: { label: string; module: string }) {
  return (
    <div className="bg-muted/30 rounded-md border border-dashed p-3.5">
      <span className="text-muted-foreground/70 block font-mono text-[10px] tracking-[0.09em] uppercase">
        {label}
      </span>
      <span className="text-muted-foreground/60 mt-2 block text-[13px]">
        Arrives with {module}
      </span>
    </div>
  );
}
