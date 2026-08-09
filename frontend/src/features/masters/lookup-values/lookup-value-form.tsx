'use client';

import { useRouter } from 'next/navigation';
import { useMemo } from 'react';
import { toast } from 'sonner';
import { z } from 'zod';

import { MasterForm, type MasterFormSection } from '@/components/form/master-form';
import { useApiForm } from '@/components/form/use-api-form';
import type { LookupValueDetail } from '@/lib/api/types';

import * as s from '../shared/form-schema';
import { useInvalidateLookups } from '../shared/use-lookups';
import { blankToNumber, numberToInput, useSaveMasterRecord } from '../shared/use-master-record';

export interface LookupValueFormValues {
  type: string;
  code: string;
  name: string;
  sortOrder: string;
  isActive: boolean;
}

/** Mirrors `SaveLookupValueValidator` and `CreateLookupValueValidator`. */
const schema = z.object({
  type: s
    .requiredText(50, 'List')
    .regex(
      /^[a-zA-Z][a-zA-Z0-9]*(\.[a-zA-Z][a-zA-Z0-9]*)?$/,
      "List must be a name like 'moc' or 'part.type' — letters and digits, at most one dot.",
    ),
  code: s.requiredText(50, 'Code'),
  name: s.requiredText(150, 'Name'),
  sortOrder: s.wholeNumber('Sort order', 9999),
  isActive: z.boolean(),
}) satisfies z.ZodType<LookupValueFormValues, LookupValueFormValues>;

/**
 * Add or edit one option.
 *
 * The list and the code are fixed after creation, and the form says so rather
 * than silently discarding them: every record that already stores `OutSource`
 * would be reinterpreted by an edit here, and there is no way to find them all
 * afterwards.
 *
 * Retiring an option is clearing `Active`, not deleting it. The option leaves
 * every dropdown and the records that already carry it stay explicable.
 */
export function LookupValueForm({ value }: { value?: LookupValueDetail }) {
  const router = useRouter();
  const isNew = !value;
  const invalidateLookups = useInvalidateLookups();

  const save = useSaveMasterRecord<LookupValueFormValues>({
    resource: 'lookup-values',
    id: value?.id,
    rowVersion: value?.rowVersion,
    toBody: (values) => ({
      ...(isNew ? { type: values.type.trim(), code: values.code.trim() } : {}),
      name: values.name.trim(),
      sortOrder: blankToNumber(values.sortOrder) ?? 0,
      isActive: values.isActive,
    }),
  });

  const sections = useMemo<MasterFormSection<LookupValueFormValues>[]>(
    () => [
      {
        id: 'option',
        label: 'Option',
        description:
          'Adding an option here makes it selectable immediately, with no deployment. The list and the code cannot be changed afterwards — records store the code.',
        fields: [
          {
            name: 'type',
            label: 'List',
            required: true,
            readOnly: !isNew,
            description:
              'Which dropdown this belongs to, e.g. moc or part.sourceCode. A new list name is only useful once a screen asks for it.',
          },
          {
            name: 'code',
            label: 'Code',
            required: true,
            readOnly: !isNew,
            description: 'What records store. Fixed once created.',
          },
          { name: 'name', label: 'Name', required: true, description: 'What the user sees.' },
          {
            name: 'sortOrder',
            label: 'Order',
            kind: 'integer',
            description: 'Position in the dropdown. Lists have a natural order that is not alphabetical.',
          },
          {
            name: 'isActive',
            label: 'Active',
            kind: 'boolean',
            wide: true,
            description: 'Clear this to retire the option. It leaves the dropdown; existing records keep it.',
          },
        ],
      },
    ],
    [isNew],
  );

  const { form, onSubmit, isSubmitting, formError } = useApiForm<LookupValueFormValues>({
    schema,
    defaultValues: {
      type: value?.type ?? '',
      code: value?.code ?? '',
      name: value?.name ?? '',
      sortOrder: numberToInput(value?.sortOrder) || '0',
      isActive: value?.isActive ?? true,
    },
    submit: (values) => save.mutateAsync(values),
    onSuccess: () => {
      toast.success(isNew ? 'Option added.' : 'Option updated.');
      void invalidateLookups();
      router.push('/masters/lookup-values');
      router.refresh();
    },
  });

  return (
    <MasterForm<LookupValueFormValues>
      sections={sections}
      form={form}
      onSubmit={onSubmit}
      isSubmitting={isSubmitting}
      formError={formError}
      submitLabel={isNew ? 'Add option' : 'Save changes'}
      onCancel={() => router.push('/masters/lookup-values')}
      title={isNew ? 'New option' : 'Edit option'}
      backLabel="Reference data"
      identityCode={value ? `${value.type} · ${value.code}` : null}
      identityPlaceholder="Set on save"
      badges={
        value ? [{ label: value.isActive ? 'Active' : 'Retired', tone: value.isActive ? 'ok' : 'neutral' }] : []
      }
    />
  );
}
