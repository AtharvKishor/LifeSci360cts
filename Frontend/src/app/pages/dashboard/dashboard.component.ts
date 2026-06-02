import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize, timeout } from 'rxjs';
import { AuthService, EnrolledUser, RoleOption, ActiveSession, AuditLogEntry, UpdateUserRequest } from '../../services/auth.service';
import {
  ReportingService, DashboardSummary, KpiReport, KpiAlert, KpiMetrics,
  KPI_SCOPES, KpiScopeName
} from '../../services/reporting.service';
import {
  NotificationService, NotificationItem, NotificationHistory, NOTIFICATION_CATEGORIES
} from '../../services/notification.service';

@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, OnDestroy {
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

  // ── Reports & Analytics section ──────────────────────────
  reportSummary: DashboardSummary | null = null;
  reportSummaryLoading = false;
  reports: KpiReport[] = [];
  reportsLoading = false;
  reportsError = '';
  reportScopeFilter: 'ALL' | KpiScopeName = 'ALL';
  scopeOptions = KPI_SCOPES;

  // KPI cards derived from the summary
  kpiCards = [
    { key: 'enrollmentRate',       label: 'Enrollment Rate',       trendKey: 'enrollmentRateChange',       color: '#e3f2fd', accent: '#1565c0' },
    { key: 'sampleProcessingRate', label: 'Sample Processing',     trendKey: 'sampleProcessingRateChange', color: '#e8f5e9', accent: '#2e7d32' },
    { key: 'complianceScore',      label: 'Compliance Score',      trendKey: 'complianceScoreChange',      color: '#f3e5f5', accent: '#6a1b9a' },
    { key: 'sitePerformanceScore', label: 'Site Performance',      trendKey: 'sitePerformanceScoreChange', color: '#fff3e0', accent: '#e65100' },
  ];

  // PDF export
  pdfDownloading = false;

  // ── Notifications & Alerts section ───────────────────────
  notifications: NotificationItem[] = [];
  notifLoading = false;
  notifError = '';
  notifUnread = 0;
  notifStatusFilter = '';     // '' = all
  notifCategoryFilter = '';   // '' = all
  notifHistory: NotificationHistory | null = null;
  notifCategories = NOTIFICATION_CATEGORIES;
  private unreadTimer: any = null;

  // Compose / send panel
  showNotifPanel = false;
  notifForm!: FormGroup;
  notifSending = false;
  notifSendError = '';
  notifSendSuccess = '';

  navItems = [
    { key: 'dashboard',     label: 'Dashboard',       icon: 'grid'     },
    { key: 'users',         label: 'Users',           icon: 'users'    },
    { key: 'audit',         label: 'Audit Log',       icon: 'audit'    },
    { key: 'trials',        label: 'Clinical Trials', icon: 'flask'    },
    { key: 'samples',       label: 'Lab & Samples',   icon: 'beaker'   },
    { key: 'protocols',     label: 'Protocols',       icon: 'doc'      },
    { key: 'reports',       label: 'Reports',         icon: 'chart'    },
    { key: 'notifications', label: 'Notifications',   icon: 'bell'     },
    { key: 'settings',      label: 'Settings',        icon: 'settings' },
  ];

  constructor(
    private auth: AuthService,
    private reporting: ReportingService,
    private notify: NotificationService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef
  ) {}

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
    this.notifForm = this.fb.group({
      mode:          ['user'],     // 'user' | 'broadcast'
      recipientRole: [''],         // single-user: pick a role first
      userId:        [''],         // single-user: then an employee in that role
      role:          [''],         // broadcast: '' = all active users
      message:       ['', [Validators.required, Validators.minLength(3)]],
      category:      ['MILESTONE', Validators.required],
    });
    if (this.isAdmin) this.loadDashboardData();

    // Near-real-time unread badge: poll every 15s.
    this.loadUnreadCount();
    this.unreadTimer = setInterval(() => this.loadUnreadCount(), 15000);
  }

  ngOnDestroy(): void {
    if (this.unreadTimer) clearInterval(this.unreadTimer);
  }

  get isSystemAdmin(): boolean { return this.role === 'SYSTEM_ADMIN'; }
  get isAdmin(): boolean { return this.role === 'ADMIN' || this.isSystemAdmin; }

  // Roles the backend lets create/delete reports (DataManager, Admin) + SYSTEM_ADMIN.
  get canManageReports(): boolean {
    const r = (this.role || '').toUpperCase().replace(/[_\s]/g, '');
    return r === 'ADMIN' || r === 'SYSTEMADMIN' || r === 'DATAMANAGER';
  }

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
    if (key === 'reports') this.loadReports();
    if (key === 'notifications') { this.loadNotifications(); this.loadNotifHistory(); }
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

  // ── Reports & Analytics ──────────────────────────────────
  loadReports(): void {
    this.reportsError = '';

    // KPI dashboard summary (cards, trends, alerts, totals)
    this.reportSummaryLoading = !this.reportSummary;
    this.reporting.getDashboard()
      .pipe(timeout(8000))
      .subscribe({
        next: s => { this.reportSummary = s; this.reportSummaryLoading = false; this.cdr.detectChanges(); },
        error: () => { this.reportSummaryLoading = false; this.cdr.detectChanges(); }
      });

    // Generated reports list
    this.reportsLoading = this.reports.length === 0;
    const list$ = this.reportScopeFilter === 'ALL'
      ? this.reporting.getReports()
      : this.reporting.getReportsByScope(this.reportScopeFilter);

    list$.pipe(timeout(8000)).subscribe({
      next: r => { this.reports = r; this.reportsLoading = false; this.cdr.detectChanges(); },
      error: err => {
        this.reportsLoading = false;
        this.reportsError = err.status === 0
          ? 'Cannot reach the Reporting service. Make sure it is running on port 5278.'
          : (err.error?.error || err.error?.message || 'Failed to load reports.');
        this.cdr.detectChanges();
      }
    });
  }

  onScopeFilterChange(scope: 'ALL' | KpiScopeName): void {
    this.reportScopeFilter = scope;
    this.reportsLoading = true;
    const list$ = scope === 'ALL'
      ? this.reporting.getReports()
      : this.reporting.getReportsByScope(scope);
    list$.pipe(timeout(8000)).subscribe({
      next: r => { this.reports = r; this.reportsLoading = false; this.cdr.detectChanges(); },
      error: () => { this.reportsLoading = false; this.cdr.detectChanges(); }
    });
  }

  kpiValue(metrics: KpiMetrics | null | undefined, key: string): number {
    return metrics ? (metrics as any)[key] ?? 0 : 0;
  }
  trendValue(key: string): number {
    return this.reportSummary ? (this.reportSummary.trends as any)[key] ?? 0 : 0;
  }

  alertLevelLabel(level: number | string): string {
    const v = typeof level === 'string' ? level : ['Normal', 'Warning', 'Critical'][level] ?? 'Normal';
    return v;
  }
  alertLevelClass(level: number | string): string {
    return this.alertLevelLabel(level).toLowerCase();
  }
  get visibleAlerts(): KpiAlert[] {
    return (this.reportSummary?.alerts ?? []).filter(a => this.alertLevelLabel(a.level) !== 'Normal');
  }

  deleteReport(report: KpiReport): void {
    if (!confirm(`Delete this ${this.scopeLabel(report.scope)} report?`)) return;
    this.reporting.deleteReport(report.reportId).subscribe({
      next: () => {
        this.reports = this.reports.filter(r => r.reportId !== report.reportId);
        this.loadReports();
        this.cdr.detectChanges();
      },
      error: err => {
        this.reportsError = err.status === 403
          ? 'Only an Admin can delete reports.'
          : 'Could not delete report.';
        this.cdr.detectChanges();
      }
    });
  }

  downloadDashboardPdf(): void {
    this.pdfDownloading = true;
    this.reporting.downloadDashboardPdf()
      .pipe(finalize(() => { this.pdfDownloading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: blob => this.saveBlob(blob, `LifeSci360_Dashboard_${this.fileStamp()}.pdf`),
        error: () => { this.reportsError = 'Could not download dashboard PDF.'; this.cdr.detectChanges(); }
      });
  }

  downloadReportPdf(report: KpiReport): void {
    this.reporting.downloadReportPdf(report.reportId).subscribe({
      next: blob => this.saveBlob(blob, `KPI_Report_${report.scope}_${this.fileStamp()}.pdf`),
      error: () => { this.reportsError = 'Could not download report PDF.'; this.cdr.detectChanges(); }
    });
  }

  private saveBlob(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
  private fileStamp(): string {
    return new Date().toISOString().slice(0, 10).replace(/-/g, '');
  }

  scopeLabel(scope: string): string {
    switch (scope) {
      case 'Enrollment':       return 'Enrollment';
      case 'SampleProcessing': return 'Sample Processing';
      case 'Compliance':       return 'Compliance';
      case 'SitePerformance':  return 'Site Performance';
      default:                 return scope;
    }
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

  // ── Notifications & Alerts ───────────────────────────────
  get canSendNotifications(): boolean {
    const r = (this.role || '').toUpperCase().replace(/[_\s]/g, '');
    return r === 'ADMIN' || r === 'SYSTEMADMIN' || r === 'DATAMANAGER';
  }
  get canBroadcast(): boolean { return this.isAdmin; }

  // Recipients for a single notification = every active user EXCEPT yourself.
  get notifRecipients(): EnrolledUser[] {
    const me = this.auth.getUserId();
    return this.users.filter(u => u.userId !== me && u.isActive);
  }

  // Distinct roles that actually have a selectable employee (for the cascade).
  get recipientRoleOptions(): string[] {
    return Array.from(new Set(this.notifRecipients.map(u => u.role))).sort();
  }

  // Employees within the currently-selected role.
  get recipientsForRole(): EnrolledUser[] {
    const role = this.notifForm?.get('recipientRole')?.value;
    if (!role) return [];
    return this.notifRecipients.filter(u => u.role === role);
  }

  onRecipientRoleChange(): void {
    this.notifForm.patchValue({ userId: '' });
  }

  loadUnreadCount(): void {
    this.notify.getUnreadCount().subscribe({
      next: c => { this.notifUnread = c.count; this.cdr.detectChanges(); },
      error: () => { /* service may be down; keep last known count */ }
    });
  }

  loadNotifications(): void {
    this.notifError = '';
    this.notifLoading = this.notifications.length === 0;
    this.notify.getMine(this.notifStatusFilter || undefined, this.notifCategoryFilter || undefined)
      .pipe(timeout(8000))
      .subscribe({
        next: n => { this.notifications = n; this.notifLoading = false; this.cdr.detectChanges(); },
        error: err => {
          this.notifLoading = false;
          this.notifError = err.status === 0
            ? 'Cannot reach the Notification service. Make sure it is running on port 5103.'
            : (err.error?.message || 'Failed to load notifications.');
          this.cdr.detectChanges();
        }
      });
  }

  loadNotifHistory(): void {
    this.notify.getHistory().subscribe({
      next: h => { this.notifHistory = h; this.cdr.detectChanges(); },
      error: () => { this.cdr.detectChanges(); }
    });
  }

  onNotifFilterChange(): void { this.loadNotifications(); }

  markNotifRead(n: NotificationItem): void {
    if (n.status === 'READ') return;
    this.notify.markRead(n.notificationId).subscribe({
      next: () => { n.status = 'READ'; n.readAt = new Date().toISOString(); this.loadUnreadCount(); this.loadNotifHistory(); this.cdr.detectChanges(); },
      error: () => { this.cdr.detectChanges(); }
    });
  }

  markAllNotifsRead(): void {
    this.notify.markAllRead().subscribe({
      next: () => { this.notifications.forEach(n => { if (n.status === 'UNREAD') n.status = 'READ'; }); this.loadUnreadCount(); this.loadNotifHistory(); this.cdr.detectChanges(); },
      error: () => { this.cdr.detectChanges(); }
    });
  }

  deleteNotif(n: NotificationItem): void {
    this.notify.delete(n.notificationId).subscribe({
      next: () => { this.notifications = this.notifications.filter(x => x.notificationId !== n.notificationId); this.loadUnreadCount(); this.loadNotifHistory(); this.cdr.detectChanges(); },
      error: () => { this.cdr.detectChanges(); }
    });
  }

  openNotifPanel(): void {
    this.notifForm.reset({ mode: 'user', recipientRole: '', userId: '', role: '', message: '', category: 'MILESTONE' });
    this.notifSendError = '';
    this.notifSendSuccess = '';
    if (this.users.length === 0) this.auth.getUsers().subscribe({ next: u => { this.users = u; this.cdr.detectChanges(); } });
    if (this.roles.length === 0) this.auth.getRoles().subscribe({ next: r => { this.roles = r; this.cdr.detectChanges(); } });
    this.showNotifPanel = true;
  }
  closeNotifPanel(): void { this.showNotifPanel = false; }

  // Notifications are delivered in-app only.
  private selectedChannels(): string[] {
    return ['IN_APP'];
  }

  submitNotif(): void {
    if (this.notifForm.invalid) return;
    const v = this.notifForm.value;
    const channels = this.selectedChannels();

    if (v.mode === 'user' && !v.userId) { this.notifSendError = 'Please choose a recipient.'; return; }

    this.notifSending = true;
    this.notifSendError = '';
    this.notifSendSuccess = '';

    const done = (label: string) => {
      this.notifSendSuccess = label;
      this.loadUnreadCount();
      if (this.activeNav === 'notifications') { this.loadNotifications(); this.loadNotifHistory(); }
      setTimeout(() => { this.showNotifPanel = false; this.notifSendSuccess = ''; this.cdr.detectChanges(); }, 1500);
    };
    const fail = (err: any) => {
      this.notifSendError = err.status === 403 ? 'Your role is not permitted to send this.'
        : (err.error?.message || 'Could not send notification.');
      this.cdr.detectChanges();
    };

    if (v.mode === 'broadcast') {
      this.notify.broadcast({ message: v.message, category: v.category, channels, role: v.role || null })
        .pipe(finalize(() => { this.notifSending = false; this.cdr.detectChanges(); }))
        .subscribe({ next: list => done(`Broadcast sent to ${list.length} recipient(s).`), error: fail });
    } else {
      this.notify.create({ userId: v.userId, message: v.message, category: v.category, channels })
        .pipe(finalize(() => { this.notifSending = false; this.cdr.detectChanges(); }))
        .subscribe({ next: () => done('Notification sent.'), error: fail });
    }
  }

  notifCategoryColor(cat: string): string {
    switch (cat) {
      case 'MILESTONE':           return '#e3f2fd';
      case 'LAB_RESULT':          return '#e8f5e9';
      case 'COMPLIANCE_DEADLINE': return '#fff3e0';
      default:                    return '#f3e5f5';
    }
  }
  notifCategoryText(cat: string): string {
    switch (cat) {
      case 'MILESTONE':           return '#1565c0';
      case 'LAB_RESULT':          return '#2e7d32';
      case 'COMPLIANCE_DEADLINE': return '#e65100';
      default:                    return '#6a1b9a';
    }
  }

  logout(): void {
    this.auth.logout().subscribe({ error: () => this.auth.clearSession() });
  }
}
