'use client';

import { useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';

import type { TreeColumn } from '@/components/tree-list/tree-list';
import type { BusinessUnitListItem } from '@/lib/api/types';
import { MasterTreeList } from '../shared/master-tree-list';
import {
  activeColumn,
  dateColumn,
  serialNumberColumn,
  textColumn,
} from '../shared/master-columns';

/** The Business Unit Master grid — the legacy column set, in the legacy order. */
export function BusinessUnitsTable() {
  const { can } = usePermissions();

  // The endpoint enforces the same permission; this only decides what to draw.
  const canEdit = can("masters.businessunit.update");

  /** `dataField` must match a field on the server's `ListBusinessUnitsHandler.Map`. */
  const columns = useMemo<TreeColumn<BusinessUnitListItem>[]>(
    () => [
      serialNumberColumn<BusinessUnitListItem>(),
      textColumn('businessName', 'Business name', 280),
      textColumn('address', 'Address', 300),
      textColumn('stateName', 'State name', 160),
      textColumn('contactNumber', 'Contact number', 160),
      textColumn('email', 'Email', 220),
      textColumn('website', 'Website', 200),
      textColumn('cin', 'CIN', 200, { mono: true }),
      textColumn('gstn', 'GSTN', 170, { mono: true }),

      // Not on the legacy grid. Kept because every other table's tenancy column
      // holds this value, so it is the number an administrator needs when reading
      // a row from anywhere else in the system.
      textColumn('businessUnitId', 'Unit id', 110, { mono: true, align: 'right' }),
      activeColumn<BusinessUnitListItem>(),
      dateColumn('createdAt', 'Created', 130, 'createdAtUtc'),
    ],
    [],
  );

  return (
    <MasterTreeList<BusinessUnitListItem>
      resource="business-units"
      columns={columns}
      keyField="id"
      stretchColumn="address"
      searchPlaceholder="Search name, email, CIN or GSTN…"
      ariaLabel="Business units"
      emptyTitle="No business units"
      emptyHint="No business units match the current filters."
      exportFileName="BusinessUnits"
      editHref={(row) => `/masters/business-units/${row.id}`}
      canEdit={canEdit}
    />
  );
}
