'use client';

import { useRouter } from 'next/navigation';
import { useMemo, useState } from 'react';
import { toast } from 'sonner';
import { z } from 'zod';

import { CheckboxField, FormError, SelectField, TextField } from '@/components/form/fields';
import { ReferenceField, referenceSource } from '@/components/form/reference-field';
import { useApiForm } from '@/components/form/use-api-form';
import { Button } from '@/components/ui/button';
import { Form } from '@/components/ui/form';
import { Spinner } from '@/components/ui/spinner';
import type { AssemblyNodeListItem, ParentPartDetail, PartListItem } from '@/lib/api/types';

import * as s from '../shared/form-schema';
import { useLookups } from '../shared/use-lookups';
import { blankToNull, blankToNumber, useSaveMasterRecord } from '../shared/use-master-record';
import { ComponentLines, newComponentLine, type ComponentLine } from './component-lines';

const LOOKUPS = ['uom', 'part.categoryCode'] as const;

/** Mirrors `CreateParentPartValidator`. The server re-checks and wins any disagreement. */
const schema = z.object({
  partId: z.string().trim().min(1, 'Choose the part this build is for.'),
  description: s.text(255, 'Build description'),
  assemblyNodeId: z.string(),
  unitOfMeasureCode: s.code(10),
  drawingNumber: s.text(50, 'Drawing number'),
  category: s.code(),
  isActive: z.boolean(),
});

type ParentPartFormValues = z.infer<typeof schema>;

/**
 * The Parent Part screen — a header and a component grid, saved together.
 *
 * It does not use `MasterForm`, and that is a deliberate exception rather than an
 * oversight: `MasterForm` lays out a flat list of fields in tabs, which is right
 * for a supplier and wrong here. A build is read down a column — people compare
 * quantities and weights across lines — so the lines need a grid, and a grid does
 * not fit a field list.
 *
 * What it does keep is everything that matters for correctness: the same
 * `useApiForm`, so server-side validation messages land under the right inputs;
 * the same field primitives; and the same replace-the-whole-record save.
 */
export function ParentPartForm({ parentPart }: { parentPart?: ParentPartDetail }) {
  const router = useRouter();
  const isNew = !parentPart;
  const { lookups } = useLookups(LOOKUPS);

  /**
   * The lines live outside react-hook-form.
   *
   * They are a repeating structure with their own add and remove, and modelling
   * them as a field array would put a second, subtly different validation path
   * next to the one the server owns. They are validated on the server, which is
   * where the part master can actually be consulted, and the obvious local
   * mistakes — no part chosen, a duplicate, a zero quantity — are caught here
   * before the request goes out.
   */
  const [lines, setLines] = useState<ComponentLine[]>(() => toLines(parentPart));
  const [lineError, setLineError] = useState<string | null>(null);

  const partSource = useMemo(
    () =>
      referenceSource<PartListItem>({
        resource: 'parts',
        filter: 'isActive:eq:true',
        searchPlaceholder: 'Search part number or description…',
        toOption: (row) => ({ value: row.id, label: row.partNumber, hint: row.description }),
      }),
    [],
  );

  const assemblySource = useMemo(
    () =>
      referenceSource<AssemblyNodeListItem>({
        resource: 'sub-assemblies',
        filter: 'isActive:eq:true',
        searchPlaceholder: 'Search sub-assembly code or name…',
        toOption: (row) => ({ value: row.id, label: row.code, hint: row.name }),
      }),
    [],
  );

  const save = useSaveMasterRecord<ParentPartFormValues>({
    resource: 'parent-parts',
    id: parentPart?.id,
    rowVersion: parentPart?.rowVersion,
    toBody: (values) => ({
      // The part is only sent on create: which part a build describes is its
      // identity, and the update endpoint does not accept it.
      ...(isNew ? { partId: values.partId } : {}),
      description: blankToNull(values.description),
      assemblyNodeId: blankToNull(values.assemblyNodeId),
      unitOfMeasureCode: blankToNull(values.unitOfMeasureCode),
      drawingNumber: blankToNull(values.drawingNumber),
      category: blankToNull(values.category),
      isActive: values.isActive,

      // The complete list as it should end up, not a set of changes. Amount and
      // line weight are absent on purpose — the server computes them.
      components: lines.map((line) => ({
        partId: line.partId,
        quantity: blankToNumber(line.quantity) ?? 0,
        unitOfMeasureCode: blankToNull(line.unitOfMeasureCode),
        unitWeightKg: blankToNumber(line.unitWeightKg),
        rate: blankToNumber(line.rate),
        drawingNumber: blankToNull(line.drawingNumber),
        remark: blankToNull(line.remark),
      })),
    }),
  });

  const { form, onSubmit, isSubmitting, formError } = useApiForm<ParentPartFormValues>({
    schema,
    defaultValues: toFormValues(parentPart),
    submit: async (values) => {
      const problem = firstLineProblem(lines);

      if (problem) {
        setLineError(problem);
        throw new Error(problem);
      }

      setLineError(null);
      await save.mutateAsync(values);
    },
    onSuccess: () => {
      toast.success(isNew ? 'Parent part created.' : 'Parent part updated.');
      router.push('/masters/parent-parts');
      router.refresh();
    },
  });

  const partLabel = parentPart
    ? `${parentPart.partNumber} — ${parentPart.partDescription}`
    : null;

  const assemblyLabel = parentPart?.assemblyCode
    ? `${parentPart.assemblyCode} — ${parentPart.assemblyName ?? ''}`.replace(/—\s*$/, '').trim()
    : null;

  return (
    <Form {...form}>
      <form onSubmit={onSubmit} noValidate className="flex min-h-0 flex-1 flex-col">
        <div className="min-h-0 flex-1 overflow-y-auto p-6">
          <section className="max-w-4xl">
            <h2 className="text-ink mb-1 text-sm font-semibold">Build</h2>
            <p className="text-ink-2 mb-4 text-sm">
              {isNew
                ? 'Choose the part being built. A part may have one build; if it already has one you will be told.'
                : 'The part being built cannot be changed — that would re-point the whole build at something else.'}
            </p>

            <div className="grid gap-4 sm:grid-cols-2">
              {isNew ? (
                <ReferenceField<ParentPartFormValues>
                  name="partId"
                  label="Parent part"
                  required
                  source={partSource}
                  description="Searched on the server — type a part number or description."
                />
              ) : (
                <div className="sm:col-span-2">
                  <p className="text-ink-2 text-xs font-medium">Parent part</p>
                  <p className="text-ink font-mono text-sm">{partLabel}</p>
                </div>
              )}

              <TextField<ParentPartFormValues>
                name="description"
                label="Build description"
                description="Optional. The part's own description is shown when this is blank."
              />

              <ReferenceField<ParentPartFormValues>
                name="assemblyNodeId"
                label="Sub-assembly"
                source={assemblySource}
                initialLabel={assemblyLabel}
                description="Optional. Where this build sits in the machine breakdown."
              />

              <SelectField<ParentPartFormValues>
                name="unitOfMeasureCode"
                label="Unit of measure"
                options={optionsOf(lookups, 'uom')}
              />

              <SelectField<ParentPartFormValues>
                name="category"
                label="Category"
                options={optionsOf(lookups, 'part.categoryCode')}
              />

              <TextField<ParentPartFormValues> name="drawingNumber" label="Drawing number" />

              <CheckboxField<ParentPartFormValues> name="isActive" label="Active" />
            </div>
          </section>

          <section className="mt-8">
            <h2 className="text-ink mb-1 text-sm font-semibold">Components</h2>
            <p className="text-ink-2 mb-4 max-w-3xl text-sm">
              Line weight and amount are computed from the quantity — the figures below are a
              preview of what the server stores, and the totals on the grid come from the same
              calculation rather than from anything typed in.
            </p>

            <ComponentLines lines={lines} onChange={setLines} disabled={isSubmitting} />
          </section>
        </div>

        <div className="border-line bg-surface flex items-center justify-between gap-3 border-t px-6 py-3">
          <div className="min-w-0 flex-1">
            <FormError message={lineError ?? formError} />
          </div>

          <div className="flex shrink-0 items-center gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => router.push('/masters/parent-parts')}
              disabled={isSubmitting}
            >
              Cancel
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? <Spinner className="mr-2 size-4" /> : null}
              {isNew ? 'Create parent part' : 'Save changes'}
            </Button>
          </div>
        </div>
      </form>
    </Form>
  );
}

