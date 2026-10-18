import { CommonModule } from '@angular/common';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZoneChangeDetection } from '@angular/core';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { TRACKING } from '../../core/constants/tracking.constants';
import { TrackingDashboardComponent } from './tracking-dashboard.component';

describe('TrackingDashboardComponent', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CommonModule],
      declarations: [TrackingDashboardComponent],
      providers: [provideZoneChangeDetection(), provideHttpClient(), provideHttpClientTesting()]
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
    http.expectOne(TRACKING.vehiclesPath).flush([waiting]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Waiting for GPS data');
    expect(fixture.nativeElement.querySelectorAll('svg g').length).toBe(0);

    tick(TRACKING.pollingIntervalMs);
    http.expectOne(TRACKING.vehiclesPath).flush([{
      ...waiting, sampleCount: 1, revision: 1,
      latestSample: { latitude: 42.697, longitude: 23.318, speedKmh: 85.5, heading: 360,
        timestamp: '2026-10-18T09:00:00Z' }
    }]);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('85.5');
    expect(fixture.nativeElement.textContent).not.toContain('Waiting for GPS data');
    expect(fixture.nativeElement.querySelector('svg g').getAttribute('transform')).toContain('rotate(0)');
    fixture.destroy();
    http.verify();
  }));
});
