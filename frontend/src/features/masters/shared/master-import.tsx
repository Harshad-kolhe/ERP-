'use client';

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Download, Upload, X } from 'lucide-react';
import Link from 'next/link';
import { useEffect, useRef, useState } from 'react';

import { Button } from '@/components/ui/button';
import { Spinner } from '@/components/ui/spinner';
import { ApiError, isProblemDetails } from '@/lib/api/problem-details';
import type { ImportResult } from '@/lib/api/types';

/**
 * The header button that opens the import dialog.
 *
 * Separate from the dialog because `MasterListScreen` is a server component and
 * cannot hold the open/closed state itself. Parts does not use this — its import
 * sits inside a menu of seven actions — but the dialog underneath is the same one.
 */
export function MasterImportAction({ resource, title }: { resource: string; title: string }) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button
        size="sm"
        variant="outline"
        onClick={() => setOpen(true)}
        title={`Upload a spreadsheet of ${title.toLowerCase()} records`}
      >
        <Upload className="size-4" aria-hidden />
        Import
      </Button>

      <MasterImportDialog
        resource={resource}
        title={title}
        open={open}
        onClose={() => setOpen(false)}
      />
    </>
  );
}

/**
 * Uploading a spreadsheet into a master, for whichever master is named.
 *
 * One component for all six imports because the API deliberately gives them one
 * shape — same route, same limits, same `ImportResultDto` — and a screen per master
 * would be six copies of this that drift the first time the report changes.
 *
 * A dialog rather than a page: the loop is upload, read the errors, fix the sheet,
 * upload again, and the list underneath is the thing being changed. Leaving the
 * screen to do it means coming back to a stale grid; here a committed import
 * invalidates the list and it refreshes behind the dialog before it closes.
 *
 * The report is the dialog's body, not a toast. An import fails a row at a time,
 * and "42 problems" is only actionable if the operator can read all 42 against the
 * sheet they are about to correct.
 */
