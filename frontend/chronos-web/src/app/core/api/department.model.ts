export interface DepartmentDto {
  id: string;
  organizationId: string;
  name: string;
  code: string;
  parentDepartmentId: string | null;
}

// organizationId is NOT sent -- the server derives it from the caller's JWT (see
// DepartmentEndpoints), same convention as CreateLeaveRequestPayload/RegisterOrganizationPayload.
export interface CreateDepartmentPayload {
  name: string;
  code: string;
  parentDepartmentId: string | null;
}
