import { Component, OnInit,ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize, timeout } from 'rxjs';
import { AuthService, EnrolledUser, RoleOption, ActiveSession, AuditLogEntry, UpdateUserRequest } from '../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  email = '';
  role  = '';
  activeNav = 'dashboard';
  sidebarCollapsed = false;

  // Users section
  users: EnrolledUser[]  = [];
  roles: RoleOption[]    = [];
  usersLoading = false;
  showEnrollPanel = false;
  enrollForm!: FormGroup;
  enrollLoading = false;
  enrollError = '';
  enrollSuccess = '';
  showEnrollPassword = false;

  // Edit user
  showEditPanel = false;
  editForm!: FormGroup;
  editingUser: EnrolledUser | null = null;
  editLoading = false;
  editError = '';
  editSuccess = '';

  // Dashboard real data
  statsLoading   = true;
  sessionsLoading = true;
  sessions: ActiveSession[] = [];

  stats = [
    { label: 'Active Users',      value: '—', delta: '', up: true,  icon: 'users',   color: '#e8f5e9', accent: '#2e7d32' },
    { label: 'Total Enrolled',    value: '—', delta: '', up: true,  icon: 'enrolled', color: '#e3f2fd', accent: '#1565c0' },
    { label: 'Active Sessions',   value: '—', delta: '', up: true,  icon: 'sessions', color: '#fff3e0', accent: '#e65100' },
    { label: 'Audit Events Today',value: '—', delta: '', up: true,  icon: 'shield',   color: '#f3e5f5', accent: '#6a1b9a' },
  ];

  // Audit log section
  auditLogs: AuditLogEntry[] = [];
  auditLoading = false;
  auditFilter  = '';

  navItems = [
    { key: 'dashboard', label: 'Dashboard',       icon: 'grid'     },
    { key: 'users',     label: 'Users',           icon: 'users'    },
    { key: 'audit',     label: 'Audit Log',       icon: 'audit'    },
    { key: 'trials',    label: 'Clinical Trials', icon: 'flask'    },
    { key: 'samples',   label: 'Lab & Samples',   icon: 'beaker'   },
    { key: 'protocols', label: 'Protocols',       icon: 'doc'      },
    { key: 'reports',   label: 'Reports',         icon: 'chart'    },
    { key: 'settings',  label: 'Settings',        icon: 'settings' },
  ];

  constructor(private auth: AuthService, private fb: FormBuilder, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.email = this.auth.getEmail() ?? '';
    this.role  = this.auth.getRole()  ?? '';
    this.enrollForm = this.fb.group({
      name:     ['', [Validators.required, Validators.minLength(2)]],
      email:    ['', [Validators.required, Validators.email]],
      phone:    [''],
      password: ['', [Validators.required, Validators.minLength(8)]],
      roleName: ['', Validators.required]
    });
    this.editForm = this.fb.group({
      name:     ['', [Validators.required, Validators.minLength(2)]],
      phone:    [''],
      roleName: ['', Validators.required],
      isActive: [true]
    });
    if (this.isAdmin) this.loadDashboardData();
  }

  get isSystemAdmin(): boolean { return this.role === 'SYSTEM_ADMIN'; }
  get isAdmin(): boolean { return this.role === 'ADMIN' || this.isSystemAdmin; }

  canEdit(user: EnrolledUser): boolean {
    return user.role?.toUpperCase() !== 'SYSTEM_ADMIN';
  }
  get initials(): string { return this.email ? this.email.substring(0, 2).toUpperCase() : 'AD'; }
  get today(): string {
    return new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
  }

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

  loadDashboardData(): void {
    this.auth.getDashboardStats().subscribe({
      next: s => {
        this.stats[0].value = s.activeUsers.toString();
        this.stats[1].value = s.totalEnrolled.toString();
        this.stats[2].value = s.activeSessions.toString();
        this.stats[3].value = s.auditEventsToday.toString();
        this.statsLoading = false;
        this.cdr.detectChanges();
      },
      error: () => { this.statsLoading = false; this.cdr.detectChanges(); }
    });

    this.auth.getUsers().subscribe({
      next: u => { this.users = u; this.sessionsLoading = false; this.cdr.detectChanges(); },
      error: () => { this.sessionsLoading = false; this.cdr.detectChanges(); }
    });
  }

  get recentEmployees() {
    return [...this.users]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5);
  }

  onNavChange(key: string): void {
    this.activeNav = key;
    if (key === 'users'  && this.isAdmin) this.loadUsers();
    if (key === 'audit'  && this.isAdmin) this.loadAuditLogs();
  }

  loadUsers(): void {
    this.usersLoading = this.users.length === 0;

    this.auth.getUsers()
      .pipe(timeout(5000))
      .subscribe({
        next: u => { this.users = u; this.usersLoading = false; this.cdr.detectChanges(); },
        error: () => { this.usersLoading = false; this.cdr.detectChanges(); }
      });

    if (this.roles.length === 0) {
      this.auth.getRoles()
        .pipe(timeout(5000))
        .subscribe({ next: r => { this.roles = r; this.cdr.detectChanges(); }, error: () => { } });
    }
  }

