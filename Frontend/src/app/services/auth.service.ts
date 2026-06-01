

import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';   
import { Router } from '@angular/router';            
import { Observable, tap } from 'rxjs';             
import { environment } from '../../environments/environment'; 


// What we SEND when logging in
export interface LoginRequest  { email: string; password: string; }

// What we RECEIVE back after successful login
export interface LoginResponse {
  token: string;     
  userId: string;   
  name: string;     
  email: string;     
  role: string;     
  expiresIn: number; 
}

// What we SEND when enrolling (creating) a new user — Admin only
export interface EnrollUserRequest  { name: string; email: string; phone?: string; password: string; roleName: string; }

// What we SEND when updating an existing user — Admin only
export interface UpdateUserRequest  { name: string; phone?: string; roleName: string; isActive: boolean; }

// What we RECEIVE when fetching a user's details
export interface EnrolledUser { userId: string; name: string; email: string; phone?: string; role: string; isActive: boolean; createdAt: string; }

// What we RECEIVE when fetching available roles (ADMIN, LAB_TECHNICIAN etc.)
export interface RoleOption { roleId: string; roleName: string; }

// What we RECEIVE for the admin dashboard stats cards
export interface DashboardStats {
  activeUsers: number;      // How many users are currently active
  totalEnrolled: number;    // Total users registered in the system
  activeSessions: number;   // How many users are currently logged in
  auditEventsToday: number; // How many actions were logged today
}

// What we RECEIVE for each active login session
export interface ActiveSession {
  name: string; email: string; role: string; ipAddress?: string;
  loginTime: string; expiresAt: string; status: string;
}

// What we RECEIVE for each audit log entry
export interface AuditLogEntry {
  logId: number; actorName: string; actorEmail: string;
  action: string; description: string; targetUserName?: string;
  ipAddress?: string; isSuccess: boolean; createdAt: string;
}

// ── Service Class ─────────────────────────────────────────────
// @Injectable({ providedIn: 'root' }) means Angular creates ONE
// instance of this service and shares it across the whole app.
@Injectable({ providedIn: 'root' })
export class AuthService {

  // Base URL for all auth API calls — reads from environment.ts
  // e.g. "http://localhost:5015/api/auth"
  private apiUrl = `${environment.apiUrl}/api/auth`;

  // Angular injects HttpClient (for API calls) and Router (for navigation)
  constructor(private http: HttpClient, private router: Router) {}

  // ── LOGIN ─────────────────────────────────────────────────
  // Sends email + password to the backend.
  // On success: saves token, role, userId, email, name to localStorage.
  // Returns an Observable — caller must .subscribe() to trigger it.
  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      // tap() runs a side effect WITHOUT changing the data flowing through
      // Here: save all user info to localStorage after successful login
      tap(res => {
        localStorage.setItem('token',  res.token);   // JWT token (sent with every request via interceptor)
        localStorage.setItem('role',   res.role);    // e.g. "LAB_TECHNICIAN" — used for role-based UI
        localStorage.setItem('userId', res.userId);  // Used when creating samples/lab results
        localStorage.setItem('email',  res.email);   // Displayed in sidebar
        localStorage.setItem('name',   res.name);    // Displayed in greeting "Good evening, Priya Sharma!"
      })
    );
  }

  // ── LOGOUT ────────────────────────────────────────────────
  // Tells the backend to invalidate the session, then clears local data.
  logout(): Observable<any> {
    return this.http.post(`${this.apiUrl}/logout`, {})
      .pipe(tap(() => this.clearSession())); // After server confirms logout, clear localStorage
  }

  // ── ENROLL USER (Admin only) ──────────────────────────────
  // Creates a new user account in the system.
  enrollUser(dto: EnrollUserRequest): Observable<EnrolledUser> {
    return this.http.post<EnrolledUser>(`${this.apiUrl}/enroll`, dto);
  }

  // ── UPDATE USER (Admin only) ──────────────────────────────
  // Updates an existing user's name, phone, role, or active status.
  updateUser(userId: string, dto: UpdateUserRequest): Observable<EnrolledUser> {
    return this.http.put<EnrolledUser>(`${this.apiUrl}/users/${userId}`, dto);
  }

  // ── GET DATA (Admin only) ─────────────────────────────────
  // Fetch all users — shown in Users section
  getUsers():          Observable<EnrolledUser[]>   { return this.http.get<EnrolledUser[]>(`${this.apiUrl}/users`); }

  // Fetch available roles (ADMIN, LAB_TECHNICIAN etc.) — used in Enroll form dropdown
  getRoles():          Observable<RoleOption[]>      { return this.http.get<RoleOption[]>(`${this.apiUrl}/roles`); }

  // Fetch stat numbers for admin dashboard cards (Active Users, Total Enrolled etc.)
  getDashboardStats(): Observable<DashboardStats>    { return this.http.get<DashboardStats>(`${this.apiUrl}/stats`); }

  // Fetch currently active sessions — who is logged in right now
  getActiveSessions(): Observable<ActiveSession[]>   { return this.http.get<ActiveSession[]>(`${this.apiUrl}/sessions`); }

  // Fetch last 100 audit log entries — all LOGIN, LOGOUT, ENROLL events
  getAuditLogs():      Observable<AuditLogEntry[]>   { return this.http.get<AuditLogEntry[]>(`${this.apiUrl}/audit-logs`); }

  // ── HELPER METHODS ────────────────────────────────────────
  // These read from localStorage — used by interceptor, guard, and dashboard

  getToken():   string | null { return localStorage.getItem('token'); }  // JWT token — sent in every API request
  getRole():    string | null { return localStorage.getItem('role'); }   // User's role — used for role-based UI
  getEmail():   string | null { return localStorage.getItem('email'); }  // User's email — shown in sidebar
  getName():    string | null { return localStorage.getItem('name'); }   // User's full name — shown in greeting

  // Returns true if a token exists in localStorage → user is logged in
  // !! converts any value to boolean (null → false, "eyJh..." → true)
  isLoggedIn(): boolean { return !!this.getToken(); }

  // ── CLEAR SESSION ─────────────────────────────────────────
  // Removes ALL data from localStorage and redirects to login page.
  // Called on logout or when a 401 Unauthorized error is received.
  clearSession(): void {
    localStorage.clear();            // Wipes token, role, userId, email, name
    this.router.navigate(['/login']); // Sends user back to login page
  }
}
