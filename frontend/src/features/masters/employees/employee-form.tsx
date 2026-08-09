'use client';

import { useRouter } from 'next/navigation';
import { useMemo } from 'react';
import { toast } from 'sonner';
import { z } from 'zod';

import { MasterForm } from '@/components/form/master-form';
import { usePermissions } from '@/components/permission/session-provider';
import { useApiForm } from '@/components/form/use-api-form';
import type { EmployeeDetail } from '@/lib/api/types';

import * as s from '../shared/form-schema';
import { useLookups } from '../shared/use-lookups';
import {
  blankToDate,
  blankToNull,
  blankToNumber,
  dateToInput,
  numberToInput,
  useSaveMasterRecord,
} from '../shared/use-master-record';
import { EMPLOYEE_LOOKUPS, employeeFormSections, type EmployeeFormValues } from './employee-form-fields';

/** Mirrors `SaveEmployeeValidator`. The server re-checks and wins any disagreement. */
const schema = z
  .object({
    employeeCode: s.wholeNumber('Employee code').refine((value) => value.trim() !== '', {
      message: 'Employee code is required.',
    }),
    firstName: s.requiredText(100, 'First name'),
    middleName: s.text(100, 'Middle name'),
    lastName: s.text(100, 'Last name'),
    gender: s.code(20),
    dateOfBirth: z.string(),
    isMarried: z.boolean(),
    bloodGroup: s.code(10),
    address: s.text(500, 'Employee address'),
    state: s.text(100, 'State'),
    phoneNo: s.text(30, 'Phone no'),
    email: s.email('Email'),
    userName: s.text(100, 'User name'),
    roleId: s.wholeNumber('Role id'),
    department: s.code(100),
    designation: s.code(100),
    joiningDate: z.string(),
    qualification: s.code(200),
    skills: z.string(),
    strengths: z.string(),
    shoeSize: s.wholeNumber('Shoe size', 30),
    aadharNo: s.aadhaar(),
    panNo: s.pan(),
    passportNo: s.text(20, 'Passport no.'),
    isOverTimeApplicable: z.boolean(),
    willingToTravel: z.boolean(),
    applicableForService: z.boolean(),
    providentFund: s.money('Provident fund'),
    employeeStateInsurance: s.money('Employee state insurance'),
    professionalTax: s.money('Professional tax'),
    incomeTaxTds: s.money('Income tax'),
    grossSalary: s.money('Gross salary'),
    netSalary: s.money('Net salary'),
    perHourSalary: s.money('Per hour salary'),
    isActive: z.boolean(),
    status: z.string(),
  })
  // Net above gross is arithmetically impossible and almost always the two typed
  // into each other's boxes. Attached to netSalary so the message lands on the
  // field the user has to change.
  .refine(
    (values) =>
      values.netSalary.trim() === '' ||
      values.grossSalary.trim() === '' ||
      Number(values.netSalary) <= Number(values.grossSalary),
    { message: 'Net salary cannot be more than gross salary.', path: ['netSalary'] },
  )
  .refine(
    (values) =>
      values.dateOfBirth === '' || values.joiningDate === '' || values.dateOfBirth < values.joiningDate,
    { message: 'Date of birth must be before the date of joining.', path: ['dateOfBirth'] },
  ) satisfies z.ZodType<EmployeeFormValues, EmployeeFormValues>;

