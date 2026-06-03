import { Component, OnInit, ChangeDetectorRef, Input } from '@angular/core';
import { timeout } from 'rxjs';
import { AuthService, AuditLogEntry } from '../../services/auth.service';

@Component({
  selector: 'app-audit',
  standalone: false,
  templateUrl: './audit.component.html',
  styleUrl: './audit.component.css'
})
export class AuditComponent implements OnInit {
  @Input() role = '';

  auditLogs: AuditLogEntry[] = [];
  auditLoading = false;
  auditFilter = '';

  get isAdmin(): boolean { return this.role === 'ADMIN' || this.role === 'SYSTEM_ADMIN'; }

  get filteredAuditLogs(): AuditLogEntry[] {
    if (!this.auditFilter.trim()) return this.auditLogs;
    const q = this.auditFilter.toLowerCase();
    return this.auditLogs.filter(l =>
      l.actorName.toLowerCase().includes(q) ||
      l.actorEmail.toLowerCase().includes(q) ||
      l.action.toLowerCase().includes(q) ||
      l.description.toLowerCase().includes(q)
    );
  }

  constructor(private auth: AuthService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void { this.loadAuditLogs(); }

  loadAuditLogs(): void {
    this.auditLoading = this.auditLogs.length === 0;
    this.auth.getAuditLogs().pipe(timeout(5000)).subscribe({
      next: l => { this.auditLogs = l; this.auditLoading = false; this.cdr.detectChanges(); },
      error: () => { this.auditLoading = false; this.cdr.detectChanges(); }
    });
  }

  avatarFor(name: string): string { return name ? name.substring(0, 2).toUpperCase() : '??'; }

  actionColor(action: string): string {
    if (action === 'LOGIN')         return '#e8f5e9';
    if (action === 'LOGOUT')        return '#fff3e0';
    if (action === 'USER_ENROLLED') return '#e3f2fd';
    if (action === 'LOGIN_FAILED')  return '#fee2e2';
    return '#f5f5f5';
  }

  actionTextColor(action: string): string {
    if (action === 'LOGIN')         return '#2e7d32';
    if (action === 'LOGOUT')        return '#e65100';
    if (action === 'USER_ENROLLED') return '#1565c0';
    if (action === 'LOGIN_FAILED')  return '#c62828';
    return '#555';
  }
}
