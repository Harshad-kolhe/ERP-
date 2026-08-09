'use client';

import type { UnitOfMeasureDetail } from '@/lib/api/types';
import { EditMasterRecord } from '../shared/edit-master-record';
import { UnitOfMeasureForm } from './unit-of-measure-form';

/** Loads one unit, then hands it to the form. See `EditRoleMaster` for why this boundary exists. */
export function EditUnitOfMeasure({ id }: { id: string }) {
  return (
    <EditMasterRecord<UnitOfMeasureDetail> resource="units-of-measure" id={id} noun="unit">
      {(record) => <UnitOfMeasureForm unit={record} />}
    </EditMasterRecord>
  );
}
