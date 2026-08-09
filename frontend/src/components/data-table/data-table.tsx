'use client';

import { flexRender, getCoreRowModel, useReactTable, type ColumnDef } from '@tanstack/react-table';
import type { PagedResult } from '@/lib/api/types';
import type { ServerTableState } from './use-server-table';

interface DataTableProps<TRow> {
  columns: ColumnDef<TRow, unknown>[];
  page: PagedResult<TRow> | undefined;
  isLoading: boolean;
  state: ServerTableState;
  onPageChange: (patch: Partial<ServerTableState>) => void;
  onToggleSort: (field: string) => void;
  emptyMessage?: string;
}

/**
 * Renders one server-supplied page.
 *
 * Note `manualPagination` and `manualSorting`: TanStack Table is told it is
 * looking at a slice, not the whole set, so it never attempts to sort or paginate
 * client-side. `rowCount` comes from the server's `totalCount`, which is what
 * lets the pager render correctly while only one page is in memory.
 */
export function DataTable<TRow>({
  columns,
  page,
  isLoading,
  state,
  onPageChange,
  onToggleSort,
  emptyMessage = 'No records match the current filters.',
}: DataTableProps<TRow>) {
  const table = useReactTable({
    data: page?.items ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
    manualSorting: true,
    manualFiltering: true,
    rowCount: page?.totalCount ?? 0,
  });

  const [sortField, sortDirection] = state.sort?.split(':') ?? [];

  return (
    <div className="space-y-3">
      <div className="overflow-x-auto rounded-md border border-slate-200">
        <table className="w-full min-w-max text-sm">
          <thead className="bg-slate-50 text-left">
            {table.getHeaderGroups().map((headerGroup) => (
              <tr key={headerGroup.id}>
                {headerGroup.headers.map((header) => {
                  const field = header.column.columnDef.id;
                  const sortable = header.column.columnDef.enableSorting !== false && field;

                  return (
                    <th key={header.id} className="px-3 py-2 font-medium text-slate-700">
                      {sortable ? (
                        <button
                          type="button"
                          className="inline-flex items-center gap-1 hover:text-slate-900"
                          onClick={() => onToggleSort(field)}
                        >
                          {flexRender(header.column.columnDef.header, header.getContext())}
                          {sortField === field ? <span aria-hidden>{sortDirection === 'desc' ? '▼' : '▲'}</span> : null}
                        </button>
                      ) : (
                        flexRender(header.column.columnDef.header, header.getContext())
                      )}
                    </th>
                  );
                })}
              </tr>
            ))}
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan={columns.length} className="px-3 py-8 text-center text-slate-500">
                  Loading…
                </td>
              </tr>
            ) : table.getRowModel().rows.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-3 py-8 text-center text-slate-500">
                  {emptyMessage}
                </td>
              </tr>
            ) : (
              table.getRowModel().rows.map((row) => (
                <tr key={row.id} className="border-t border-slate-100 hover:bg-slate-50">
                  {row.getVisibleCells().map((cell) => (
                    <td key={cell.id} className="px-3 py-2">
                      {flexRender(cell.column.columnDef.cell, cell.getContext())}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="flex items-center justify-between text-sm text-slate-600">
        <span>
          {page ? (
            <>
              Page {page.page} of {Math.max(page.totalPages ?? 0, 1)} · {page.totalCount} records
            </>
          ) : null}
        </span>
        <div className="flex gap-2">
          <button
            type="button"
            className="rounded border border-slate-300 px-3 py-1 disabled:opacity-40"
            disabled={!page?.hasPreviousPage}
            onClick={() => onPageChange({ page: state.page - 1 })}
          >
            Previous
          </button>
          <button
            type="button"
            className="rounded border border-slate-300 px-3 py-1 disabled:opacity-40"
            disabled={!page?.hasNextPage}
            onClick={() => onPageChange({ page: state.page + 1 })}
          >
            Next
          </button>
        </div>
      </div>
    </div>
  );
}
