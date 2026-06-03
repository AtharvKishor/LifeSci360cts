import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent }    from './pages/login/login.component';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { AuthGuard }          from './guards/auth.guard';

import { PatientsList }   from './pages/modules/patientmodule/patients-list/patients-list';
import { AddPatient }     from './pages/modules/patientmodule/add-patient/add-patient';
import { EnrollPatient }  from './pages/modules/patientmodule/enroll-patient/enroll-patient';
import { PatientDetail }  from './pages/modules/patientmodule/patient-detail/patient-detail';
import { ProtocolVisits } from './pages/modules/patientmodule/protocol-visits/protocol-visits';
import { Visits }         from './pages/modules/patientmodule/visits/visits';
import { VisitDetail }    from './pages/modules/patientmodule/visit-detail/visit-detail';
import { MarkVisits }     from './pages/modules/patientmodule/mark-visits/mark-visits';

const routes: Routes = [
  { path: 'login',                          component: LoginComponent },
  { path: 'dashboard',                      component: DashboardComponent,  canActivate: [AuthGuard] },
  { path: 'patients-list',                  component: PatientsList,        canActivate: [AuthGuard] },
  { path: 'add-patient',                    component: AddPatient,          canActivate: [AuthGuard] },
  { path: 'enroll-patient',                 component: EnrollPatient,       canActivate: [AuthGuard] },
  { path: 'patient-detail/:patientId',      component: PatientDetail,       canActivate: [AuthGuard] },
  { path: 'protocol-visits',                component: ProtocolVisits,      canActivate: [AuthGuard] },
  { path: 'visits',                         component: Visits,              canActivate: [AuthGuard] },
  { path: 'visit-detail/:enrollmentId',     component: VisitDetail,         canActivate: [AuthGuard] },
  { path: 'mark-visits',                    component: MarkVisits,          canActivate: [AuthGuard] },
  { path: '',                               redirectTo: 'login', pathMatch: 'full' },
  { path: '**',                             redirectTo: 'login' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
