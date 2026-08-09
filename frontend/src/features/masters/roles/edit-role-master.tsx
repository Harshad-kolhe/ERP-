'use client';

import { RoleMasterForm } from './role-master-form';
import { EditMasterRecord } from '../shared/edit-master-record';
import type { RoleMasterDetail } from '@/lib/api/types';

/**
 * Loads one role, then hands it to the form.
 *
 * A client component, and that is the point rather than an accident.
 * `EditMasterRecord` takes a render function, and a function cannot cross the
 * server/client boundary — passing one straight from the route file made every
 * edit screen answer 500 with "Functions are not valid as a child of Client
 * Components". The route stays a server component so it can await its params;
 * this is where the boundary is drawn.
 */
export function EditRoleMaster({ id }: { id: string }) {
  return (
    <EditMasterRecord<RoleMasterDetail> resource="roles" id={id} noun="role">
      {(record) => <RoleMasterForm role={record} />}
    </EditMasterRecord>
  );
}
