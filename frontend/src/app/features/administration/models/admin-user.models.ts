export type UserRole   = 'Admin' | 'Member' | 'Viewer';
export type UserStatus = 'Active' | 'Pending' | 'Inactive';

export interface AdminUserDto {
  id:          string;
  displayName: string;
  email:       string;
  role:        UserRole;
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
  role:        UserRole;
}

export interface UpdateUserRoleRequest {
  role: UserRole;
}
