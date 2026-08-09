'use client';

import { useRouter } from 'next/navigation';
import { useMemo } from 'react';
import { toast } from 'sonner';
import { z } from 'zod';

import { MasterForm } from '@/components/form/master-form';
import { useApiForm } from '@/components/form/use-api-form';
import type { SupplierDetail } from '@/lib/api/types';

import * as s from '../shared/form-schema';
import { useLookups } from '../shared/use-lookups';
import {
  blankToNull,
  blankToNumber,
  numberToInput,
  useSaveMasterRecord,
} from '../shared/use-master-record';
import { SUPPLIER_LOOKUPS, supplierFormSections, type SupplierFormValues } from './supplier-form-fields';

/** Mirrors `SaveSupplierValidator`. The server re-checks and wins any disagreement. */
const schema = z.object({
  supplierCode: z
    .string()
    .trim()
    .min(1, 'Supplier code is required.')
    .max(50, 'Supplier code must be 50 characters or fewer.')
    .regex(
      /^[A-Za-z0-9][A-Za-z0-9._/-]*$/,
      'Supplier code may contain only letters, digits, dot, underscore, slash and hyphen.',
    ),
  supplierName: s.requiredText(200, 'Supplier name'),
  supplierType: s.code(),
  primaryContact: s.text(100, 'Primary contact'),
  secondaryContact: s.text(100, 'Secondary contact'),
  phone: s.text(30, 'Phone'),
  altPhone: s.text(30, 'Alt phone'),
  email: s.email('Email'),
  altEmail: s.email('Alt email'),
  website: s.text(200, 'Website'),
  billingAddress: s.text(500, 'Billing address'),
  billingCountry: s.text(100, 'Billing country'),
  billingState: s.text(100, 'Billing state'),
  billingCity: s.text(100, 'Billing city'),
  billingZipCode: s.text(20, 'Billing zipcode'),
  shippingAddress: s.text(500, 'Shipping address'),
  shippingCountry: s.text(100, 'Shipping country'),
  shippingState: s.text(100, 'Shipping state'),
  shippingCity: s.text(100, 'Shipping city'),
  shippingZipCode: s.text(20, 'Shipping zipcode'),
  pan: s.pan(),
  taxId: s.text(50, 'Tax ID'),
  gstNo: s.gstin('GST no'),
  bankName: s.text(150, 'Bank name'),
  accountNumber: s.text(50, 'Account number'),
  ifsc: s.ifsc(),
  swift: s.swift(),
  paymentTerms: s.text(100, 'Payment terms'),
  currency: s.code(3),
  taxCode: s.code(),
  qualityCompliance: s.text(200, 'Quality compliance'),
  igst: s.taxRate('IGST'),
  cgst: s.taxRate('CGST'),
  sgst: s.taxRate('SGST'),
  activeStatus: s.text(50, 'Active status note'),
  isActive: z.boolean(),
  status: z.string(),
}) satisfies z.ZodType<SupplierFormValues, SupplierFormValues>;

