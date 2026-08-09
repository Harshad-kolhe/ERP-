'use client';

import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { MasterPageHeader } from '../shared/master-page-header';
import type { AssemblyNodeDetail } from '@/lib/api/types';

import { EditMasterRecord } from '../shared/edit-master-record';
import { AssemblyNodeForm } from './assembly-node-form';
import type { AssemblyLevelScreen } from './assembly-node-level';
import { AssemblyNodesTable } from './assembly-nodes-table';

/**
 * The three page bodies, written once.
 *
 * Next.js needs a `page.tsx` per route, so there are nine route files — but they
 * are nine three-line files that name a screen definition, not nine copies of a
 * screen. The difference between Sections and Sub-assemblies is data, and it lives
 * in `assembly-node-level.ts`.
 */

export function AssemblyNodeListScreen({ screen }: { screen: AssemblyLevelScreen }) {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <Suspense fallback={<div className="border-line h-[69px] shrink-0 border-b" />}>
        <MasterPageHeader
          icon={screen.icon}
          title={screen.masterTitle}
          resource={screen.resource}
          stats={[
            { label: screen.plural.toLowerCase() },
            { label: 'inactive', filter: 'isActive:eq:false' },
          ]}
          actions={
            <Can permission={screen.permissions.create}>
              <Link
                href={`/masters/${screen.resource}/new`}
                className="bg-primary hover:bg-primary/90 text-primary-foreground inline-flex h-8 items-center gap-1.5 rounded-lg px-3.5 text-[13px] font-semibold shadow-sm"
              >
                <span aria-hidden="true" className="-mt-px text-base leading-none">
                  +
                </span>
                New {screen.noun}
              </Link>
            </Can>
          }
        />
      </Suspense>

      <div className="flex min-h-0 flex-1 flex-col p-4">
        {/* useSearchParams needs a Suspense boundary during prerender. */}
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <AssemblyNodesTable screen={screen} />
        </Suspense>
      </div>
    </div>
  );
}

export function NewAssemblyNodeScreen({ screen }: { screen: AssemblyLevelScreen }) {
  // No PageHeader: the form renders the approved identity bar itself, and two
  // headers stacked is what the tabbed layout used to look like.
  return (
    <div className="flex h-full min-h-0 flex-col">
      <AssemblyNodeForm screen={screen} />
    </div>
  );
}

export function EditAssemblyNodeScreen({
  screen,
  id,
}: {
  screen: AssemblyLevelScreen;
  id: string;
}) {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <EditMasterRecord<AssemblyNodeDetail> resource={screen.resource} id={id} noun={screen.noun}>
        {(node) => <AssemblyNodeForm screen={screen} node={node} />}
      </EditMasterRecord>
    </div>
  );
}
