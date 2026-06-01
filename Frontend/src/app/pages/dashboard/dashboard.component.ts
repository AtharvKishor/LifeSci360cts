import { Component, OnInit, ChangeDetectorRef, HostListener } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize, timeout } from 'rxjs';
import { AuthService, EnrolledUser, RoleOption, ActiveSession, AuditLogEntry, UpdateUserRequest } from '../../services/auth.service';
import { SampleService, SampleListDto, SampleCreateDto, SampleUpdateDto, LabResultListDto, LabResultCreateDto, LabResultUpdateDto } from '../../services/sample.service';

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

  // ── Users section ─────────────────────────────────────────
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

  // ── Dashboard real data ───────────────────────────────────
  statsLoading    = true;
  sessionsLoading = true;
  sessions: ActiveSession[] = [];

  stats = [
    { label: 'Active Users',       value: '—', delta: '', up: true,  icon: 'users',    color: '#e8f5e9', accent: '#2e7d32' },
    { label: 'Total Enrolled',     value: '—', delta: '', up: true,  icon: 'enrolled', color: '#e3f2fd', accent: '#1565c0' },
    { label: 'Active Sessions',    value: '—', delta: '', up: true,  icon: 'sessions', color: '#fff3e0', accent: '#e65100' },
    { label: 'Audit Events Today', value: '—', delta: '', up: true,  icon: 'shield',   color: '#f3e5f5', accent: '#6a1b9a' },
  ];

  // ── Audit log section ─────────────────────────────────────
  auditLogs: AuditLogEntry[] = [];
  auditLoading = false;
  auditFilter  = '';

  // ── Samples section ───────────────────────────────────────
  samples: SampleListDto[] = [];
  samplesLoading = false;
  showLabResultsView = false;
  selectedSample: SampleListDto | null = null;

  sampleStatuses = ['COLLECTED', 'IN_PROCESS', 'TESTED', 'ANALYZED', 'REJECTED'];

  // Create Sample panel
  showCreateSamplePanel = false;
  createSampleForm!: FormGroup;
  createSampleLoading = false;
  createSampleError = '';
  createSampleSuccess = '';

  // Edit Sample panel
  showEditSamplePanel = false;
  editSampleForm!: FormGroup;
  editingSample: SampleListDto | null = null;
  editSampleLoading = false;
  editSampleError = '';
  editSampleSuccess = '';

  // Update Status panel
  showUpdateStatusPanel = false;
  updatingStatusSample: SampleListDto | null = null;
  updateStatusForm!: FormGroup;
  updateStatusLoading = false;
  updateStatusError = '';
  updateStatusSuccess = '';

  // ── Lab Results ───────────────────────────────────────────
  labResults: LabResultListDto[] = [];
  labResultsLoading = false;
  labResultsError = '';

  // Create Lab Result panel
  showCreateLabResultPanel = false;
  createLabResultForm!: FormGroup;
  createLabResultLoading = false;
  createLabResultError = '';
  createLabResultSuccess = '';

  // ── Sample notes (scientist only, saved to DB) ────────────
  noteEditId = '';   // sampleId currently being edited
  noteDraft  = '';   // text in the textarea

  // Edit Lab Result panel
  showEditLabResultPanel = false;
  editLabResultForm!: FormGroup;
  editingLabResult: LabResultListDto | null = null;
  editLabResultLoading = false;
  editLabResultError = '';
  editLabResultSuccess = '';

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

  constructor(
    private auth: AuthService,
    private sampleSvc: SampleService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef
  ) {}

  name  = '';
  showProfileMenu = false;

  @HostListener('document:click')
  onDocumentClick(): void { this.showProfileMenu = false; }

  toggleProfileMenu(e: Event): void {
    e.stopPropagation();
    this.showProfileMenu = !this.showProfileMenu;
  }

  ngOnInit(): void {
    this.email = this.auth.getEmail() ?? '';
    this.role  = this.auth.getRole()  ?? '';
    this.name  = this.auth.getName()  ?? this.email.split('@')[0];

    // User management forms
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

    // Sample forms
    this.createSampleForm = this.fb.group({
      enrollmentId:      ['', Validators.required],
      collectedByUserId: [this.currentUserId],
      sampleType:        ['', Validators.required],
      collectedDate:     ['', Validators.required]
    });
    this.editSampleForm = this.fb.group({
      sampleType: ['']
    });
    this.updateStatusForm = this.fb.group({
      status: ['', Validators.required]
    });

    // Lab Result forms
    this.createLabResultForm = this.fb.group({
      sampleId:         [''],
      recordedByUserId: [this.currentUserId],
      testType:         ['', Validators.required],
      resultValue:      ['', Validators.required],
      resultDate:       ['', Validators.required]
    });
    this.editLabResultForm = this.fb.group({
      testType:    [''],
      resultValue: [''],
      resultDate:  ['']
    });

    if (this.isAdmin) {
      this.loadDashboardData();
    } else {
      // Non-admin: stop admin spinners; pre-load their own samples for dashboard stats
      this.statsLoading    = false;
      this.sessionsLoading = false;
      this.loadSamples();
    }
  }

  // ── Role helpers ──────────────────────────────────────────
  get isSystemAdmin(): boolean { return this.role === 'SYSTEM_ADMIN'; }
  get isAdmin(): boolean { return this.role === 'ADMIN' || this.isSystemAdmin; }
  get currentUserId(): string { return localStorage.getItem('userId') ?? ''; }

  // Sample role gates
  get canCreateSample(): boolean  { return this.role === 'LAB_TECHNICIAN' || this.isAdmin; }
  get canEditSample(): boolean    { return this.role === 'LAB_TECHNICIAN' || this.isAdmin; }
  get canUpdateStatus(): boolean  { return this.role === 'LAB_TECHNICIAN' || this.role === 'RESEARCH_SCIENTIST' || this.isAdmin; }
  get canManageLabResults(): boolean { return this.role === 'LAB_TECHNICIAN' || this.isAdmin; }

  canEdit(user: EnrolledUser): boolean {
    return user.role?.toUpperCase() !== 'SYSTEM_ADMIN';
  }

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

  // ── Non-admin dashboard stats (derived from samples array) ──
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
  get myRecentSamples() {
    return [...this.samples].slice(0, 4);
  }

  // Returns true if the current role can access the given nav section
  canAccess(key: string): boolean {
    if (this.isAdmin) return true;
    // LAB_TECHNICIAN and RESEARCH_SCIENTIST can only access dashboard and samples
    return key === 'samples' || key === 'dashboard';
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

  // ── Dashboard data ────────────────────────────────────────
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
    this.showLabResultsView = false;
    if (key === 'users'   && this.isAdmin) this.loadUsers();
    if (key === 'audit'   && this.isAdmin) this.loadAuditLogs();
    if (key === 'samples') this.loadSamples();
  }

  // ── Users ─────────────────────────────────────────────────
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

  // ── Samples ───────────────────────────────────────────────
  samplesError = '';

  loadSamples(): void {
    this.samplesLoading = this.samples.length === 0;
    this.samplesError = '';
    this.sampleSvc.getAllSamples()
      .pipe(timeout(8000), finalize(() => { this.samplesLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: data => {
          // LAB_TECHNICIAN sees only samples they collected; others see all
          this.samples = (this.role === 'LAB_TECHNICIAN')
            ? data.filter(s => s.collectedByUserId === this.currentUserId)
            : data;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.samplesError = err?.name === 'TimeoutError'
            ? 'SampleService is not running. Start it on port 5025.'
            : (err?.error?.message || err?.message || 'Failed to load samples. Check SampleService is running on port 5025.');
          this.cdr.detectChanges();
        }
      });
  }

  openCreateSamplePanel(): void {
    this.createSampleForm.reset({ collectedByUserId: this.currentUserId });
    this.createSampleError   = '';
    this.createSampleSuccess = '';
    this.showCreateSamplePanel = true;//makes the form panel appear
  }
  closeCreateSamplePanel(): void { this.showCreateSamplePanel = false; }

  submitCreateSample(): void {
    if (this.createSampleForm.invalid) return;
    this.createSampleLoading = true;
    this.createSampleError   = '';
    const v = this.createSampleForm.value;
    const dto: SampleCreateDto = {
      enrollmentId:      v.enrollmentId,
      collectedByUserId: v.collectedByUserId || this.currentUserId,
      sampleType:        v.sampleType,
      collectedDate:     new Date(v.collectedDate).toISOString()
    }; // Build the data object to send to backend
    this.sampleSvc.createSample(dto)
      .pipe(finalize(() => { this.createSampleLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: sample => {
          this.samples.unshift(sample);
          this.createSampleSuccess = `Sample "${sample.sampleType}" created successfully.`;
          this.cdr.detectChanges();
          setTimeout(() => { this.showCreateSamplePanel = false; this.createSampleSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.createSampleError = err.error?.message || 'Failed to create sample. Check Enrollment ID is valid.';
          this.cdr.detectChanges();
        }//Send sample data to backend → on success add to table and close panel → on failure show error message
      });
  }

  openEditSamplePanel(sample: SampleListDto): void {
    this.editingSample   = sample;
    this.editSampleError = '';
    this.editSampleSuccess = '';
    this.editSampleForm.patchValue({ sampleType: sample.sampleType });
    this.showEditSamplePanel = true;
  }
  closeEditSamplePanel(): void { this.showEditSamplePanel = false; this.editingSample = null; }

  submitEditSample(): void {
    if (!this.editingSample) return;
    this.editSampleLoading = true;
    this.editSampleError   = '';
    const dto: SampleUpdateDto = this.editSampleForm.value;
    this.sampleSvc.updateSample(this.editingSample.sampleId, dto)
      .pipe(finalize(() => { this.editSampleLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: updated => {
          const idx = this.samples.findIndex(s => s.sampleId === updated.sampleId);
          if (idx !== -1) this.samples[idx] = { ...updated };
          this.editSampleSuccess = 'Sample updated successfully.';
          this.cdr.detectChanges();
          setTimeout(() => { this.showEditSamplePanel = false; this.editingSample = null; this.editSampleSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.editSampleError = err.error?.message || 'Failed to update sample.';
          this.cdr.detectChanges();
        }
      });
  }

  openUpdateStatusPanel(sample: SampleListDto): void {
    this.updatingStatusSample = sample;
    this.updateStatusError   = '';
    this.updateStatusSuccess = '';
    this.updateStatusForm.setValue({ status: sample.status });
    this.showUpdateStatusPanel = true;
  }
  closeUpdateStatusPanel(): void { this.showUpdateStatusPanel = false; this.updatingStatusSample = null; }

  submitUpdateStatus(): void {
    if (!this.updatingStatusSample || this.updateStatusForm.invalid) return;
    this.updateStatusLoading = true;
    this.updateStatusError   = '';
    const status = this.updateStatusForm.value.status;
    this.sampleSvc.updateSampleStatus(this.updatingStatusSample.sampleId, status)
      .pipe(finalize(() => { this.updateStatusLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => {
          const idx = this.samples.findIndex(s => s.sampleId === this.updatingStatusSample?.sampleId);
          if (idx !== -1) this.samples[idx] = { ...this.samples[idx], status };
          this.updateStatusSuccess = `Status updated to "${status}".`;
          this.cdr.detectChanges();
          setTimeout(() => { this.showUpdateStatusPanel = false; this.updatingStatusSample = null; this.updateStatusSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.updateStatusError = err.error?.message || 'Failed to update status.';
          this.cdr.detectChanges();
        }
      });
  }

  deleteSampleById(id: string, sampleType: string): void {
    if (!confirm(`Delete sample "${sampleType}"? This will also remove all linked lab results.`)) return;
    this.sampleSvc.deleteSample(id)
      .pipe(timeout(5000))
      .subscribe({
        next: () => { this.samples = this.samples.filter(s => s.sampleId !== id); this.cdr.detectChanges(); },
        error: () => { this.cdr.detectChanges(); }
      });
  }

  // ── Lab Results ───────────────────────────────────────────
  viewLabResults(sample: SampleListDto): void {
    this.selectedSample     = sample;
    this.showLabResultsView = true;
    this.labResults         = [];
    this.loadLabResults(sample.sampleId);
  }

  backToSamples(): void {
    this.showLabResultsView = false;
    this.selectedSample     = null;
    this.labResults         = [];
    this.labResultsError    = '';
    this.noteEditId         = '';
  }

  loadLabResults(sampleId: string): void {
    this.labResultsLoading = true;
    this.labResultsError = '';
    this.sampleSvc.getLabResultsBySample(sampleId)
      .pipe(timeout(8000), finalize(() => { this.labResultsLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: data => {
          this.labResults = (this.role === 'LAB_TECHNICIAN')
            ? data.filter(r => r.recordedByUserId === this.currentUserId)
            : data;
          this.cdr.detectChanges();
        },
        error: (err) => {
          this.labResultsError = err?.name === 'TimeoutError'
            ? 'SampleService is taking too long. Please restart it, then try again.'
            : (err?.error?.message || 'Failed to load lab results. Check SampleService is running.');
          this.cdr.detectChanges();
        }
      });
  }

  openCreateLabResultPanel(): void {
    if (!this.selectedSample) return;
    const today = new Date().toISOString().split('T')[0];
    this.createLabResultForm.reset({
      sampleId:         this.selectedSample.sampleId,
      recordedByUserId: this.currentUserId,
      resultDate:       today
    });
    this.createLabResultError   = '';
    this.createLabResultSuccess = '';
    this.showCreateLabResultPanel = true;
  }
  closeCreateLabResultPanel(): void { this.showCreateLabResultPanel = false; }

  submitCreateLabResult(): void {
    if (this.createLabResultForm.invalid) return;
    this.createLabResultLoading = true;
    this.createLabResultError   = '';
    const v = this.createLabResultForm.value;
    const dto: LabResultCreateDto = {
      sampleId:         v.sampleId,
      recordedByUserId: v.recordedByUserId || this.currentUserId,
      testType:         v.testType,
      resultValue:      v.resultValue,
      resultDate:       new Date(v.resultDate).toISOString()
    };
    this.sampleSvc.createLabResult(dto)
      .pipe(finalize(() => { this.createLabResultLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: result => {
          this.labResults.unshift(result);
          this.createLabResultSuccess = `Lab result "${result.testType}" recorded successfully.`;
          this.cdr.detectChanges();
          setTimeout(() => { this.showCreateLabResultPanel = false; this.createLabResultSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.createLabResultError = err.error?.message || 'Failed to record lab result.';
          this.cdr.detectChanges();
        }
      });
  }

  openEditLabResultPanel(result: LabResultListDto): void {
    this.editingLabResult     = result;
    this.editLabResultError   = '';
    this.editLabResultSuccess = '';
    this.editLabResultForm.setValue({
      testType:    result.testType,
      resultValue: result.resultValue,
      resultDate:  result.resultDate ? result.resultDate.split('T')[0] : ''
    });
    this.showEditLabResultPanel = true;
  }
  closeEditLabResultPanel(): void { this.showEditLabResultPanel = false; this.editingLabResult = null; }

  submitEditLabResult(): void {
    if (!this.editingLabResult) return;
    this.editLabResultLoading = true;
    this.editLabResultError   = '';
    const v = this.editLabResultForm.value;
    const dto: LabResultUpdateDto = {
      testType:    v.testType    || undefined,
      resultValue: v.resultValue || undefined,
      resultDate:  v.resultDate  ? new Date(v.resultDate).toISOString() : undefined
    };
    this.sampleSvc.updateLabResult(this.editingLabResult.resultId, dto)
      .pipe(finalize(() => { this.editLabResultLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: updated => {
          const idx = this.labResults.findIndex(r => r.resultId === updated.resultId);
          if (idx !== -1) this.labResults[idx] = { ...updated };
          this.editLabResultSuccess = 'Lab result updated successfully.';
          this.cdr.detectChanges();
          setTimeout(() => { this.showEditLabResultPanel = false; this.editingLabResult = null; this.editLabResultSuccess = ''; this.cdr.detectChanges(); }, 1800);
        },
        error: err => {
          this.editLabResultError = err.error?.message || 'Failed to update lab result.';
          this.cdr.detectChanges();
        }
      });
  }

  // ── Sample notes (saved to DB via updateSample) ───────────
  openNoteEdit(sampleId: string): void {
    const sample = this.samples.find(s => s.sampleId === sampleId);
    this.noteEditId = sampleId;
    this.noteDraft  = sample?.notes ?? '';
    this.cdr.detectChanges();
  }

  saveNote(sampleId: string): void {
    const text = this.noteDraft.trim();
    this.sampleSvc.updateSample(sampleId, { notes: text })
      .subscribe({
        next: updated => {
          const idx = this.samples.findIndex(s => s.sampleId === sampleId);
          if (idx !== -1) this.samples[idx] = { ...this.samples[idx], notes: text };
          if (this.selectedSample?.sampleId === sampleId)
            this.selectedSample = { ...this.selectedSample, notes: text };
          this.noteEditId = '';
          this.cdr.detectChanges();
        },
        error: () => { this.noteEditId = ''; this.cdr.detectChanges(); }
      });
  }

  clearNote(sampleId: string): void {
    this.sampleSvc.updateSample(sampleId, { notes: '' })
      .subscribe({
        next: () => {
          const idx = this.samples.findIndex(s => s.sampleId === sampleId);
          if (idx !== -1) this.samples[idx] = { ...this.samples[idx], notes: '' };
          if (this.selectedSample?.sampleId === sampleId)
            this.selectedSample = { ...this.selectedSample, notes: '' };
          this.noteEditId = '';
          this.cdr.detectChanges();
        },
        error: () => { this.cdr.detectChanges(); }
      });
  }

  deleteLabResultById(id: string, testType: string): void {
    if (!confirm(`Delete lab result "${testType}"?`)) return;
    this.sampleSvc.deleteLabResult(id)
      .subscribe({
        next: () => { this.labResults = this.labResults.filter(r => r.resultId !== id); this.cdr.detectChanges(); },
        error: () => { this.cdr.detectChanges(); }
      });
  }

  // ── UI helpers ────────────────────────────────────────────
  avatarFor(name: string): string { return name ? name.substring(0, 2).toUpperCase() : '??'; }

  statusColor(status: string): string {
    const map: Record<string, string> = {
      'COLLECTED':  '#dbeafe', 'IN_PROCESS': '#fef3c7',
      'TESTED':     '#dcfce7', 'ANALYZED':   '#e0f2fe',
      'REJECTED':   '#fee2e2'
    };
    return map[status] ?? '#f5f5f5';
  }

  statusTextColor(status: string): string {
    const map: Record<string, string> = {
      'COLLECTED':  '#1d4ed8', 'IN_PROCESS': '#92400e',
      'TESTED':     '#15803d', 'ANALYZED':   '#0369a1',
      'REJECTED':   '#b91c1c'
    };
    return map[status] ?? '#555';
  }

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

  logout(): void {
    this.auth.logout().subscribe({ error: () => this.auth.clearSession() });
  }
}
