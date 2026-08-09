import type { MasterFormSection } from '@/components/form/master-form';

export interface PartFormValues {
  partNumber: string;
  description: string;
  unitOfMeasureCode: string;
  hsnCode: string;
  drawingNumber: string;
  itemNumber: string;
  technicalSpecification: string;
  moc: string;
  partCategoryCode: string;
  partType: string;
  formCategory: string;
  purchaseUomCode: string;
  sellingUomCode: string;
  materialType: string;
  seriesCode: string;
  partRevisionNo: string;
  sourceCode: string;
  weightKg: string;
  leadTimeDays: string;
  minimumStockLevel: string;
  reorderPoint: string;
  revisionRemark: string;
  holdRemark: string;
  inactiveRemark: string;
}

/**
 * Numbers are held as strings all the way to the edge.
 *
 * A form input produces text, and coercing it to a number early turns a
 * half-finished "1." into NaN and an empty box into 0 — so a field the user never
 * touched arrives at the server as a real value. They are parsed once, in the save
 * hook, where blank can be mapped to null deliberately.
 */

/**
 * Every option list this form needs, fetched in one request.
 *
 * Nothing here is a list of values — only the names of lists the server holds.
 * The legacy screen wrote `items: ["OutSource", "In House"]` straight into its
 * JavaScript, so adding a source code meant a deployment.
 */
export const PART_LOOKUPS = [
  'uom',
  'moc',
  'part.categoryCode',
  'part.type',
  'part.formCategory',
  'part.materialType',
  'part.seriesCode',
  'part.sourceCode',
  'part.revisionNo',
] as const;

/**
 * The part form, grouped the way someone filling it in thinks about a part:
 * what it is, how it is classified, how it is measured and stocked, what
 * paperwork it carries, and why it was changed.
 *
 * `isNew` controls one field only. The part number is the business key and
 * renaming it silently re-points every BOM line and purchase order that refers to
 * it, so on an existing part it is shown and locked rather than hidden — people
 * need to read it while they edit the rest.
 */
export function partFormSections(isNew: boolean): MasterFormSection<PartFormValues>[] {
  return [
    {
      id: 'identity',
      label: 'Identity',
      description: isNew
        ? 'The part number is the business key and cannot be changed afterwards.'
        : 'The part number cannot be changed here — that is a separate, audited operation.',
      fields: [
        {
          name: 'partNumber',
          label: 'System part number',
          required: true,
          readOnly: !isNew,
          placeholder: 'MS-PLT-000001-00',
          description: 'Letters, digits, dot, underscore, slash and hyphen.',
        },
        { name: 'itemNumber', label: 'Item code (manual)', placeholder: 'PPE1043' },
        { name: 'description', label: 'Part description', required: true, wide: true },
        {
          name: 'technicalSpecification',
          label: 'Technical specification',
          kind: 'textarea',
          rows: 4,
          description: 'Up to 2,000 characters. Unicode symbols such as Ω, µ and Ø are kept as typed.',
        },
      ],
    },
    {
      id: 'classification',
      label: 'Classification',
      fields: [
        { name: 'partCategoryCode', label: 'Part category code', lookup: 'part.categoryCode' },
        { name: 'partType', label: 'Part type', lookup: 'part.type' },
        { name: 'formCategory', label: 'Form category', lookup: 'part.formCategory' },
        { name: 'materialType', label: 'Material type', lookup: 'part.materialType' },
        { name: 'seriesCode', label: 'Series code', lookup: 'part.seriesCode' },
        { name: 'moc', label: 'MOC', lookup: 'moc', description: 'Material of construction.' },
        { name: 'sourceCode', label: 'Source code', lookup: 'part.sourceCode' },
        { name: 'partRevisionNo', label: 'Part revision no', lookup: 'part.revisionNo' },
      ],
    },
    {
      id: 'stock',
      label: 'Units & stock',
      fields: [
        {
          name: 'unitOfMeasureCode',
          label: 'Primary UOM',
          required: true,
          lookup: 'uom',
        },
        { name: 'purchaseUomCode', label: 'Purchase UOM', lookup: 'uom' },
        { name: 'sellingUomCode', label: 'Selling UOM', lookup: 'uom' },
        { name: 'weightKg', label: 'Weight (kg)', kind: 'number', placeholder: '0.0000' },
        { name: 'leadTimeDays', label: 'Lead time (days)', kind: 'integer' },
        { name: 'minimumStockLevel', label: 'Minimum stock level', kind: 'number' },
        { name: 'reorderPoint', label: 'Reorder point', kind: 'integer' },
      ],
    },
    {
      id: 'compliance',
      label: 'Compliance & drawing',
      fields: [
        {
          name: 'hsnCode',
          label: 'HSN code',
          placeholder: '84821011',
          description: '4, 6 or 8 digits. Used for GST on purchase and dispatch documents.',
        },
        { name: 'drawingNumber', label: 'Drawing revision path', wide: true },
      ],
    },
    {
      id: 'remarks',
      label: 'Remarks',
      fields: [
        { name: 'revisionRemark', label: 'Revision remark', kind: 'textarea', rows: 2 },
        { name: 'holdRemark', label: 'Hold remark', kind: 'textarea', rows: 2 },
        { name: 'inactiveRemark', label: 'Inactive remark', kind: 'textarea', rows: 2 },
      ],
    },
  ];
}
