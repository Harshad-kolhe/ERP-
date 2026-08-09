/**
 * The API's types, as the app names them.
 *
 * Every type here is re-exported from `generated/erp.ts`, which orval derives from
 * `contracts/openapi.json`, which the backend build emits from the C# contracts.
 * Nothing in this file describes a shape; it only renames one. Change a DTO in C#
 * and the error surfaces here, at compile time, in the screens that read the field.
 *
 * Why rename at all rather than import `PartListItemDto` everywhere: the generated
 * suffix is an artefact of the C# naming convention, and threading it through 40-odd
 * component files would make the app's vocabulary a function of the generator's. This
 * file is the one place the two vocabularies meet, so a future change of generator is
 * a change to one file.
 *
 * Do not add a hand-written shape here. A type the document does not describe is a
 * type nothing checks — which is what this file used to be, in full, while the
 * generated output sat beside it unimported and the drift gate guarded nobody.
 */

import type {
  AssemblyLevelDto,
  AssemblyNodeAttributesDto,
  AssemblyNodeDetailDto,
  AssemblyNodeListItemDto,
  BusinessUnitDetailDto,
  BusinessUnitListItemDto,
  CurrentUserDto,
  CustomerDetailDto,
  CustomerListItemDto,
  EmployeeDetailDto,
  EmployeeListItemDto,
  MasterStatusDto,
  PagedResultOfPartListItemDto,
  ParentPartComponentDto,
  ParentPartDetailDto,
  ParentPartListItemDto,
  PartAttributesDto,
  PartDetailDto,
  PartListItemDto,
  PartStatusDto,
  PermissionDefinition as PermissionDefinitionDto,
  RoleDetailDto,
  RoleListItemDto,
  RoleMasterDetailDto,
  RoleMasterListItemDto,
  SupplierDetailDto,
  SupplierListItemDto,
} from './generated/erp';

/**
 * The pagination envelope, derived from a generated instance of it rather than
 * written out again. The API returns one concrete `PagedResultOfXDto` per row type;
 * they share an envelope, and taking it from one of them means a change to
 * `PagedResult<T>` in C# still breaks this build.
 */
export type PagedResult<T> = Omit<PagedResultOfPartListItemDto, 'items'> & { items: T[] };

export type PartStatus = PartStatusDto;
export type MasterStatus = MasterStatusDto;
export type AssemblyLevel = AssemblyLevelDto;

export type PartListItem = PartListItemDto;
export type PartAttributes = PartAttributesDto;
export type PartDetail = PartDetailDto;

export type SupplierListItem = SupplierListItemDto;
export type SupplierDetail = SupplierDetailDto;

export type CustomerListItem = CustomerListItemDto;
export type CustomerDetail = CustomerDetailDto;

export type EmployeeListItem = EmployeeListItemDto;
export type EmployeeDetail = EmployeeDetailDto;

export type BusinessUnitListItem = BusinessUnitListItemDto;
export type BusinessUnitDetail = BusinessUnitDetailDto;

/**
 * The legacy role master, which does *not* grant permissions — authorisation runs on
 * Identity roles, listed by `AdminRoleListItem`. The two were both called
 * `RoleListItemDto` in C# until they collided into a single OpenAPI schema; see
 * `ContractSchemaTests`.
 */
export type RoleListItem = RoleMasterListItemDto;
export type RoleMasterDetail = RoleMasterDetailDto;

export type AdminRoleListItem = RoleListItemDto;
export type AdminRoleDetail = RoleDetailDto;
export type PermissionDefinition = PermissionDefinitionDto;

export type CurrentUser = CurrentUserDto;

export type AssemblyNodeListItem = AssemblyNodeListItemDto;
export type AssemblyNodeAttributes = AssemblyNodeAttributesDto;
export type AssemblyNodeDetail = AssemblyNodeDetailDto;

export type ParentPartComponent = ParentPartComponentDto;
export type ParentPartListItem = ParentPartListItemDto;
export type ParentPartDetail = ParentPartDetailDto;
