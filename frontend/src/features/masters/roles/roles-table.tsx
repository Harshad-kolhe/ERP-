'use client';

import { useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';

import type { TreeColumn } from '@/components/tree-list/tree-list';
import type { RoleListItem } from '@/lib/api/types';
import { MasterTreeList } from '../shared/master-tree-list';
import {
  activeColumn,
  booleanColumn,
  serialNumberColumn,
  textColumn,
} from '../shared/master-columns';

/**
 * The legacy Role Master grid, which had three columns: Sr No, Role Id and Roles
 * Name. The two extra columns here are not decoration — `Cross business unit` is
 * the flag that lets a holder read every tenant's data, and it was invisible on the
 * legacy screen that edited it.
 */
export function RolesTable() {
  const { can } = usePermissions();

  // The endpoint enforces the same permission; this only decides what to draw.
  const canEdit = can("masters.role.update");

  /** `dataField` must match a field on the server's `ListRolesHandler.Map`. */
  const columns = useMemo<TreeColumn<RoleListItem>[]>(
    () => [
      serialNumberColumn<RoleListItem>(),
      textColumn('roleId', 'Role id', 120, { mono: true, align: 'right' }),
      textColumn('rolesName', 'Roles name', 280),
      booleanColumn('bypassBusinessUnit', 'Cross business unit', 180),
      activeColumn<RoleListItem>(),
    ],
    [],
  );

  return (
    <MasterTreeList<RoleListItem>
      resource="roles"
      columns={columns}
      keyField="id"
      stretchColumn="rolesName"
      searchPlaceholder="Search role name…"
      ariaLabel="Roles"
      emptyTitle="No roles"
      emptyHint="No roles match the current filters."
      exportFileName="Roles"
      editHref={(row) => `/masters/roles/${row.id}`}
      canEdit={canEdit}
    />
  );
}
