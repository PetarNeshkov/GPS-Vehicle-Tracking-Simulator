import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HistoryViewerComponent } from './history-viewer.component';
import { TRACKING } from '../../../../core/constants/tracking.constants';

describe('HistoryViewerComponent', () => {
  it('bounds rendered rows, browses newest first and resets to the newest page on refresh', () => {
    TestBed.configureTestingModule({ imports: [HistoryViewerComponent], providers: [provideHttpClient(), provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(HistoryViewerComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('vehicleId', 'vehicle-1');
    fixture.componentRef.setInput('sessionId', 'first');
    fixture.detectChanges();
    const samples = Array.from({ length: 45 }, (_, index) => ({
      timestamp: new Date(Date.UTC(2026, 9, 20, 12, 0, index * 10)).toISOString(),
      latitude: 42, longitude: 23, speedKmh: index, heading: 0
    }));
    fixture.componentInstance.load();
    http.expectOne(TRACKING.vehicleHistoryPath('vehicle-1')).flush({
      sessionId: 'first', vehicleId: 'vehicle-1', revision: 45, samples
    });
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const buttons = root.querySelectorAll<HTMLButtonElement>('nav button');
    expect(root.querySelectorAll('tbody tr').length).toBe(20);
    expect(fixture.componentInstance.visibleSamples()[0].speedKmh).toBe(44);
    expect(buttons[0].disabled).toBeTrue();

    buttons[1].click();
    fixture.detectChanges();
    expect(root.textContent).toContain('Page 2 of 3');
    expect(fixture.componentInstance.visibleSamples()[0].speedKmh).toBe(24);
    buttons[1].click();
    fixture.detectChanges();
    expect(root.querySelectorAll('tbody tr').length).toBe(5);
    expect(fixture.componentInstance.visibleSamples()[4].speedKmh).toBe(0);
    expect(buttons[1].disabled).toBeTrue();
    expect(fixture.componentInstance.history()!.samples[0].speedKmh).toBe(0);

    fixture.componentInstance.load();
    http.expectOne(TRACKING.vehicleHistoryPath('vehicle-1')).flush({
      sessionId: 'first', vehicleId: 'vehicle-1', revision: 0, samples: []
    });
    fixture.detectChanges();
    expect(root.textContent).toContain('No observations yet.');
    expect(root.textContent).toContain('Page 1 of 1');
    expect(buttons[0].disabled).toBeTrue();
    expect(buttons[1].disabled).toBeTrue();
    fixture.destroy();
    http.verify();
  });

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
