import type { MasterFilterField } from './master-filters';

/**
 * What each master's filters panel offers, in one file.
 *
 * Declarations, not components. There is exactly one panel implementation —
 * `MasterFilters` — and this is the only thing that differs between the screens
 * that use it, so adding a searchable field to a master is a line here rather
 * than a form somewhere.
 *
 * Every `field` must be declared on that endpoint's `QueryMap` on the server. If
 * it is not, the request comes back 400 rather than quietly returning unfiltered
 * rows — so a typo here fails loudly, which is the intended trade.
 *
 * What belongs on a panel and what belongs in the column filter row is a
 * judgement about frequency, not capability: the panel carries the handful of
 * fields people actually search a master by, and the column row already covers
 * every other column. Putting all twenty-eight part columns on the panel would
 * make the useful five harder to find.
 */

export const PART_FILTERS: readonly MasterFilterField[] = [
  { field: 'partNumber', label: 'System part number', placeholder: 'Contains…' },
  { field: 'itemNumber', label: 'Item code (manual)', placeholder: 'Contains…' },
  { field: 'description', label: 'Part description', placeholder: 'Contains…' },
  { field: 'technicalSpecification', label: 'Technical specification', placeholder: 'Contains…' },
  { field: 'moc', label: 'MOC', lookup: 'moc', operator: 'eq' },
  { field: 'partCategoryCode', label: 'Part category', lookup: 'part.categoryCode', operator: 'eq' },
  { field: 'formCategory', label: 'Form category', lookup: 'part.formCategory', operator: 'eq' },
  { field: 'sourceCode', label: 'Source code', lookup: 'part.sourceCode', operator: 'eq' },
];

/**
 * The three assembly levels share a panel, because they share a record. The
 * parent's code is worth a box on all of them — "show me everything under S1" is
 * the question these screens exist to answer — and it is simply empty on
 * sections, which have no parent.
 */
export const ASSEMBLY_NODE_FILTERS: readonly MasterFilterField[] = [
  { field: 'code', label: 'Code', placeholder: 'Contains…' },
  { field: 'name', label: 'Name', placeholder: 'Contains…' },
  { field: 'manualCode', label: 'Manual code', placeholder: 'Contains…' },
  { field: 'machineType', label: 'Machine type', lookup: 'assembly.machineType', operator: 'eq' },
  { field: 'drivenBy', label: 'Driven by', lookup: 'assembly.drivenBy', operator: 'eq' },
  { field: 'parentCode', label: 'Parent code', placeholder: 'Contains…' },
];

export const PARENT_PART_FILTERS: readonly MasterFilterField[] = [
  { field: 'partNumber', label: 'Parent part number', placeholder: 'Contains…' },
  { field: 'partDescription', label: 'Part description', placeholder: 'Contains…' },
  { field: 'description', label: 'Build description', placeholder: 'Contains…' },
  { field: 'assemblyCode', label: 'Assembly code', placeholder: 'Contains…' },
  { field: 'category', label: 'Category', lookup: 'part.categoryCode', operator: 'eq' },
  { field: 'unitOfMeasureCode', label: 'Unit of measure', lookup: 'uom', operator: 'eq' },
];

export const SUPPLIER_FILTERS: readonly MasterFilterField[] = [
  { field: 'supplierCode', label: 'Supplier code', placeholder: 'Contains…' },
  { field: 'supplierName', label: 'Supplier name', placeholder: 'Contains…' },
  { field: 'supplierType', label: 'Supplier type', lookup: 'supplier.type', operator: 'eq' },
  { field: 'primaryContact', label: 'Primary contact', placeholder: 'Contains…' },
  { field: 'gstNo', label: 'GST no', placeholder: 'Contains…' },
  { field: 'billingCity', label: 'Billing city', placeholder: 'Contains…' },
  { field: 'billingState', label: 'Billing state', placeholder: 'Contains…' },
  { field: 'currency', label: 'Currency', lookup: 'currency', operator: 'eq' },
];

export const CUSTOMER_FILTERS: readonly MasterFilterField[] = [
  { field: 'customerCode', label: 'Customer code', placeholder: 'Contains…' },
  { field: 'customerName', label: 'Customer name', placeholder: 'Contains…' },
  { field: 'industry', label: 'Industry', lookup: 'customer.industry', operator: 'eq' },
  { field: 'primaryContact', label: 'Primary contact', placeholder: 'Contains…' },
  { field: 'gst', label: 'GST', placeholder: 'Contains…' },
  { field: 'billingCity', label: 'Billing city', placeholder: 'Contains…' },
  { field: 'billingState', label: 'Billing state', placeholder: 'Contains…' },
  { field: 'currency', label: 'Currency', lookup: 'currency', operator: 'eq' },
];

/**
 * No pay fields here, deliberately. They are gated behind
 * `masters.employee.payroll.read`, and the server withholds them from its sort
 * and filter map for anyone without it — so offering a box that 400s for most of
 * the people who see it would be worse than not offering one.
 */
export const EMPLOYEE_FILTERS: readonly MasterFilterField[] = [
  // Numeric on the server, so it matches exactly and rejects letters with
  // "'x' is not a valid value for 'employeeCode'". The placeholder says so
  // rather than leaving someone to discover it by typing a name into it.
  { field: 'employeeCode', label: 'Employee code', operator: 'eq', placeholder: 'Exact number…' },
  // First and last separately, not the grid's "Name" column: `fullName` is
  // composed for display and the server has no such field to filter on.
  { field: 'firstName', label: 'First name', placeholder: 'Contains…' },
  { field: 'lastName', label: 'Last name', placeholder: 'Contains…' },
  { field: 'department', label: 'Department', lookup: 'employee.department', operator: 'eq' },
  { field: 'designation', label: 'Designation', lookup: 'employee.designation', operator: 'eq' },
  { field: 'roleName', label: 'Role', placeholder: 'Contains…' },
  { field: 'email', label: 'Email', placeholder: 'Contains…' },
  { field: 'phoneNo', label: 'Phone', placeholder: 'Contains…' },
];
