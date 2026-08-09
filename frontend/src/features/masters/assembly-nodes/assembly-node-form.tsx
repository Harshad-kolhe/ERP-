'use client';

import { useRouter } from 'next/navigation';
import { useMemo } from 'react';
import { toast } from 'sonner';
import { z } from 'zod';

import { MasterForm } from '@/components/form/master-form';
import { useApiForm } from '@/components/form/use-api-form';
import type { AssemblyNodeDetail } from '@/lib/api/types';

import * as s from '../shared/form-schema';
import { useLookups } from '../shared/use-lookups';
import {
  blankToNull,
  blankToNumber,
  numberToInput,
  useSaveMasterRecord,
} from '../shared/use-master-record';
import {
  ASSEMBLY_NODE_LOOKUPS,
  assemblyNodeFormSections,
  type AssemblyNodeFormValues,
} from './assembly-node-form-fields';
import { sentenceCase, type AssemblyLevelScreen } from './assembly-node-level';

/**
 * Mirrors `CreateAssemblyNodeValidator` and `AssemblyNodeAttributesValidator`.
 *
 * A mirror, not the authority: this exists so a typo is caught before a round
 * trip. The server re-checks everything and wins any disagreement, and
 * `useApiForm` puts its per-field messages back under the right inputs.
 *
 * The parent is validated by level on the server — that it exists, and that it is
 * a section and not another sub-assembly — because only the server can see the
 * other rows. All this schema knows is whether the box was filled in.
 */
function buildSchema(requiresParent: boolean) {
  return z.object({
    code: z
      .string()
      .trim()
      .min(1, 'Code is required.')
      .max(30, 'Code must be 30 characters or fewer.')
      .regex(
        /^[A-Za-z0-9][A-Za-z0-9._/-]*$/,
        'Code may contain only letters, digits, dot, underscore, slash and hyphen.',
      ),
    name: s.requiredText(255, 'Name'),
    parentId: requiresParent
      ? z.string().trim().min(1, 'Choose the level above.')
      : z.string(),
    manualCode: s.text(50, 'Manual code'),
    machineType: s.code(),
    drivenBy: s.code(100),
    drawingPath: s.text(500, 'Drawing path'),
    technicalSpecification: s.text(2500, 'Technical specification'),
    remark: s.text(500, 'Remark'),
    quantity: s.quantity('Quantity', 999_999_999),
    weightKg: s.quantity('Weight'),
    displaySequence: s.wholeNumber('Sequence'),
    isActive: z.boolean(),
  }) satisfies z.ZodType<AssemblyNodeFormValues, AssemblyNodeFormValues>;
}

/**
 * Create and edit, for all three levels, in one component.
 *
 * The level arrives as a screen definition rather than being baked in, so the
 * three routes cannot drift apart in what they send or how they behave — which is
 * exactly what happened to the legacy system's three save methods.
 */
export function AssemblyNodeForm({
  screen,
  node,
}: {
  screen: AssemblyLevelScreen;
  node?: AssemblyNodeDetail;
}) {
  const router = useRouter();
  const isNew = !node;
  const listHref = `/masters/${screen.resource}`;
  const { lookups, isError: lookupsFailed } = useLookups(ASSEMBLY_NODE_LOOKUPS);

  const schema = useMemo(() => buildSchema(screen.parent !== null), [screen.parent]);

  const save = useSaveMasterRecord<AssemblyNodeFormValues>({
    resource: screen.resource,
    id: node?.id,
    rowVersion: node?.rowVersion,
    toBody: (values) => ({
      // The code is only sent on create: it is the business key, and the update
      // endpoint does not accept it.
      ...(isNew ? { code: values.code.trim() } : {}),
      name: values.name.trim(),
      parentId: screen.parent ? blankToNull(values.parentId) : null,
      isActive: values.isActive,

      // Sent whole. The update endpoint is a replace, not a patch: a field left
      // out is cleared, which is what makes a value deletable.
      attributes: {
        manualCode: blankToNull(values.manualCode),
        machineType: blankToNull(values.machineType),
        drivenBy: blankToNull(values.drivenBy),
        drawingPath: blankToNull(values.drawingPath),
        technicalSpecification: blankToNull(values.technicalSpecification),
        remark: blankToNull(values.remark),
        quantity: blankToNumber(values.quantity),
        weightKg: blankToNumber(values.weightKg),
        displaySequence: blankToNumber(values.displaySequence),
      },
    }),
  });

  const parentLabel = node?.parentCode
    ? `${node.parentCode} — ${node.parentName ?? ''}`.trim().replace(/—\s*$/, '').trim()
    : null;

  const sections = useMemo(
    () => assemblyNodeFormSections(screen, isNew, parentLabel),
    [screen, isNew, parentLabel],
  );

  const { form, onSubmit, isSubmitting, formError } = useApiForm<AssemblyNodeFormValues>({
    schema,
    defaultValues: toFormValues(node),
    submit: (values) => save.mutateAsync(values),
    onSuccess: () => {
      toast.success(isNew ? `${sentenceCase(screen.noun)} created.` : `${sentenceCase(screen.noun)} updated.`);
      router.push(listHref);
      router.refresh();
    },
  });

  return (
    <MasterForm<AssemblyNodeFormValues>
      sections={sections}
      form={form}
      onSubmit={onSubmit}
      isSubmitting={isSubmitting}
      formError={formError}
      submitLabel={isNew ? `Create ${screen.noun}` : 'Save changes'}
      onCancel={() => router.push(listHref)}
      lookups={lookups}
      lookupsFailed={lookupsFailed}
      title={isNew ? `New ${screen.noun}` : `Edit ${screen.noun}`}
      backLabel={screen.plural}
      identityCode={node?.code}
      badges={node ? [{ label: node.isActive ? 'Active' : 'Inactive', tone: node.isActive ? 'ok' : 'neutral' }] : []}
      auditLine={node ? `Created ${new Date(node.createdAtUtc).toLocaleDateString('en-IN')}${node.modifiedAtUtc ? ` · Modified ${new Date(node.modifiedAtUtc).toLocaleDateString('en-IN')}` : ''}` : null}
    />
  );
}

/**
 * Every field defaults to "" rather than undefined, so each input is controlled
 * from the first render — React warns the moment one flips from uncontrolled to
 * controlled, and whatever was typed before the flip is lost.
 */
function toFormValues(node?: AssemblyNodeDetail): AssemblyNodeFormValues {
  const a = node?.attributes;

  return {
    code: node?.code ?? '',
    name: node?.name ?? '',
    parentId: node?.parentId ?? '',
    manualCode: a?.manualCode ?? '',
    machineType: a?.machineType ?? '',
    drivenBy: a?.drivenBy ?? '',
    drawingPath: a?.drawingPath ?? '',
    technicalSpecification: a?.technicalSpecification ?? '',
    remark: a?.remark ?? '',
    quantity: numberToInput(a?.quantity),
    weightKg: numberToInput(a?.weightKg),
    displaySequence: numberToInput(a?.displaySequence),
    isActive: node?.isActive ?? true,
  };
}
