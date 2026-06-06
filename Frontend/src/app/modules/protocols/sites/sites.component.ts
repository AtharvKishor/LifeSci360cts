import { Component, OnInit, Output, EventEmitter, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ProtocolService, SiteResponse, ProtocolSiteResponse, UpdateSiteRequest } from '../../../services/protocol.service';

@Component({
  selector: 'app-sites',
  standalone: false,
  templateUrl: './sites.component.html',
  styleUrl: './sites.component.css'
})
export class SitesComponent implements OnInit {
  @Output() navigate = new EventEmitter<string>();

  sites: SiteResponse[] = [];
  loading = false;
  error   = '';

  searchName     = '';
  searchLocation = '';

  // View modal
  viewSite: SiteResponse | null = null;
  viewProtocols: ProtocolSiteResponse[] = [];
  viewProtocolsLoading = false;

  // Edit slide panel
  showEditPanel  = false;
  editingSite: SiteResponse | null = null;
  editForm!: FormGroup;
  editLoading  = false;
  editError    = '';
  editSuccess  = '';

  // Delete confirm modal
  showDeleteModal = false;
  deletingSite: SiteResponse | null = null;
  deleteLoading   = false;
  deleteError     = '';

  constructor(
    private svc: ProtocolService,
    private fb:  FormBuilder,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.editForm = this.fb.group({
      name:     ['', [Validators.required, Validators.maxLength(150)]],
      location: ['', Validators.maxLength(255)]
    });
    this.loadSites();
  }

  loadSites(): void {
    this.loading = true;
    this.error   = '';
    this.svc.getSites().subscribe({
      next: data => { this.sites = data; this.loading = false; this.cdr.detectChanges(); },
      error: ()   => { this.error = 'Could not load sites. Is the service running?'; this.loading = false; this.cdr.detectChanges(); }
    });
  }

  get filteredSites(): SiteResponse[] {
    let list = this.sites;
    if (this.searchName.trim())
      list = list.filter(s => s.name.toLowerCase().includes(this.searchName.toLowerCase()));
    if (this.searchLocation.trim())
      list = list.filter(s => s.location?.toLowerCase().includes(this.searchLocation.toLowerCase()));
    return list;
  }

  clearFilters(): void { this.searchName = ''; this.searchLocation = ''; }

  get stats() {
    return { total: this.sites.length, active: this.sites.filter(s => s.protocolCount > 0).length };
  }

  goCreate(): void { this.navigate.emit('create-site'); }

  // View
  openView(s: SiteResponse): void {
    this.viewSite = s;
    this.viewProtocols = [];
    this.viewProtocolsLoading = true;
    this.svc.getSiteProtocols(s.siteId).subscribe({
      next: data => { this.viewProtocols = data; this.viewProtocolsLoading = false; this.cdr.detectChanges(); },
      error: ()   => { this.viewProtocolsLoading = false; this.cdr.detectChanges(); }
    });
  }
  closeView(): void { this.viewSite = null; this.viewProtocols = []; }

  // Edit
  openEdit(s: SiteResponse): void {
    this.editingSite = s;
    this.editError   = '';
    this.editSuccess = '';
    this.editForm.setValue({ name: s.name, location: s.location ?? '' });
    this.showEditPanel = true;
  }
  closeEdit(): void { this.showEditPanel = false; this.editingSite = null; }

  submitEdit(): void {
    if (this.editForm.invalid || !this.editingSite) return;
    this.editLoading = true;
    this.editError   = '';
    this.editSuccess = '';
    const dto: UpdateSiteRequest = {
      name: this.editForm.value.name.trim(),
      location: this.editForm.value.location?.trim() || undefined
    };
    this.svc.updateSite(this.editingSite.siteId, dto)
      .pipe(finalize(() => { this.editLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => {
          this.editSuccess = 'Site updated successfully.';
          this.loadSites();
          setTimeout(() => { this.closeEdit(); }, 1800);
        },
        error: err => { this.editError = err.error?.message || 'Update failed.'; }
      });
  }

  // Delete
  openDelete(s: SiteResponse): void {
    this.deletingSite  = s;
    this.deleteError   = '';
    this.deleteLoading = false;
    this.showDeleteModal = true;
  }
  closeDelete(): void { this.showDeleteModal = false; this.deletingSite = null; }

  submitDelete(): void {
    if (!this.deletingSite) return;
    this.deleteLoading = true;
    this.svc.deleteSite(this.deletingSite.siteId)
      .pipe(finalize(() => { this.deleteLoading = false; this.cdr.detectChanges(); }))
      .subscribe({
        next: () => { this.loadSites(); this.closeDelete(); },
        error: err => { this.deleteError = err.error?.message || 'Delete failed.'; }
      });
  }

  initials(name: string): string { return name ? name.substring(0, 2).toUpperCase() : 'SI'; }

  assignBg(s: string): string {
    return ({ ACTIVE: '#dcfce7', INACTIVE: '#fef9c3', CLOSED: '#fee2e2' })[s] ?? '#f5f5f5';
  }
  assignFg(s: string): string {
    return ({ ACTIVE: '#15803d', INACTIVE: '#a16207', CLOSED: '#991b1b' })[s] ?? '#555';
  }
}
