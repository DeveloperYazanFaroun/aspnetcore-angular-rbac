export interface TokenResponse {
  accessToken: string;
  expiresAtUtc: string;
}

/** Returned by GET /api/me. */
export interface CurrentUser {
  id: string;
  fullName: string;
  email: string;
  tenantId: string;
  tenantName: string;
  roles: string[];
  permissions: string[];
}
