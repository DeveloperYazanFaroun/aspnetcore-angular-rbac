/**
 * Mirror of the server permission catalogue (server/src/Rbac.Api/Authorization/Permissions.cs).
 * Keep both files in sync. The server is the source of truth: hiding UI is for usability only.
 */
export const Permissions = {
  Invoices: {
    View: 'invoices.view',
    Create: 'invoices.create',
    Edit: 'invoices.edit',
    Delete: 'invoices.delete',
    Submit: 'invoices.submit',
    Approve: 'invoices.approve',
    Export: 'invoices.export',
  },
  Users: {
    View: 'users.view',
    Manage: 'users.manage',
  },
  Roles: {
    View: 'roles.view',
    Manage: 'roles.manage',
  },
} as const;

type ValueOf<T> = T[keyof T];

/** Union of every permission code, e.g. 'invoices.view' | 'invoices.create' | ... */
export type Permission = ValueOf<{ [Group in keyof typeof Permissions]: ValueOf<(typeof Permissions)[Group]> }>;
