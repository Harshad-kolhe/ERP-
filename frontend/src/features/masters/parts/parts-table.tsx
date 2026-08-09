'use client';

import { useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';
import type { TreeColumn } from '@/components/tree-list/tree-list';
import type { PartListItem, PartStatus } from '@/lib/api/types';
import { PART_FILTERS } from '../shared/master-filter-fields';
import { MasterTreeList } from '../shared/master-tree-list';
import {
  activeColumn,
  dateColumn,
  numberColumn,
  serialNumberColumn,
  textColumn,
} from '../shared/master-columns';

const STATUS_STYLES: Record<PartStatus, string> = {
  Draft: 'bg-surface-3 text-ink-2',
  PendingApproval: 'bg-amber-500/15 text-amber-700 dark:text-amber-300',
  Approved: 'bg-emerald-500/15 text-emerald-700 dark:text-emerald-300',
  Inactive: 'bg-rose-500/15 text-rose-700 dark:text-rose-300',
};

const STATUS_LABELS: Record<PartStatus, string> = {
  Draft: 'Draft',
  PendingApproval: 'Pending approval',
  Approved: 'Approved',
  Inactive: 'Inactive',
};

/**
 * The Part Master grid.
 *
 * Column for column what the legacy screen showed, in the same order, with the
 * same five columns off by default — people who move across know where to look,
 * and nothing they relied on has quietly gone. What changed is everything around
 * the columns: the database does the filtering, sorting, counting and paging, so
 * the browser holds one page instead of the whole master.
 *
 * The legacy captions are kept in substance and tidied in form — sentence case to
 * match the rest of this app, and "Part Cate.code" spelled out. Two legacy columns
 * have no equivalent yet: the row's Edit and View buttons, which pointed at a
 * detail screen this app has not built. They arrive with the form kit rather than
 * as buttons that lead nowhere.
 */
export function PartsTable() {
  const { can } = usePermissions();

  // The endpoint enforces the same permission, so this is about not offering a
  // row action that would fail — not about security.
  const canEdit = can('masters.part.update');

  /**
   * `dataField` must match a field on the server's `ListPartsHandler.Map`. A name
   * not on that allow-list is rejected with 400 rather than concatenated into SQL,
   * so the sortable set is finite and deliberate.
   *
   * Parts keeps its own status pill rather than the shared one: `PartStatus` is a
   * separate wire contract from `MasterStatus`, and collapsing them here would
   * hide the day one of them gains a member the other does not have.
   */
  const columns = useMemo<TreeColumn<PartListItem>[]>(
    () => [
      serialNumberColumn<PartListItem>(),
      activeColumn<PartListItem>(),
      {
        dataField: 'status',
        caption: 'Status',
        width: 150,
        minWidth: 120,
        calculateCellValue: (row) => STATUS_LABELS[row.status] ?? '',
        cellRender: (row) => (
          <span
            className={`rounded-full px-2 py-0.5 text-[11px] font-medium ${STATUS_STYLES[row.status] ?? ''}`}
          >
            {STATUS_LABELS[row.status] ?? row.status}
          </span>
        ),
      },
      textColumn('partNumber', 'System part number', 170, { mono: true }),

      // Not a legacy grid column. Off by default, but here because it is the only
      // way to see that -00 and -01 are the same part: the legacy grid showed the
      // revisions as unrelated rows.
      textColumn('originalPartNumber', 'Original part number', 180, {
        mono: true,
        defaultVisible: false,
      }),

      textColumn('itemNumber', 'Item code (manual)', 150, { mono: true }),
      textColumn('description', 'Part description', 300),
      textColumn('technicalSpecification', 'Technical specification', 300),
      textColumn('moc', 'MOC', 110, { align: 'center' }),
      textColumn('partCategoryCode', 'Part category code', 150, { align: 'center' }),
      textColumn('partType', 'Part type', 130, { align: 'center' }),
      textColumn('formCategory', 'Form category', 140, { align: 'center' }),
      textColumn('unitOfMeasureCode', 'Primary UOM', 120, { align: 'center' }),

      // Off by default, exactly as they were on the legacy grid — most parts are
      // bought and sold in the primary unit, so these repeat it on nearly every row.
      textColumn('purchaseUomCode', 'Purchase UOM', 130, {
        align: 'center',
        defaultVisible: false,
      }),
      textColumn('sellingUomCode', 'Selling UOM', 130, {
        align: 'center',
        defaultVisible: false,
      }),

      textColumn('materialType', 'Material type', 140, { align: 'center' }),
      textColumn('seriesCode', 'Series code', 130, { align: 'center' }),
      textColumn('partRevisionNo', 'Part revision no', 140, { align: 'center' }),
      textColumn('sourceCode', 'Source code', 130, { align: 'center' }),
      numberColumn('weightKg', 'Weight (kg)', 120, { decimals: 4 }),
      numberColumn('leadTimeDays', 'Lead time (days)', 140, { defaultVisible: false }),
      numberColumn('minimumStockLevel', 'Minimum stock level', 170, { decimals: 4 }),
      numberColumn('reorderPoint', 'Reorder point', 140, { defaultVisible: false }),
      textColumn('hsnCode', 'HSN code', 120, { mono: true, align: 'center' }),
      textColumn('drawingNumber', 'Drawing revision path', 200),

      textColumn('createdBy', 'Created by', 150),
      dateColumn('createdAt', 'Created on', 130, 'createdAtUtc'),
      textColumn('modifiedBy', 'Modified by', 150),
      dateColumn('modifiedAt', 'Modified on', 130, 'modifiedAtUtc'),

      textColumn('revisionRemark', 'Revision remark', 220, { defaultVisible: false }),
      textColumn('holdRemark', 'Hold remark', 220),
      textColumn('inactiveRemark', 'Inactive remark', 220),
    ],
    [],
  );

  return (
    <MasterTreeList<PartListItem>
      resource="parts"
      filters={PART_FILTERS}
      filtersNoun="Part"
      columns={columns}
      keyField="id"
      stretchColumn="description"
      searchPlaceholder="Search part number, item code, description or HSN…"
      ariaLabel="Parts"
      emptyTitle="No parts"
      emptyHint="No parts match the current filters."
      exportFileName="Parts"
      editHref={(row) => `/masters/parts/${row.id}`}
      canEdit={canEdit}
    />
  );
}
