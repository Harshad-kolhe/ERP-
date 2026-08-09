'use client';

import { Building2, Search } from 'lucide-react';

import { useSession } from '@/components/permission/session-provider';
import { ThemeToggle } from '@/components/theme-toggle';

import { UserMenu } from './user-menu';

export function Topbar() {
  const user = useSession();

  return (
    <header className="bg-card flex h-12 shrink-0 items-center gap-2.5 border-b px-4">
      {/* Placeholder for the command palette. Rendered disabled rather than omitted,
          because the keyboard hint teaches the shortcut before the feature lands. */}
      <button
        type="button"
        disabled
        title="Coming with the command palette"
        className="text-muted-foreground/70 bg-background hover:border-border flex h-7 w-full max-w-sm cursor-default items-center gap-2 rounded-md border px-2.5 text-left text-[12.5px]"
      >
        <Search className="size-3.5" aria-hidden />
        Search parts, orders, machines…
        <kbd className="border-border text-muted-foreground/70 ml-auto rounded border px-1 font-mono text-[10px] leading-4">
          ⌘K
        </kbd>
      </button>

      {/*
        The business unit scopes every query below it, enforced by a database query
        filter that no handler can skip. It is shown permanently because scope that
        is applied invisibly is scope people forget they are inside — which is how
        the legacy `.ApplyBu()` helper leaked between units for years.

        Read-only for now: switching needs the Business Unit master, and a dropdown
        that cannot actually change anything is worse than a label that is honest.
      */}
      <span
        className="border-border text-muted-foreground ml-auto flex shrink-0 items-center gap-1.5 rounded-md border px-2 py-1 font-mono text-[11px]"
        title={
          user.canAccessAllBusinessUnits
            ? 'You can read across every business unit'
            : 'All data on screen is scoped to this business unit'
        }
      >
        <Building2 className="size-3.5 opacity-70" aria-hidden />
        {user.canAccessAllBusinessUnits ? 'All units' : `BU ${user.businessUnitId}`}
      </span>

      <ThemeToggle />
      <UserMenu />
    </header>
  );
}
