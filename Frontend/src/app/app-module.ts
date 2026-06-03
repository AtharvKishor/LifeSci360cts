import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CommonModule, TitleCasePipe } from '@angular/common';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AppRoutingModule }      from './app-routing-module';
import { App }                   from './app';
import { AuthInterceptor }       from './interceptors/auth.interceptor';
import { LoginComponent }        from './pages/login/login.component';
import { DashboardComponent }    from './pages/dashboard/dashboard.component';
import { ProtocolsComponent }    from './pages/module/protocol/protocols/protocols.component';
import { CreateProtocolComponent } from './pages/module/protocol/create-protocol/create-protocol.component';
import { SitesComponent }        from './pages/module/protocol/sites/sites.component';
import { CreateSiteComponent }   from './pages/module/protocol/create-site/create-site.component';

@NgModule({
  declarations: [
    App,
    LoginComponent,
    DashboardComponent,
    ProtocolsComponent,
    CreateProtocolComponent,
    SitesComponent,
    CreateSiteComponent
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
