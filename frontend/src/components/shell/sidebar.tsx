'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

import { usePermissions } from '@/components/permission/session-provider';
import { NAV, type NavItem } from '@/config/nav';
import { APP_HOME } from '@/lib/routes';
import { cn } from '@/lib/utils';

export function Sidebar() {
  const pathname = usePathname();
  const { can } = usePermissions();

  // A group with nothing visible in it renders no heading either, so a restricted
  // user sees a shorter menu rather than a menu full of empty sections.
  const groups = NAV.map((group) => ({
    ...group,
    items: group.items.filter((item) => can(item.permission)),
  })).filter((group) => group.items.length > 0);

  return (
    <nav
      aria-label="Main"
      className="bg-card flex w-56 shrink-0 flex-col gap-1 overflow-y-auto border-r py-3"
    >
      <Link
        href={APP_HOME}
        className="mb-2 flex items-center gap-2 px-4 py-1 text-sm font-semibold tracking-tight"
      >
        <span className="bg-primary size-2 rounded-full" />
        ERP
      </Link>

      {groups.map((group) => (
        <div key={group.label} className="mb-1">
          <p className="text-muted-foreground px-4 pt-3 pb-1.5 font-mono text-[10px] tracking-[0.13em] uppercase">
            {group.label}
          </p>
          {group.items.map((item) => (
            <SidebarLink key={item.href} item={item} active={isActive(pathname, item.href)} />
          ))}
        </div>
      ))}
    </nav>
  );
}

function SidebarLink({ item, active }: { item: NavItem; active: boolean }) {
  const Icon = item.icon;

  const shared = 'relative flex items-center gap-2.5 px-4 py-1.5 text-[13px]';

  // A planned item is shown but inert: it communicates where a screen will live
  // without offering a link to a page that does not exist.
  if (item.status === 'planned') {
    return (
      <span
        className={cn(shared, 'text-muted-foreground/55 cursor-default')}
        title="Not built yet"
        aria-disabled="true"
      >
        <Icon className="size-4 shrink-0 opacity-60" aria-hidden />
        {item.label}
        <span className="border-border text-muted-foreground/70 ml-auto rounded border px-1 font-mono text-[9px] leading-[13px]">
          soon
        </span>
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
      {item.label}
    </Link>
  );
}

/** Home matches exactly; every other section matches its subtree. */
function isActive(pathname: string, href: string): boolean {
  return href === APP_HOME
    ? pathname === href
    : pathname === href || pathname.startsWith(`${href}/`);
}
