export interface Role {
  id: string;
  name: string;
  description: string | null;
  isSystem: boolean;
  userCount: number;
  permissions: string[];
}

export interface SaveRoleRequest {
  name: string;
  description: string | null;
  permissions: string[];
}

export interface PermissionGroup {
  group: string;
  permissions: { code: string; description: string }[];
}
