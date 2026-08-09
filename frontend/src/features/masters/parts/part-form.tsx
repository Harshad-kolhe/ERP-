'use client';

import { useRouter } from 'next/navigation';
import { useMemo } from 'react';
import { toast } from 'sonner';
import { z } from 'zod';

import { MasterForm } from '@/components/form/master-form';
import { useApiForm } from '@/components/form/use-api-form';
import type { PartDetail } from '@/lib/api/types';

import { PART_LOOKUPS, partFormSections, type PartFormValues } from './part-form-fields';
import { useLookups } from '../shared/use-lookups';
import { useSavePart } from './use-parts';

/** Text that must parse as a number if it is filled in at all. */
const numeric = (label: string, max: number) =>
  z
    .string()
    .refine((value) => value.trim() === '' || Number.isFinite(Number(value)), {
      message: `${label} must be a number.`,
    })
    .refine((value) => value.trim() === '' || Number(value) >= 0, {
      message: `${label} cannot be negative.`,
    })
    .refine((value) => value.trim() === '' || Number(value) <= max, {
      message: `${label} is too large.`,
    });

const wholeNumber = (label: string) =>
  numeric(label, 1_000_000_000).refine(
    (value) => value.trim() === '' || Number.isInteger(Number(value)),
    { message: `${label} must be a whole number.` },
  );

/**
 * Mirrors `CreatePartValidator` and `PartAttributesValidator` on the server.
 *
 * A mirror, not the authority: this exists so a typo is caught before a round
 * trip. The server re-checks everything and wins any disagreement, and
 * `useApiForm` puts its per-field messages back under the right inputs.
 */
const schema = z.object({
  partNumber: z
    .string()
    .trim()
    .min(1, 'Part number is required.')
    .max(50, 'Part number must be 50 characters or fewer.')
    .regex(
      /^[A-Za-z0-9][A-Za-z0-9._/-]*$/,
      'Part number may contain only letters, digits, dot, underscore, slash and hyphen.',
    ),
  description: z
    .string()
    .trim()
    .min(1, 'Description is required.')
    .max(250, 'Description must be 250 characters or fewer.'),
  unitOfMeasureCode: z
    .string()
    .trim()
    .min(1, 'Unit of measure is required.')
    .max(10, 'Unit of measure must be 10 characters or fewer.'),
  hsnCode: z
    .string()
    .refine((value) => value.trim() === '' || /^[0-9]{4}([0-9]{2}([0-9]{2})?)?$/.test(value.trim()), {
      message: 'HSN code must be 4, 6 or 8 digits.',
    }),
  drawingNumber: z.string().max(50, 'Drawing path must be 50 characters or fewer.'),
  itemNumber: z.string().max(50, 'Item code must be 50 characters or fewer.'),
  technicalSpecification: z.string().max(2000, 'Technical specification is too long.'),
  moc: z.string().max(50, 'MOC must be 50 characters or fewer.'),
  partCategoryCode: z.string().max(50, 'Part category code must be 50 characters or fewer.'),
  partType: z.string().max(100, 'Part type must be 100 characters or fewer.'),
  formCategory: z.string().max(50, 'Form category must be 50 characters or fewer.'),
  purchaseUomCode: z.string().max(10, 'Purchase UOM must be 10 characters or fewer.'),
  sellingUomCode: z.string().max(10, 'Selling UOM must be 10 characters or fewer.'),
  materialType: z.string().max(50, 'Material type must be 50 characters or fewer.'),
  seriesCode: z.string().max(50, 'Series code must be 50 characters or fewer.'),
  partRevisionNo: z.string().max(10, 'Part revision number must be 10 characters or fewer.'),
  sourceCode: z.string().max(50, 'Source code must be 50 characters or fewer.'),
  weightKg: numeric('Weight', 9_999_999.9999),
  leadTimeDays: wholeNumber('Lead time'),
  minimumStockLevel: numeric('Minimum stock level', 9_999_999.9999),
  reorderPoint: wholeNumber('Reorder point'),
}) satisfies z.ZodType<PartFormValues, PartFormValues>;

/**
 * Create and edit in one component: they differ only in what they start from and
 * where they go afterwards, and two files would drift on the first change.
 */
export function PartForm({ part }: { part?: PartDetail }) {
  const router = useRouter();
  const save = useSavePart(part);
  const isNew = !part;
  const { lookups } = useLookups(PART_LOOKUPS);

  const sections = useMemo(() => partFormSections(isNew), [isNew]);

  const { form, onSubmit, isSubmitting, formError } = useApiForm<PartFormValues>({
    schema,
    defaultValues: toFormValues(part),
    submit: (values) => save.mutateAsync(values),
    onSuccess: () => {
      toast.success(isNew ? 'Part created.' : 'Part updated.');
      router.push('/masters/parts');
      router.refresh();
    },
  });

  return (
    <MasterForm<PartFormValues>
      sections={sections}
      form={form}
      onSubmit={onSubmit}
      isSubmitting={isSubmitting}
      formError={formError}
      submitLabel={isNew ? 'Create part' : 'Save changes'}
      onCancel={() => router.push('/masters/parts')}
      lookups={lookups}
      title={isNew ? 'New part' : 'Edit part'}
      backLabel="Parts"
      identityCode={part?.partNumber}
      badges={part ? [{ label: part.status === 'Approved' ? 'Approved' : part.status === 'PendingApproval' ? 'Pending approval' : part.status, tone: part.status === 'Approved' ? 'ok' : part.status === 'PendingApproval' ? 'warn' : 'neutral' }, { label: part.isActive ? 'Active' : 'Inactive', tone: part.isActive ? 'ok' : 'neutral' }] : []}
      auditLine={part ? `Created ${new Date(part.createdAtUtc).toLocaleDateString('en-IN')}${part.modifiedAtUtc ? ` · Modified ${new Date(part.modifiedAtUtc).toLocaleDateString('en-IN')}` : ''}` : null}
    />
  );
}

/**
 * Every field defaults to "" rather than undefined, so each input is controlled
 * from the first render — React logs a warning the moment one flips from
 * uncontrolled to controlled, and the value typed before the flip is lost.
 */
function toFormValues(part?: PartDetail): PartFormValues {
  const a = part?.attributes;

  return {
    partNumber: part?.partNumber ?? '',
    description: part?.description ?? '',
    unitOfMeasureCode: part?.unitOfMeasureCode ?? '',
    hsnCode: part?.hsnCode ?? '',
    drawingNumber: part?.drawingNumber ?? '',
    itemNumber: a?.itemNumber ?? '',
    technicalSpecification: a?.technicalSpecification ?? '',
    moc: a?.moc ?? '',
    partCategoryCode: a?.partCategoryCode ?? '',
    partType: a?.partType ?? '',
    formCategory: a?.formCategory ?? '',
    purchaseUomCode: a?.purchaseUomCode ?? '',
    sellingUomCode: a?.sellingUomCode ?? '',
    materialType: a?.materialType ?? '',
    seriesCode: a?.seriesCode ?? '',
    partRevisionNo: a?.partRevisionNo ?? '',
    sourceCode: a?.sourceCode ?? '',
    weightKg: numberToText(a?.weightKg),
    leadTimeDays: numberToText(a?.leadTimeDays),
    minimumStockLevel: numberToText(a?.minimumStockLevel),
    reorderPoint: numberToText(a?.reorderPoint),
  };
}

function numberToText(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : String(value);
}
