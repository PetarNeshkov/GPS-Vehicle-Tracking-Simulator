import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HistoryViewerComponent } from './history-viewer.component';
import { TRACKING } from '../../core/constants/tracking.constants';

describe('HistoryViewerComponent', () => {
  it('loads raw observations on demand and cancels obsolete session requests', () => {
    TestBed.configureTestingModule({ imports: [HistoryViewerComponent], providers: [provideHttpClient(), provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(HistoryViewerComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('vehicleId', 'vehicle-1');
    fixture.componentRef.setInput('sessionId', 'first');
    fixture.detectChanges();
    http.expectNone(TRACKING.vehicleHistoryPath('vehicle-1'));
    fixture.componentInstance.load();
    http.expectOne(TRACKING.vehicleHistoryPath('vehicle-1')).flush({ sessionId: 'first', vehicleId: 'vehicle-1', revision: 3,
      samples: [0, 10, 20].map(second => ({ timestamp: `2026-10-20T12:00:${second.toString().padStart(2, '0')}Z`, latitude: 42, longitude: 23, speedKmh: 0, heading: 0 })) });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('tbody tr').length).toBe(3);
    fixture.componentInstance.load();
    const pending = http.expectOne(TRACKING.vehicleHistoryPath('vehicle-1'));
    fixture.componentRef.setInput('sessionId', 'second');
    fixture.detectChanges();
    expect(pending.cancelled).toBeTrue();
    expect(fixture.componentInstance.history()).toBeNull();
    fixture.componentInstance.load();
    http.expectOne(TRACKING.vehicleHistoryPath('vehicle-1')).flush({ sessionId: 'first', vehicleId: 'vehicle-1', revision: 3, samples: [] });
    expect(fixture.componentInstance.history()).toBeNull();
    expect(fixture.componentInstance.error()).not.toBeNull();
    fixture.destroy();
    http.verify();
  });
});
