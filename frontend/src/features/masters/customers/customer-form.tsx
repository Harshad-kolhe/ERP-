'use client';

import { useRouter } from 'next/navigation';
import { useMemo } from 'react';
import { toast } from 'sonner';
import { z } from 'zod';

import { MasterForm } from '@/components/form/master-form';
import { useApiForm } from '@/components/form/use-api-form';
import type { CustomerDetail } from '@/lib/api/types';

import * as s from '../shared/form-schema';
import { useLookups } from '../shared/use-lookups';
import { blankToNull, blankToNumber, numberToInput, useSaveMasterRecord } from '../shared/use-master-record';
import { CUSTOMER_LOOKUPS, customerFormSections, type CustomerFormValues } from './customer-form-fields';

/** Mirrors `SaveCustomerValidator`. The server re-checks and wins any disagreement. */
const schema = z.object({
  customerCode: z
    .string()
    .trim()
    .min(1, 'Customer code is required.')
    .max(50, 'Customer code must be 50 characters or fewer.')
    .regex(
      /^[A-Za-z0-9][A-Za-z0-9._/-]*$/,
      'Customer code may contain only letters, digits, dot, underscore, slash and hyphen.',
    ),
  customerName: s.requiredText(200, 'Customer name'),
  industry: s.code(100),
  primaryContact: s.text(100, 'Primary contact person'),
  secondaryContact: s.text(100, 'Secondary contact person'),
  phone: s.text(30, 'Phone'),
  altPhone: s.text(30, 'Alt phone'),
  email: s.email('Email'),
  altEmail: s.email('Alt email'),
  website: s.text(200, 'Website'),
  billingAddress: s.text(500, 'Billing address'),
  billingCountry: s.text(100, 'Billing country'),
  billingState: s.text(100, 'Billing state'),
  billingCity: s.text(100, 'Billing city'),
  billingZipCode: s.text(20, 'Billing zip code'),
  shippingAddress: s.text(500, 'Shipping address'),
  shippingCountry: s.text(100, 'Shipping country'),
  shippingState: s.text(100, 'Shipping state'),
  shippingCity: s.text(100, 'Shipping city'),
  shippingZipCode: s.text(20, 'Shipping zip code'),
  taxId: s.text(50, 'Tax id'),
  gst: s.gstin('GST'),
  pan: s.pan(),
  igst: s.taxRate('IGST'),
  cgst: s.taxRate('CGST'),
  sgst: s.taxRate('SGST'),
  currency: s.code(3),
  taxCode: s.code(),
  isActive: z.boolean(),
  status: z.string(),
}) satisfies z.ZodType<CustomerFormValues, CustomerFormValues>;

export function CustomerForm({ customer }: { customer?: CustomerDetail }) {
  const router = useRouter();
  const isNew = !customer;
  const { lookups } = useLookups(CUSTOMER_LOOKUPS);

  const save = useSaveMasterRecord<CustomerFormValues>({
    resource: 'customers',
    id: customer?.id,
    rowVersion: customer?.rowVersion,
    toBody: (values) => ({
      ...(isNew ? { customerCode: values.customerCode.trim() } : {}),
      customerName: values.customerName.trim(),
      industry: blankToNull(values.industry),
      primaryContact: blankToNull(values.primaryContact),
      secondaryContact: blankToNull(values.secondaryContact),
      phone: blankToNull(values.phone),
      altPhone: blankToNull(values.altPhone),
      email: blankToNull(values.email),
      altEmail: blankToNull(values.altEmail),
      website: blankToNull(values.website),
      billingAddress: blankToNull(values.billingAddress),
      billingCountry: blankToNull(values.billingCountry),
      billingState: blankToNull(values.billingState),
      billingCity: blankToNull(values.billingCity),
      billingZipCode: blankToNull(values.billingZipCode),
      shippingAddress: blankToNull(values.shippingAddress),
      shippingCountry: blankToNull(values.shippingCountry),
      shippingState: blankToNull(values.shippingState),
      shippingCity: blankToNull(values.shippingCity),
      shippingZipCode: blankToNull(values.shippingZipCode),
      taxId: blankToNull(values.taxId),
      gst: blankToNull(values.gst),
      pan: blankToNull(values.pan),
      igst: blankToNumber(values.igst),
      cgst: blankToNumber(values.cgst),
      sgst: blankToNumber(values.sgst),
      currency: blankToNull(values.currency),
      taxCode: blankToNull(values.taxCode),
      isActive: values.isActive,
      status: values.status || 'Draft',
    }),
  });

  const sections = useMemo(() => customerFormSections(isNew), [isNew]);

  const { form, onSubmit, isSubmitting, formError } = useApiForm<CustomerFormValues>({
    schema,
    defaultValues: toFormValues(customer),
    submit: (values) => save.mutateAsync(values),
    onSuccess: () => {
      toast.success(isNew ? 'Customer created.' : 'Customer updated.');
      router.push('/masters/customers');
      router.refresh();
    },
  });

  return (
    <MasterForm<CustomerFormValues>
      sections={sections}
      form={form}
      onSubmit={onSubmit}
      isSubmitting={isSubmitting}
      formError={formError}
      submitLabel={isNew ? 'Create customer' : 'Save changes'}
      onCancel={() => router.push('/masters/customers')}
      lookups={lookups}
    />
  );
}

/** Controlled from the first render — see the note in `SupplierForm`. */
function toFormValues(customer?: CustomerDetail): CustomerFormValues {
  return {
    customerCode: customer?.customerCode ?? '',
    customerName: customer?.customerName ?? '',
    industry: customer?.industry ?? '',
    primaryContact: customer?.primaryContact ?? '',
    secondaryContact: customer?.secondaryContact ?? '',
    phone: customer?.phone ?? '',
    altPhone: customer?.altPhone ?? '',
    email: customer?.email ?? '',
    altEmail: customer?.altEmail ?? '',
    website: customer?.website ?? '',
    billingAddress: customer?.billingAddress ?? '',
    billingCountry: customer?.billingCountry ?? '',
    billingState: customer?.billingState ?? '',
    billingCity: customer?.billingCity ?? '',
    billingZipCode: customer?.billingZipCode ?? '',
    shippingAddress: customer?.shippingAddress ?? '',
    shippingCountry: customer?.shippingCountry ?? '',
    shippingState: customer?.shippingState ?? '',
    shippingCity: customer?.shippingCity ?? '',
    shippingZipCode: customer?.shippingZipCode ?? '',
    taxId: customer?.taxId ?? '',
    gst: customer?.gst ?? '',
    pan: customer?.pan ?? '',
    igst: numberToInput(customer?.igst),
    cgst: numberToInput(customer?.cgst),
    sgst: numberToInput(customer?.sgst),
    currency: customer?.currency ?? '',
    taxCode: customer?.taxCode ?? '',
    isActive: customer?.isActive ?? true,
    status: customer?.status ?? 'Draft',
  };
}
