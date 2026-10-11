import { Routes } from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './core/auth/auth.guards';
import { Permissions } from './core/auth/permissions';
import { Shell } from './layout/shell';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./features/home/home-page').then((m) => m.HomePage),
      },
      {
        path: 'invoices',
        canActivate: [permissionGuard],
        data: { permissions: [Permissions.Invoices.View] },
        loadComponent: () => import('./features/invoices/invoice-list-page').then((m) => m.InvoiceListPage),
      },
      {
        path: 'users',
        canActivate: [permissionGuard],
        data: { permissions: [Permissions.Users.View] },
        loadComponent: () => import('./features/users/user-list-page').then((m) => m.UserListPage),
      },
      {
        path: 'roles',
        canActivate: [permissionGuard],
        data: { permissions: [Permissions.Roles.View] },
        loadComponent: () => import('./features/roles/role-list-page').then((m) => m.RoleListPage),
      },
      {
        path: 'roles/new',
        canActivate: [permissionGuard],
        data: { permissions: [Permissions.Roles.Manage] },
        loadComponent: () => import('./features/roles/role-editor-page').then((m) => m.RoleEditorPage),
      },
      {
        path: 'roles/:id',
        canActivate: [permissionGuard],
        data: { permissions: [Permissions.Roles.View] },
        loadComponent: () => import('./features/roles/role-editor-page').then((m) => m.RoleEditorPage),
      },
      {
        path: 'forbidden',
        loadComponent: () => import('./features/errors/forbidden-page').then((m) => m.ForbiddenPage),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () => import('./features/errors/not-found-page').then((m) => m.NotFoundPage),
  },
];
