import { Plus } from 'lucide-react';
import Link from 'next/link';
import { Suspense, type ReactNode } from 'react';

import { Can } from '@/components/permission/can';
import { Button } from '@/components/ui/button';
import { Skeleton } from '@/components/ui/skeleton';

import { MasterPageHeader, type MasterIconName, type MasterStat } from './master-page-header';

/**
 * The page body every master list screen shares.
 *
 * Six routes had this as a copy-pasted file that differed only in an icon, a
 * title, a resource, two stats and a permission — the same six values the
 * assembly-node screens had already reduced to data. Copying it meant six places
 * to fix the missing focus ring on the create button, and six chances for the
 * next master to land with a seventh variation.
 *
 * A server component, so the routes that use it keep exporting `metadata`. The
 * grid arrives as `children` rather than through a registry: each master's table
 * is its own client component, and a server component can render one it was
 * handed. That is the whole reason this can be shared without a lookup table.
 */
export function MasterListScreen({
  icon,
  title,
  resource,
  noun,
  createPermission,
  stats,
  children,
}: {
  icon: MasterIconName;
  /** The screen's name, e.g. "Customer Master". */
  title: string;
  /** Path segment under `/masters`, used to count and to build the create link. */
  resource: string;
  /** Singular, title case — "Customer" gives "New Customer". */
  noun: string;
  createPermission: string;
  stats: MasterStat[];
  /** This master's grid. */
  children: ReactNode;
}) {
  return (
    <div className="flex h-full min-h-0 flex-col">
      {/* The header counts through the API, so it needs a boundary during
          prerender. The fallback is the band's real height, which is what keeps
          the grid below it from jumping when the counts land. */}
      <Suspense fallback={<div className="border-border h-[69px] shrink-0 border-b" />}>
        <MasterPageHeader
          icon={icon}
          title={title}
          resource={resource}
          stats={stats}
          actions={
            <Can permission={createPermission}>
              {/* `Button asChild` rather than a hand-styled link: the six copies of
                  that link all omitted the focus-visible ring this variant carries,
                  so the primary action on every master list was invisible to a
                  keyboard. */}
              <Button size="sm" asChild>
                <Link href={`/masters/${resource}/new`}>
                  <Plus className="size-4" aria-hidden />
                  New {noun}
                </Link>
              </Button>
            </Can>
          }
        />
      </Suspense>

      <div className="flex min-h-0 flex-1 flex-col p-4">
        {/* useSearchParams needs a Suspense boundary during prerender. */}
        <Suspense fallback={<GridSkeleton />}>{children}</Suspense>
      </div>
    </div>
  );
}

/**
 * Holds the grid's space while it resolves.
 *
 * The fallback used to be the word "Loading…" on one line, which collapses the
 * page to nothing and then shoves it back — the layout shift is the cost of a
 * fallback that is not the shape of the thing it stands in for.
 */
export function GridSkeleton() {
  return (
    <div className="border-border min-h-0 flex-1 overflow-hidden rounded-2xl border">
      <Skeleton className="h-full w-full rounded-none" />
      <span className="sr-only">Loading records…</span>
    </div>
  );
}
