'use client';

import { useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';
import type { TreeColumn } from '@/components/tree-list/tree-list';
import {
  numberColumn,
  serialNumberColumn,
  textColumn,
} from '@/features/masters/shared/master-columns';
import { MasterTreeList } from '@/features/masters/shared/master-tree-list';
import type { AdminRoleListItem } from '@/lib/api/types';

/**
 * The identity roles grid.
 *
 * On `MasterTreeList` like every other list in the app. It used to be the one
 * screen built on a second, simpler table component, which is how it became the
 * only screen whose colours ignored the theme toggle — it hardcoded a grey ramp
 * while everything else used tokens.
 *
 * Nothing here is sortable or filterable, and that is a property of the endpoint
 * rather than a decision about the screen: `/admin/roles` publishes no `QueryMap`,
 * so a sort or filter term would come back 400. Offering the control and failing
 * is worse than not offering it, so the columns say so.
 */
export function RolesTable() {
  const { can } = usePermissions();

  const columns = useMemo<TreeColumn<AdminRoleListItem>[]>(
    () => [
      serialNumberColumn<AdminRoleListItem>(),
      inert(textColumn('name', 'Role', 240)),
      inert(textColumn('description', 'Description', 420)),
      {
        dataField: 'permissionCount',
        caption: 'Permissions',
        width: 130,
        minWidth: 110,
        align: 'right',
        allowSorting: false,
        allowFiltering: false,
        // "All" rather than a count: a super-administrator role stores no
        // permission rows, so its count is zero — which would read as "grants
        // nothing", the exact opposite of the truth.
        calculateCellValue: (row) =>
          row.isSuperAdministrator ? 'All' : String(row.permissionCount),
      },
      // Shown because editing a role nobody holds is safe and editing one that
      // fifty people hold is not. The number is the warning.
      inert(numberColumn('userCount', 'Users', 110)),
    ],
    [],
  );

  return (
    <MasterTreeList<AdminRoleListItem>
      basePath="/admin"
      resource="roles"
      columns={columns}
      keyField="id"
      stretchColumn="description"
      searchPlaceholder="Search roles…"
      ariaLabel="Roles"
      emptyTitle="No roles"
      emptyHint="No roles have been defined yet."
      exportFileName="Roles"
      editHref={(row) => `/admin/roles/${row.id}`}
      canEdit={can('admin.role.update')}
    />
  );
}

/** Turns off the controls this endpoint cannot serve. See the note above. */
function inert(column: TreeColumn<AdminRoleListItem>): TreeColumn<AdminRoleListItem> {
  return { ...column, allowSorting: false, allowFiltering: false };
}
