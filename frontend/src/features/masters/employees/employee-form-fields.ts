import type { MasterFormSection } from '@/components/form/master-form';

export interface EmployeeFormValues {
  employeeCode: string;
  firstName: string;
  middleName: string;
  lastName: string;
  gender: string;
  dateOfBirth: string;
  isMarried: boolean;
  bloodGroup: string;
  address: string;
  state: string;
  phoneNo: string;
  email: string;
  userName: string;
  roleId: string;
  department: string;
  designation: string;
  joiningDate: string;
  qualification: string;
  skills: string;
  strengths: string;
  shoeSize: string;
  aadharNo: string;
  panNo: string;
  passportNo: string;
  isOverTimeApplicable: boolean;
  willingToTravel: boolean;
  applicableForService: boolean;
  providentFund: string;
  employeeStateInsurance: string;
  professionalTax: string;
  incomeTaxTds: string;
  grossSalary: string;
  netSalary: string;
  perHourSalary: string;
  isActive: boolean;
  status: string;
}

export const EMPLOYEE_LOOKUPS = [
  'employee.gender',
  'employee.department',
  'employee.designation',
  'employee.bloodGroup',
  'employee.qualification',
  'state',
  'masterStatus',
] as const;

/**
 * The employee form.
 *
 * There is no password field, and there will not be — the legacy screen edited a
 * clear-text credential, and sign-in here runs on a hash that cannot be displayed.
 *
 * The Pay section is only present for someone holding
 * `masters.employee.payroll.read`. Hiding it is tidiness; the server is what
 * actually refuses to read or write those fields, and it leaves existing values
 * untouched rather than clearing them when a caller without the right saves.
 */
export function employeeFormSections(
  isNew: boolean,
  canEditPayroll: boolean,
): MasterFormSection<EmployeeFormValues>[] {
  const sections: MasterFormSection<EmployeeFormValues>[] = [
    {
      id: 'personal',
      label: 'Personal',
      description: isNew
        ? 'The employee code is the business key and cannot be changed afterwards.'
        : 'The employee code cannot be changed here.',
      fields: [
        {
          name: 'employeeCode',
          label: 'Employee code',
          kind: 'integer',
          required: true,
          readOnly: !isNew,
          placeholder: '1043',
          description: 'Digits only. Shown everywhere as PPE/1043.',
        },
        { name: 'firstName', label: 'First name', required: true },
        { name: 'middleName', label: 'Middle name' },
        { name: 'lastName', label: 'Last name' },
        { name: 'gender', label: 'Gender', lookup: 'employee.gender' },
        { name: 'dateOfBirth', label: 'Date of birth', kind: 'date' },
        { name: 'bloodGroup', label: 'Blood group', lookup: 'employee.bloodGroup' },
        { name: 'isMarried', label: 'Married', kind: 'boolean' },
        { name: 'address', label: 'Employee address', kind: 'textarea', rows: 2 },
        { name: 'state', label: 'State', lookup: 'state' },
        { name: 'phoneNo', label: 'Phone no' },
        { name: 'email', label: 'Email' },
      ],
    },
    {
      id: 'employment',
      label: 'Employment',
      fields: [
        { name: 'userName', label: 'User name', description: 'The legacy login name. Not a credential.' },
        {
          name: 'roleId',
          label: 'Role id',
          kind: 'integer',
          description: 'From the legacy role master. Grants nothing on its own.',
        },
        { name: 'department', label: 'Department', lookup: 'employee.department' },
        { name: 'designation', label: 'Designation', lookup: 'employee.designation' },
        { name: 'joiningDate', label: 'Date of joining', kind: 'date' },
        { name: 'qualification', label: 'Qualification', lookup: 'employee.qualification' },
        {
          name: 'skills',
          label: 'Skills',
          kind: 'textarea',
          rows: 2,
          description: 'Comma-separated, e.g. Welding, Turning, Assembly.',
        },
        { name: 'strengths', label: 'Strength', kind: 'textarea', rows: 2, description: 'Comma-separated.' },
        { name: 'isOverTimeApplicable', label: 'Overtime applicable', kind: 'boolean' },
        { name: 'willingToTravel', label: 'Willing to travel', kind: 'boolean' },
        { name: 'applicableForService', label: 'Applicable for service', kind: 'boolean' },
        { name: 'shoeSize', label: 'Shoe size', kind: 'integer' },
      ],
    },
    {
      id: 'identity',
      label: 'Identity documents',
      fields: [
        { name: 'aadharNo', label: 'Aadhar card no.', placeholder: '123456789012' },
        { name: 'panNo', label: 'Pan card no.', placeholder: 'AAAPA1234A' },
        { name: 'passportNo', label: 'Passport no.' },
      ],
    },
  ];

  if (canEditPayroll) {
    sections.push({
      id: 'pay',
      label: 'Pay',
      description: 'Visible because you hold the employee payroll permission.',
      fields: [
        { name: 'grossSalary', label: 'Gross salary', kind: 'number' },
        { name: 'netSalary', label: 'Net salary', kind: 'number' },
        { name: 'perHourSalary', label: 'Per hour salary', kind: 'number' },
        { name: 'providentFund', label: 'Provident fund (PF)', kind: 'number' },
        { name: 'employeeStateInsurance', label: 'Employee state insurance (ESI)', kind: 'number' },
        { name: 'professionalTax', label: 'Professional tax (PT)', kind: 'number' },
        { name: 'incomeTaxTds', label: 'Income tax (TDS)', kind: 'number' },
      ],
    });
  }

  sections.push({
    id: 'status',
    label: 'Status',
    fields: [
      { name: 'isActive', label: 'Active', kind: 'boolean', wide: true },
      { name: 'status', label: 'Approval status', lookup: 'masterStatus' },
    ],
  });

  return sections;
}
