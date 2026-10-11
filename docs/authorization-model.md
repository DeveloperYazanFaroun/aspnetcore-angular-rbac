# Authorization model

## Vocabulary

| Term | Meaning in this project |
|---|---|
| **User** | `ApplicationUser`: a person who can sign in. Belongs to one tenant. |
| **Tenant** | An organization. Users, roles and invoices are isolated per tenant. |
| **Permission** | One protected action, `resource.action` (e.g. `invoices.approve`). Defined in `Permissions.cs`. |
| **Role** | `ApplicationRole`: a named set of permissions inside one tenant (e.g. *Approver*). |
| **Claim** | A fact about the user inside the token: `sub`, `email`, `name`, `tenant_id`, `role`. |
| **Effective permissions** | The union of the permissions of all the user's roles. |

## Permission catalogue

| Code | Description |
|---|---|
| `invoices.view` | View invoices |
| `invoices.create` | Create draft invoices |
| `invoices.edit` | Edit draft invoices |
| `invoices.delete` | Delete draft invoices |
| `invoices.submit` | Submit draft invoices for approval |
| `invoices.approve` | Approve or reject submitted invoices |
| `invoices.export` | Export invoices to CSV |
| `users.view` | View users in the organization |
| `users.manage` | Create users, assign roles, activate and deactivate users |
| `roles.view` | View roles and their permissions |
| `roles.manage` | Create, edit and delete roles |

## Default roles (created for every tenant)

| Permission | Administrator | Accountant | Approver | Viewer |
|---|:-:|:-:|:-:|:-:|
| `invoices.view` | ✔ | ✔ | ✔ | ✔ |
| `invoices.create` | ✔ | ✔ | | |
| `invoices.edit` | ✔ | ✔ | | |
| `invoices.delete` | ✔ | ✔ | | |
| `invoices.submit` | ✔ | ✔ | | |
| `invoices.approve` | ✔ | | ✔ | |
| `invoices.export` | ✔ | ✔ | | |
| `users.*`, `roles.*` | ✔ | | | |

*Administrator* is a **system role**. It always receives every permission, including new ones, at startup, and it can't be edited or deleted.

## Design decisions

### 1. Code checks permissions, never role names

`[Authorize(Roles = "Admin,Accountant")]` puts business decisions in code. Every new job title would need a deployment. Here, controllers only use `[HasPermission(...)]`, and administrators shape roles at runtime.

### 2. A custom filter instead of policy-based authorization

`HasPermissionAttribute` is a `TypeFilterAttribute` that creates `PermissionAuthorizationFilter`. It is explicit, easy to unit-test and has no registration step per permission. Two attributes on one action mean **AND**.

### 3. Identity in the token, permissions on the server

The JWT carries *who you are* (`sub`, `tenant_id`, roles for display). `PermissionService` resolves *what you may do* from the database and caches it for five minutes.

| | Permissions in the token | Permissions on the server (this project) |
|---|---|---|
| Token size | grows with permissions | small |
| Effect of a role change | after the token expires | next request (cache invalidated) |
| Deactivated user | still allowed until expiry | denied at once |
| Cost | none per request | one cached lookup |

The cache is cleared for the affected users whenever a role's permissions, a user's roles or a user's status changes (`PermissionChangeNotifier`). A request that is running while the change is saved can still write old values back to the cache. That window lasts at most the cache duration. Use a version stamp or a shorter duration if you need stronger guarantees.

### 4. Tenant isolation with a global query filter

`AppDbContext` adds `WHERE TenantId = @currentTenant` to every `Invoice` query. A record of another tenant is "not found", so the API returns **404**, not 403, and doesn't confirm that the record exists.

Identity tables (`Users`, `Roles`) have **no** global filter, because sign-in runs before a tenant is known. Services filter them explicitly by `TenantId`.

### 5. Business rules live in the domain

A permission can't express "the creator must not approve the invoice". `Invoice.Approve` checks it and returns `403` with a clear message. The UI disables the button for the same reason, but the server enforces it.

### 6. No privilege escalation

`PrivilegeEscalationGuard` rejects a role or a role assignment that grants a permission the caller doesn't hold. Users can't change their own roles or deactivate themselves, and the last active Administrator of a tenant can't be removed.

### 7. Token handling

- Access token: 15 minutes, kept in memory in the browser, never in `localStorage`.
- Refresh token: random 64 bytes, stored as a SHA-256 hash, sent as an `HttpOnly; Secure; SameSite=Strict` cookie limited to `/api/auth`.
- Rotation: each refresh revokes the old token. Reusing a revoked token revokes **all** sessions of the user.
- `MapInboundClaims = false` keeps claim names as they are in the token (`sub`, not a long .NET URI).

## Adding a new permission

1. Add a constant with a `[Description]` to `server/src/Rbac.Api/Authorization/Permissions.cs`.
2. Add the same code to `client/src/app/core/auth/permissions.ts`.
3. Protect the endpoint with `[HasPermission(Permissions.Module.Action)]`.
4. Start the API: the seeder inserts the new row and gives it to every Administrator role.
5. Assign it to other roles from the **Roles** screen (or the API). Don't do it in SQL.
6. In Angular, use `*appHasPermission` and/or `permissionGuard`.
7. Add an integration test for 403 (without the permission) and success (with it).

## Extending

- **Resource ownership** (e.g. "edit only your own drafts"): add the check in the domain method, like separation of duties.
- **Hierarchical roles**: prefer explicit permission lists. Inheritance hides what a role can really do.
- **ABAC rules** (amount limits, working hours): add attributes to the user or role and check them in the service layer.
- **Many services behind a gateway**: validate the JWT in every service, and share `Permissions.cs` and the permission service as a package backed by a distributed cache.