export function MasterImportDialog({
  resource,
  title,
  open,
  onClose,
}: {
  resource: string;
  title: string;
  open: boolean;
  onClose: () => void;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [result, setResult] = useState<ImportResult | null>(null);
  const queryClient = useQueryClient();

  /*
   * A native `dialog`, opened imperatively because that is the only way to get a
   * modal one. `showModal()` is what buys the focus trap, the inert background,
   * Escape-to-close and the `::backdrop` — all of which a div would have to
   * reimplement, and all of which the popover above deliberately does not want.
   */
  useEffect(() => {
    const element = ref.current;
    if (!element) return;

    if (open && !element.open) element.showModal();
    if (!open && element.open) element.close();
  }, [open]);

  const upload = useMutation({
    mutationFn: (chosen: File) => importFile(resource, chosen),
    onSuccess: (report) => {
      setResult(report);
      if (report.committed) {
        setFile(null);
        void queryClient.invalidateQueries({ queryKey: ['masters', resource] });
      }
    },
  });

  return (
    <dialog
      ref={ref}
      aria-labelledby={`import-${resource}-title`}
      // `onClose` fires for Escape too, so state cannot drift out of step with
      // the element's own idea of whether it is open.
      onClose={() => {
        setFile(null);
        setResult(null);
        upload.reset();
        onClose();
      }}
      // The backdrop is part of the dialog element, so a click that lands on the
      // element itself rather than on the panel inside it is a click outside.
      onClick={(event) => {
        if (event.target === ref.current) ref.current?.close();
      }}
      className="border-border bg-card text-foreground m-auto w-[min(46rem,calc(100vw-2rem))] rounded-2xl border p-0 shadow-2xl backdrop:bg-black/50"
    >
      <div className="flex max-h-[85vh] flex-col">
        <div className="border-border flex items-start gap-4 border-b px-5 py-4">
          <div className="min-w-0">
            <h2 id={`import-${resource}-title`} className="text-[15px] font-semibold tracking-tight">
              Import {title}
            </h2>
            <p className="text-muted-foreground mt-1 text-xs">
              All or nothing: every row is checked first, and if anything is wrong nothing is
              written and every problem is listed here. Fix the sheet and upload it again.
            </p>
          </div>

          <button
            type="button"
            onClick={() => ref.current?.close()}
            aria-label="Close"
            className="text-muted-foreground hover:bg-accent hover:text-foreground ml-auto rounded-lg p-1.5"
          >
            <X className="size-4" aria-hidden />
          </button>
        </div>

        <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-auto p-5">
          <div className="flex flex-wrap items-center gap-3">
            <input
              type="file"
              accept=".xlsx"
              aria-label={`${title} spreadsheet`}
              onChange={(event) => {
                setFile(event.target.files?.[0] ?? null);
                setResult(null);
              }}
              className="text-muted-foreground file:border-border file:bg-muted file:text-foreground hover:file:bg-accent max-w-full text-xs file:mr-3 file:cursor-pointer file:rounded-lg file:border file:px-3 file:py-1.5 file:text-xs file:font-medium"
            />

            <Button
              size="sm"
              disabled={!file || upload.isPending}
              onClick={() => file && upload.mutate(file)}
            >
              {upload.isPending ? (
                <Spinner className="size-4" />
              ) : (
                <Upload className="size-4" aria-hidden />
              )}
              Upload
            </Button>

            <Link
              href={`/api/v1/masters/${resource}/import-template`}
              className="text-primary ml-auto inline-flex items-center gap-1.5 text-xs font-medium hover:underline"
            >
              <Download className="size-3.5" aria-hidden />
              Template
            </Link>
          </div>

          <p className="text-ink-faint text-[11px]">
            .xlsx only, up to 5,000 rows and 16 MB. A larger migration goes in several files.
          </p>

          {result && <Report result={result} />}
        </div>
      </div>
    </dialog>
  );
}

function Report({ result }: { result: ImportResult }) {
  if (result.committed) {
    return (
      <p role="status" className="border-border bg-muted/60 rounded-xl border p-4 text-sm">
        {result.importedRows.toLocaleString('en-IN')} of {result.totalRows.toLocaleString('en-IN')}{' '}
        rows imported. The list behind this dialog has been refreshed.
      </p>
    );
  }

  return (
    <div role="alert" className="border-destructive/40 rounded-xl border">
      <p className="text-destructive border-destructive/40 border-b px-4 py-3 text-sm font-medium">
        Nothing was imported. {result.errors.length.toLocaleString('en-IN')}
        {result.errorsTruncated ? '+' : ''} problem
        {result.errors.length === 1 ? '' : 's'} in {result.totalRows.toLocaleString('en-IN')} rows.
      </p>

      <div className="max-h-80 overflow-auto">
        <table className="w-full text-left text-xs">
          <thead className="bg-muted text-muted-foreground sticky top-0">
            <tr>
              <th scope="col" className="w-16 px-4 py-2 font-medium">Row</th>
              <th scope="col" className="w-56 px-4 py-2 font-medium">Column</th>
              <th scope="col" className="px-4 py-2 font-medium">Problem</th>
            </tr>
          </thead>
          <tbody>
            {result.errors.map((error, index) => (
              <tr key={`${error.row}-${error.column ?? ''}-${index}`} className="border-border border-t">
                <td className="px-4 py-2 tabular-nums">{error.row}</td>
                <td className="text-muted-foreground px-4 py-2">{error.column ?? '—'}</td>
                <td className="px-4 py-2">{error.message}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {result.errorsTruncated && (
        <p className="text-ink-faint border-destructive/40 border-t px-4 py-2 text-[11px]">
          Only the first {result.errors.length} problems are listed. Fix these and upload again to
          see the rest.
        </p>
      )}
    </div>
  );
}

/**
 * Not `apiFetch`: this posts multipart rather than JSON, and a rejected file comes
 * back as 422 carrying the whole report. That report is what the operator came for,
 * so it is a result here — only a response that is *not* a report is an error.
 */
async function importFile(resource: string, file: File): Promise<ImportResult> {
  const body = new FormData();
  body.append('file', file);

  // No content-type header: the browser must set it, because only it knows the
  // multipart boundary it generated.
  const response = await fetch(`/api/v1/masters/${resource}/import`, {
    method: 'POST',
    body,
    headers: { accept: 'application/json' },
    credentials: 'same-origin',
  });

  const text = await response.text();
  const payload: unknown = text ? JSON.parse(text) : undefined;

  if (isImportResult(payload)) {
    return payload;
  }

  throw new ApiError(
    isProblemDetails(payload)
      ? payload
      : { type: 'https://problems.erp/unexpected', title: 'Import failed', status: response.status },
  );
}

function isImportResult(value: unknown): value is ImportResult {
  return typeof value === 'object' && value !== null && 'committed' in value;
}
