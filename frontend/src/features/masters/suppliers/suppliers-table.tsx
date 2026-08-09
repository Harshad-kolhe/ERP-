'use client';

import { useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';

import type { TreeColumn } from '@/components/tree-list/tree-list';
import type { SupplierListItem } from '@/lib/api/types';
import { SUPPLIER_FILTERS } from '../shared/master-filter-fields';
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
 * The Supplier Master grid — the legacy column set, in the legacy order.
 *
 * Purchasing works the contact block, both addresses, the tax block and the bank
 * block from this one screen, so the whole set is here. Every column is toggleable
 * from the column chooser, which is the part the legacy grid did not have.
 */
export function SuppliersTable() {
  const { can } = usePermissions();

  // The endpoint enforces the same permission; this only decides what to draw.
  const canEdit = can("masters.supplier.update");

  /**
   * `dataField` must match a field on the server's `ListSuppliersHandler.Map`.
   * A name not on that allow-list is rejected with 400 rather than concatenated
   * into SQL, so the sortable set is finite and deliberate.
   */
  const columns = useMemo<TreeColumn<SupplierListItem>[]>(
    () => [
      serialNumberColumn<SupplierListItem>(),
      textColumn('supplierCode', 'Supplier code', 150, { mono: true }),
      textColumn('supplierName', 'Supplier name', 260),
      textColumn('supplierType', 'Supplier type', 160),
      textColumn('primaryContact', 'Primary contact', 180),
      textColumn('secondaryContact', 'Secondary contact', 180),
      textColumn('phone', 'Phone', 140),
      textColumn('altPhone', 'Alt phone', 140),
      textColumn('email', 'Email', 220),
      textColumn('altEmail', 'Alt email', 220),
      textColumn('website', 'Website', 200),

      textColumn('billingAddress', 'Billing address', 260),
      textColumn('billingCountry', 'Billing country', 150),
      textColumn('billingState', 'Billing state', 150),
      textColumn('billingCity', 'Billing city', 150),
      textColumn('billingZipCode', 'Billing zipcode', 140, { mono: true }),

      textColumn('shippingAddress', 'Shipping address', 260),
      textColumn('shippingCountry', 'Shipping country', 150),
      textColumn('shippingState', 'Shipping state', 150),
      textColumn('shippingCity', 'Shipping city', 150),
      textColumn('shippingZipCode', 'Shipping zipcode', 150, { mono: true }),

      textColumn('pan', 'PAN', 140, { mono: true }),
      textColumn('taxId', 'Tax ID', 150, { mono: true }),
      textColumn('gstNo', 'GST no', 170, { mono: true }),

      textColumn('bankName', 'Bank name', 190),
      textColumn('accountNumber', 'Account number', 180, { mono: true }),
      textColumn('ifsc', 'IFSC', 140, { mono: true }),
      textColumn('swift', 'SWIFT', 140, { mono: true }),

      textColumn('paymentTerms', 'Payment terms', 170),
      textColumn('currency', 'Currency', 110, { align: 'center' }),
      textColumn('taxCode', 'Tax code', 130, { align: 'center' }),
      textColumn('qualityCompliance', 'Quality compliance', 190),

      // Percentage rates, not amounts — two decimals is what a GST rate needs.
      numberColumn('igst', 'IGST', 100, { decimals: 2 }),
      numberColumn('cgst', 'CGST', 100, { decimals: 2 }),
      numberColumn('sgst', 'SGST', 100, { decimals: 2 }),

      // Two different questions, which is why both are here rather than one
      // standing in for the other: whether we still buy from them, and what the
      // legacy free text said about why not.
      activeColumn<SupplierListItem>(),
      textColumn('activeStatus', 'Active status', 150, { align: 'center' }),
      statusColumn<SupplierListItem>(),

      textColumn('createdBy', 'Created by', 150),
      dateColumn('createdAt', 'Created on', 130, 'createdAtUtc'),
      textColumn('modifiedBy', 'Modified by', 150),
      dateColumn('modifiedAt', 'Modified on', 130, 'modifiedAtUtc'),
    ],
    [],
  );

  return (
    <MasterTreeList<SupplierListItem>
      resource="suppliers"
      filters={SUPPLIER_FILTERS}
      filtersNoun="Supplier"
      columns={columns}
      keyField="id"
      stretchColumn="supplierName"
      searchPlaceholder="Search code, name, contact, email or GST…"
      ariaLabel="Suppliers"
      emptyTitle="No suppliers"
      emptyHint="No suppliers match the current filters."
      exportFileName="Suppliers"
      editHref={(row) => `/masters/suppliers/${row.id}`}
      canEdit={canEdit}
    />
  );
}
