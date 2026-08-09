'use client';

import { Download, FileText, Layers, MapPin, SendHorizontal, Upload } from 'lucide-react';
import Link from 'next/link';
import type { ReactNode } from 'react';

import { Can } from '@/components/permission/can';
import Popover from '@/components/tree-list/popover';
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
 * Two actions are primary because they are the two people arrive to do; the rest
 * live behind "More actions" rather than competing for the same row. The legacy
 * page put all seven side by side and the important one was not findable.
 */
export function PartsPageHeader() {
  return (
    <MasterPageHeader
      icon="part"
      title="Part Master"
      resource="parts"
      stats={[
        { label: 'parts' },
        { label: 'awaiting approval', filter: 'status:eq:PendingApproval', emphasise: true },
        { label: 'on hold', filter: 'status:eq:Hold', emphasise: true },
      ]}
      actions={
        <>
      <Can permission="masters.part.create">
        <Link
          href="/masters/parts/new"
          className="bg-primary hover:bg-primary/90 text-primary-foreground inline-flex h-8 items-center gap-1.5 rounded-lg px-3.5 text-[13px] font-semibold shadow-sm"
        >
          <span aria-hidden="true" className="-mt-px text-base leading-none">
            +
          </span>
          New Part
        </Link>
      </Can>

      <Can permission="masters.part.import">
        <Link
          href="/api/v1/masters/parts/import-template"
          className="border-line bg-surface text-ink-2 hover:border-line-strong hover:text-ink inline-flex h-8 items-center gap-1.5 rounded-lg border px-2.5 text-xs font-medium transition-colors max-sm:hidden"
          title="An empty workbook whose headings are exactly what the importer expects"
        >
          <Download className="h-3.5 w-3.5" />
          Import template
        </Link>
      </Can>

      <Popover align="right" width={252} title="More actions" label={<span aria-hidden="true">···</span>}>
        {() => (
          <div>
            <Can permission="masters.part.import">
              <MenuLink href="/masters/parts/import" icon={<Upload className="h-3.5 w-3.5" />}>
                Import from Excel
              </MenuLink>
            </Can>

            <Can permission="masters.part.approve">
              <MenuLink href="/masters/parts?filter=status:eq:PendingApproval" icon={<Layers className="h-3.5 w-3.5" />}>
                Approval list
              </MenuLink>
            </Can>

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

function MenuLink({ href, icon, children }: { href: string; icon: ReactNode; children: ReactNode }) {
  return (
    <Link
      href={href}
      className="hover:bg-surface-3 text-ink flex w-full items-center gap-2 rounded-lg px-2 py-1.5 text-left text-xs"
    >
      {icon}
      {children}
    </Link>
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
      className="text-ink-3 flex w-full cursor-default items-center gap-2 rounded-lg px-2 py-1.5 text-left text-xs"
    >
      {icon}
      {children}
      <span className="ml-auto text-[10px]">soon</span>
    </span>
  );
}
