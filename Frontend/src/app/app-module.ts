import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CommonModule, TitleCasePipe } from '@angular/common';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { LoginComponent } from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';

import { PatientsList }    from './pages/modules/patientmodule/patients-list/patients-list';
import { AddPatient }      from './pages/modules/patientmodule/add-patient/add-patient';
import { EnrollPatient }   from './pages/modules/patientmodule/enroll-patient/enroll-patient';
import { PatientDetail }   from './pages/modules/patientmodule/patient-detail/patient-detail';
import { ProtocolVisits }  from './pages/modules/patientmodule/protocol-visits/protocol-visits';
import { Visits }          from './pages/modules/patientmodule/visits/visits';
import { VisitDetail }     from './pages/modules/patientmodule/visit-detail/visit-detail';
import { MarkVisits }      from './pages/modules/patientmodule/mark-visits/mark-visits';

@NgModule({
  declarations: [
    App,
    LoginComponent,
    DashboardComponent,
    PatientsList,
    AddPatient,
    EnrollPatient,
    PatientDetail,
    ProtocolVisits,
    Visits,
    VisitDetail,
    MarkVisits
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
  bootstrap: [App]
})
export class AppModule { }
