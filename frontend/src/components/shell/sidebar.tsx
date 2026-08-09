'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

import { usePermissions } from '@/components/permission/session-provider';
import { NAV, type NavItem } from '@/config/nav';
import { APP_HOME } from '@/lib/routes';
import { cn } from '@/lib/utils';

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
        'bg-card relative flex shrink-0 flex-col border-r transition-[width] duration-200 ease-out motion-reduce:transition-none',
        collapsed ? 'w-14' : 'w-56',
      )}
    >
      {/* Same height and rule as the top bar, so the two halves of the shell read as
          one header line across the top of the screen. */}
      <Link
        href={APP_HOME}
        title={collapsed ? 'ERP — home' : undefined}
        className={cn(
          'flex h-12 shrink-0 items-center gap-2 border-b text-sm font-semibold tracking-tight',
          collapsed ? 'justify-center' : 'px-4',
        )}
      >
        <span className="bg-primary size-2 shrink-0 rounded-full" />
        <span className={cn('whitespace-nowrap', collapsed && 'sr-only')}>ERP</span>
      </Link>

      {/* The toggle straddles the divider, halfway down. Sitting on the seam it is the
          same target in both states — it does not move when the rail narrows, so the
          way back out is exactly where the way in was. */}
      <button
        type="button"
        onClick={toggle}
        aria-expanded={!collapsed}
        aria-controls="main-nav"
        aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        title={`${collapsed ? 'Expand' : 'Collapse'} sidebar (Ctrl or ⌘ + B)`}
        className="bg-card border-border text-muted-foreground hover:text-primary hover:border-primary/40 focus-visible:ring-ring/50 absolute top-1/2 right-0 z-20 flex size-6 -translate-y-1/2 translate-x-1/2 items-center justify-center rounded-full border shadow-sm transition-colors focus-visible:ring-2 focus-visible:outline-none"
      >
        {collapsed ? (
          <ChevronRight className="size-3.5" aria-hidden />
        ) : (
          <ChevronLeft className="size-3.5" aria-hidden />
        )}
      </button>

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
    'relative flex items-center gap-2.5 py-1.5 text-[13px]',
    collapsed ? 'justify-center px-0' : 'px-4',
  );

  // A planned item is shown but inert: it communicates where a screen will live
  // without offering a link to a page that does not exist.
  if (item.status === 'planned') {
    return (
      <span
        className={cn(shared, 'text-muted-foreground/55 cursor-default')}
        title={collapsed ? `${item.label} — not built yet` : 'Not built yet'}
        aria-disabled="true"
      >
        <Icon className="size-4 shrink-0 opacity-60" aria-hidden />
        <span className={cn('whitespace-nowrap', collapsed && 'sr-only')}>{item.label}</span>
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
      title={collapsed ? item.label : undefined}
      className={cn(
        shared,
        'hover:text-foreground hover:bg-accent/60 text-muted-foreground transition-colors',
        active &&
          'text-primary bg-primary/10 hover:bg-primary/10 hover:text-primary font-medium before:bg-primary before:absolute before:inset-y-1 before:left-0 before:w-0.5 before:content-[""]',
      )}
    >
      <Icon className="size-4 shrink-0 opacity-75" aria-hidden />
      <span className={cn('whitespace-nowrap', collapsed && 'sr-only')}>{item.label}</span>
    </Link>
  );
}

/** Home matches exactly; every other section matches its subtree. */
function isActive(pathname: string, href: string): boolean {
  return href === APP_HOME
    ? pathname === href
    : pathname === href || pathname.startsWith(`${href}/`);
}
