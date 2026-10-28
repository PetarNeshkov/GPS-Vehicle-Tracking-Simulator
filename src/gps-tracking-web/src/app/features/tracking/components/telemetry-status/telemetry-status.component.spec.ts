import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { TelemetryStatusComponent } from './telemetry-status.component';

describe('TelemetryStatusComponent', () => {
  it('waits before telemetry, becomes stale at 30 seconds and recovers on receipt', fakeAsync(() => {
    TestBed.configureTestingModule({ imports: [TelemetryStatusComponent] });
    const fixture = TestBed.createComponent(TelemetryStatusComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('Waiting');

    fixture.componentRef.setInput('lastReceivedAt', new Date().toISOString());
    fixture.detectChanges();
    expect(root.textContent).toContain('Telemetry: Live');

    tick(29000);
    fixture.detectChanges();
    expect(root.textContent).toContain('29 seconds ago');
    expect(root.querySelector('.stale')).toBeNull();

    tick(1000);
    fixture.detectChanges();
    expect(root.textContent).toContain('Telemetry: Stale');
    expect(root.querySelector('.stale')).not.toBeNull();

    fixture.componentRef.setInput('lastReceivedAt', new Date().toISOString());
    fixture.detectChanges();
    expect(root.textContent).toContain('Telemetry: Live');
    expect(root.textContent).toContain('0 seconds ago');
    fixture.destroy();
    // fakeAsync also verifies that destruction leaves no periodic timer behind.
  }));
});
