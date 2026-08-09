'use client';

import type { ReactNode } from 'react';

import { Spinner } from '@/components/ui/spinner';
import { ApiError } from '@/lib/api/problem-details';

import { useMasterRecord } from './use-master-record';

/**
 * Loads a record, then hands it to a form.
 *
 * Split from the page so each form takes a fully-loaded record rather than an
 * optional one. Seeding a form with undefined and filling it in when the fetch
 * lands means react-hook-form registers its defaults before the data exists, and
 * every field then needs resetting — which discards anything typed meanwhile.
 */
export function EditMasterRecord<TDetail>({
  resource,
  id,
  noun,
  children,
}: {
  resource: string;
  id: string;
  /** Used in the messages, e.g. "supplier".  */
  noun: string;
  children: (record: TDetail) => ReactNode;
}) {
  const { data, isPending, isError, error } = useMasterRecord<TDetail>(resource, id);

  if (isPending) {
    return (
      <div className="text-ink-2 flex items-center gap-2 p-6 text-sm">
        <Spinner className="size-4" />
        Loading {noun}…
      </div>
    );
  }

  if (isError) {
    // A record in another business unit is filtered out server-side and comes back
    // as 404, so this wording covers both "deleted" and "not yours" without
    // confirming which — the server is deliberately not telling us either.
    const message =
      error instanceof ApiError && error.problem.status === 404
        ? `This ${noun} no longer exists, or you do not have access to it.`
        : `This ${noun} could not be loaded.`;

    return (
      <p role="alert" className="text-destructive p-6 text-sm">
        {message}
      </p>
    );
  }

  return <>{children(data)}</>;
}
