'use client';

import {
  Blocks,
  Building2,
  Component,
  Contact,
  IdCard,
  Layers,
  Package,
  Truck,
  Users,
  Workflow,
  type LucideIcon,
} from 'lucide-react';
import type { ReactNode } from 'react';

import { useMasterCount } from './use-master-count';

/**
 * Which icon a master's header shows, by name rather than by component.
 *
 * A name, because the list pages are server components — they export `metadata`,
 * so they cannot be anything else — and a Lucide icon is a function. React refuses
 * to send a function across that boundary, and the page renders as an error rather
 * than a header. Passing the name and resolving it here keeps the pages on the
 * server and the icon on the client, where it belongs.
 */
export type MasterIconName = keyof typeof ICONS;

const ICONS = {
  part: Package,
  supplier: Truck,
  customer: Users,
  employee: Contact,
  role: IdCard,
  businessUnit: Building2,
  parentPart: Workflow,
  section: Layers,
  assembly: Component,
  subAssembly: Blocks,
} satisfies Record<string, LucideIcon>;

/**
 * One statistic in the header band.
 *
 * `filter` is the server's filter syntax, so the number is counted by the database
 * under the same rules the grid uses — a header that computed its own totals is a
 * header that eventually disagrees with the rows underneath it.
 */
export interface MasterStat {
  label: string;
  /** Omit to count every record. */
  filter?: string;
  /** Draws attention when the number is not zero — for queues that want clearing. */
  emphasise?: boolean;
}

/**
 * The band at the top of every master list: what this screen is, how much is in
 * it, and what can be done to it.
 *
 * The counts are the reason it exists rather than a title bar. "27 parts · 0
 * awaiting approval" as a sentence reads as decoration and gets skipped; the same
 * two numbers as figures with labels under them read as a dashboard, which is what
 * somebody opening a master actually wants to know first.
 *
 * One component for every master, configured by props. A header per screen is how
 * ten masters end up with ten different ideas of where the primary action goes.
 */
export function MasterPageHeader({
  icon,
  title,
  resource,
  stats,
  actions,
}: {
  icon: MasterIconName;
  title: string;
  /** Path segment under `/masters`, used to count. */
  resource: string;
  stats: MasterStat[];
  actions?: ReactNode;
}) {
  const Icon = ICONS[icon];

  return (
    <header className="border-border from-card to-muted relative flex shrink-0 flex-wrap items-center gap-x-4 gap-y-3 border-b bg-gradient-to-r px-4 py-3">
      {/* A hairline of brand colour along the top. Enough to make the band read as
          a header rather than as the first row of the page. */}
      <span
        aria-hidden="true"
        className="from-primary/70 via-primary/30 absolute inset-x-0 top-0 h-px bg-gradient-to-r to-transparent"
      />

      <span className="from-primary to-primary/70 text-primary-foreground flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br shadow-sm">
        <Icon className="h-[18px] w-[18px]" />
      </span>

      <div className="min-w-0">
        <h1 className="text-foreground truncate text-[15px] leading-tight font-semibold tracking-tight">
          {title}
        </h1>
        <div className="mt-1 flex flex-wrap items-center gap-1.5">
          {stats.map((stat) => (
            <Stat key={stat.label} resource={resource} stat={stat} />
          ))}
        </div>
      </div>

      <div className="flex flex-1 flex-wrap items-center justify-end gap-2">{actions}</div>
    </header>
  );
}

function Stat({ resource, stat }: { resource: string; stat: MasterStat }) {
  const { count, isLoading, isError } = useMasterCount(resource, stat.filter);

  // Emphasis only when there is something to act on. A queue badge that glows at
  // zero teaches people to ignore it on the day it is not zero.
  const live = stat.emphasise && (count ?? 0) > 0;

  return (
    <span
      // The em dash means "counting" or "count failed"; the tooltip is the only
      // thing that separates the two, and a stat that never resolves should not
      // look identical to one that is about to.
      title={isError ? 'Count unavailable' : undefined}
      className={`inline-flex items-baseline gap-1.5 rounded-md border px-2 py-0.5 ${
        live
          ? 'border-warning/40 bg-warning/10'
          : 'border-border bg-muted/60'
      }`}
    >
      <span
        className={`text-[13px] leading-none font-semibold tabular-nums ${
          live ? 'text-warning-foreground' : 'text-foreground'
        }`}
      >
        {/* An em dash while counting, never 0 — zero is a fact about the data and
            would send somebody looking for records that are simply not counted yet. */}
        {isLoading || count === null ? '—' : count.toLocaleString('en-IN')}
      </span>
      <span className="text-ink-faint text-[11px] leading-none">{stat.label}</span>
    </span>
  );
}
