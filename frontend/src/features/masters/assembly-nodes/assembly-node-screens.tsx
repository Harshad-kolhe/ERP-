'use client';

import type { AssemblyNodeDetail } from '@/lib/api/types';

import { EditMasterRecord } from '../shared/edit-master-record';
import { MasterListScreen } from '../shared/master-list-screen';
import { AssemblyNodeForm } from './assembly-node-form';
import { sentenceCase, type AssemblyLevelScreen } from './assembly-node-level';
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
    <MasterListScreen
      icon={screen.icon}
      title={screen.masterTitle}
      resource={screen.resource}
      noun={sentenceCase(screen.noun)}
      createPermission={screen.permissions.create}
      stats={[
        { label: screen.plural.toLowerCase() },
        { label: 'inactive', filter: 'isActive:eq:false' },
      ]}
    >
      <AssemblyNodesTable screen={screen} />
    </MasterListScreen>
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
