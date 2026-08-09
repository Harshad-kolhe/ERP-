import type { MasterFormSection } from '@/components/form/master-form';

export interface SupplierFormValues {
  supplierCode: string;
  supplierName: string;
  supplierType: string;
  primaryContact: string;
  secondaryContact: string;
  phone: string;
  altPhone: string;
  email: string;
  altEmail: string;
  website: string;
  billingAddress: string;
  billingCountry: string;
  billingState: string;
  billingCity: string;
  billingZipCode: string;
  shippingAddress: string;
  shippingCountry: string;
  shippingState: string;
  shippingCity: string;
  shippingZipCode: string;
  pan: string;
  taxId: string;
  gstNo: string;
  bankName: string;
  accountNumber: string;
  ifsc: string;
  swift: string;
  paymentTerms: string;
  currency: string;
  taxCode: string;
  qualityCompliance: string;
  igst: string;
  cgst: string;
  sgst: string;
  activeStatus: string;
  isActive: boolean;
  status: string;
}

/** Every list this form needs, fetched in one request. */
export const SUPPLIER_LOOKUPS = [
  'supplier.type',
  'country',
  'state',
  'currency',
  'paymentTerms',
  'taxCode',
  'masterStatus',
] as const;

/**
 * The supplier form, grouped the way purchasing thinks about a supplier: who they
 * are, how to reach them, where things go, what the tax and bank details are, and
 * whether we are still buying from them.
 *
 * Thirty-seven fields is a lot for one screen and exactly why they are grouped —
 * the legacy form put the same set behind four collapsible panels for the same
 * reason. What is different here is that no list of choices is written into this
 * file: each dropdown names a server-held lookup instead.
 */
export function supplierFormSections(isNew: boolean): MasterFormSection<SupplierFormValues>[] {
  return [
    {
      id: 'basic',
      label: 'Basic details',
      description: isNew
        ? 'The supplier code is the business key and cannot be changed afterwards.'
        : 'The supplier code cannot be changed here — every purchase order refers to it.',
      fields: [
        {
          name: 'supplierCode',
          label: 'Supplier code',
          required: true,
          readOnly: !isNew,
          placeholder: 'SUPP2021',
          description: 'Letters, digits, dot, underscore, slash and hyphen.',
        },
        { name: 'supplierName', label: 'Supplier name', required: true },
        { name: 'supplierType', label: 'Supplier type', lookup: 'supplier.type' },
        { name: 'qualityCompliance', label: 'Quality compliance', placeholder: 'ISO 9001:2015' },
      ],
    },
    {
      id: 'contact',
      label: 'Contact',
      fields: [
        { name: 'primaryContact', label: 'Primary contact' },
        { name: 'secondaryContact', label: 'Secondary contact' },
        { name: 'phone', label: 'Phone' },
        { name: 'altPhone', label: 'Alt phone' },
        { name: 'email', label: 'Email' },
        { name: 'altEmail', label: 'Alt email' },
        { name: 'website', label: 'Website', wide: true },
      ],
    },
    {
      id: 'addresses',
      label: 'Addresses',
      fields: [
        { name: 'billingAddress', label: 'Billing address', kind: 'textarea', rows: 2 },
        { name: 'billingCountry', label: 'Billing country', lookup: 'country' },
        { name: 'billingState', label: 'Billing state', lookup: 'state' },
        { name: 'billingCity', label: 'Billing city' },
        { name: 'billingZipCode', label: 'Billing zipcode' },
        { name: 'shippingAddress', label: 'Shipping address', kind: 'textarea', rows: 2 },
        { name: 'shippingCountry', label: 'Shipping country', lookup: 'country' },
        { name: 'shippingState', label: 'Shipping state', lookup: 'state' },
        { name: 'shippingCity', label: 'Shipping city' },
        { name: 'shippingZipCode', label: 'Shipping zipcode' },
      ],
    },
    {
      id: 'tax',
      label: 'Tax & bank',
      description: 'GST rates are percentages, not amounts.',
      fields: [
        { name: 'pan', label: 'PAN', placeholder: 'AAAPA1234A' },
        { name: 'gstNo', label: 'GST no', placeholder: '27AAAPA1234A1Z5' },
        { name: 'taxId', label: 'Tax ID' },
        { name: 'taxCode', label: 'Tax code', lookup: 'taxCode' },
        { name: 'igst', label: 'IGST %', kind: 'number' },
        { name: 'cgst', label: 'CGST %', kind: 'number' },
        { name: 'sgst', label: 'SGST %', kind: 'number' },
        { name: 'currency', label: 'Currency', lookup: 'currency' },
        { name: 'paymentTerms', label: 'Payment terms', lookup: 'paymentTerms' },
        { name: 'bankName', label: 'Bank name' },
        { name: 'accountNumber', label: 'Account number' },
        { name: 'ifsc', label: 'IFSC', placeholder: 'HDFC0001234' },
        { name: 'swift', label: 'SWIFT' },
      ],
    },
    {
      id: 'status',
      label: 'Status',
      description:
        'Active decides whether the supplier can be used on new documents. Status is where the record sits in approval — they answer different questions.',
      fields: [
        { name: 'isActive', label: 'Active', kind: 'boolean', wide: true },
        {
          name: 'activeStatus',
          label: 'Active status note',
          description: 'Why, if not active — for example Blacklisted or On hold.',
        },
        { name: 'status', label: 'Approval status', lookup: 'masterStatus' },
      ],
    },
  ];
}