/** The first thing wrong with the lines, or null. Mirrors what the server checks. */
function firstLineProblem(lines: ComponentLine[]): string | null {
  const seen = new Set<string>();

  for (const [index, line] of lines.entries()) {
    if (!line.partId) {
      return `Line ${index + 1} has no component part.`;
    }

    if (seen.has(line.partId)) {
      return `Line ${index + 1} repeats a part already on the build. Change its quantity instead.`;
    }

    seen.add(line.partId);

    const quantity = Number(line.quantity.trim());

    if (!Number.isFinite(quantity) || quantity <= 0) {
      return `Line ${index + 1} needs a quantity greater than zero.`;
    }
  }

  return null;
}

function toFormValues(parentPart?: ParentPartDetail): ParentPartFormValues {
  return {
    partId: parentPart?.partId ?? '',
    description: parentPart?.description ?? '',
    assemblyNodeId: parentPart?.assemblyNodeId ?? '',
    unitOfMeasureCode: parentPart?.unitOfMeasureCode ?? '',
    drawingNumber: parentPart?.drawingNumber ?? '',
    category: parentPart?.category ?? '',
    isActive: parentPart?.isActive ?? true,
  };
}

function toLines(parentPart?: ParentPartDetail): ComponentLine[] {
  if (!parentPart) return [newComponentLine()];

  return parentPart.components.map((component) => ({
    key: crypto.randomUUID(),
    partId: component.partId,
    partLabel: component.partNumber
      ? `${component.partNumber} — ${component.partDescription ?? ''}`.replace(/—\s*$/, '').trim()
      : null,
    quantity: String(component.quantity),
    unitOfMeasureCode: component.unitOfMeasureCode ?? '',
    unitWeightKg: component.unitWeightKg === null ? '' : String(component.unitWeightKg),
    rate: component.rate === null ? '' : String(component.rate),
    drawingNumber: component.drawingNumber ?? '',
    remark: component.remark ?? '',
  }));
}

function optionsOf(
  lookups: Record<string, { code: string; name: string }[]>,
  type: string,
): { value: string; label: string }[] {
  return (lookups[type] ?? []).map((option) => ({ value: option.code, label: option.name }));
}
