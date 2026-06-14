export interface PermissionDto {
  id:          string;
  key:         string;
  description: string;
}

export interface RoleDto {
  id:           string;
  name:         string;
  description:  string;
  isSystemRole: boolean;
}

export interface RoleDetailDto {
  id:          string;
  name:        string;
  description: string;
  isSystemRole: boolean;
  permissions: PermissionDto[];
}

export interface CreateRoleRequest {
  name:          string;
  description:   string;
  permissionIds: string[];
}

export interface UpdateRoleRequest {
  name:          string;
  description:   string;
  permissionIds: string[];
}
