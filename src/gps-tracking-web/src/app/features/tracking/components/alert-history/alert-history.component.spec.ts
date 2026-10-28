import { TestBed } from '@angular/core/testing';
import { ALERTS } from '../../../../core/constants/alerts.constants';
import { TrackingAlert } from '../../../../core/models/tracking-settings';
import { AlertHistoryComponent } from './alert-history.component';

describe('AlertHistoryComponent', () => {
  it('shows friendly vehicle and event names with fallbacks for unknown values', () => {
    TestBed.configureTestingModule({ imports: [AlertHistoryComponent] });
    const fixture = TestBed.createComponent(AlertHistoryComponent);
    fixture.componentRef.setInput('vehicles', [{ id: 'vehicle-0', name: 'First vehicle' }]);
    fixture.componentRef.setInput('alerts', [
      { ...alerts[0], type: 'GeofenceEntered', geofenceName: 'West' },
      { ...alerts[1], type: 'Future event' }
    ]);
    fixture.detectChanges();

    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('First vehicle');
    expect(root.textContent).toContain('Geofence entered');
    expect(root.textContent).toContain('vehicle-1');
    expect(root.textContent).toContain('Future event');
    fixture.destroy();
  });

  const alerts: TrackingAlert[] = Array.from({ length: 45 }, (_, index) => ({
    id: `alert-${index}`,
    vehicleId: `vehicle-${index}`,
    type: 'Speeding',
    occurredAt: '2026-10-20T12:00:00Z',
    createdAt: '2026-10-20T12:00:01Z',
    speedKmh: 85,
    speedLimitKmh: 80,
    speedingDurationSeconds: 30
  }));

  it('limits rendered entries, scrolls within the panel and keeps all history reachable', () => {
    TestBed.configureTestingModule({ imports: [AlertHistoryComponent] });
    const fixture = TestBed.createComponent(AlertHistoryComponent);
    fixture.componentRef.setInput('alerts', alerts);
    fixture.componentRef.setInput('sessionId', 'first');
    fixture.detectChanges();

    const root: HTMLElement = fixture.nativeElement;
    const list = root.querySelector<HTMLElement>('.alert-list')!;
    const buttons = root.querySelectorAll<HTMLButtonElement>('button');
    expect(root.querySelectorAll('.alert-card').length).toBe(ALERTS.pageSize);
    expect(root.textContent).toContain('45 total');
    expect(buttons[0].disabled).toBeTrue();
    expect(getComputedStyle(list).overflowY).toBe('auto');
    expect(list.scrollHeight).toBeGreaterThan(list.clientHeight);

    list.scrollTop = 100;
    buttons[1].click();
    fixture.detectChanges();
    expect(list.scrollTop).toBe(0);
    expect(root.textContent).toContain('Page 2 of 3');
    expect(root.querySelector('.alert-card strong')?.textContent).toContain('vehicle-20');

    buttons[1].click();
    fixture.detectChanges();
    expect(root.querySelectorAll('.alert-card').length).toBe(5);
    expect(root.textContent).toContain('vehicle-44');
    expect(buttons[1].disabled).toBeTrue();

    buttons[0].click();
    fixture.detectChanges();
    expect(root.textContent).toContain('Page 2 of 3');
    expect(alerts.length).toBe(45);
    fixture.destroy();
  });

  it('updates live totals, clamps shorter histories and resets paging for a new session', () => {
    TestBed.configureTestingModule({ imports: [AlertHistoryComponent] });
    const fixture = TestBed.createComponent(AlertHistoryComponent);
    fixture.componentRef.setInput('alerts', alerts);
    fixture.componentRef.setInput('sessionId', 'first');
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    root.querySelectorAll<HTMLButtonElement>('button')[1].click();
    fixture.detectChanges();

    fixture.componentRef.setInput('alerts', [{ ...alerts[0], id: 'new' }, ...alerts]);
    fixture.detectChanges();
    expect(root.textContent).toContain('46 total');
    expect(root.textContent).toContain('Page 2 of 3');
    expect(root.querySelectorAll('.alert-card').length).toBe(ALERTS.pageSize);

    fixture.componentRef.setInput('alerts', []);
    fixture.componentRef.setInput('sessionId', 'second');
    fixture.detectChanges();
    expect(root.textContent).toContain('No alerts in this session.');
    expect(root.textContent).toContain('Page 1 of 1');
    expect(Array.from(root.querySelectorAll<HTMLButtonElement>('button')).every(button => button.disabled)).toBeTrue();

    fixture.componentRef.setInput('alerts', alerts);
    fixture.detectChanges();
    root.querySelectorAll<HTMLButtonElement>('button')[1].click();
    fixture.detectChanges();
    fixture.componentRef.setInput('alerts', alerts.slice(0, 1));
    fixture.detectChanges();
    expect(root.textContent).toContain('Page 1 of 1');
    expect(root.querySelectorAll('.alert-card').length).toBe(1);
    fixture.destroy();
  });
});
