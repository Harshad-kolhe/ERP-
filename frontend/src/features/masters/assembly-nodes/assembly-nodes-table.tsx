'use client';

import { useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';
import type { AssemblyNodeListItem } from '@/lib/api/types';

import { ASSEMBLY_NODE_FILTERS } from '../shared/master-filter-fields';
import { MasterTreeList } from '../shared/master-tree-list';
import { assemblyNodeColumns } from './assembly-node-columns';
import { sentenceCase, type AssemblyLevelScreen } from './assembly-node-level';

/**
 * The grid behind all three assembly-node screens.
 *
 * The screen definition supplies the resource, the wording and the permissions;
 * everything else — server paging, the column chooser, export — comes from
 * `MasterTreeList` exactly as it does for every other master.
 */
export function AssemblyNodesTable({ screen }: { screen: AssemblyLevelScreen }) {
  const { can } = usePermissions();

  // The endpoint enforces the same permission, so this is about not offering a
  // row action that would fail — not about security.
  const canEdit = can(screen.permissions.update);

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
      // One declaration for all three levels, because they are one record type.
      // The parent-code box is simply empty on sections, which have no parent.
      filters={ASSEMBLY_NODE_FILTERS}
      filtersNoun={sentenceCase(screen.noun)}
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
      editHref={(row) => `/masters/${screen.resource}/${row.id}`}
      canEdit={canEdit}
    />
  );
}
