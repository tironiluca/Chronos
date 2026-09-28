export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  role: string;
  departmentId: string | null;
}
