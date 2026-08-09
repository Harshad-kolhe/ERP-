'use client';

import { useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';

import type { TreeColumn } from '@/components/tree-list/tree-list';
import type { CustomerListItem } from '@/lib/api/types';
import { CUSTOMER_FILTERS } from '../shared/master-filter-fields';
import { MasterTreeList } from '../shared/master-tree-list';
import {
  activeColumn,
  dateColumn,
  numberColumn,
  serialNumberColumn,
  statusColumn,
  textColumn,
} from '../shared/master-columns';

/**
 * The Customer Master grid — the legacy column set, in the legacy order.
 * See `SuppliersTable` for why a ported master grid is this wide.
 */
export function CustomersTable() {
  const { can } = usePermissions();

  // The endpoint enforces the same permission; this only decides what to draw.
  const canEdit = can("masters.customer.update");

  /** `dataField` must match a field on the server's `ListCustomersHandler.Map`. */
  const columns = useMemo<TreeColumn<CustomerListItem>[]>(
    () => [
      serialNumberColumn<CustomerListItem>(),
      textColumn('customerCode', 'Customer code', 150, { mono: true }),
      textColumn('customerName', 'Customer name', 260),
      textColumn('industry', 'Industry', 170),
      textColumn('primaryContact', 'Primary contact person', 200),
      textColumn('secondaryContact', 'Secondary contact person', 200),
      textColumn('phone', 'Phone', 140),
      textColumn('altPhone', 'Alt phone', 140),
      textColumn('email', 'Email', 220),
      textColumn('altEmail', 'Alt email', 220),
      textColumn('website', 'Website', 200),

      textColumn('billingAddress', 'Billing address', 260),
      textColumn('billingCountry', 'Billing country', 150),
      textColumn('billingState', 'Billing state', 150),
      textColumn('billingCity', 'Billing city', 150),
      textColumn('billingZipCode', 'Billing zip code', 150, { mono: true }),

      textColumn('shippingAddress', 'Shipping address', 260),
      textColumn('shippingCountry', 'Shipping country', 150),
      textColumn('shippingState', 'Shipping state', 150),
      textColumn('shippingCity', 'Shipping city', 150),
      textColumn('shippingZipCode', 'Shipping zip code', 160, { mono: true }),

      textColumn('taxId', 'Tax id', 150, { mono: true }),
      textColumn('gst', 'GST', 170, { mono: true }),
      textColumn('pan', 'PAN', 140, { mono: true }),

      // Percentage rates, not amounts.
      numberColumn('igst', 'IGST', 100, { decimals: 2 }),
      numberColumn('cgst', 'CGST', 100, { decimals: 2 }),
      numberColumn('sgst', 'SGST', 100, { decimals: 2 }),

      textColumn('currency', 'Currency', 110, { align: 'center' }),
      textColumn('taxCode', 'Tax code', 130, { align: 'center' }),

      textColumn('createdBy', 'Created by', 150),
      dateColumn('createdAt', 'Created on', 130, 'createdAtUtc'),
      textColumn('modifiedBy', 'Modified by', 150),
      dateColumn('modifiedAt', 'Modified on', 130, 'modifiedAtUtc'),

      activeColumn<CustomerListItem>(),
      statusColumn<CustomerListItem>(),
    ],
    [],
  );

  return (
    <MasterTreeList<CustomerListItem>
      resource="customers"
      filters={CUSTOMER_FILTERS}
      filtersNoun="Customer"
      columns={columns}
      keyField="id"
      stretchColumn="customerName"
      searchPlaceholder="Search code, name, contact, email or GST…"
      ariaLabel="Customers"
      emptyTitle="No customers"
      emptyHint="No customers match the current filters."
      exportFileName="Customers"
      editHref={(row) => `/masters/customers/${row.id}`}
      canEdit={canEdit}
    />
  );
}
