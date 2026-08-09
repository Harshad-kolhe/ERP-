'use client';

import { useRouter } from 'next/navigation';
import { useCallback, useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';
import type { AssemblyNodeListItem } from '@/lib/api/types';

import { MasterTreeList } from '../shared/master-tree-list';
import { assemblyNodeColumns } from './assembly-node-columns';
import type { AssemblyLevelScreen } from './assembly-node-level';

/**
 * The grid behind all three assembly-node screens.
 *
 * The screen definition supplies the resource, the wording and the permissions;
 * everything else — server paging, the column chooser, export — comes from
 * `MasterTreeList` exactly as it does for every other master.
 */
export function AssemblyNodesTable({ screen }: { screen: AssemblyLevelScreen }) {
  const router = useRouter();
  const { can } = usePermissions();

  // The endpoint enforces the same permission, so this is about not offering a
  // row action that would fail — not about security.
  const canEdit = can(screen.permissions.update);

  const open = useCallback(
    (row: AssemblyNodeListItem) => router.push(`/masters/${screen.resource}/${row.id}`),
    [router, screen.resource],
  );

  const columns = useMemo(
    () =>
      assemblyNodeColumns({
        parentLabel: screen.parent?.noun ?? null,
        childLabel: screen.childNoun,
      }),
    [screen.parent, screen.childNoun],
  );

  return (
    <MasterTreeList<AssemblyNodeListItem>
      resource={screen.resource}
      columns={columns}
      keyField="id"
      stretchColumn="name"
      searchPlaceholder={
        screen.parent
          ? `Search code, name, manual code or ${screen.parent.noun.toLowerCase()} code…`
          : 'Search code, name or manual code…'
      }
      ariaLabel={screen.plural}
      emptyTitle={`No ${screen.plural.toLowerCase()}`}
      emptyHint={`No ${screen.plural.toLowerCase()} match the current filters.`}
      exportFileName={screen.plural}
      onRowClick={canEdit ? open : undefined}
      rowActions={
        canEdit
          ? {
              caption: '',
              width: 72,
              render: (row) => (
                <button
                  type="button"
                  className="border-line bg-surface text-ink-2 hover:border-line-strong hover:text-ink rounded-lg border px-2 py-0.5 text-xs font-medium"
                  onClick={(event) => {
                    // The row's own click handler would fire too and race this one.
                    event.stopPropagation();
                    open(row);
                  }}
                >
                  Edit
                </button>
              ),
            }
          : undefined
      }
    />
  );
}
