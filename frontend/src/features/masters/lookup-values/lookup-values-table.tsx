'use client';

import { useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';

import type { TreeColumn } from '@/components/tree-list/tree-list';
import type { LookupValueListItem } from '@/lib/api/types';
import { MasterTreeList } from '../shared/master-tree-list';
import {
  activeColumn,
  numberColumn,
  serialNumberColumn,
  textColumn,
} from '../shared/master-columns';

/**
 * Every dropdown option in the system, in one grid.
 *
 * One screen rather than twenty, because they are one table. `List` is the first
 * column and the default sort for that reason: the grid is read a list at a time,
 * and search matches the list name as well as the code, so an administrator finds
 * the source-code options by typing "source" without knowing the key is
 * `part.sourceCode`.
 */
export function LookupValuesTable() {
  const { can } = usePermissions();

  const canEdit = can('masters.referencedata.update');

  /** `dataField` must match a field on the server's `ListLookupValuesHandler.Map`. */
  const columns = useMemo<TreeColumn<LookupValueListItem>[]>(
    () => [
      serialNumberColumn<LookupValueListItem>(),
      textColumn('type', 'List', 200, { mono: true }),
      textColumn('code', 'Code', 200, { mono: true }),
      textColumn('name', 'Name', 260),
      numberColumn('sortOrder', 'Order', 100),
      activeColumn<LookupValueListItem>(),
    ],
    [],
  );

  return (
    <MasterTreeList<LookupValueListItem>
      resource="lookup-values"
      columns={columns}
      keyField="id"
      stretchColumn="name"
      searchPlaceholder="Search list, code or name…"
      ariaLabel="Reference data"
      emptyTitle="No options"
      emptyHint="No options match the current filters."
      exportFileName="Reference data"
      editHref={(row) => `/masters/lookup-values/${row.id}`}
      canEdit={canEdit}
    />
  );
}
