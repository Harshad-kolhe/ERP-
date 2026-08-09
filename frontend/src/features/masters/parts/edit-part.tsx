'use client';

import { Spinner } from '@/components/ui/spinner';

import { PartForm } from './part-form';
import { usePart } from './use-parts';

/**
 * Loads the part, then hands it to the form.
 *
 * Split from the page so the form can take a fully-loaded part rather than an
 * optional one. Seeding a form with undefined and filling it in when the fetch
 * lands means react-hook-form registers its defaults before the data exists, and
 * every field would need resetting afterwards — which discards anything typed in
 * the meantime.
 */
export function EditPart({ id }: { id: string }) {
  const { data, isPending, isError, error } = usePart(id);

  if (isPending) {
    return (
      <div className="text-ink-2 flex items-center gap-2 p-6 text-sm">
        <Spinner className="size-4" />
        Loading part…
      </div>
    );
  }

  if (isError) {
    return (
      <p role="alert" className="text-destructive p-6 text-sm">
        {error instanceof Error ? error.message : 'This part could not be loaded.'}
      </p>
    );
  }

  return <PartForm part={data} />;
}
