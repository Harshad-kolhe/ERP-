'use client';

import { Building2 } from 'lucide-react';

import { useSession } from '@/components/permission/session-provider';
import { ThemeToggle } from '@/components/theme-toggle';

import { Breadcrumbs } from './breadcrumbs';
import { UserMenu } from './user-menu';

export function Topbar() {
  const user = useSession();

  return (
    <header className="bg-card flex h-12 shrink-0 items-center gap-2.5 border-b px-4">
      <Breadcrumbs />

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
