'use client';

import { FileText, MapPin, MoreHorizontal, Plus, SendHorizontal } from 'lucide-react';
import Link from 'next/link';
import type { ReactNode } from 'react';

import { Can } from '@/components/permission/can';
import { Button } from '@/components/ui/button';
import Popover from '@/components/tree-list/popover';
import { MasterImportAction } from '../shared/master-import';
import { MasterPageHeader } from '../shared/master-page-header';

/**
 * The Part Master page header.
 *
 * The action set, on the shared `MasterPageHeader` band. Everything about how the
 * band looks lives there; this file decides only what goes in it, which is what
 * lets the other masters get the same header without copying it.
 *
 * The action set is the legacy screen's, which resolved each of these through
 * `PERMISSION_MAP` against `GetPageButtonPermissions`. Here each one is wrapped in
 * `Can`, so the same decision is made from the permission catalogue instead — and
 * the server re-checks on every request regardless of what is drawn.
 *
 * Create and import are the two people arrive to do, so they are the two buttons.
 * What is left in "More actions" is only what has not been built yet. The legacy
 * page put all seven side by side and the important one was not findable.
 *
 * There is no "Approval list" item, and no counts on the band: the status chips
 * above the grid already carry all six figures, lit so it is obvious which state is
 * showing, and clicking one writes `status:eq:…`. Repeating three of them up here
 * put the same number on screen twice — once as a control, once as a label — and
 * arrived at it by different arithmetic, so the two could disagree.
 */
export function PartsPageHeader() {
  return (
    <MasterPageHeader
      icon="part"
      title="Part Master"
      resource="parts"
      actions={
        <>
      <Can permission="masters.part.create">
        <Button size="sm" asChild>
          <Link href="/masters/parts/new">
            <Plus className="size-4" aria-hidden />
            New Part
          </Link>
        </Button>
      </Can>

      {/* Beside New Part, not inside the menu, and the same button the other five
          masters draw. It went in the menu first and was simply not found; the
          template it used to sit next to now lives inside the dialog, next to the
          file picker that needs it. */}
      <Can permission="masters.part.import">
        <MasterImportAction resource="parts" title="Part Master" />
      </Can>

      <Popover
        align="right"
        width={252}
        title="More actions"
        label={<MoreHorizontal className="size-4" aria-hidden />}
      >
        {() => (
          <div>
            {/*
              Rendered disabled rather than omitted, exactly as the navigation
              marks a planned screen. Hiding them would make the legacy feature set
              unknowable — the thing the nav config exists to prevent — while a
              button that silently does nothing is worse than one that says why.
            */}
            <MenuItemDisabled icon={<SendHorizontal className="h-3.5 w-3.5" />} reason="Needs the Approvals module">
              Send for approval (bulk)
            </MenuItemDisabled>

            <MenuItemDisabled icon={<MapPin className="h-3.5 w-3.5" />} reason="Needs the Part Location master">
              Part multiple location
            </MenuItemDisabled>

            <MenuItemDisabled icon={<FileText className="h-3.5 w-3.5" />} reason="Needs the Reporting module">
              Part report
            </MenuItemDisabled>
            </div>
          )}
        </Popover>
        </>
      }
    />
  );
}

function MenuItemDisabled({
  icon,
  reason,
  children,
}: {
  icon: ReactNode;
  reason: string;
  children: ReactNode;
}) {
  return (
    <span
      title={reason}
      aria-disabled="true"
      className="text-ink-faint flex w-full cursor-default items-center gap-2 rounded-lg px-2 py-1.5 text-left text-xs"
    >
      {icon}
      {children}
      <span className="ml-auto text-[10px]">soon</span>
    </span>
  );
}
