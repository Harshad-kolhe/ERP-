'use client';

import { useRouter } from 'next/navigation';
import { useMemo } from 'react';
import { toast } from 'sonner';
import { z } from 'zod';

import { MasterForm, type MasterFormSection } from '@/components/form/master-form';
import { useApiForm } from '@/components/form/use-api-form';
import type { HsnCodeDetail } from '@/lib/api/types';

import * as s from '../shared/form-schema';
import { useInvalidateLookups } from '../shared/use-lookups';
import { useSaveMasterRecord } from '../shared/use-master-record';

export interface HsnCodeFormValues {
  code: string;
  description: string;
  ratePercent: string;
  effectiveFrom: string;
  isActive: boolean;
}

/** Mirrors `CreateHsnCodeValidator` and `SaveHsnCodeValidator`. */
const schema = z.object({
  code: s.requiredText(8, 'HSN code').regex(/^[0-9]{4}([0-9]{2}([0-9]{2})?)?$/, 'HSN code must be 4, 6 or 8 digits.'),
  description: s.requiredText(250, 'Description'),
  ratePercent: s.taxRate('Rate'),
  effectiveFrom: z.string(),
  isActive: z.boolean(),
}) satisfies z.ZodType<HsnCodeFormValues, HsnCodeFormValues>;

/**
 * Add or edit an HSN code.
 *
 * A new code is created with its opening rate in one step, and the rate is
 * required. A code with none would pass the existence check on a part and then
 * produce an invoice line taxed at nothing — the failure mode of a master that
 * validates a shape without carrying a value.
 *
 * Afterwards, rates are amended in `HsnRateHistory` rather than here: they are
 * appended, and a form that could rewrite one would rewrite the tax on documents
 * already raised.
 */
export function HsnCodeForm({ hsn }: { hsn?: HsnCodeDetail }) {
  const router = useRouter();
  const isNew = !hsn;
  const invalidateLookups = useInvalidateLookups();

  const save = useSaveMasterRecord<HsnCodeFormValues>({
    resource: 'hsn-codes',
    id: hsn?.id,
    rowVersion: hsn?.rowVersion,
    toBody: (values) => ({
      ...(isNew
        ? {
            code: values.code.trim(),
            ratePercent: Number(values.ratePercent),
            effectiveFrom: values.effectiveFrom,
          }
        : {}),
      description: values.description.trim(),
      isActive: values.isActive,
    }),
  });

  const sections = useMemo<MasterFormSection<HsnCodeFormValues>[]>(
    () => [
      {
        id: 'code',
        label: 'HSN code',
        description: 'The code cannot be changed after creation — parts store the digits, not a key.',
        fields: [
          {
            name: 'code',
            label: 'HSN code',
            required: true,
            readOnly: !isNew,
            description: '4, 6 or 8 digits.',
          },
          { name: 'description', label: 'Description', required: true, wide: true },
          {
            name: 'isActive',
            label: 'Active',
            kind: 'boolean',
            wide: true,
            description: 'Clear this to retire the code. Parts already carrying it keep it.',
          },
        ],
      },
      // Only on create. Afterwards a rate is a row in the history below, and
      // showing these boxes on an existing code would invite somebody to try
      // editing the opening rate through them.
      ...(isNew
        ? [
            {
              id: 'rate',
              label: 'Opening GST rate',
              description:
                'Required. Later changes are recorded against the code as separate rates, so a document keeps the rate that applied when it was raised.',
              fields: [
                { name: 'ratePercent' as const, label: 'Rate %', required: true },
                { name: 'effectiveFrom' as const, label: 'Effective from', kind: 'date' as const, required: true },
              ],
            },
          ]
        : []),
    ],
    [isNew],
  );

  const { form, onSubmit, isSubmitting, formError } = useApiForm<HsnCodeFormValues>({
    schema,
    defaultValues: {
      code: hsn?.code ?? '',
      description: hsn?.description ?? '',
      ratePercent: '',
      effectiveFrom: '',
      isActive: hsn?.isActive ?? true,
    },
    submit: (values) => save.mutateAsync(values),
    onSuccess: () => {
      toast.success(isNew ? 'HSN code created.' : 'HSN code updated.');
      void invalidateLookups();
      router.push('/masters/hsn-codes');
      router.refresh();
    },
  });

  return (
    <MasterForm<HsnCodeFormValues>
      sections={sections}
      form={form}
      onSubmit={onSubmit}
      isSubmitting={isSubmitting}
      formError={formError}
      submitLabel={isNew ? 'Create HSN code' : 'Save changes'}
      onCancel={() => router.push('/masters/hsn-codes')}
      title={isNew ? 'New HSN code' : 'Edit HSN code'}
      backLabel="HSN codes"
      identityCode={hsn?.code}
      identityPlaceholder="Set on save"
      badges={hsn ? [{ label: hsn.isActive ? 'Active' : 'Retired', tone: hsn.isActive ? 'ok' : 'neutral' }] : []}
    />
  );
}