loadAuditLogs(): void {
  this.auditLoading = this.auditLogs.length === 0;

  this.auth.getAuditLogs()
    .pipe(timeout(5000))
    .subscribe({
      next: l => { this.auditLogs = l; this.auditLoading = false; this.cdr.detectChanges(); },
      error: () => { this.auditLoading = false; this.cdr.detectChanges(); }
    });
}

  enrollEmployee(): void {
    this.activeNav = 'users';
    if (this.isAdmin) {
      this.loadUsers();
      setTimeout(() => this.openEnrollPanel(), 100);
    }
  }

  openEnrollPanel(): void {
    this.enrollForm.reset();
    this.enrollError   = '';
    this.enrollSuccess = '';
    this.showEnrollPanel = true;
  }

  closeEnrollPanel(): void { this.showEnrollPanel = false; }

  openEditPanel(user: EnrolledUser): void {
    if (!this.canEdit(user)) return;
    this.editingUser  = user;
    this.editError    = '';
    this.editSuccess  = '';
    this.editForm.setValue({ name: user.name, phone: user.phone ?? '', roleName: user.role, isActive: user.isActive });
    if (this.roles.length === 0) this.auth.getRoles().subscribe({ next: r => this.roles = r });
    this.showEditPanel = true;
  }

  closeEditPanel(): void { this.showEditPanel = false; this.editingUser = null; }

  submitEdit(): void {
    if (this.editForm.invalid || !this.editingUser) return;
    this.editLoading = true;
    this.editError   = '';
    this.editSuccess = '';

    const dto: UpdateUserRequest = this.editForm.value;
    this.auth.updateUser(this.editingUser.userId, dto)
      .pipe(finalize(() => { this.editLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: updated => {
          const idx = this.users.findIndex(u => u.userId === updated.userId);
          if (idx !== -1) this.users[idx] = { ...updated };
          this.editSuccess = `"${updated.name}" updated successfully.`;
          this.cdr.detectChanges();
          setTimeout(() => { this.showEditPanel = false; this.editingUser = null; this.editSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.editError = err.error?.message || 'Update failed. Please try again.';
          this.cdr.detectChanges();
        }
      });
  }

  submitEnroll(): void {
    if (this.enrollForm.invalid) return;
    this.enrollLoading = true;
    this.enrollError   = '';
    this.enrollSuccess = '';

    this.auth.enrollUser(this.enrollForm.value).subscribe({
      next: newUser => {
        this.users.unshift(newUser);
        this.enrollSuccess = `"${newUser.name}" enrolled successfully.`;
        this.enrollLoading = false;
        this.enrollForm.reset();
        setTimeout(() => { this.showEnrollPanel = false; this.enrollSuccess = ''; }, 2000);
      },
      error: err => {
        this.enrollError = err.error?.message || 'Enrollment failed. Please try again.';
        this.enrollLoading = false;
      }
    });
  }

  avatarFor(name: string): string { return name ? name.substring(0, 2).toUpperCase() : '??'; }

  actionColor(action: string): string {
    if (action === 'LOGIN')          return '#e8f5e9';
    if (action === 'LOGOUT')         return '#fff3e0';
    if (action === 'USER_ENROLLED')  return '#e3f2fd';
    if (action === 'LOGIN_FAILED')   return '#fee2e2';
    return '#f5f5f5';
  }

  actionTextColor(action: string): string {
    if (action === 'LOGIN')          return '#2e7d32';
    if (action === 'LOGOUT')         return '#e65100';
    if (action === 'USER_ENROLLED')  return '#1565c0';
    if (action === 'LOGIN_FAILED')   return '#c62828';
    return '#555';
  }

  logout(): void {
    this.auth.logout().subscribe({ error: () => this.auth.clearSession() });
  }
}
