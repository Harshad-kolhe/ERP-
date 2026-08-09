import type { AssemblyLevel } from '@/lib/api/types';

/**
 * Everything that differs between the Section, Assembly and Sub-assembly screens,
 * in one table.
 *
 * The three are one record type at three depths, so the screens differ only in
 * their route, their wording, their permissions and which level sits above them.
 * Writing that out three times is how the legacy system ended up with three save
 * methods whose rules had already drifted apart — see `AssemblyLevels` on the
 * server, which is this same table on the other side of the wire.
 */
export interface AssemblyLevelScreen {
  level: AssemblyLevel;
  /** Path segment under `/masters`, and the API resource name. They are the same on purpose. */
  resource: 'sections' | 'assemblies' | 'sub-assemblies';
  /** Sentence-case singular, used in headings and toasts. */
  noun: string;
  /** Plural, used in page titles. */
  plural: string;
  /** The screen one level up, or null at the top of the breakdown. */
  parent: { noun: string; resource: 'sections' | 'assemblies' } | null;
  /** What sits below, for the child-count column. Null at the bottom. */
  childNoun: string | null;
  permissions: { read: string; create: string; update: string };
}

export const SECTION_SCREEN: AssemblyLevelScreen = {
  level: 'Section',
  resource: 'sections',
  noun: 'section',
  plural: 'Sections',
  parent: null,
  childNoun: 'Assembly',
  permissions: {
    read: 'masters.section.read',
    create: 'masters.section.create',
    update: 'masters.section.update',
  },
};

export const ASSEMBLY_SCREEN: AssemblyLevelScreen = {
  level: 'Assembly',
  resource: 'assemblies',
  noun: 'assembly',
  plural: 'Assemblies',
  parent: { noun: 'Section', resource: 'sections' },
  childNoun: 'Sub-assembly',
  permissions: {
    read: 'masters.assembly.read',
    create: 'masters.assembly.create',
    update: 'masters.assembly.update',
  },
};

export const SUB_ASSEMBLY_SCREEN: AssemblyLevelScreen = {
  level: 'SubAssembly',
  resource: 'sub-assemblies',
  noun: 'sub-assembly',
  plural: 'Sub-assemblies',
  parent: { noun: 'Assembly', resource: 'assemblies' },
  childNoun: null,
  permissions: {
    read: 'masters.subassembly.read',
    create: 'masters.subassembly.create',
    update: 'masters.subassembly.update',
  },
};
