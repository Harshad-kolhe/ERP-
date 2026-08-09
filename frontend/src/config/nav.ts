import {
  Blocks,
  Boxes,
  Building2,
  CalendarRange,
  ClipboardCheck,
  Cog,
  Component,
  Contact,
  Home,
  IdCard,
  KeyRound,
  Layers,
  ListTree,
  Package,
  Percent,
  Ruler,
  ShieldCheck,
  ShoppingCart,
  Truck,
  Users,
  Warehouse,
  Workflow,
  type LucideIcon,
} from 'lucide-react';

import { APP_HOME } from '@/lib/routes';

/**
 * The navigation tree.
 *
 * A typed file in source, not database rows. The legacy system drove its menu from
 * three tables — `ApplicationMaster`, `UMScreenMaster`, `UMControlMaster` — none of
 * which were seeded in the repository, so the only way to discover what screens
 * existed was to query production. Here the feature surface is answerable by
 * reading this file.
 *
 * Every item declares the permission required to see it. Items the signed-in user
 * lacks are not rendered at all. That is a *visibility* decision only: the server
 * re-checks the permission on every request regardless of what the client renders,
 * which is the distinction the old system got wrong by enforcing permissions
 * exclusively in JavaScript.
 */

export interface NavItem {
  label: string;
  href: string;
  icon: LucideIcon;

  /** Permission code required to see this item. Omitted means any signed-in user. */
  permission?: string;

  /**
   * `planned` items render disabled, so the shape of the finished system is legible
   * from day one and nobody has to ask where a screen will eventually live.
   * Flip to `ready` when the screen exists.
   */
  status: 'ready' | 'planned';
}

export interface NavGroup {
  label: string;
  items: NavItem[];
}

/** Grouped by how a machine gets built, not by which module owns the code. */
export const NAV: NavGroup[] = [
  {
    label: 'Overview',
    items: [
      { label: 'Home', href: APP_HOME, icon: Home, status: 'ready' },
      { label: 'My approvals', href: '/approvals', icon: ClipboardCheck, status: 'planned' },
    ],
  },
  {
    label: 'Build',
    items: [
      { label: 'Engineering', href: '/engineering', icon: Boxes, status: 'planned' },
      { label: 'Planning', href: '/planning', icon: CalendarRange, status: 'planned' },
      { label: 'Procurement', href: '/procurement', icon: ShoppingCart, status: 'planned' },
      { label: 'Stores', href: '/stores', icon: Warehouse, status: 'planned' },
      { label: 'Production', href: '/production', icon: Cog, status: 'planned' },
      { label: 'Quality', href: '/quality', icon: ShieldCheck, status: 'planned' },
      { label: 'Dispatch', href: '/dispatch', icon: Truck, status: 'planned' },
    ],
  },
  {
    label: 'Masters',
    items: [
      {
        label: 'Parts',
        href: '/masters/parts',
        icon: Package,
        permission: 'masters.part.read',
        status: 'ready',
      },
      {
        // The machine breakdown, top to bottom. Three entries rather than one
        // "Assemblies" screen with a level filter: they carry three separate
        // permissions, and a filter is not an access control.
        label: 'Sections',
        href: '/masters/sections',
        icon: Layers,
        permission: 'masters.section.read',
        status: 'ready',
      },
      {
        label: 'Assemblies',
        href: '/masters/assemblies',
        icon: Component,
        permission: 'masters.assembly.read',
        status: 'ready',
      },
      {
        label: 'Sub-assemblies',
        href: '/masters/sub-assemblies',
        icon: Blocks,
        permission: 'masters.subassembly.read',
        status: 'ready',
      },
      {
        label: 'Parent parts',
        href: '/masters/parent-parts',
        icon: Workflow,
        permission: 'masters.parentpart.read',
        status: 'ready',
      },
      {
        label: 'Suppliers',
        href: '/masters/suppliers',
        icon: Truck,
        permission: 'masters.supplier.read',
        status: 'ready',
      },
      {
        label: 'Customers',
        href: '/masters/customers',
        icon: Users,
        permission: 'masters.customer.read',
        status: 'ready',
      },
      {
        label: 'Employees',
        href: '/masters/employees',
        icon: Contact,
        permission: 'masters.employee.read',
        status: 'ready',
      },
      {
        label: 'Roles',
        href: '/masters/roles',
        icon: IdCard,
        permission: 'masters.role.read',
        status: 'ready',
      },
      {
        label: 'Business units',
        href: '/masters/business-units',
        icon: Building2,
        permission: 'masters.businessunit.read',
        status: 'ready',
      },
      {
        // The lists every screen above picks from. Last in the group because it is
        // the least-visited and the most consequential: editing an option here
        // changes what every other master will accept.
        label: 'Reference data',
        href: '/masters/lookup-values',
        icon: ListTree,
        permission: 'masters.referencedata.read',
        status: 'ready',
      },
      {
        label: 'Units of measure',
        href: '/masters/units-of-measure',
        icon: Ruler,
        permission: 'masters.referencedata.read',
        status: 'ready',
      },
      {
        label: 'HSN codes',
        href: '/masters/hsn-codes',
        icon: Percent,
        permission: 'masters.referencedata.read',
        status: 'ready',
      },
    ],
  },
  {
    label: 'Administer',
    items: [
      { label: 'Users', href: '/admin/users', icon: Users, status: 'planned' },
      {
        // Identity roles — the things that actually grant permissions. Distinct
        // from Masters › Roles, which is the legacy role reference table.
        label: 'Roles & permissions',
        href: '/admin/roles',
        icon: KeyRound,
        permission: 'admin.role.read',
        status: 'ready',
      },
    ],
  },
];
