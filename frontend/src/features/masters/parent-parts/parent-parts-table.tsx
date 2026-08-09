'use client';

import { useRouter } from 'next/navigation';
import { useCallback, useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';
import type { TreeColumn } from '@/components/tree-list/tree-list';
import type { ParentPartListItem } from '@/lib/api/types';

import { MasterTreeList } from '../shared/master-tree-list';
import {
  activeColumn,
  dateColumn,
  numberColumn,
  serialNumberColumn,
  textColumn,
} from '../shared/master-columns';

/**
 * The Parent Part grid — one row per build, not one per component line.
 *
 * The legacy screen listed both from the same table, told apart by whether the
 * child column was null, so the record count on screen was neither the number of
 * builds nor the number of lines. The lines are on the detail screen, where they
 * belong.
 */
export function ParentPartsTable() {
  const router = useRouter();
  const { can } = usePermissions();

  const canEdit = can('masters.parentpart.update');

  const open = useCallback(
    (row: ParentPartListItem) => router.push(`/masters/parent-parts/${row.id}`),
    [router],
  );

  /**
   * `dataField` must match a field on the server's `ListParentPartsHandler.Map`.
   * A name not on that allow-list is rejected with 400 rather than concatenated
   * into SQL, so the sortable set is finite and deliberate.
   */
  const columns = useMemo<TreeColumn<ParentPartListItem>[]>(
    () => [
      serialNumberColumn<ParentPartListItem>(),
      activeColumn<ParentPartListItem>(),

      textColumn('partNumber', 'Parent part number', 180, { mono: true }),
      textColumn('partDescription', 'Part description', 300),
      textColumn('description', 'Build description', 260),

      textColumn('assemblyCode', 'Assembly code', 150, { mono: true }),
      textColumn('assemblyName', 'Assembly name', 240),

      // Not sortable: it is a subquery, and sorting on it would make the database
      // count every build's lines before it could order one page.
      {
        dataField: 'componentCount',
        caption: 'Components',
        width: 120,
        minWidth: 100,
        align: 'right',
        allowSorting: false,
        allowFiltering: false,
        calculateCellValue: (row) => String(row.componentCount),
      },

      // Both are rolled up from the lines by the server. They are sortable because
      // they are stored columns, not subqueries — which is the whole reason the
      // aggregate maintains them rather than computing them at read time.
      numberColumn('totalWeightKg', 'Total weight (kg)', 160, { decimals: 4 }),
      numberColumn('totalAmount', 'Total amount', 150, { decimals: 2 }),

      textColumn('unitOfMeasureCode', 'UOM', 100, { align: 'center' }),
      textColumn('category', 'Category', 140, { align: 'center' }),
      textColumn('drawingNumber', 'Drawing number', 180, { defaultVisible: false }),

      textColumn('createdBy', 'Created by', 150, { defaultVisible: false }),
      dateColumn('createdAt', 'Created on', 130, 'createdAtUtc'),
      textColumn('modifiedBy', 'Modified by', 150, { defaultVisible: false }),
      dateColumn('modifiedAt', 'Modified on', 130, 'modifiedAtUtc'),
    ],
    [],
  );

  return (
    <MasterTreeList<ParentPartListItem>
      resource="parent-parts"
      columns={columns}
      keyField="id"
      stretchColumn="partDescription"
      searchPlaceholder="Search part number, description or assembly code…"
      ariaLabel="Parent parts"
      emptyTitle="No parent parts"
      emptyHint="No parent parts match the current filters."
      exportFileName="Parent parts"
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
