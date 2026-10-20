import { HistoryViewerComponent } from './features/tracking/history-viewer.component';
import { NgModule, provideZoneChangeDetection } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { TrackingDashboardComponent } from './features/tracking/tracking-dashboard.component';
import { SettingsEditorComponent } from './features/tracking/settings-editor.component';

import { AppComponent } from './app.component';

@NgModule({
  declarations: [
    AppComponent,
    TrackingDashboardComponent
  ],
  imports: [
    BrowserModule,
    SettingsEditorComponent,
    HistoryViewerComponent
  ],
  providers: [provideZoneChangeDetection({ eventCoalescing: true }), provideHttpClient()],
  bootstrap: [AppComponent]
})
export class AppModule { }
