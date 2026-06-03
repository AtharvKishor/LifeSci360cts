import { Component, OnInit, OnDestroy, ChangeDetectorRef, HostListener, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { timeout, Subscription } from 'rxjs';
import { AuthService, EnrolledUser, ActiveSession, AuditLogEntry } from '../../services/auth.service';
import { SampleService, SampleListDto } from '../../services/sample.service';
import { NotificationService } from '../../services/notification.service';
import { UsersComponent } from '../../modules/users/users.component';
import { SamplesComponent } from '../../modules/samples/samples/samples.component';
import { TrialsNavService } from '../../services/trials-nav.service';

@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit, OnDestroy {
  @ViewChild('usersComp')   usersComp!: UsersComponent;
  @ViewChild('samplesComp') samplesComp!: SamplesComponent;

  private navSub?: Subscription;
  email = '';
  role  = '';
  name  = '';
  activeNav = 'dashboard';
  sidebarCollapsed = false;
  showProfileMenu = false;

  notifUnread = 0;
  private unreadTimer: any = null;

  statsLoading    = true;
  sessionsLoading = true;
  sessions: ActiveSession[] = [];

  enrollForm!: FormGroup;
  editForm!: FormGroup;
  filteredAuditLogs: AuditLogEntry[] = [];
  auditLoading = false;

  stats = [
    { label: 'Active Users',       value: '—', delta: '', up: true,  icon: 'users',    color: '#e8f5e9', accent: '#2e7d32' },
    { label: 'Total Enrolled',     value: '—', delta: '', up: true,  icon: 'enrolled', color: '#e3f2fd', accent: '#1565c0' },
    { label: 'Active Sessions',    value: '—', delta: '', up: true,  icon: 'sessions', color: '#fff3e0', accent: '#e65100' },
    { label: 'Audit Events Today', value: '—', delta: '', up: true,  icon: 'shield',   color: '#f3e5f5', accent: '#6a1b9a' },
  ];

  samples: SampleListDto[] = [];
  samplesLoading = false;
  samplesError = '';

  users: EnrolledUser[] = [];

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
    private sampleSvc: SampleService,
    private notify: NotificationService,
    private cdr: ChangeDetectorRef,
    private fb: FormBuilder,
    private trialsNav: TrialsNavService
  ) {}

  @HostListener('document:click')
  onDocumentClick(): void { this.showProfileMenu = false; }

  toggleProfileMenu(e: Event): void {
    e.stopPropagation();
    this.showProfileMenu = !this.showProfileMenu;
  }

  // Protocol module nav keys (not shown in sidebar, navigated programmatically)
  readonly protocolSubNavs = ['create-protocol', 'create-site'];

  onProtocolNavigate(key: string): void { this.activeNav = key; }

  constructor(private auth: AuthService, private fb: FormBuilder, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.email = this.auth.getEmail() ?? '';
    this.role  = this.auth.getRole()  ?? '';
    this.name  = this.auth.getName()  ?? this.email.split('@')[0];

    this.loadUnreadCount();
    this.unreadTimer = setInterval(() => this.loadUnreadCount(), 15000);

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

    if (this.isAdmin) {
      this.loadDashboardData();
    } else {
      this.statsLoading    = false;
      this.sessionsLoading = false;
      this.loadSamples();
    }

    if (this.isClinicalTrialManager && !this.isAdmin) this.activeNav = 'trials';

    const pending = this.trialsNav.consumePending();
    if (pending) {
      this.activeNav = 'trials';
      this.trialsView = pending;
    }

    this.navSub = this.trialsNav.nav$.subscribe(view => {
      if (!view) return;
      this.activeNav = 'trials';
      this.trialsView = view;
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy() {
    this.navSub?.unsubscribe();
    if (this.unreadTimer) clearInterval(this.unreadTimer);
  }

  get isSystemAdmin(): boolean { return this.role === 'SYSTEM_ADMIN'; }
  get isAdmin(): boolean { return this.role === 'ADMIN' || this.isSystemAdmin; }
  get currentUserId(): string { return localStorage.getItem('userId') ?? ''; }
  get canCreateSample(): boolean { return this.role === 'LAB_TECHNICIAN' || this.isAdmin; }
  get isClinicalTrialManager(): boolean {
    const r = (this.role || '').toUpperCase();
    return r === 'CLINICAL_TRIAL' || r === 'CLINICAL_TRIAL_MANAGER' || r.startsWith('CLINICAL');
  }
  get canAccessTrials(): boolean { return this.isAdmin || this.isClinicalTrialManager; }

  trialsView = 'patients-list';
  setTrialsView(view: string) { this.trialsView = view; }

  get initials(): string {
    const n = this.name || this.email;
    return n ? n.substring(0, 2).toUpperCase() : 'AD';
  }

  get greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  get today(): string {
    return new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
  }

  get myTotalSamples(): number { return this.samples.length; }
  get myActiveSamples(): number {
    return this.samples.filter(s => s.status === 'COLLECTED' || s.status === 'IN_PROCESS').length;
  }
  get myCompletedSamples(): number {
    return this.samples.filter(s => s.status === 'TESTED' || s.status === 'ANALYZED').length;
  }
  get myRejectedSamples(): number {
    return this.samples.filter(s => s.status === 'REJECTED').length;
  }
  get myRecentSamples(): SampleListDto[] { return [...this.samples].slice(0, 4); }

  get recentEmployees(): EnrolledUser[] {
    return [...this.users]
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5);
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

  loadSamples(): void {
    this.samplesLoading = this.samples.length === 0;
    this.samplesError = '';
    this.sampleSvc.getAllSamples()
      .pipe(timeout(8000))
      .subscribe({
        next: data => {
          this.samples = this.role === 'LAB_TECHNICIAN'
            ? data.filter(s => s.collectedByUserId === this.currentUserId)
            : data;
          this.samplesLoading = false;
          this.cdr.detectChanges();
        },
        error: err => {
          this.samplesLoading = false;
          this.samplesError = err?.name === 'TimeoutError'
            ? 'SampleService is not running.'
            : (err?.error?.message || 'Failed to load samples.');
          this.cdr.detectChanges();
        }
      });
  }

  loadUnreadCount(): void {
    this.notify.getUnreadCount().subscribe({
      next: c => { this.notifUnread = c.count; this.cdr.detectChanges(); },
      error: () => {}
    });
  }

  onNavChange(key: string): void {
    this.activeNav = key;
    this.cdr.detectChanges();
  }

  enrollEmployee(): void {
    this.activeNav = 'users';
    this.cdr.detectChanges();
    setTimeout(() => this.usersComp?.openEnrollPanel(), 150);
  }

  navigateToSamplesAndCreate(): void {
    this.activeNav = 'samples';
    this.cdr.detectChanges();
    setTimeout(() => this.samplesComp?.openCreatePanel(), 150);
  }

  avatarFor(name: string): string { return name ? name.substring(0, 2).toUpperCase() : '??'; }

  statusColor(status: string): string {
    const map: Record<string, string> = {
      'COLLECTED': '#dbeafe', 'IN_PROCESS': '#fef3c7',
      'TESTED':    '#dcfce7', 'ANALYZED':  '#e0f2fe', 'REJECTED': '#fee2e2'
    };
    return map[status] ?? '#f5f5f5';
  }

  statusTextColor(status: string): string {
    const map: Record<string, string> = {
      'COLLECTED': '#1d4ed8', 'IN_PROCESS': '#92400e',
      'TESTED':    '#15803d', 'ANALYZED':  '#0369a1', 'REJECTED': '#b91c1c'
    };
    return map[status] ?? '#555';
  }

  actionColor(action: string): string {
    const map: Record<string, string> = {
      'CREATE': '#dcfce7', 'UPDATE': '#dbeafe', 'DELETE': '#fee2e2',
      'LOGIN':  '#e0f2fe', 'LOGOUT': '#f3e5f5', 'VIEW':   '#fef3c7'
    };
    return map[(action || '').toUpperCase()] ?? '#f5f5f5';
  }

  actionTextColor(action: string): string {
    const map: Record<string, string> = {
      'CREATE': '#15803d', 'UPDATE': '#1d4ed8', 'DELETE': '#b91c1c',
      'LOGIN':  '#0369a1', 'LOGOUT': '#6a1b9a', 'VIEW':   '#92400e'
    };
    return map[(action || '').toUpperCase()] ?? '#555';
  }

  logout(): void {
    this.auth.logout().subscribe({ error: () => this.auth.clearSession() });
  }
}
