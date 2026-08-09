'use client';

import { useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';

import type { TreeColumn } from '@/components/tree-list/tree-list';
import type { HsnCodeListItem } from '@/lib/api/types';
import { MasterTreeList } from '../shared/master-tree-list';
import {
  activeColumn,
  numberColumn,
  serialNumberColumn,
  textColumn,
} from '../shared/master-columns';

/**
 * The HSN grid.
 *
 * `GST %` is the rate in force *today*, computed by the server. A code amended
 * for next quarter already carries that row, and showing it here would tell an
 * administrator a rate is live when no invoice will use it yet.
 */
export function HsnCodesTable() {
  const { can } = usePermissions();

  const canEdit = can('masters.referencedata.update');

  /** `dataField` must match a field on the server's `ListHsnCodesHandler.Map`. */
  const columns = useMemo<TreeColumn<HsnCodeListItem>[]>(
    () => [
      serialNumberColumn<HsnCodeListItem>(),
      textColumn('code', 'HSN code', 150, { mono: true }),
      textColumn('description', 'Description', 380),
      numberColumn('currentRatePercent', 'GST % today', 140, { decimals: 2 }),
      activeColumn<HsnCodeListItem>(),
    ],
    [],
  );

  return (
    <MasterTreeList<HsnCodeListItem>
      resource="hsn-codes"
      columns={columns}
      keyField="id"
      stretchColumn="description"
      searchPlaceholder="Search code or description…"
      ariaLabel="HSN codes"
      emptyTitle="No HSN codes"
      emptyHint="No codes match the current filters."
      exportFileName="HSN codes"
      editHref={(row) => `/masters/hsn-codes/${row.id}`}
      canEdit={canEdit}
    />
  );
}
