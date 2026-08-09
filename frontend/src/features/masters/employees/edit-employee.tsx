'use client';

import { EmployeeForm } from './employee-form';
import { EditMasterRecord } from '../shared/edit-master-record';
import type { EmployeeDetail } from '@/lib/api/types';

/**
 * Loads one employee, then hands it to the form.
 *
 * A client component, and that is the point rather than an accident.
 * `EditMasterRecord` takes a render function, and a function cannot cross the
 * server/client boundary — passing one straight from the route file made every
 * edit screen answer 500 with "Functions are not valid as a child of Client
 * Components". The route stays a server component so it can await its params;
 * this is where the boundary is drawn.
 */
export function EditEmployee({ id }: { id: string }) {
  return (
    <EditMasterRecord<EmployeeDetail> resource="employees" id={id} noun="employee">
      {(record) => <EmployeeForm employee={record} />}
    </EditMasterRecord>
  );
}
