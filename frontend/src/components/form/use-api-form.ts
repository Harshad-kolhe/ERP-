'use client';

import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { useState } from 'react';
import { useForm, type DefaultValues, type FieldValues, type Path, type UseFormSetError } from 'react-hook-form';
import type { ZodType } from 'zod';

import { ApiError } from '@/lib/api/problem-details';

/**
 * One hook behind every create and edit screen.
 *
 * It does the thing that decides whether server-side validation is usable: when the
 * API rejects a submission, the per-field messages in the RFC 9457 response are
 * attached to the matching form fields, so the error appears under the input that
 * caused it rather than as a toast that says "Invalid request" and leaves the user
 * hunting.
 *
 * That is what lets FluentValidation stay the single authority. The Zod schema here
 * is for immediate feedback; when the two disagree, the server wins and the user
 * still sees exactly which field it objected to.
 */
export function useApiForm<TValues extends FieldValues, TResult = unknown>({
  schema,
  defaultValues,
  submit,
  onSuccess,
}: {
  // Both type arguments pinned to TValues. Left as ZodType<TValues> the input type
  // resolves to `unknown`, which zodResolver cannot reconcile with FieldValues.
  schema: ZodType<TValues, TValues>;
  defaultValues: DefaultValues<TValues>;
  submit: (values: TValues) => Promise<TResult>;
  onSuccess?: (result: TResult, values: TValues) => void;
}) {
  const [formError, setFormError] = useState<string | null>(null);

  // The third argument is the transformed-values type: without it, handleSubmit
  // hands the callback a bare FieldValues rather than TValues.
  const form = useForm<TValues, unknown, TValues>({
    resolver: zodResolver(schema),
    defaultValues,
    /*
     * Validate a field once the user has left it, then keep it live.
     *
     * The default is `onSubmit`, which on a form this size means filling in forty
     * fields and being told about the third one at the end. `onChange` is the
     * other extreme — it marks a required field invalid while you are still typing
     * the first character of it.
     *
     * `onTouched` over `onBlur` for what happens *after* the first error: both
     * first check on blur, but `onBlur` leaves the message sitting there while the
     * user corrects the field and only clears it when they leave again, so the
     * screen contradicts what is in the box.
     */
    mode: 'onTouched',
  });

  const mutation = useMutation({
    mutationFn: submit,
    onMutate: () => setFormError(null),
    onSuccess,
    onError: (error) => {
      if (error instanceof ApiError && error.isValidation && error.problem.errors) {
        const unmatched = applyServerErrors(error.problem.errors, form.setError, defaultValues);

        // A field the form does not have still has to be shown somewhere, or the
        // request fails silently and the user presses submit again forever.
        setFormError(unmatched.length > 0 ? unmatched.join(' ') : null);
        return;
      }

      setFormError(
        error instanceof ApiError
          ? (error.problem.detail ?? error.problem.title)
          : 'The request could not be completed.',
      );
    },
  });

  return {
    form,
    /** Wire to <form onSubmit={...}>. Client validation runs first; the server re-checks. */
    onSubmit: form.handleSubmit((values) => mutation.mutateAsync(values).catch(() => undefined)),
    isSubmitting: mutation.isPending,
    /** Form-level failure: a conflict, a server fault, or a field error with no matching input. */
    formError,
  };
}

/**
 * Attaches server field errors to the form.
 *
 * The API names fields as the C# property does — `PartNumber` — while the form uses
 * `partNumber`. Rather than guess at a general case conversion, the lookup is against
 * the form's own field names, so a mapping either matches a real input or is reported
 * as a form-level message instead of being dropped.
 *
 * Nested payloads are matched on their last segment. A request that groups fields —
 * `Attributes.WeightKg` — still renders one flat form, so without this the error for
 * a weight would surface as a form-level message with no indication of which of
 * twenty inputs to look at.
 *
 * @returns messages that matched no field.
 */
function applyServerErrors<TValues extends FieldValues>(
  errors: Record<string, string[]>,
  setError: UseFormSetError<TValues>,
  defaultValues: DefaultValues<TValues>,
): string[] {
  const fieldNames = Object.keys(defaultValues);
  const unmatched: string[] = [];

  for (const [key, messages] of Object.entries(errors)) {
    const message = messages[0];

    if (!message) {
      continue;
    }

    const leaf = key.slice(key.lastIndexOf('.') + 1);

    const field =
      fieldNames.find((name) => name.toLowerCase() === key.toLowerCase()) ??
      fieldNames.find((name) => name.toLowerCase() === leaf.toLowerCase());

    if (field) {
      setError(field as Path<TValues>, { type: 'server', message });
    } else {
      unmatched.push(message);
    }
  }

  return unmatched;
}
