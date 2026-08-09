'use client';

import { useMemo } from 'react';

import { usePermissions } from '@/components/permission/session-provider';
import type { TreeColumn } from '@/components/tree-list/tree-list';
import type { EmployeeListItem } from '@/lib/api/types';
import { EMPLOYEE_FILTERS } from '../shared/master-filter-fields';
import { MasterTreeList } from '../shared/master-tree-list';
import {
  activeColumn,
  dateColumn,
  lookupColumn,
  numberColumn,
  optionalBooleanColumn,
  serialNumberColumn,
  statusColumn,
  tagsColumn,
  textColumn,
} from '../shared/master-columns';

/** Legacy codes, stored as `01` and `02`. */
const GENDER = { '01': 'Male', '02': 'Female' };

/**
 * The Employee Master grid — the legacy column set, with two deliberate departures.
 *
 * The legacy grid carried a `Password` column showing the stored value in clear
 * text to anyone who could open the screen. It is not here and will not be: sign-in
 * runs on a PBKDF2 hash that cannot be displayed even if someone wanted it.
 *
 * The pay columns are behind `masters.employee.payroll.read`. Everyone who raises a
 * job card needs to look an employee up; almost nobody needs to know what they
 * earn, and the legacy screen put both behind the same single check. Hiding them
 * here is only tidiness — the server sends null and refuses to sort on them for a
 * caller without the permission, which is the check that counts.
 */
export function EmployeesTable() {
  const { can } = usePermissions();
  const canReadPayroll = can("masters.employee.payroll.read");
  const canEdit = can("masters.employee.update");

  /**
   * `dataField` must match a field on the server's `ListEmployeesHandler.Map`.
   * The name column sorts on `firstName`: the grid shows the joined full name,
   * but the server keeps the parts separate so the sort can use the name index.
   */
  const columns = useMemo<TreeColumn<EmployeeListItem>[]>(() => {
    const base: TreeColumn<EmployeeListItem>[] = [
      serialNumberColumn<EmployeeListItem>(),

      // Everyone writes and says "PPE/1043", so that is what the column shows.
      {
        ...textColumn<EmployeeListItem>('employeeCode', 'Employee code', 150, { mono: true }),
        align: 'center',
        calculateCellValue: (row) =>
          row.employeeCode === null || row.employeeCode === undefined
            ? ''
            : `PPE/${row.employeeCode}`,
      },

      textColumn('firstName', 'First name', 160),
      textColumn('lastName', 'Last name', 160),
      lookupColumn<EmployeeListItem>('gender', 'Gender', GENDER, 110),
      textColumn('address', 'Employee address', 260),
      textColumn('userName', 'User name', 160),
      textColumn('roleName', 'Role name', 170),
      textColumn('designation', 'Designation', 180),
      textColumn('phoneNo', 'Phone no', 150),
      dateColumn('dateOfBirth', 'Date of birth', 140),
      dateColumn('joiningDate', 'Date of joining', 150),

      // Legacy stored this inverted — its lookup mapped false to "Married". The
      // column here reads the field by its name, so the label matches the value.
      {
        ...optionalBooleanColumn<EmployeeListItem>('isMarried', 'Marital status', 140),
        calculateCellValue: (row) => (row.isMarried ? 'Married' : 'Unmarried'),
      },

      textColumn('bloodGroup', 'Blood group', 140, { align: 'center' }),
      numberColumn('shoeSize', 'Shoe size', 120),
      textColumn('aadharNo', 'Aadhar card no.', 170, { mono: true }),
      textColumn('panNo', 'Pan card no.', 150, { mono: true }),
      textColumn('passportNo', 'Passport no.', 150, { mono: true }),
      textColumn('qualification', 'Qualification', 180),
      tagsColumn<EmployeeListItem>('skills', 'Skills', 220),
      tagsColumn<EmployeeListItem>('strengths', 'Strength', 200),
      optionalBooleanColumn<EmployeeListItem>('isOverTimeApplicable', 'Is over time applicable', 190),
      optionalBooleanColumn<EmployeeListItem>('willingToTravel', 'Willing to travel', 160),
      optionalBooleanColumn<EmployeeListItem>('applicableForService', 'Applicable for service', 190),
      textColumn('department', 'Department', 170),
      textColumn('email', 'Email', 220),
      textColumn('businessUnit', 'Business unit', 190),
    ];

    // Currency amounts: two decimals, right-aligned, so a column of pay reads as
    // a column of pay rather than a ragged edge.
    const payroll: TreeColumn<EmployeeListItem>[] = [
      numberColumn('providentFund', 'Provident fund (PF)', 170, { decimals: 2 }),
      numberColumn('employeeStateInsurance', 'Employee state insurance (ESI)', 230, { decimals: 2 }),
      numberColumn('professionalTax', 'Professional tax (PT)', 180, { decimals: 2 }),
      numberColumn('incomeTaxTds', 'Income tax (TDS)', 160, { decimals: 2 }),
      numberColumn('netSalary', 'Net salary', 150, { decimals: 2 }),
      numberColumn('grossSalary', 'Gross salary', 150, { decimals: 2 }),
      numberColumn('perHourSalary', 'Per hour salary', 160, { decimals: 2 }),
    ];

    const tail: TreeColumn<EmployeeListItem>[] = [
      textColumn('createdBy', 'Created by', 150),
      dateColumn('createdAt', 'Created date', 140, 'createdAtUtc'),
      textColumn('modifiedBy', 'Modified by', 150),
      dateColumn('modifiedAt', 'Modified date', 140, 'modifiedAtUtc'),
      activeColumn<EmployeeListItem>(),
      statusColumn<EmployeeListItem>(),
    ];

    return canReadPayroll ? [...base, ...payroll, ...tail] : [...base, ...tail];
  }, [canReadPayroll]);

  return (
    <MasterTreeList<EmployeeListItem>
      resource="employees"
      filters={EMPLOYEE_FILTERS}
      filtersNoun="Employee"
      columns={columns}
      keyField="id"
      stretchColumn="address"
      searchPlaceholder="Search name, user name or email…"
      ariaLabel="Employees"
      emptyTitle="No employees"
      emptyHint="No employees match the current filters."
      exportFileName="Employees"
      editHref={(row) => `/masters/employees/${row.id}`}
      canEdit={canEdit}
    />
  );
}
