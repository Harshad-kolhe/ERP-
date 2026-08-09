import type { MasterFormSection } from '@/components/form/master-form';

export interface CustomerFormValues {
  customerCode: string;
  customerName: string;
  industry: string;
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
  taxId: string;
  gst: string;
  pan: string;
  igst: string;
  cgst: string;
  sgst: string;
  currency: string;
  taxCode: string;
  isActive: boolean;
  status: string;
}

export const CUSTOMER_LOOKUPS = [
  'customer.industry',
  'country',
  'state',
  'currency',
  'taxCode',
  'masterStatus',
] as const;

/** The customer form, grouped the way sales thinks about a customer. */
export function customerFormSections(isNew: boolean): MasterFormSection<CustomerFormValues>[] {
  return [
    {
      id: 'basic',
      label: 'Basic details',
      description: isNew
        ? 'The customer code is the business key and cannot be changed afterwards.'
        : 'The customer code cannot be changed here — every order and invoice refers to it.',
      fields: [
        {
          name: 'customerCode',
          label: 'Customer code',
          required: true,
          readOnly: !isNew,
          placeholder: 'CUST1001',
          description: 'Letters, digits, dot, underscore, slash and hyphen.',
        },
        { name: 'customerName', label: 'Customer name', required: true },
        { name: 'industry', label: 'Industry', lookup: 'customer.industry' },
      ],
    },
    {
      id: 'contact',
      label: 'Contact',
      fields: [
        { name: 'primaryContact', label: 'Primary contact person' },
        { name: 'secondaryContact', label: 'Secondary contact person' },
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
        { name: 'billingZipCode', label: 'Billing zip code' },
        { name: 'shippingAddress', label: 'Shipping address', kind: 'textarea', rows: 2 },
        { name: 'shippingCountry', label: 'Shipping country', lookup: 'country' },
        { name: 'shippingState', label: 'Shipping state', lookup: 'state' },
        { name: 'shippingCity', label: 'Shipping city' },
        { name: 'shippingZipCode', label: 'Shipping zip code' },
      ],
    },
    {
      id: 'tax',
      label: 'Tax',
      description: 'GST rates are percentages, not amounts.',
      fields: [
        { name: 'pan', label: 'PAN', placeholder: 'AAAPA1234A' },
        { name: 'gst', label: 'GST', placeholder: '27AAAPA1234A1Z5' },
        { name: 'taxId', label: 'Tax id' },
        { name: 'taxCode', label: 'Tax code', lookup: 'taxCode' },
        { name: 'igst', label: 'IGST %', kind: 'number' },
        { name: 'cgst', label: 'CGST %', kind: 'number' },
        { name: 'sgst', label: 'SGST %', kind: 'number' },
        { name: 'currency', label: 'Currency', lookup: 'currency' },
      ],
    },
    {
      id: 'status',
      label: 'Status',
      description:
        'Active decides whether the customer can be used on new documents. Status is where the record sits in approval.',
      fields: [
        { name: 'isActive', label: 'Active', kind: 'boolean', wide: true },
        { name: 'status', label: 'Approval status', lookup: 'masterStatus' },
      ],
    },
  ];
}
