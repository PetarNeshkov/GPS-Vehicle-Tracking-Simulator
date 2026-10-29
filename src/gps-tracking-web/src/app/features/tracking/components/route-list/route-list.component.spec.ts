import { provideZoneChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouteSegment } from '../../../../core/models/movement';
import { RouteListComponent } from './route-list.component';

describe('RouteListComponent', () => {
  let fixture: ComponentFixture<RouteListComponent>;
  const routes: RouteSegment[] = Array.from({ length: 103 }, (_, index) => ({
    id: `route-${index}`,
    startedAt: new Date(Date.UTC(2026, 9, 1, 0, index)).toISOString(),
    endedAt: index === 102 ? null : new Date(Date.UTC(2026, 9, 1, 0, index + 1)).toISOString(),
    points: []
  }));
  const rows = (): NodeListOf<HTMLElement> => fixture.nativeElement.querySelectorAll('.route-row');
  const click = (selector: string): void => {
    fixture.nativeElement.querySelector(selector).click();
    fixture.detectChanges();
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [RouteListComponent], providers: [provideZoneChangeDetection()] });
    fixture = TestBed.createComponent(RouteListComponent);
    fixture.componentRef.setInput('segments', routes);
    fixture.componentRef.setInput('sessionId', 'first');
    fixture.detectChanges();
  });

  it('bounds a large list to five newest routes and navigates through the final partial page', () => {
    expect(rows().length).toBe(5);
    expect(rows()[0].textContent).toContain('In progress');
    expect(fixture.nativeElement.querySelector('[aria-label="Previous route page"]').disabled).toBeTrue();
    for (let page = 0; page < 20; page++) click('[aria-label="Next route page"]');
    expect(rows().length).toBe(3);
    expect(fixture.nativeElement.querySelector('.range').textContent).toContain('101–103 of 103');
    expect(fixture.nativeElement.querySelector('[aria-label="Next route page"]').disabled).toBeTrue();
    click('[aria-label="Previous route page"]');
    expect(rows().length).toBe(5);
    expect(routes[0].id).toBe('route-0');
  });

  it('resets pagination on filtering and updates the active filter when a route completes', () => {
    click('[aria-label="Next route page"]');
    click('.filters button:nth-child(2)');
    expect(rows().length).toBe(1);
    expect(fixture.nativeElement.querySelector('.range').textContent).toContain('1–1 of 1');
    fixture.componentRef.setInput('segments', routes.map(route => ({ ...route, endedAt: route.startedAt })));
    fixture.detectChanges();
    expect(rows().length).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('No routes in progress yet');
    click('.filters button:first-child');
    expect(rows().length).toBe(5);
  });

  it('handles a shrinking list and clears filters and pagination for a new session', () => {
    click('[aria-label="Next route page"]');
    fixture.componentRef.setInput('segments', routes.slice(0, 2));
    fixture.detectChanges();
    expect(rows().length).toBe(2);
    click('.filters button:nth-child(3)');
    fixture.componentRef.setInput('sessionId', 'second');
    fixture.componentRef.setInput('segments', []);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No routes yet');
    fixture.componentRef.setInput('segments', routes);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.filters button').getAttribute('aria-pressed')).toBe('true');
    expect(fixture.nativeElement.querySelector('.range').textContent).toContain('1–5 of 103');
  });
});
