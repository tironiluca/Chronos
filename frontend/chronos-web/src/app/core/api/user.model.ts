// GET /api/users/directory -- readable by any org member, so no email/role (see
// UserDirectoryEntryDto server-side).
export interface UserDirectoryEntryDto {
  id: string;
  displayName: string;
  departmentId: string | null;
}
