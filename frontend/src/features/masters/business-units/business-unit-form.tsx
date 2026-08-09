'use client';

import { useRouter } from 'next/navigation';
import { useMemo } from 'react';
import { toast } from 'sonner';
import { z } from 'zod';

import { MasterForm, type MasterFormSection } from '@/components/form/master-form';
import { useApiForm } from '@/components/form/use-api-form';
import type { BusinessUnitDetail } from '@/lib/api/types';

import * as s from '../shared/form-schema';
import { useLookups } from '../shared/use-lookups';
import { blankToNull, blankToNumber, numberToInput, useSaveMasterRecord } from '../shared/use-master-record';

export interface BusinessUnitFormValues {
  businessUnitId: string;
  businessName: string;
  address: string;
  stateName: string;
  stateCode: string;
  contactNumber: string;
  email: string;
  website: string;
  cin: string;
  gstn: string;
  pan: string;
  isActive: boolean;
}

const LOOKUPS = ['state'] as const;

/** Mirrors `SaveBusinessUnitValidator`. */
const schema = z.object({
  businessUnitId: s.wholeNumber('Unit id').refine((value) => value.trim() !== '', {
    message: 'Unit id is required.',
  }),
  businessName: s.requiredText(200, 'Business name'),
  address: s.text(500, 'Address'),
  stateName: s.text(100, 'State name'),
  stateCode: s.text(10, 'State code'),
  contactNumber: s.text(30, 'Contact number'),
  email: s.email('Email'),
  website: s.text(200, 'Website'),
  cin: s.cin(),
  gstn: s.gstin('GSTN'),
  pan: s.pan(),
  isActive: z.boolean(),
}) satisfies z.ZodType<BusinessUnitFormValues, BusinessUnitFormValues>;

/**
 * The business unit form.
 *
 * Short, and one field on it matters more than the rest: the unit id is what every
 * other table carries in its tenancy column, so it is set once and never changed.
 * There is no approval status here — a business unit is the tenancy boundary
 * itself, not a record awaiting sign-off.
 */
export function BusinessUnitForm({ unit }: { unit?: BusinessUnitDetail }) {
  const router = useRouter();
  const isNew = !unit;
  const { lookups, isError: lookupsFailed } = useLookups(LOOKUPS);

  const save = useSaveMasterRecord<BusinessUnitFormValues>({
    resource: 'business-units',
    id: unit?.id,
    rowVersion: unit?.rowVersion,
    toBody: (values) => ({
      ...(isNew ? { businessUnitId: blankToNumber(values.businessUnitId) } : {}),
      businessName: values.businessName.trim(),
      address: blankToNull(values.address),
      stateName: blankToNull(values.stateName),
      stateCode: blankToNull(values.stateCode),
      contactNumber: blankToNull(values.contactNumber),
      email: blankToNull(values.email),
      website: blankToNull(values.website),
      cin: blankToNull(values.cin),
      gstn: blankToNull(values.gstn),
      pan: blankToNull(values.pan),
      isActive: values.isActive,
    }),
  });

  const sections = useMemo<MasterFormSection<BusinessUnitFormValues>[]>(
    () => [
      {
        id: 'unit',
        label: 'Business unit',
        description: isNew
          ? 'The unit id is what every other record carries in its tenancy column. It cannot be changed afterwards.'
          : 'The unit id cannot be changed — every record in the system points at it.',
        fields: [
          {
            name: 'businessUnitId',
            label: 'Unit id',
            kind: 'integer',
            required: true,
            readOnly: !isNew,
            placeholder: '1',
          },
          {
            name: 'businessName',
            label: 'Business name',
            required: true,
            description: 'Unique across the whole system, not just within a tenant.',
          },
          { name: 'address', label: 'Address', kind: 'textarea', rows: 2 },
          { name: 'stateName', label: 'State name', lookup: 'state' },
          { name: 'stateCode', label: 'State code', placeholder: '27' },
          { name: 'contactNumber', label: 'Contact number' },
          { name: 'email', label: 'Email' },
          { name: 'website', label: 'Website' },
          { name: 'cin', label: 'CIN', description: 'Corporate Identification Number, 21 characters.' },
          { name: 'gstn', label: 'GSTN', placeholder: '27AAAPA1234A1Z5' },
          { name: 'pan', label: 'PAN', placeholder: 'AAAPA1234A' },
          { name: 'isActive', label: 'Active', kind: 'boolean', wide: true },
        ],
      },
    ],
    [isNew],
  );

  const { form, onSubmit, isSubmitting, formError } = useApiForm<BusinessUnitFormValues>({
    schema,
    defaultValues: {
      businessUnitId: numberToInput(unit?.businessUnitId),
      businessName: unit?.businessName ?? '',
      address: unit?.address ?? '',
      stateName: unit?.stateName ?? '',
      stateCode: unit?.stateCode ?? '',
      contactNumber: unit?.contactNumber ?? '',
      email: unit?.email ?? '',
      website: unit?.website ?? '',
      cin: unit?.cin ?? '',
      gstn: unit?.gstn ?? '',
      pan: unit?.pan ?? '',
      isActive: unit?.isActive ?? true,
    },
    submit: (values) => save.mutateAsync(values),
    onSuccess: () => {
      toast.success(isNew ? 'Business unit created.' : 'Business unit updated.');
      router.push('/masters/business-units');
      router.refresh();
    },
  });

  return (
    <MasterForm<BusinessUnitFormValues>
      sections={sections}
      form={form}
      onSubmit={onSubmit}
      isSubmitting={isSubmitting}
      formError={formError}
      submitLabel={isNew ? 'Create business unit' : 'Save changes'}
      onCancel={() => router.push('/masters/business-units')}
      lookups={lookups}
      lookupsFailed={lookupsFailed}
      title={isNew ? 'New business unit' : 'Edit business unit'}
      backLabel="Business units"
      identityCode={unit?.businessName}
      badges={unit ? [{ label: unit.isActive ? 'Active' : 'Inactive', tone: unit.isActive ? 'ok' : 'neutral' }] : []}
      auditLine={unit ? `Created ${new Date(unit.createdAtUtc).toLocaleDateString('en-IN')}${unit.modifiedAtUtc ? ` · Modified ${new Date(unit.modifiedAtUtc).toLocaleDateString('en-IN')}` : ''}` : null}
    />
  );
}
