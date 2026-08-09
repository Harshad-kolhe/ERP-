'use client';

import { useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';

import type { TreeColumn } from '@/components/tree-list/tree-list';
import type { UnitOfMeasureListItem } from '@/lib/api/types';
import { MasterTreeList } from '../shared/master-tree-list';
import {
  activeColumn,
  numberColumn,
  serialNumberColumn,
  textColumn,
} from '../shared/master-columns';

/**
 * The units grid.
 *
 * `Base unit` and `Factor` are on the grid rather than hidden on the detail
 * because they are the whole reason units are a master: a list showing only code
 * and name would look exactly like the dropdown this table replaced, and an
 * administrator could not see at a glance which units convert and which do not.
 */
export function UnitsOfMeasureTable() {
  const { can } = usePermissions();

  const canEdit = can('masters.referencedata.update');

  /** `dataField` must match a field on the server's `ListUnitsOfMeasureHandler.Map`. */
  const columns = useMemo<TreeColumn<UnitOfMeasureListItem>[]>(
    () => [
      serialNumberColumn<UnitOfMeasureListItem>(),
      textColumn('code', 'Code', 120, { mono: true }),
      textColumn('name', 'Name', 220),
      numberColumn('decimals', 'Decimals', 110),
      textColumn('baseUnitCode', 'Base unit', 130, { mono: true }),
      numberColumn('conversionToBase', 'Factor', 140, { decimals: 6 }),
      numberColumn('sortOrder', 'Order', 100),
      activeColumn<UnitOfMeasureListItem>(),
    ],
    [],
  );

  return (
    <MasterTreeList<UnitOfMeasureListItem>
      resource="units-of-measure"
      columns={columns}
      keyField="id"
      stretchColumn="name"
      searchPlaceholder="Search code or name…"
      ariaLabel="Units of measure"
      emptyTitle="No units"
      emptyHint="No units match the current filters."
      exportFileName="Units of measure"
      editHref={(row) => `/masters/units-of-measure/${row.id}`}
      canEdit={canEdit}
    />
  );
}
