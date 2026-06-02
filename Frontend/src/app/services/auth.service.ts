import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface LoginRequest  { email: string; password: string; }
export interface LoginResponse { token: string; userId: string; email: string; role: string; expiresIn: number; }

export interface EnrollUserRequest  { name: string; email: string; phone?: string; password: string; roleName: string; }
export interface UpdateUserRequest  { name: string; phone?: string; roleName: string; isActive: boolean; }
export interface EnrolledUser      { userId: string; name: string; email: string; phone?: string; role: string; isActive: boolean; createdAt: string; }
export interface RoleOption        { roleId: string; roleName: string; }

export interface DashboardStats {
  activeUsers: number; totalEnrolled: number; activeSessions: number; auditEventsToday: number;
}
export interface ActiveSession {
  name: string; email: string; role: string; ipAddress?: string;
  loginTime: string; expiresAt: string; status: string;
}
export interface AuditLogEntry {
  logId: number; actorName: string; actorEmail: string;
  action: string; description: string; targetUserName?: string;
  ipAddress?: string; isSuccess: boolean; createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = `${environment.apiUrl}/api/auth`;

  constructor(private http: HttpClient, private router: Router) {}

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap(res => {
        localStorage.setItem('token', res.token);
        localStorage.setItem('role',  res.role);
        localStorage.setItem('userId', res.userId);
        localStorage.setItem('email', res.email);
      })
    );
  }

  logout(): Observable<any> {
    return this.http.post(`${this.apiUrl}/logout`, {}).pipe(tap(() => this.clearSession()));
  }

  enrollUser(dto: EnrollUserRequest): Observable<EnrolledUser> {
    return this.http.post<EnrolledUser>(`${this.apiUrl}/enroll`, dto);
  }

  updateUser(userId: string, dto: UpdateUserRequest): Observable<EnrolledUser> {
    return this.http.put<EnrolledUser>(`${this.apiUrl}/users/${userId}`, dto);
  }

  getUsers():          Observable<EnrolledUser[]>   { return this.http.get<EnrolledUser[]>(`${this.apiUrl}/users`); }
  getRoles():          Observable<RoleOption[]>      { return this.http.get<RoleOption[]>(`${this.apiUrl}/roles`); }
  getDashboardStats(): Observable<DashboardStats>    { return this.http.get<DashboardStats>(`${this.apiUrl}/stats`); }
  getActiveSessions(): Observable<ActiveSession[]>   { return this.http.get<ActiveSession[]>(`${this.apiUrl}/sessions`); }
  getAuditLogs():      Observable<AuditLogEntry[]>   { return this.http.get<AuditLogEntry[]>(`${this.apiUrl}/audit-logs`); }

  getToken():  string | null { return localStorage.getItem('token'); }
  getRole():   string | null { return localStorage.getItem('role'); }
  getEmail():  string | null { return localStorage.getItem('email'); }
  getName():   string | null { return localStorage.getItem('name'); }
  getUserId(): string | null { return localStorage.getItem('userId'); }
  isLoggedIn(): boolean      { return !!this.getToken(); }

  clearSession(): void { localStorage.clear(); this.router.navigate(['/login']); }
}
