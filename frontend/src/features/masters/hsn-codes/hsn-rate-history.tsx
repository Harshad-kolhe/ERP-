'use client';

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { z } from 'zod';

import { FormError, TextField } from '@/components/form/fields';
import { useApiForm } from '@/components/form/use-api-form';
import { Can } from '@/components/permission/can';
import { Button } from '@/components/ui/button';
import { Form } from '@/components/ui/form';
import { Spinner } from '@/components/ui/spinner';
import { apiFetch } from '@/lib/api/fetcher';
import type { HsnGstRate } from '@/lib/api/types';

import * as s from '../shared/form-schema';

interface AddRateValues {
  ratePercent: string;
  effectiveFrom: string;
}

/** Mirrors `AddHsnGstRateValidator`. */
const schema = z.object({
  ratePercent: s.taxRate('Rate').refine((value) => value.trim() !== '', { message: 'Rate is required.' }),
  effectiveFrom: z.string().trim().min(1, 'Effective from is required.'),
}) satisfies z.ZodType<AddRateValues, AddRateValues>;

/**
 * The rate history, and the one control that changes it.
 *
 * Deliberately not part of the form above. A rate is appended, never edited: the
 * reason the rates are a table at all is that an invoice raised last March must
 * still price at last March's rate, and an editable row would rewrite the tax on
 * every document that reads it. Rendering the history read-only with a single
 * "record a change" control is what makes that visible rather than a rule written
 * in a comment somewhere.
 *
 * A wrong rate is corrected by superseding it from the date the right one applies.
 */
export function HsnRateHistory({ id, rates }: { id: number; rates: HsnGstRate[] }) {
  const queryClient = useQueryClient();

  const add = useMutation({
    mutationFn: (values: AddRateValues) =>
      apiFetch<void>(`/masters/hsn-codes/${id}/rates`, {
        method: 'POST',
        body: JSON.stringify({
          ratePercent: Number(values.ratePercent),
          // A date input already produces yyyy-MM-dd, which is what a DateOnly
          // takes. Appending a time here would make the rate's start depend on the
          // reader's timezone.
          effectiveFrom: values.effectiveFrom,
        }),
      }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['masters', 'hsn-codes'] }),
  });

  const { form, onSubmit, isSubmitting, formError } = useApiForm<AddRateValues>({
    schema,
    defaultValues: { ratePercent: '', effectiveFrom: '' },
    submit: (values) => add.mutateAsync(values),
    onSuccess: () => {
      toast.success('Rate recorded.');
      form.reset({ ratePercent: '', effectiveFrom: '' });
    },
  });

  return (
    <section className="border-border bg-card rounded-lg border p-4">
      <h2 className="text-sm font-semibold">GST rate history</h2>
      <p className="text-muted-foreground mt-1 text-xs">
        Rates are added, never changed. A document keeps the rate that applied when it was raised.
      </p>

      {rates.length === 0 ? (
        <p className="text-muted-foreground mt-4 text-sm">No rate recorded yet.</p>
      ) : (
        <div className="mt-4 overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-muted-foreground border-border border-b text-left text-xs">
                <th scope="col" className="py-2 pr-4 font-medium">
                  Effective from
                </th>
                <th scope="col" className="py-2 text-right font-medium">
                  Rate
                </th>
              </tr>
            </thead>
            <tbody>
              {rates.map((rate) => (
                <tr key={rate.effectiveFrom} className="border-border/60 border-b last:border-0">
                  <td className="py-2 pr-4 font-mono">{rate.effectiveFrom}</td>
                  <td className="py-2 text-right tabular-nums">{rate.ratePercent}%</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Can permission="masters.referencedata.update">
        <Form {...form}>
          <form onSubmit={onSubmit} className="border-border mt-4 grid gap-3 border-t pt-4 sm:grid-cols-[1fr_1fr_auto]">
            <TextField<AddRateValues>
              name="ratePercent"
              label="New rate %"
              inputMode="decimal"
              disabled={isSubmitting}
            />
            <TextField<AddRateValues>
              name="effectiveFrom"
              label="Effective from"
              type="date"
              disabled={isSubmitting}
            />
            <div className="flex items-end">
              <Button type="submit" size="sm" variant="outline" disabled={isSubmitting}>
                {isSubmitting ? <Spinner className="size-4" /> : null}
                Record change
              </Button>
            </div>
          </form>
          <FormError message={formError} />
        </Form>
      </Can>
    </section>
  );
}
