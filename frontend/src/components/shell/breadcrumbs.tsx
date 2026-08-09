'use client';

import { ChevronRight } from 'lucide-react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';

import { NAV } from '@/config/nav';
import { APP_HOME } from '@/lib/routes';

import { isActive } from './sidebar';

/**
 * Where you are, in the empty left half of the top bar.
 *
 * The bar previously started with `ml-auto`, so the whole left side was blank and
 * `/masters/parts/{id}` told you nothing about your location — the sidebar
 * highlight is off screen when the rail is collapsed, and the form's own back
 * button is the only other clue.
 *
 * Built from `NAV`, which already maps every href to a group and a label, so this
 * adds no second source of truth about the app's structure.
 *
 * Two levels and no more. The routes here are `/section/resource[/id]`, and a
 * third crumb on that shape produces "Masters › Parts › Parts".
 */
export function Breadcrumbs() {
  const pathname = usePathname();

  if (pathname === APP_HOME) return null;

  const match = NAV.flatMap((group) => group.items.map((item) => ({ group, item }))).find(
    ({ item }) => item.status === 'ready' && isActive(pathname, item.href),
  );

  if (!match) return null;

  // Anything past the nav item's own href: `new`, or a record id. An id is not
  // worth printing — it is a guid — so the leaf is named by what the URL does.
  const rest = pathname.slice(match.item.href.length).replace(/^\//, '');
  const leaf = rest === '' ? null : rest === 'new' ? 'New' : 'Edit';

  return (
    <nav aria-label="Breadcrumb" className="min-w-0">
      <ol className="text-muted-foreground flex min-w-0 items-center gap-1 text-[13px]">
        <li className="hidden shrink-0 sm:block">{match.group.label}</li>
        <li aria-hidden className="hidden shrink-0 sm:block">
          <ChevronRight className="size-3.5 opacity-50" />
        </li>

        <li className="min-w-0 truncate">
          {leaf ? (
            <Link href={match.item.href} className="hover:text-foreground transition-colors">
              {match.item.label}
            </Link>
          ) : (
            <span className="text-foreground font-medium">{match.item.label}</span>
          )}
        </li>

        {leaf ? (
          <>
            <li aria-hidden className="shrink-0">
              <ChevronRight className="size-3.5 opacity-50" />
            </li>
            <li className="text-foreground shrink-0 font-medium">{leaf}</li>
          </>
        ) : null}
      </ol>
    </nav>
  );
}
