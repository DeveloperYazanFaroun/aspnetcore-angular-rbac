import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PermissionGroup, Role, SaveRoleRequest } from './role.models';

@Injectable({ providedIn: 'root' })
export class RoleApi {
  private readonly http = inject(HttpClient);

  list(): Observable<Role[]> {
    return this.http.get<Role[]>('/api/roles');
  }

  get(id: string): Observable<Role> {
    return this.http.get<Role>(`/api/roles/${id}`);
  }

  create(request: SaveRoleRequest): Observable<Role> {
    return this.http.post<Role>('/api/roles', request);
  }

  update(id: string, request: SaveRoleRequest): Observable<Role> {
    return this.http.put<Role>(`/api/roles/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`/api/roles/${id}`);
  }

  permissionCatalogue(): Observable<PermissionGroup[]> {
    return this.http.get<PermissionGroup[]>('/api/permissions');
  }
}
