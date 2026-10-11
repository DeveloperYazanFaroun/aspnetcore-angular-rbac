export interface User {
  id: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roles: { id: string; name: string }[];
}

export interface CreateUserRequest {
  fullName: string;
  email: string;
  password: string;
  roleIds: string[];
}
