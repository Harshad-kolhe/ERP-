'use client';

import { BusinessUnitForm } from './business-unit-form';
import { EditMasterRecord } from '../shared/edit-master-record';
import type { BusinessUnitDetail } from '@/lib/api/types';

/**
 * Loads one business unit, then hands it to the form.
 *
 * A client component, and that is the point rather than an accident.
 * `EditMasterRecord` takes a render function, and a function cannot cross the
 * server/client boundary — passing one straight from the route file made every
 * edit screen answer 500 with "Functions are not valid as a child of Client
 * Components". The route stays a server component so it can await its params;
 * this is where the boundary is drawn.
 */
export function EditBusinessUnit({ id }: { id: string }) {
  return (
    <EditMasterRecord<BusinessUnitDetail> resource="business-units" id={id} noun="business unit">
      {(record) => <BusinessUnitForm unit={record} />}
    </EditMasterRecord>
  );
}
