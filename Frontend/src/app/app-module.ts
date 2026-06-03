import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CommonModule, TitleCasePipe } from '@angular/common';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AppRoutingModule } from './app-routing-module';
import { AppComponent } from './app';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { LoginComponent } from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';

import { UsersComponent } from './modules/users/users.component';
import { AuditComponent } from './modules/audit/audit.component';
import { SamplesComponent } from './modules/samples/samples/samples.component';
import { LabResultComponent } from './modules/samples/labresult/labresult.component';
import { ReportsComponent } from './modules/reports/reports.component';
import { NotificationsComponent } from './modules/notifications/notifications.component';
import { TrialsComponent } from './modules/trials/trials.component';
import { ProtocolsComponent } from './modules/protocols/protocols.component';
import { SettingsComponent } from './modules/settings/settings.component';

@NgModule({
  declarations: [
    AppComponent,
    LoginComponent,
    DashboardComponent,
    UsersComponent,
    AuditComponent,
    SamplesComponent,
    LabResultComponent,
    ReportsComponent,
    NotificationsComponent,
    TrialsComponent,
    ProtocolsComponent,
    SettingsComponent,
  ],
  imports: [
    BrowserModule,
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    AppRoutingModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    TitleCasePipe
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
