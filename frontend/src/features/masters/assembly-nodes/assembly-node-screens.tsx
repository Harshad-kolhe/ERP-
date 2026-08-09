import Link from 'next/link';
import { Suspense } from 'react';

import { Can } from '@/components/permission/can';
import { PageHeader } from '@/components/shell/page-header';
import { Button } from '@/components/ui/button';
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
      <PageHeader
        title={screen.plural}
        description={
          screen.parent
            ? `Every ${screen.noun} belongs to one ${screen.parent.noun.toLowerCase()}. Filtering, sorting and paging run in the database.`
            : `The top of the machine breakdown. Filtering, sorting and paging run in the database.`
        }
        actions={
          // Rendered only for users holding the create permission. The endpoint
          // enforces the same check, so this is about not offering an action that
          // would fail — not about security.
          <Can permission={screen.permissions.create}>
            <Button size="sm" asChild>
              <Link href={`/masters/${screen.resource}/new`}>New {screen.noun}</Link>
            </Button>
          </Can>
        }
      />

      <div className="flex min-h-0 flex-1 flex-col p-6">
        {/* useSearchParams needs a Suspense boundary during prerender. */}
        <Suspense fallback={<p className="text-muted-foreground text-sm">Loading…</p>}>
          <AssemblyNodesTable screen={screen} />
        </Suspense>
      </div>
    </div>
  );
}

export function NewAssemblyNodeScreen({ screen }: { screen: AssemblyLevelScreen }) {
  return (
    <div className="flex h-full min-h-0 flex-col">
      <PageHeader
        title={`New ${screen.noun}`}
        description={
          screen.parent
            ? `The code must be unique across sections, assemblies and sub-assemblies, and the ${screen.parent.noun.toLowerCase()} is required.`
            : 'The code must be unique across sections, assemblies and sub-assemblies.'
        }
      />
      {/* The form owns its own scrolling and sticky action bar, so no padding here. */}
      <div className="flex min-h-0 flex-1 flex-col">
        <AssemblyNodeForm screen={screen} />
      </div>
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
      <PageHeader
        title={`Edit ${screen.noun}`}
        description="Saving checks the version you loaded, so a colleague's concurrent edit is reported rather than overwritten."
      />
      <div className="flex min-h-0 flex-1 flex-col">
        <EditMasterRecord<AssemblyNodeDetail> resource={screen.resource} id={id} noun={screen.noun}>
          {(node) => <AssemblyNodeForm screen={screen} node={node} />}
        </EditMasterRecord>
      </div>
    </div>
  );
}
