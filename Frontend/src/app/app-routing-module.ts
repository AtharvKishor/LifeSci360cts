import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ReportingComponent } from './features/reporting/reporting.component';

const routes: Routes = [
  {
    path: 'reporting',
    component: ReportingComponent
  },
  {
    path: '',
    redirectTo: 'reporting',
    pathMatch: 'full'
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
