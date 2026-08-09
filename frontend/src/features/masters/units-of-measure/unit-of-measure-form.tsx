'use client';

import { useRouter } from 'next/navigation';
import { useMemo } from 'react';
import { toast } from 'sonner';
import { z } from 'zod';

import { MasterForm, type MasterFormSection } from '@/components/form/master-form';
import { useApiForm } from '@/components/form/use-api-form';
import type { UnitOfMeasureDetail } from '@/lib/api/types';

import * as s from '../shared/form-schema';
import { useInvalidateLookups, useLookups } from '../shared/use-lookups';
import {
  blankToNull,
  blankToNumber,
  numberToInput,
  useSaveMasterRecord,
} from '../shared/use-master-record';

export interface UnitOfMeasureFormValues {
  code: string;
  name: string;
  decimals: string;
  baseUnitCode: string;
  conversionToBase: string;
  sortOrder: string;
  isActive: boolean;
}

/** Mirrors `SaveUnitOfMeasureValidator` and `CreateUnitOfMeasureValidator`. */
const schema = z
  .object({
    code: s
      .requiredText(10, 'Code')
      .regex(/^[A-Za-z][A-Za-z0-9]*$/, 'Code may contain only letters and digits, starting with a letter.'),
    name: s.requiredText(100, 'Name'),
    decimals: s.wholeNumber('Decimals', 6),
    baseUnitCode: s.code(10),
    conversionToBase: s.quantity('Conversion factor'),
    sortOrder: s.wholeNumber('Sort order', 9999),
    isActive: z.boolean(),
  })
  // The pair rule, checked here as well as on the server: a base unit without a
  // factor is a unit that silently converts at one, which is the wrong answer
  // rather than a missing one.
  .refine((values) => values.baseUnitCode.trim() === '' || Number(values.conversionToBase) > 0, {
    path: ['conversionToBase'],
    message: 'A unit with a base unit needs a conversion factor greater than zero.',
  }) satisfies z.ZodType<UnitOfMeasureFormValues, UnitOfMeasureFormValues>;

/**
 * Add or edit a unit.
 *
 * The base unit is picked from the units that already exist — the same `uom` list
 * every other form uses, which now answers from this table. Leaving it blank is
 * the normal case: most units are the base of their own family, and only the ones
 * that convert (TON to KG) name another.
 */
export function UnitOfMeasureForm({ unit }: { unit?: UnitOfMeasureDetail }) {
  const router = useRouter();
  const isNew = !unit;

  const { lookups, isError } = useLookups(['uom']);
  const invalidateLookups = useInvalidateLookups();

  const save = useSaveMasterRecord<UnitOfMeasureFormValues>({
    resource: 'units-of-measure',
    id: unit?.id,
    rowVersion: unit?.rowVersion,
    toBody: (values) => ({
      ...(isNew ? { code: values.code.trim().toUpperCase() } : {}),
      name: values.name.trim(),
      decimals: blankToNumber(values.decimals) ?? 0,
      baseUnitCode: blankToNull(values.baseUnitCode),
      conversionToBase: values.baseUnitCode.trim() === '' ? null : blankToNumber(values.conversionToBase),
      sortOrder: blankToNumber(values.sortOrder) ?? 0,
      isActive: values.isActive,
    }),
  });

  const sections = useMemo<MasterFormSection<UnitOfMeasureFormValues>[]>(
    () => [
      {
        id: 'unit',
        label: 'Unit',
        description:
          'The code cannot be changed after creation — parts store the letters, not a key.',
        fields: [
          { name: 'code', label: 'Code', required: true, readOnly: !isNew },
          { name: 'name', label: 'Name', required: true },
          {
            name: 'decimals',
            label: 'Decimals',
            kind: 'integer',
            description: 'Decimal places a quantity may have. 0 for anything counted — half a bearing is a typo.',
          },
          {
            name: 'sortOrder',
            label: 'Order',
            kind: 'integer',
            description: 'Position in the dropdown. NOS belongs first, not AMP.',
          },
        ],
      },
      {
        id: 'conversion',
        label: 'Conversion',
        description:
          'Only for a unit that converts to another the same way for every part — TON to KG. A box of 12 for one part and 50 for another is a fact about the part, not the box, and does not belong here.',
        fields: [
          {
            name: 'baseUnitCode',
            label: 'Base unit',
            lookup: 'uom',
            description: 'Leave blank if this unit is itself a base. The base must not convert to anything else.',
          },
          {
            name: 'conversionToBase',
            label: 'Factor',
            kind: 'number',
            description: 'How many base units one of this unit is — 1000 for TON when the base is KG.',
          },
          {
            name: 'isActive',
            label: 'Active',
            kind: 'boolean',
            wide: true,
            description: 'Clear this to retire the unit. Parts already measured in it keep it.',
          },
        ],
      },
    ],
    [isNew],
  );

  const { form, onSubmit, isSubmitting, formError } = useApiForm<UnitOfMeasureFormValues>({
    schema,
    defaultValues: {
      code: unit?.code ?? '',
      name: unit?.name ?? '',
      decimals: numberToInput(unit?.decimals) || '0',
      baseUnitCode: unit?.baseUnitCode ?? '',
      conversionToBase: numberToInput(unit?.conversionToBase),
      sortOrder: numberToInput(unit?.sortOrder) || '0',
      isActive: unit?.isActive ?? true,
    },
    submit: (values) => save.mutateAsync(values),
    onSuccess: () => {
      toast.success(isNew ? 'Unit created.' : 'Unit updated.');
      void invalidateLookups();
      router.push('/masters/units-of-measure');
      router.refresh();
    },
  });

  return (
    <MasterForm<UnitOfMeasureFormValues>
      sections={sections}
      form={form}
      onSubmit={onSubmit}
      isSubmitting={isSubmitting}
      formError={formError}
      lookups={lookups}
      lookupsFailed={isError}
      submitLabel={isNew ? 'Create unit' : 'Save changes'}
      onCancel={() => router.push('/masters/units-of-measure')}
      title={isNew ? 'New unit of measure' : 'Edit unit of measure'}
      backLabel="Units of measure"
      identityCode={unit?.code}
      identityPlaceholder="Set on save"
      badges={
        unit ? [{ label: unit.isActive ? 'Active' : 'Retired', tone: unit.isActive ? 'ok' : 'neutral' }] : []
      }
    />
  );
}
