'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useCallback, useEffect, useState, type ReactNode } from 'react';

import { usePermissions } from '@/components/permission/session-provider';
import { NAV, type NavItem } from '@/config/nav';
import { APP_HOME } from '@/lib/routes';
import { cn } from '@/lib/utils';

import { BrandMark } from './brand-mark';
import { NavPending } from './nav-pending';
import { SIDEBAR_COOKIE, SIDEBAR_COOKIE_MAX_AGE } from './sidebar-preference';

/**
 * The navigation rail.
 *
 * Collapsing narrows it to icons rather than hiding it: every destination stays
 * one click away, only the words go. That matters on the screens this system
 * exists for — a bill of materials or a stock ledger is wide, and the 168px back
 * is a column of data nobody has to scroll sideways to read.
 *
 * `defaultCollapsed` is the cookie the server already read, so the first paint is
 * the right width. See {@link SIDEBAR_COOKIE}.
 */
export function Sidebar({ defaultCollapsed = false }: { defaultCollapsed?: boolean }) {
  const pathname = usePathname();
  const { can } = usePermissions();
  const [collapsed, setCollapsed] = useState(defaultCollapsed);

  const toggle = useCallback(() => {
    const next = !collapsed;
    setCollapsed(next);
    document.cookie = `${SIDEBAR_COOKIE}=${next ? '1' : '0'}; path=/; max-age=${SIDEBAR_COOKIE_MAX_AGE}; samesite=lax`;
  }, [collapsed]);

  // ⌘B / Ctrl+B — the shortcut every editor already uses for the same thing.
  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if ((event.metaKey || event.ctrlKey) && !event.altKey && event.key.toLowerCase() === 'b') {
        event.preventDefault();
        toggle();
      }
    }

    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [toggle]);

  // A group with nothing visible in it renders no heading either, so a restricted
  // user sees a shorter menu rather than a menu full of empty sections.
  const groups = NAV.map((group) => ({
    ...group,
    items: group.items.filter((item) => can(item.permission)),
  })).filter((group) => group.items.length > 0);

  return (
    <div
      className={cn(
        'bg-card flex shrink-0 flex-col border-r transition-[width] duration-200 ease-out motion-reduce:transition-none',
        collapsed ? 'w-14' : 'w-56',
      )}
    >
      {/*
        Brand and toggle share the header row.

        The toggle used to be absolutely positioned on the seam, vertically centred
        — which put it on top of whichever nav link happened to sit at the midpoint,
        stealing clicks meant for that destination. Here it is in normal flow, still
        in the same place in both states, and over nothing.

        Same height and rule as the top bar, so the two halves of the shell read as
        one header line across the top of the screen.
      */}
      <div
        className={cn(
          'flex h-12 shrink-0 items-center border-b',
          collapsed ? 'justify-center' : 'gap-2 px-3',
        )}
      >
        {!collapsed && (
          <Link
            href={APP_HOME}
            className="flex min-w-0 items-center gap-2 text-sm font-semibold tracking-tight"
          >
            <BrandMark />
            <span className="whitespace-nowrap">ERP</span>
          </Link>
        )}

        {!collapsed && <span className="flex-1" />}

        <button
          type="button"
          onClick={toggle}
          aria-expanded={!collapsed}
          aria-controls="main-nav"
          aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
          title={`${collapsed ? 'Expand' : 'Collapse'} sidebar (Ctrl or ⌘ + B)`}
          className="text-muted-foreground hover:text-primary hover:bg-accent focus-visible:ring-ring/50 flex size-7 shrink-0 items-center justify-center rounded-md transition-colors focus-visible:ring-2 focus-visible:outline-none"
        >
          {collapsed ? (
            <ChevronRight className="size-4" aria-hidden />
          ) : (
            <ChevronLeft className="size-4" aria-hidden />
          )}
        </button>
      </div>

      {/* Collapsed, the mark moves below the toggle rather than competing with it
          for a 56px row — and stays a link home, which is the only navigation the
          header itself offers. */}
      {collapsed && (
        <Link
          href={APP_HOME}
          title="ERP — home"
          className="flex h-10 shrink-0 items-center justify-center border-b"
        >
          <BrandMark />
          <span className="sr-only">ERP — home</span>
        </Link>
      )}

      <nav id="main-nav" aria-label="Main" className="flex-1 overflow-y-auto py-2">
        {groups.map((group, index) => (
          <div key={group.label} className="mb-1">
            {/* Collapsed, the heading stays for screen readers and becomes a rule for
                everyone else: grouping is the only structure left once the words go.
                The first group needs none — the header's rule already sits above it. */}
            {collapsed && index > 0 && <div className="border-border/70 mx-3 mt-3 mb-1.5 border-t" />}
            <p
              className={cn(
                'text-muted-foreground px-4 pt-3 pb-1.5 font-mono text-[10px] tracking-[0.13em] whitespace-nowrap uppercase',
                collapsed && 'sr-only',
              )}
            >
              {group.label}
            </p>
            {group.items.map((item) => (
              <SidebarLink
                key={item.href}
                item={item}
                active={isActive(pathname, item.href)}
                collapsed={collapsed}
              />
            ))}
          </div>
        ))}
      </nav>
    </div>
  );
}

