/**
 * Hand-written for the Phase 0 vertical proof.
 *
 * These are replaced by `pnpm generate:api`, which derives them from the API's
 * OpenAPI document via orval. CI then fails if the generated output differs from
 * what is committed, so the client and server contracts cannot drift — the
 * legacy system had no shared contract at all and no way to detect a mismatch
 * except at runtime.
 */

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export type PartStatus = 'Draft' | 'PendingApproval' | 'Approved' | 'Inactive';

/**
 * One row of the parts grid — every column the legacy Part Master showed.
 *
 * Wide on purpose. The people this screen replaces work across all of it, and the
 * grid starts with about two thirds visible; the rest is one click away in the
 * column chooser. The width costs one larger row per page, not one request per
 * column: the server projects the whole thing in a single SELECT.
 */
export interface PartListItem {
  id: string;
  /** Legacy "System Part Number". */
  partNumber: string;
  /** The number this part's family was first issued under. Equals `partNumber` unless it is a revision. */
  originalPartNumber: string;
  /** Legacy "Item Code (Manual)". */
  itemNumber: string | null;
  description: string;
  technicalSpecification: string | null;
  /** Material of construction. */
  moc: string | null;
  partCategoryCode: string | null;
  partType: string | null;
  formCategory: string | null;
  /** Legacy "Primary UOM". */
  unitOfMeasureCode: string;
  purchaseUomCode: string | null;
  sellingUomCode: string | null;
  materialType: string | null;
  seriesCode: string | null;
  partRevisionNo: string | null;
  sourceCode: string | null;
  weightKg: number | null;
  leadTimeDays: number | null;
  /** Legacy `SafetyStockLevel`, labelled "Minimum Stock Level". */
  minimumStockLevel: number | null;
  reorderPoint: number | null;
  hsnCode: string | null;
  /** Legacy "Drawing Revision Path". */
  drawingNumber: string | null;
  /** Usable on new documents. Independent of `status`. */
  isActive: boolean;
  status: PartStatus;
  revisionRemark: string | null;
  holdRemark: string | null;
  inactiveRemark: string | null;
  /** Display name, resolved server-side. Null if that user no longer exists. */
  createdBy: string | null;
  createdAtUtc: string;
  modifiedBy: string | null;
  modifiedAtUtc: string | null;
}

/** The descriptive fields, in the shape the update endpoint accepts back. */
export interface PartAttributes {
  itemNumber: string | null;
  technicalSpecification: string | null;
  moc: string | null;
  partCategoryCode: string | null;
  partType: string | null;
  formCategory: string | null;
  purchaseUomCode: string | null;
  sellingUomCode: string | null;
  materialType: string | null;
  seriesCode: string | null;
  partRevisionNo: string | null;
  sourceCode: string | null;
  weightKg: number | null;
  leadTimeDays: number | null;
  minimumStockLevel: number | null;
  reorderPoint: number | null;
  revisionRemark: string | null;
  holdRemark: string | null;
  inactiveRemark: string | null;
}

export interface PartDetail {
  id: string;
  partNumber: string;
  description: string;
  categoryId: string | null;
  unitOfMeasureCode: string;
  hsnCode: string | null;
  drawingNumber: string | null;
  attributes: PartAttributes;
  isActive: boolean;
  status: PartStatus;
  businessUnitId: number;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
  /** Send back unchanged on update; a stale value yields 409. */
  rowVersion: string;
}

/**
 * Approval lifecycle shared by the masters ported from the legacy system.
 * Distinct from `PartStatus` on the wire even though the members match today.
 */
export type MasterStatus = 'Draft' | 'PendingApproval' | 'Approved' | 'Inactive';

export interface SupplierListItem {
  id: number;
  supplierCode: string | null;
  supplierName: string | null;
  supplierType: string | null;
  primaryContact: string | null;
  secondaryContact: string | null;
  phone: string | null;
  altPhone: string | null;
  email: string | null;
  altEmail: string | null;
  website: string | null;
  billingAddress: string | null;
  billingCity: string | null;
  billingState: string | null;
  billingCountry: string | null;
  billingZipCode: string | null;
  shippingAddress: string | null;
  shippingCity: string | null;
  shippingState: string | null;
  shippingCountry: string | null;
  shippingZipCode: string | null;
  pan: string | null;
  taxId: string | null;
  gstNo: string | null;
  bankName: string | null;
  accountNumber: string | null;
  ifsc: string | null;
  swift: string | null;
  paymentTerms: string | null;
  currency: string | null;
  taxCode: string | null;
  qualityCompliance: string | null;
  /** Percentage rates, not amounts. */
  igst: number | null;
  cgst: number | null;
  sgst: number | null;
  /** Legacy free-text status — "Blacklisted", "On hold" — that the boolean cannot express. */
  activeStatus: string | null;
  isActive: boolean;
  status: MasterStatus;
  createdBy: string | null;
  createdAtUtc: string;
  modifiedBy: string | null;
  modifiedAtUtc: string | null;
}

