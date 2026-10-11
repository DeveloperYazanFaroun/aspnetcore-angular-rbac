import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateUserRequest, User } from './user.models';

@Injectable({ providedIn: 'root' })
export class UserApi {
  private readonly http = inject(HttpClient);

  list(): Observable<User[]> {
    return this.http.get<User[]>('/api/users');
  }

  create(request: CreateUserRequest): Observable<User> {
    return this.http.post<User>('/api/users', request);
  }

  assignRoles(id: string, roleIds: string[]): Observable<User> {
    return this.http.put<User>(`/api/users/${id}/roles`, { roleIds });
  }

  activate(id: string): Observable<User> {
    return this.http.post<User>(`/api/users/${id}/activate`, {});
  }

  deactivate(id: string): Observable<User> {
    return this.http.post<User>(`/api/users/${id}/deactivate`, {});
  }
}
