export type UserStatus = 'Active' | 'Pending' | 'Inactive';

export interface AdminUserDto {
  id:          string;
  displayName: string;
  email:       string;
  roleId:      string | null;
  role:        string;
  isActive:    boolean;
  isPending:   boolean;
  createdAt:   string;
}

export interface PagedResult<T> {
  items:           T[];
  totalCount:      number;
  page:            number;
  pageSize:        number;
  totalPages:      number;
  hasNextPage:     boolean;
  hasPreviousPage: boolean;
}

export interface InviteUserRequest {
  email:       string;
  displayName: string;
  roleId?:     string;
}

export interface AssignUserRoleRequest {
  roleId: string;
}
