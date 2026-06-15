import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { RoleDetailDto } from '../../administration/models/role.models';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);

  getMyRoles(): Observable<RoleDetailDto[]> {
    return this.http.get<RoleDetailDto[]>('/bff/identity/users/me/roles');
  }
}