export interface CustomerListItem {
  id: number;
  customerCode: string | null;
  customerName: string | null;
  industry: string | null;
  primaryContact: string | null;
  secondaryContact: string | null;
  phone: string | null;
  altPhone: string | null;
  email: string | null;
  altEmail: string | null;
  website: string | null;
  billingAddress: string | null;
  billingCity: string | null;
  billingState: string | null;
  billingCountry: string | null;
  billingZipCode: string | null;
  shippingAddress: string | null;
  shippingCity: string | null;
  shippingState: string | null;
  shippingCountry: string | null;
  shippingZipCode: string | null;
  taxId: string | null;
  gst: string | null;
  pan: string | null;
  igst: number | null;
  cgst: number | null;
  sgst: number | null;
  currency: string | null;
  taxCode: string | null;
  isActive: boolean;
  status: MasterStatus;
  createdBy: string | null;
  createdAtUtc: string;
  modifiedBy: string | null;
  modifiedAtUtc: string | null;
}

/**
 * Carries no credential field, ever — the legacy grid had a clear-text `Password`
 * column and nothing here reproduces it.
 *
 * The pay fields arrive null unless the caller holds
 * `masters.employee.payroll.read`, which the server enforces both by nulling the
 * values and by refusing to sort or filter on them.
 */
export interface EmployeeListItem {
  id: number;
  employeeCode: number | null;
  firstName: string | null;
  lastName: string | null;
  fullName: string;
  /** Legacy code: `01` male, `02` female. */
  gender: string | null;
  address: string | null;
  userName: string | null;
  /** From the legacy role master. Not the Identity role that grants permissions. */
  roleName: string | null;
  department: string | null;
  designation: string | null;
  email: string | null;
  phoneNo: string | null;
  dateOfBirth: string | null;
  joiningDate: string | null;
  isMarried: boolean;
  bloodGroup: string | null;
  shoeSize: number | null;
  aadharNo: string | null;
  panNo: string | null;
  passportNo: string | null;
  qualification: string | null;
  skills: string[];
  strengths: string[];
  isOverTimeApplicable: boolean | null;
  willingToTravel: boolean | null;
  applicableForService: boolean | null;
  businessUnit: string | null;
  /** Payroll-gated: null without `masters.employee.payroll.read`. */
  providentFund: number | null;
  employeeStateInsurance: number | null;
  professionalTax: number | null;
  incomeTaxTds: number | null;
  grossSalary: number | null;
  netSalary: number | null;
  perHourSalary: number | null;
  isActive: boolean;
  status: MasterStatus;
  createdBy: string | null;
  createdAtUtc: string;
  modifiedBy: string | null;
  modifiedAtUtc: string | null;
}

/**
 * A business unit. Not tenant-scoped — this table defines the tenancy boundary,
 * so the list returns every unit and `masters.businessunit.read` is the only gate.
 */
