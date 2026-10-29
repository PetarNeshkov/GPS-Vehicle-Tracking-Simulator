import { TelemetryStatusComponent } from '../../components/telemetry-status/telemetry-status.component';
import { SimulationEditorComponent } from '../../components/simulation-editor/simulation-editor.component';
import { AlertHistoryComponent } from '../../components/alert-history/alert-history.component';
import { HistoryViewerComponent } from '../../components/history-viewer/history-viewer.component';
import { CommonModule } from '@angular/common';
import { RouteListComponent } from '../../components/route-list/route-list.component';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZoneChangeDetection } from '@angular/core';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { TRACKING } from '../../../../core/constants/tracking.constants';
import { TrackingDashboardComponent } from './tracking-dashboard.component';
import { SettingsEditorComponent } from '../../components/settings-editor/settings-editor.component';
import { TrackingConnectionService } from '../../../../core/services/tracking-connection.service';
import { TrackingConnectionStub } from '../../../../core/services/tracking-connection.stub';
import { TrackingMapComponent } from '../../components/tracking-map/tracking-map.component';
import { MAP_TILES } from '../../../../core/constants/map.constants';
import { TEST_MAP_TILES } from '../../components/tracking-map/tracking-map.test-config';

describe('TrackingDashboardComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CommonModule, RouteListComponent, SettingsEditorComponent, HistoryViewerComponent, AlertHistoryComponent,
        TelemetryStatusComponent, SimulationEditorComponent, TrackingMapComponent],
      declarations: [TrackingDashboardComponent],
      providers: [provideZoneChangeDetection(), provideHttpClient(), provideHttpClientTesting(),
        { provide: MAP_TILES, useValue: TEST_MAP_TILES },
        { provide: TrackingConnectionService, useClass: TrackingConnectionStub }]
    });
  });

  it('shows waiting vehicles and then renders supplied telemetry and heading', fakeAsync(() => {
    const fixture = TestBed.createComponent(TrackingDashboardComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    tick(0);
    const waiting = {
      id: 'vehicle-1', name: 'Vehicle 1', latestSample: null,
      lastReceivedAt: null, sampleCount: 0, revision: 0
    };
    http.expectOne(TRACKING.vehiclesPath).flush({ sessionId: 'session', vehicles: [waiting] });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Waiting for GPS data');
    expect(fixture.nativeElement.querySelectorAll('.tracking-map-marker').length).toBe(0);

    const connection = TestBed.inject(TrackingConnectionService) as unknown as TrackingConnectionStub;
    connection.updates$.next({ sessionId: 'session', vehicle: {
      ...waiting, sampleCount: 1, revision: 1, lastReceivedAt: new Date().toISOString(),
      latestSample: { latitude: 42.697, longitude: 23.318, speedKmh: 85.5, heading: 360,
        timestamp: '2026-10-18T09:00:00Z' }
    }});
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('85.5');
    expect(fixture.nativeElement.textContent).not.toContain('Waiting for GPS data');
    expect(fixture.nativeElement.querySelector('.tracking-map-heading').style.transform).toBe('rotate(0deg)');
    fixture.destroy();
    http.verify();
  }));

  it('renders geofences, confirmed membership and type-specific alerts', fakeAsync(() => {
    const fixture = TestBed.createComponent(TrackingDashboardComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    tick(0);
    const base = { vehicleId: 'vehicle-1', occurredAt: '2026-10-19T12:00:40Z', createdAt: '2026-10-19T12:00:41Z' };
    http.expectOne(TRACKING.vehiclesPath).flush({ sessionId: 'session',
      geofences: [{ id: 'west', name: 'West zone', minLatitude: 42.695, maxLatitude: 42.700,
        minLongitude: 23.315, maxLongitude: 23.320 }],
      vehicles: [{ id: 'vehicle-1', name: 'First', latestSample: null, lastReceivedAt: null, sampleCount: 5, revision: 5,
        geofenceMemberships: [{ geofenceId: 'west', state: 'Inside' }],
        alerts: [
          { ...base, id: 'speed', type: 'Speeding', speedKmh: 85, speedLimitKmh: 80, speedingDurationSeconds: 30 },
          { ...base, id: 'entry', type: 'GeofenceEntered', geofenceId: 'west', geofenceName: 'West zone',
            speedKmh: null, speedLimitKmh: null, speedingDurationSeconds: null },
          { ...base, id: 'exit', type: 'GeofenceExited', geofenceId: 'west', geofenceName: 'West zone',
            speedKmh: null, speedLimitKmh: null, speedingDurationSeconds: null }
        ] }]
    });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('path.geofence-region')).not.toBeNull();
    const text: string = fixture.nativeElement.textContent;
    expect(text).toContain('Inside');
    expect(text).toContain('Entered West zone.');
    expect(text).toContain('Exited West zone.');
    expect(text).toContain('above 80 km/h for at least 30 seconds');
    fixture.destroy();
    http.verify();
  }));

  it('draws separate routes without joining them and leaves one-point routes as markers', fakeAsync(() => {
    const fixture = TestBed.createComponent(TrackingDashboardComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    tick(0);
    const point = (longitude: number) => ({ latitude: 42.697, longitude, speedKmh: 0, heading: 0, timestamp: '2026-10-20T12:00:00Z' });
    const segment = (id: string, longitudes: number[]) => ({ id, startedAt: '2026-10-20T12:00:00Z', endedAt: null, points: longitudes.map(point) });
    http.expectOne(TRACKING.vehiclesPath).flush({ sessionId: 'session', vehicles: [{
      id: 'vehicle-1', name: 'First', latestSample: point(23.330), lastReceivedAt: null, sampleCount: 5, revision: 5,
      movement: { state: 'Stopped', stopStartedAt: '2026-10-20T12:00:00Z', stopElapsedSeconds: 180 },
      segments: [segment('one', [23.312, 23.314]), segment('two', [23.320, 23.322]), segment('three', [23.330])]
    }] });
    fixture.detectChanges();
    const lines = fixture.nativeElement.querySelectorAll('path.route-line');
    expect(lines.length).toBe(2);
    expect(lines[0].getAttribute('d')).not.toBe(lines[1].getAttribute('d'));
    expect(fixture.nativeElement.textContent).toContain('Stopped');
    expect(fixture.nativeElement.textContent).toContain('180 seconds of GPS coverage');
    fixture.destroy();
    http.verify();
  }));
});