function SidebarLink({
  item,
  active,
  collapsed,
}: {
  item: NavItem;
  active: boolean;
  collapsed: boolean;
}) {
  const Icon = item.icon;

  const shared = cn(
    'group/nav relative flex items-center gap-2.5 py-1.5 text-[13px]',
    collapsed ? 'justify-center px-0' : 'px-4',
  );

  // A planned item is shown but inert: it communicates where a screen will live
  // without offering a link to a page that does not exist.
  if (item.status === 'planned') {
    return (
      <span
        className={cn(shared, 'text-muted-foreground/55 cursor-default')}
        aria-disabled="true"
      >
        <Icon className="size-4 shrink-0 opacity-60" aria-hidden />
        <span className={cn('whitespace-nowrap', collapsed && 'sr-only')}>{item.label}</span>
        {collapsed && <CollapsedTip>{item.label} — not built yet</CollapsedTip>}
        {!collapsed && (
          <span className="border-border text-muted-foreground/70 ml-auto rounded border px-1 font-mono text-[9px] leading-[13px]">
            soon
          </span>
        )}
      </span>
    );
  }

  return (
    <Link
      href={item.href}
      aria-current={active ? 'page' : undefined}
      className={cn(
        shared,
        'hover:text-foreground hover:bg-accent/60 text-muted-foreground transition-colors',
        active &&
          'text-primary bg-primary/10 hover:bg-primary/10 hover:text-primary font-medium before:bg-primary before:absolute before:inset-y-1 before:left-0 before:w-0.5 before:content-[""]',
      )}
    >
      <Icon className="size-4 shrink-0 opacity-75" aria-hidden />
      <span className={cn('whitespace-nowrap', collapsed && 'sr-only')}>{item.label}</span>
      {!collapsed && <span className="flex-1" />}
      <NavPending />
      {collapsed && <CollapsedTip>{item.label}</CollapsedTip>}
    </Link>
  );
}

/**
 * The label for a collapsed rail, as a real element rather than a `title`.
 *
 * `title` has a delay of roughly a second and a half, never appears for a
 * keyboard user, and cannot be styled — on a rail where the label is the only way
 * to tell two icons apart, that is the whole affordance behind an attribute most
 * people never see. This shows on hover *and* on focus, immediately.
 *
 * `sr-only` stays on the label itself, so screen readers still read the name from
 * the link and this is purely visual.
 */
function CollapsedTip({ children }: { children: ReactNode }) {
  return (
    <span
      aria-hidden
      className="bg-popover text-popover-foreground border-border pointer-events-none absolute left-full z-50 ml-1 hidden rounded-md border px-2 py-1 text-xs whitespace-nowrap shadow-md group-hover/nav:block group-focus-visible/nav:block"
    >
      {children}
    </span>
  );
}

/** Home matches exactly; every other section matches its subtree. */
export function isActive(pathname: string, href: string): boolean {
  return href === APP_HOME
    ? pathname === href
    : pathname === href || pathname.startsWith(`${href}/`);
}
