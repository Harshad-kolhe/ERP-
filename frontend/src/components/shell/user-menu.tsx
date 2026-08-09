'use client';

import { LogOut } from 'lucide-react';
import { useState } from 'react';

import { useSession } from '@/components/permission/session-provider';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Spinner } from '@/components/ui/spinner';

export function UserMenu() {
  const user = useSession();
  const [signingOut, setSigningOut] = useState(false);

  async function signOut() {
    setSigningOut(true);

    await fetch('/api/v1/auth/logout', { method: 'POST' });

    // A full load, not router.push. The cookie is gone, and every RSC payload
    // cached in this tab was rendered against the signed-in session.
    window.location.href = '/login';
  }

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          aria-label="Account"
          className="bg-primary text-primary-foreground focus-visible:ring-ring/50 grid size-7 shrink-0 place-items-center rounded-full font-mono text-[10px] font-semibold outline-none focus-visible:ring-[3px]"
        >
          {initials(user.userName)}
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" className="w-60">
        <DropdownMenuLabel className="font-normal">
          <p className="text-sm font-medium">{user.userName}</p>
          <p className="text-muted-foreground font-mono text-[11px]">
            {user.permissions.length} permission{user.permissions.length === 1 ? '' : 's'}
            {user.canAccessAllBusinessUnits ? ' · all business units' : ''}
          </p>
        </DropdownMenuLabel>

        <DropdownMenuSeparator />

        <DropdownMenuItem disabled={signingOut} onSelect={(event) => {
          // Keep the menu open while the request is in flight, so the spinner is visible.
          event.preventDefault();
          void signOut();
        }}>
          {signingOut ? <Spinner className="size-4" /> : <LogOut className="size-4" />}
          {signingOut ? 'Signing out…' : 'Sign out'}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);

  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0]!.slice(0, 2).toUpperCase();

  return (parts[0]![0]! + parts[parts.length - 1]![0]!).toUpperCase();
}
