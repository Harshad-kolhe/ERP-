import type { TreeColumn } from '@/components/tree-list/tree-list';
import type { AssemblyNodeListItem } from '@/lib/api/types';

import {
  activeColumn,
  dateColumn,
  numberColumn,
  serialNumberColumn,
  textColumn,
} from '../shared/master-columns';

/**
 * The column set the three assembly-node grids share.
 *
 * One builder rather than three column arrays, because the three screens list the
 * same record: any column that differed between them would be an accident rather
 * than a decision. What legitimately differs — whether there is a parent, what the
 * parent is called, and what the children are called — is passed in.
 */
export function assemblyNodeColumns({
  parentLabel,
  childLabel,
}: {
  /** What the level above is called here: "Section" on the assembly grid. Null for sections. */
  parentLabel: string | null;
  /** What the level below is called, for the child-count column. Null for sub-assemblies. */
  childLabel: string | null;
}): TreeColumn<AssemblyNodeListItem>[] {
  return [
    serialNumberColumn<AssemblyNodeListItem>(),
    activeColumn<AssemblyNodeListItem>(),

    textColumn('code', 'Code', 130, { mono: true }),
    textColumn('name', 'Name', 300),
    textColumn('manualCode', 'Manual code', 140, { mono: true }),

    // Only where there is one. A "Parent" column full of dashes on the section
    // grid is a column that teaches the reader to ignore a column.
    ...(parentLabel
      ? [
          textColumn<AssemblyNodeListItem>('parentCode', `${parentLabel} code`, 140, { mono: true }),
          textColumn<AssemblyNodeListItem>('parentName', `${parentLabel} name`, 240),
        ]
      : []),

    // Not sortable — it is a subquery, and sorting on it would make the database
    // count every node's children before it could order one page. Worth showing,
    // not worth sorting by.
    ...(childLabel
      ? [
          {
            dataField: 'childCount',
            caption: `${childLabel} count`,
            width: 140,
            minWidth: 100,
            align: 'right' as const,
            allowSorting: false,
            allowFiltering: false,
            calculateCellValue: (row: AssemblyNodeListItem) => String(row.childCount),
          },
        ]
      : []),

    textColumn('machineType', 'Machine type', 150, { align: 'center' }),
    textColumn('drivenBy', 'Driven by', 150),
    numberColumn('quantity', 'Quantity', 120, { decimals: 2 }),
    numberColumn('weightKg', 'Weight (kg)', 130, { decimals: 4 }),
    numberColumn('displaySequence', 'Sequence', 110),

    textColumn('technicalSpecification', 'Technical specification', 300, { defaultVisible: false }),
    textColumn('drawingPath', 'Drawing path', 240, { defaultVisible: false }),
    textColumn('remark', 'Remark', 240),

    textColumn('createdBy', 'Created by', 150, { defaultVisible: false }),
    dateColumn('createdAt', 'Created on', 130, 'createdAtUtc'),
    textColumn('modifiedBy', 'Modified by', 150, { defaultVisible: false }),
    dateColumn('modifiedAt', 'Modified on', 130, 'modifiedAtUtc'),
  ];
}
