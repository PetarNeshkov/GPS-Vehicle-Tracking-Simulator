import { TelemetryStatusComponent } from './features/tracking/components/telemetry-status/telemetry-status.component';
import { SimulationEditorComponent } from './features/tracking/components/simulation-editor/simulation-editor.component';
import { AlertHistoryComponent } from './features/tracking/components/alert-history/alert-history.component';
import { HistoryViewerComponent } from './features/tracking/components/history-viewer/history-viewer.component';
import { NgModule, provideZoneChangeDetection } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { TrackingDashboardComponent } from './features/tracking/pages/tracking-dashboard/tracking-dashboard.component';
import { TrackingMapComponent } from './features/tracking/components/tracking-map/tracking-map.component';
import { SettingsEditorComponent } from './features/tracking/components/settings-editor/settings-editor.component';

import { AppComponent } from './app.component';

@NgModule({
  declarations: [
    AppComponent,
    TrackingDashboardComponent
  ],
  imports: [
    BrowserModule,
    TrackingMapComponent,
    SimulationEditorComponent,
    SettingsEditorComponent,
    HistoryViewerComponent,
    AlertHistoryComponent,
    TelemetryStatusComponent
  ],
  providers: [provideZoneChangeDetection({ eventCoalescing: true }), provideHttpClient()],
  bootstrap: [AppComponent]
})
export class AppModule { }
