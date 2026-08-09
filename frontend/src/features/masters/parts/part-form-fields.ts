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

/** Quarter width, as the prototype's Classification and Flags rows use. */
const Q = 'sm:col-span-3 lg:col-span-3';
/** Sixth width — the prototype's Units and Stock rows fit six across. */
const S = 'sm:col-span-2 lg:col-span-2';
/** Half width, for the part description. */
const H = 'sm:col-span-6 lg:col-span-6';

/**
 * The part form, in the approved prototype's sections and order.
 *
 * Classification, then Description & Drawing, then Units/Source/Weight, then
 * Stock Levels & Codes — the same five cards, the same field grouping and the
 * same column spans as `PartMasterForm`, so someone moving between the two is
 * looking at the same screen.
 *
 * Two deliberate differences, both forced by what this API actually accepts:
 *
 * - The system part number is a field here. The prototype assigns it on save and
 *   shows "Assigned on save" in its identity bar; this API takes a user-entered
 *   number until the numbering allocator lands, so it has to be typed somewhere.
 * - The fifth card is Remarks, not Flags. The prototype's flags are Is Active and
 *   QC Required; `UpdatePartRequest` accepts neither — activation is its own
 *   audited operation and there is no QC flag on the part at all. An empty card,
 *   or checkboxes that silently fail to save, would be worse than showing the
 *   three remark fields this master really has.
 *
 * `isNew` controls one field. The part number is the business key and renaming it
 * silently re-points every BOM line and purchase order that refers to it, so on an
 * existing part it is shown and locked rather than hidden — people need to read it
 * while they edit the rest.
 */
export function partFormSections(isNew: boolean): MasterFormSection<PartFormValues>[] {
  return [
    {
      id: 'classification',
      label: 'Classification',
      fields: [
        {
          name: 'partNumber',
          label: 'System Part Number',
          required: true,
          readOnly: !isNew,
          placeholder: 'MS-PLT-000001-00',
          span: Q,
          description: isNew ? 'Cannot be changed afterwards.' : undefined,
        },
        { name: 'seriesCode', label: 'Series Code', lookup: 'part.seriesCode', span: Q },
        { name: 'partCategoryCode', label: 'Part Category Code', lookup: 'part.categoryCode', span: Q },
        { name: 'partType', label: 'Part Type', lookup: 'part.type', span: Q },
        { name: 'formCategory', label: 'Form Category', lookup: 'part.formCategory', span: Q },
        { name: 'materialType', label: 'Material Type', lookup: 'part.materialType', span: Q },
        { name: 'itemNumber', label: 'Item Code (Manual)', placeholder: 'PPE1043', span: Q },
      ],
    },
    {
      id: 'description',
      label: 'Description & Drawing',
      fields: [
        { name: 'description', label: 'Part Description', required: true, span: H },
        { name: 'moc', label: 'MOC (Material of Construction)', lookup: 'moc', span: Q },
        {
          name: 'technicalSpecification',
          label: 'Technical Specification',
          kind: 'textarea',
          rows: 4,
          description: 'Up to 2,000 characters. Unicode symbols such as Ω, µ and Ø are kept as typed.',
        },
        { name: 'drawingNumber', label: 'Drawing Path', wide: true },
      ],
    },
    {
      id: 'units',
      label: 'Units, Source & Weight',
      fields: [
        {
          name: 'unitOfMeasureCode',
          label: 'Primary UOM',
          required: true,
          lookup: 'uom',
          span: S,
        },
        { name: 'purchaseUomCode', label: 'Purchase UOM', lookup: 'uom', span: S },
        { name: 'sellingUomCode', label: 'Selling UOM', lookup: 'uom', span: S },
        { name: 'sourceCode', label: 'Source Code', lookup: 'part.sourceCode', span: S },
        { name: 'partRevisionNo', label: 'Part Revision No', lookup: 'part.revisionNo', span: S },
        { name: 'weightKg', label: 'Weight (Kg)', kind: 'number', placeholder: '0.0000', span: S },
      ],
    },
    {
      id: 'stock',
      label: 'Stock Levels & Codes',
      fields: [
        { name: 'minimumStockLevel', label: 'Minimum Stock Level', kind: 'number', span: S },
        { name: 'reorderPoint', label: 'Reorder Point (Days)', kind: 'integer', span: S },
        // No Maximum Stock Level: the prototype has one, this master does not.
        { name: 'leadTimeDays', label: 'Lead Time (Days)', kind: 'integer', span: S },
        {
          name: 'hsnCode',
          label: 'HSN Code',
          placeholder: '84821011',
          span: S,
          description: '4, 6 or 8 digits.',
        },
      ],
    },
  ];
}
