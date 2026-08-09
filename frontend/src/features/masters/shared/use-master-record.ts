'use client';

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { apiFetch } from '@/lib/api/fetcher';

/**
 * One record, for an edit screen.
 *
 * Generic over the resource rather than one hook per master: every detail endpoint
 * under `/masters` has the same shape — `GET /masters/{resource}/{id}` returning a
 * DTO with a `rowVersion` — so five copies would differ only in a string literal.
 */
export function useMasterRecord<TDetail>(resource: string, id: string | number) {
  return useQuery({
    queryKey: ['masters', resource, 'detail', String(id)],
    queryFn: () => apiFetch<TDetail>(`/masters/${resource}/${id}`),
  });
}

/**
 * Creates or updates a master record.
 *
 * The two are one mutation because the form is one form. The differences are the
 * verb, the business key (present only on create — it is not editable afterwards)
 * and the row version, which must return exactly as it arrived so a concurrent
 * edit yields 409 rather than silently overwriting the other person's work.
 *
 * `toBody` is supplied per master: it converts form values, which are all strings
 * because inputs produce strings, into the typed payload the API expects.
 */
export function useSaveMasterRecord<TValues>({
  resource,
  id,
  toBody,
  rowVersion,
}: {
  resource: string;
  /** Absent for a create. */
  id?: string | number;
  toBody: (values: TValues) => Record<string, unknown>;
  /** Required when `id` is set. */
  rowVersion?: string;
}) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (values: TValues): Promise<void> => {
      const body = toBody(values);

      if (id !== undefined) {
        await apiFetch<void>(`/masters/${resource}/${id}`, {
          method: 'PUT',
          body: JSON.stringify({ ...body, rowVersion }),
        });
        return;
      }

      await apiFetch<{ id: number }>(`/masters/${resource}`, {
        method: 'POST',
        body: JSON.stringify(body),
      });
    },
    // Every cached page of this master is now potentially wrong, and there is no
    // way to know which without re-running the server's filter.
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['masters', resource] }),
  });
}

/**
 * An untouched text box holds "", and "" is not the same statement as null: the
 * first says "I typed nothing", the second says "this record has no GST number".
 * The API models absence as null, so the mapping happens here rather than sending
 * empty strings the server would have to guess about.
 */
export function blankToNull(value: string | null | undefined): string | null {
  const trimmed = (value ?? '').trim();
  return trimmed === '' ? null : trimmed;
}

/** As {@link blankToNull}, for a numeric input. Non-numeric text becomes null and the server rejects it. */
export function blankToNumber(value: string | null | undefined): number | null {
  const trimmed = (value ?? '').trim();
  if (trimmed === '') return null;

  const parsed = Number(trimmed);
  return Number.isFinite(parsed) ? parsed : null;
}

/** A date input produces `yyyy-MM-dd`; the API takes an offset timestamp. */
export function blankToDate(value: string | null | undefined): string | null {
  const trimmed = (value ?? '').trim();
  return trimmed === '' ? null : `${trimmed}T00:00:00+00:00`;
}

/** The reverse: an ISO timestamp back into what a date input renders. */
export function dateToInput(value: string | null | undefined): string {
  return value ? value.slice(0, 10) : '';
}

/** A number back into text, keeping "" for absent rather than "0". */
export function numberToInput(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : String(value);
}