export function EmployeeForm({ employee }: { employee?: EmployeeDetail }) {
  const router = useRouter();
  const { can } = usePermissions();
  const isNew = !employee;
  const { lookups } = useLookups(EMPLOYEE_LOOKUPS);

  // On an existing record the server has already said whether this caller may see
  // the pay fields; on a new one there is nothing to load, so ask the session.
  const canEditPayroll = employee?.canEditPayroll ?? can('masters.employee.payroll.read');

  const save = useSaveMasterRecord<EmployeeFormValues>({
    resource: 'employees',
    id: employee?.id,
    rowVersion: employee?.rowVersion,
    toBody: (values) => ({
      ...(isNew ? { employeeCode: blankToNumber(values.employeeCode) } : {}),
      firstName: values.firstName.trim(),
      middleName: blankToNull(values.middleName),
      lastName: blankToNull(values.lastName),
      gender: blankToNull(values.gender),
      address: blankToNull(values.address),
      state: blankToNull(values.state),
      userName: blankToNull(values.userName),
      roleId: blankToNumber(values.roleId),
      department: blankToNull(values.department),
      designation: blankToNull(values.designation),
      email: blankToNull(values.email),
      phoneNo: blankToNull(values.phoneNo),
      dateOfBirth: blankToDate(values.dateOfBirth),
      joiningDate: blankToDate(values.joiningDate),
      isMarried: values.isMarried,
      bloodGroup: blankToNull(values.bloodGroup),
      shoeSize: blankToNumber(values.shoeSize),
      aadharNo: blankToNull(values.aadharNo),
      panNo: blankToNull(values.panNo),
      passportNo: blankToNull(values.passportNo),
      qualification: blankToNull(values.qualification),
      skills: splitList(values.skills),
      strengths: splitList(values.strengths),
      isOverTimeApplicable: values.isOverTimeApplicable,
      willingToTravel: values.willingToTravel,
      applicableForService: values.applicableForService,
      providentFund: blankToNumber(values.providentFund),
      employeeStateInsurance: blankToNumber(values.employeeStateInsurance),
      professionalTax: blankToNumber(values.professionalTax),
      incomeTaxTds: blankToNumber(values.incomeTaxTds),
      grossSalary: blankToNumber(values.grossSalary),
      netSalary: blankToNumber(values.netSalary),
      perHourSalary: blankToNumber(values.perHourSalary),
      isActive: values.isActive,
      status: values.status || 'Draft',
    }),
  });

  const sections = useMemo(
    () => employeeFormSections(isNew, canEditPayroll),
    [isNew, canEditPayroll],
  );

  const { form, onSubmit, isSubmitting, formError } = useApiForm<EmployeeFormValues>({
    schema,
    defaultValues: toFormValues(employee),
    submit: (values) => save.mutateAsync(values),
    onSuccess: () => {
      toast.success(isNew ? 'Employee created.' : 'Employee updated.');
      router.push('/masters/employees');
      router.refresh();
    },
  });

  return (
    <MasterForm<EmployeeFormValues>
      sections={sections}
      form={form}
      onSubmit={onSubmit}
      isSubmitting={isSubmitting}
      formError={formError}
      submitLabel={isNew ? 'Create employee' : 'Save changes'}
      onCancel={() => router.push('/masters/employees')}
      lookups={lookups}
      title={isNew ? 'New employee' : 'Edit employee'}
      backLabel="Employees"
      identityCode={employee?.employeeCode ? String(employee.employeeCode) : null}
      badges={employee ? [{ label: employee.isActive ? 'Active' : 'Inactive', tone: employee.isActive ? 'ok' : 'neutral' }] : []}
      auditLine={employee ? `Created ${new Date(employee.createdAtUtc).toLocaleDateString('en-IN')}${employee.modifiedAtUtc ? ` · Modified ${new Date(employee.modifiedAtUtc).toLocaleDateString('en-IN')}` : ''}` : null}
    />
  );
}

/** One textarea, one list. Blank entries are dropped so a trailing comma is harmless. */
function splitList(value: string): string[] {
  return value
    .split(',')
    .map((item) => item.trim())
    .filter((item) => item.length > 0);
}

/** Controlled from the first render — see the note in `SupplierForm`. */
function toFormValues(employee?: EmployeeDetail): EmployeeFormValues {
  return {
    employeeCode: numberToInput(employee?.employeeCode),
    firstName: employee?.firstName ?? '',
    middleName: employee?.middleName ?? '',
    lastName: employee?.lastName ?? '',
    gender: employee?.gender ?? '',
    dateOfBirth: dateToInput(employee?.dateOfBirth),
    isMarried: employee?.isMarried ?? false,
    bloodGroup: employee?.bloodGroup ?? '',
    address: employee?.address ?? '',
    state: employee?.state ?? '',
    phoneNo: employee?.phoneNo ?? '',
    email: employee?.email ?? '',
    userName: employee?.userName ?? '',
    roleId: numberToInput(employee?.roleId),
    department: employee?.department ?? '',
    designation: employee?.designation ?? '',
    joiningDate: dateToInput(employee?.joiningDate),
    qualification: employee?.qualification ?? '',
    skills: (employee?.skills ?? []).join(', '),
    strengths: (employee?.strengths ?? []).join(', '),
    shoeSize: numberToInput(employee?.shoeSize),
    aadharNo: employee?.aadharNo ?? '',
    panNo: employee?.panNo ?? '',
    passportNo: employee?.passportNo ?? '',
    isOverTimeApplicable: employee?.isOverTimeApplicable ?? false,
    willingToTravel: employee?.willingToTravel ?? false,
    applicableForService: employee?.applicableForService ?? false,
    providentFund: numberToInput(employee?.providentFund),
    employeeStateInsurance: numberToInput(employee?.employeeStateInsurance),
    professionalTax: numberToInput(employee?.professionalTax),
    incomeTaxTds: numberToInput(employee?.incomeTaxTds),
    grossSalary: numberToInput(employee?.grossSalary),
    netSalary: numberToInput(employee?.netSalary),
    perHourSalary: numberToInput(employee?.perHourSalary),
    isActive: employee?.isActive ?? true,
    status: employee?.status ?? 'Draft',
  };
}
