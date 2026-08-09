'use client';

import type { ColumnDef } from '@tanstack/react-table';
import Link from 'next/link';
import { useMemo } from 'react';

import { DataTable } from '@/components/data-table/data-table';
import { useServerTable } from '@/components/data-table/use-server-table';
import type { AdminRoleListItem } from '@/lib/api/types';

import { useRoles } from './use-roles';

export function RolesTable() {
  const { state, apply, toggleSort, queryString } = useServerTable();
  const { data, isFetching } = useRoles(queryString);

  const columns = useMemo<ColumnDef<AdminRoleListItem, unknown>[]>(
    () => [
      {
        id: 'name',
        header: 'Role',
        accessorKey: 'name',
        enableSorting: false,
        cell: ({ row }) => (
          <Link
            href={`/admin/roles/${row.original.id}`}
            className="text-primary font-medium underline-offset-4 hover:underline"
          >
            {row.original.name}
          </Link>
        ),
      },
      {
        id: 'description',
        header: 'Description',
        enableSorting: false,
        cell: ({ row }) => (
          <span className="text-muted-foreground font-sans">{row.original.description || '—'}</span>
        ),
      },
      {
        id: 'permissionCount',
        header: 'Permissions',
        enableSorting: false,
        // "All" rather than a count: a super-administrator role stores no permission
        // rows, so its count is zero — which would read as "grants nothing", the
        // exact opposite of the truth.
        cell: ({ row }) =>
          row.original.isSuperAdministrator ? (
            <span className="text-primary font-medium">All</span>
          ) : (
            row.original.permissionCount
          ),
      },
      {
        id: 'userCount',
        header: 'Users',
        enableSorting: false,
        // Shown because editing a role that nobody holds is safe, and editing one
        // that fifty people hold is not. The number is the warning.
        cell: ({ row }) => row.original.userCount,
      },
    ],
    [],
  );

  return (
    <DataTable
      columns={columns}
      page={data}
      isLoading={isFetching && !data}
      state={state}
      onPageChange={apply}
      onToggleSort={toggleSort}
      emptyMessage="No roles yet."
    />
  );
}