/** Create and edit in one component — they differ only in what they start from. */
export function SupplierForm({ supplier }: { supplier?: SupplierDetail }) {
  const router = useRouter();
  const isNew = !supplier;
  const { lookups } = useLookups(SUPPLIER_LOOKUPS);

  const save = useSaveMasterRecord<SupplierFormValues>({
    resource: 'suppliers',
    id: supplier?.id,
    rowVersion: supplier?.rowVersion,
    toBody: (values) => ({
      // The code is only sent on create: it is the business key, and the update
      // endpoint does not accept it.
      ...(isNew ? { supplierCode: values.supplierCode.trim() } : {}),
      supplierName: values.supplierName.trim(),
      supplierType: blankToNull(values.supplierType),
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
      pan: blankToNull(values.pan),
      taxId: blankToNull(values.taxId),
      gstNo: blankToNull(values.gstNo),
      bankName: blankToNull(values.bankName),
      accountNumber: blankToNull(values.accountNumber),
      ifsc: blankToNull(values.ifsc),
      swift: blankToNull(values.swift),
      paymentTerms: blankToNull(values.paymentTerms),
      currency: blankToNull(values.currency),
      taxCode: blankToNull(values.taxCode),
      qualityCompliance: blankToNull(values.qualityCompliance),
      igst: blankToNumber(values.igst),
      cgst: blankToNumber(values.cgst),
      sgst: blankToNumber(values.sgst),
      activeStatus: blankToNull(values.activeStatus),
      isActive: values.isActive,
      status: values.status || 'Draft',
    }),
  });

  const sections = useMemo(() => supplierFormSections(isNew), [isNew]);

  const { form, onSubmit, isSubmitting, formError } = useApiForm<SupplierFormValues>({
    schema,
    defaultValues: toFormValues(supplier),
    submit: (values) => save.mutateAsync(values),
    onSuccess: () => {
      toast.success(isNew ? 'Supplier created.' : 'Supplier updated.');
      router.push('/masters/suppliers');
      router.refresh();
    },
  });

  return (
    <MasterForm<SupplierFormValues>
      sections={sections}
      form={form}
      onSubmit={onSubmit}
      isSubmitting={isSubmitting}
      formError={formError}
      submitLabel={isNew ? 'Create supplier' : 'Save changes'}
      onCancel={() => router.push('/masters/suppliers')}
      lookups={lookups}
      title={isNew ? 'New supplier' : 'Edit supplier'}
      backLabel="Suppliers"
      identityCode={supplier?.supplierCode}
      badges={supplier ? [{ label: supplier.isActive ? 'Active' : 'Inactive', tone: supplier.isActive ? 'ok' : 'neutral' }] : []}
      auditLine={supplier ? `Created ${new Date(supplier.createdAtUtc).toLocaleDateString('en-IN')}${supplier.modifiedAtUtc ? ` · Modified ${new Date(supplier.modifiedAtUtc).toLocaleDateString('en-IN')}` : ''}` : null}
    />
  );
}

/**
 * Every field defaults to "" rather than undefined, so each input is controlled
 * from the first render — React warns the moment one flips from uncontrolled to
 * controlled, and whatever was typed before the flip is lost.
 */
function toFormValues(supplier?: SupplierDetail): SupplierFormValues {
  return {
    supplierCode: supplier?.supplierCode ?? '',
    supplierName: supplier?.supplierName ?? '',
    supplierType: supplier?.supplierType ?? '',
    primaryContact: supplier?.primaryContact ?? '',
    secondaryContact: supplier?.secondaryContact ?? '',
    phone: supplier?.phone ?? '',
    altPhone: supplier?.altPhone ?? '',
    email: supplier?.email ?? '',
    altEmail: supplier?.altEmail ?? '',
    website: supplier?.website ?? '',
    billingAddress: supplier?.billingAddress ?? '',
    billingCountry: supplier?.billingCountry ?? '',
    billingState: supplier?.billingState ?? '',
    billingCity: supplier?.billingCity ?? '',
    billingZipCode: supplier?.billingZipCode ?? '',
    shippingAddress: supplier?.shippingAddress ?? '',
    shippingCountry: supplier?.shippingCountry ?? '',
    shippingState: supplier?.shippingState ?? '',
    shippingCity: supplier?.shippingCity ?? '',
    shippingZipCode: supplier?.shippingZipCode ?? '',
    pan: supplier?.pan ?? '',
    taxId: supplier?.taxId ?? '',
    gstNo: supplier?.gstNo ?? '',
    bankName: supplier?.bankName ?? '',
    accountNumber: supplier?.accountNumber ?? '',
    ifsc: supplier?.ifsc ?? '',
    swift: supplier?.swift ?? '',
    paymentTerms: supplier?.paymentTerms ?? '',
    currency: supplier?.currency ?? '',
    taxCode: supplier?.taxCode ?? '',
    qualityCompliance: supplier?.qualityCompliance ?? '',
    igst: numberToInput(supplier?.igst),
    cgst: numberToInput(supplier?.cgst),
    sgst: numberToInput(supplier?.sgst),
    activeStatus: supplier?.activeStatus ?? '',
    isActive: supplier?.isActive ?? true,
    status: supplier?.status ?? 'Draft',
  };
}
