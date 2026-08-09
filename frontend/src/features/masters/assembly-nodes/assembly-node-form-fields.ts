import type { MasterFormSection } from '@/components/form/master-form';
import { referenceSource } from '@/components/form/reference-field';
import type { AssemblyNodeListItem } from '@/lib/api/types';

import type { AssemblyLevelScreen } from './assembly-node-level';

export interface AssemblyNodeFormValues {
  code: string;
  name: string;
  parentId: string;
  manualCode: string;
  machineType: string;
  drivenBy: string;
  drawingPath: string;
  technicalSpecification: string;
  remark: string;
  quantity: string;
  weightKg: string;
  displaySequence: string;
  isActive: boolean;
}

/**
 * The option lists this form needs, fetched in one request.
 *
 * Both are server-held. The legacy screens had their machine types typed into the
 * page, which is why the same machine appeared under three spellings.
 */
export const ASSEMBLY_NODE_LOOKUPS = ['assembly.machineType', 'assembly.drivenBy'] as const;

/**
 * The form, grouped the way someone filling it in thinks about a node: what it is
 * and where it belongs, then what it is made of and weighs, then the paperwork.
 *
 * `isNew` controls one field. The code is the business key and every drawing and
 * mapping refers to it, so on an existing record it is shown and locked rather
 * than hidden — people need to read it while they edit the rest.
 *
 * `parentLabel` is what the parent picker shows before anything is searched. The
 * detail endpoint sends the parent's code and name with the id precisely so this
 * costs no extra request.
 */
export function assemblyNodeFormSections(
  screen: AssemblyLevelScreen,
  isNew: boolean,
  parentLabel: string | null,
): MasterFormSection<AssemblyNodeFormValues>[] {
  const parent = screen.parent;

  return [
    {
      id: 'identity',
      label: 'Identity',
      description: isNew
        ? 'The code is the business key and cannot be changed afterwards.'
        : 'The code cannot be changed here — every drawing and mapping refers to it.',
      fields: [
        {
          name: 'code',
          label: 'Code',
          required: true,
          readOnly: !isNew,
          placeholder: screen.level === 'Section' ? 'S1' : screen.level === 'Assembly' ? 'A1' : 'SA1',
          description: 'Unique across sections, assemblies and sub-assemblies.',
        },
        { name: 'manualCode', label: 'Manual code' },
        { name: 'name', label: 'Name', required: true, wide: true },

        // Only where there is a level above. A picker that can never have a value
        // is worse than no picker.
        ...(parent
          ? [
              {
                name: 'parentId' as const,
                label: parent.noun,
                required: true,
                referenceLabel: parentLabel,
                description: `Searched on the server — type a code or name.`,
                reference: referenceSource<AssemblyNodeListItem>({
                  resource: parent.resource,
                  // Only nodes still in use can be chosen. A withdrawn section is
                  // deliberately unselectable, and the server refuses to withdraw
                  // one that still has active children.
                  filter: 'isActive:eq:true',
                  searchPlaceholder: `Search ${parent.noun.toLowerCase()}s…`,
                  toOption: (row) => ({ value: row.id, label: row.code, hint: row.name }),
                }),
              },
            ]
          : []),

        { name: 'isActive', label: 'Active', kind: 'boolean' as const },
      ],
    },
    {
      id: 'engineering',
      label: 'Engineering',
      fields: [
        { name: 'machineType', label: 'Machine type', lookup: 'assembly.machineType' },
        { name: 'drivenBy', label: 'Driven by', lookup: 'assembly.drivenBy' },
        {
          name: 'quantity',
          label: 'Quantity',
          kind: 'number',
          description: 'How many of this the level above carries.',
        },
        { name: 'weightKg', label: 'Weight (kg)', kind: 'number', placeholder: '0.0000' },
        {
          name: 'displaySequence',
          label: 'Sequence',
          kind: 'integer',
          description: 'The order it appears in on drawings and reports. Blank sorts first.',
        },
        {
          name: 'technicalSpecification',
          label: 'Technical specification',
          kind: 'textarea',
          rows: 4,
          description: 'Up to 2,500 characters. Unicode symbols such as Ω, µ and Ø are kept as typed.',
        },
      ],
    },
    {
      id: 'documents',
      label: 'Drawing & remarks',
      fields: [
        { name: 'drawingPath', label: 'Drawing path', wide: true },
        { name: 'remark', label: 'Remark', kind: 'textarea', rows: 3 },
      ],
    },
  ];
}