export interface BusinessUnitListItem {
  id: number;
  /** The value other tables carry in their tenancy column. */
  businessUnitId: number | null;
  businessName: string | null;
  address: string | null;
  contactNumber: string | null;
  email: string | null;
  website: string | null;
  /** Corporate Identification Number. */
  cin: string | null;
  gstn: string | null;
  stateName: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

/** The legacy role master. Does NOT grant permissions — Identity roles do that. */
export interface RoleListItem {
  id: number;
  rolesName: string | null;
  roleId: number;
  isActive: boolean;
  bypassBusinessUnit: boolean;
  createdAtUtc: string;
}

/** One permission the system defines. Assembled from every module at startup. */
export interface PermissionDefinition {
  code: string;
  name: string;
  group: string;
  module: string;
}

export interface AdminRoleListItem {
  id: string;
  name: string;
  description: string | null;
  permissionCount: number;
  userCount: number;
  isSuperAdministrator: boolean;
}

export interface AdminRoleDetail {
  id: string;
  name: string;
  description: string | null;
  permissions: string[];
  userCount: number;
  isSuperAdministrator: boolean;
}

export interface CurrentUser {
  userId: string;
  userName: string;
  businessUnitId: number;
  canAccessAllBusinessUnits: boolean;
  /** Drives menu and button visibility only. The server re-checks every call. */
  permissions: string[];
  /** Holds a super-administrator role: permissions above already lists everything. */
  isSuperAdministrator: boolean;
}

/**
 * Detail shapes for the master edit screens.
 *
 * Field names match the corresponding save request exactly, so a form fills itself
 * from one and posts the other back. A detail response whose names differ from the
 * request's is how an edit screen silently drops a field.
 */

export interface SupplierDetail {
  id: number;
  supplierCode: string | null;
  supplierName: string | null;
  supplierType: string | null;
  primaryContact: string | null;
  secondaryContact: string | null;
  phone: string | null;
  altPhone: string | null;
  email: string | null;
  altEmail: string | null;
  website: string | null;
  billingAddress: string | null;
  billingCity: string | null;
  billingState: string | null;
  billingCountry: string | null;
  billingZipCode: string | null;
  shippingAddress: string | null;
  shippingCity: string | null;
  shippingState: string | null;
  shippingCountry: string | null;
  shippingZipCode: string | null;
  pan: string | null;
  taxId: string | null;
  gstNo: string | null;
  bankName: string | null;
  accountNumber: string | null;
  ifsc: string | null;
  swift: string | null;
  paymentTerms: string | null;
  currency: string | null;
  taxCode: string | null;
  qualityCompliance: string | null;
  igst: number | null;
  cgst: number | null;
  sgst: number | null;
  activeStatus: string | null;
  isActive: boolean;
  status: MasterStatus;
  businessUnitId: number;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
  rowVersion: string;
}

export interface CustomerDetail {
  id: number;
  customerCode: string | null;
  customerName: string | null;
  industry: string | null;
  primaryContact: string | null;
  secondaryContact: string | null;
  phone: string | null;
  altPhone: string | null;
  email: string | null;
  altEmail: string | null;
  website: string | null;
  billingAddress: string | null;
  billingCity: string | null;
  billingState: string | null;
  billingCountry: string | null;
  billingZipCode: string | null;
  shippingAddress: string | null;
  shippingCity: string | null;
  shippingState: string | null;
  shippingCountry: string | null;
  shippingZipCode: string | null;
  taxId: string | null;
  gst: string | null;
  pan: string | null;
  igst: number | null;
  cgst: number | null;
  sgst: number | null;
  currency: string | null;
  taxCode: string | null;
  isActive: boolean;
  status: MasterStatus;
  businessUnitId: number;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
  rowVersion: string;
}

export interface EmployeeDetail {
  id: number;
  employeeCode: number | null;
  firstName: string | null;
  middleName: string | null;
  lastName: string | null;
  gender: string | null;
  address: string | null;
  state: string | null;
  userName: string | null;
  roleId: number | null;
  department: string | null;
  designation: string | null;
  email: string | null;
  phoneNo: string | null;
  dateOfBirth: string | null;
  joiningDate: string | null;
  isMarried: boolean;
  bloodGroup: string | null;
  shoeSize: number | null;
  aadharNo: string | null;
  panNo: string | null;
  passportNo: string | null;
  qualification: string | null;
  skills: string[];
  strengths: string[];
  isOverTimeApplicable: boolean | null;
  willingToTravel: boolean | null;
  applicableForService: boolean | null;
  providentFund: number | null;
  employeeStateInsurance: number | null;
  professionalTax: number | null;
  incomeTaxTds: number | null;
  grossSalary: number | null;
  netSalary: number | null;
  perHourSalary: number | null;
  /** Whether the caller may see and change the pay fields. Not inferred from nulls. */
  canEditPayroll: boolean;
  isActive: boolean;
  status: MasterStatus;
  businessUnitId: number;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
  rowVersion: string;
}

export interface BusinessUnitDetail {
  id: number;
  businessUnitId: number | null;
  businessName: string | null;
  address: string | null;
  stateName: string | null;
  stateCode: string | null;
  contactNumber: string | null;
  email: string | null;
  website: string | null;
  cin: string | null;
  gstn: string | null;
  pan: string | null;
  isActive: boolean;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
  rowVersion: string;
}

/** The legacy role master. Grants nothing — Identity roles carry permissions. */
export interface RoleMasterDetail {
  id: number;
  roleId: number;
  rolesName: string | null;
  bypassBusinessUnit: boolean;
  isActive: boolean;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
  rowVersion: string;
}

/**
 * Where a node sits in the machine breakdown. One record type at three depths —
 * see `AssemblyNodeListItem`.
 */
export type AssemblyLevel = 'Section' | 'Assembly' | 'SubAssembly';

/**
 * One row of the Section, Assembly or Sub-assembly grid.
 *
 * The same shape for all three, because they are the same record: the legacy
 * system stored them in a single table discriminated by a `Level` column, and that
 * part of its design was right. What the three grids differ in is their columns
 * and their permission, which is where the difference belongs.
 */
export interface AssemblyNodeListItem {
  id: string;
  /** Legacy `AssemblyCode` — the business key. */
  code: string;
  name: string;
  /** The code a person assigned, distinct from `code`. */
  manualCode: string | null;
  level: AssemblyLevel;
  /** Null for a section, which is the top of the breakdown. */
  parentId: string | null;
  /** Resolved server-side, so the grid never looks a parent up per row. */
  parentCode: string | null;
  parentName: string | null;
  /** How many nodes sit directly under this one. Counted in the same query. */
  childCount: number;
  machineType: string | null;
  /** What powers it — motor, hydraulic, manual. Legacy `DrivenBy`. */
  drivenBy: string | null;
  drawingPath: string | null;
  technicalSpecification: string | null;
  remark: string | null;
  quantity: number | null;
  weightKg: number | null;
  /** Legacy `SrNo` — the order the node appears in on drawings, not the grid row number. */
  displaySequence: number | null;
  isActive: boolean;
  createdBy: string | null;
  createdAtUtc: string;
  modifiedBy: string | null;
  modifiedAtUtc: string | null;
}

/** The descriptive fields, in the shape the create and update endpoints accept back. */
export interface AssemblyNodeAttributes {
  manualCode: string | null;
  machineType: string | null;
  drivenBy: string | null;
  drawingPath: string | null;
  technicalSpecification: string | null;
  remark: string | null;
  quantity: number | null;
  weightKg: number | null;
  displaySequence: number | null;
}

export interface AssemblyNodeDetail {
  id: string;
  code: string;
  name: string;
  manualCode: string | null;
  level: AssemblyLevel;
  parentId: string | null;
  /** Sent with the id so the parent picker can label itself without a second request. */
  parentCode: string | null;
  parentName: string | null;
  attributes: AssemblyNodeAttributes;
  isActive: boolean;
  businessUnitId: number;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
  /** Send back unchanged on update; a stale value yields 409. */
  rowVersion: string;
}

/**
 * One component line of a parent part.
 *
 * `amount` and `lineWeightKg` are computed by the server from quantity × rate and
 * quantity × unit weight. They are sent for display and ignored on the way in —
 * the legacy screen accepted them from the browser and then summed that column
 * into the header totals.
 */
export interface ParentPartComponent {
  partId: string;
  partNumber: string | null;
  partDescription: string | null;
  quantity: number;
  unitOfMeasureCode: string | null;
  unitWeightKg: number | null;
  rate: number | null;
  amount: number | null;
  lineWeightKg: number | null;
  drawingNumber: string | null;
  remark: string | null;
}

/** One row of the Parent Part grid — a part that is built from other parts. */
export interface ParentPartListItem {
  id: string;
  partId: string;
  partNumber: string;
  partDescription: string;
  /** Legacy `AssemblyDesc` — what this build is called, when that differs from the part. */
  description: string | null;
  assemblyNodeId: string | null;
  assemblyCode: string | null;
  assemblyName: string | null;
  unitOfMeasureCode: string | null;
  drawingNumber: string | null;
  category: string | null;
  componentCount: number;
  /** Summed from the component lines by the server, never typed in. */
  totalWeightKg: number;
  totalAmount: number;
  isActive: boolean;
  createdBy: string | null;
  createdAtUtc: string;
  modifiedBy: string | null;
  modifiedAtUtc: string | null;
}

export interface ParentPartDetail {
  id: string;
  partId: string;
  partNumber: string;
  partDescription: string;
  description: string | null;
  assemblyNodeId: string | null;
  assemblyCode: string | null;
  assemblyName: string | null;
  unitOfMeasureCode: string | null;
  drawingNumber: string | null;
  category: string | null;
  /** In the stored order, which is the order the user arranged them in. */
  components: ParentPartComponent[];
  totalWeightKg: number;
  totalAmount: number;
  isActive: boolean;
  businessUnitId: number;
  createdAtUtc: string;
  modifiedAtUtc: string | null;
  rowVersion: string;
}
