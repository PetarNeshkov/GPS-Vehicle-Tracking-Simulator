import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { FEEDBACK } from '../../../../core/constants/feedback.constants';
import { SIMULATION } from '../../../../core/constants/simulation.constants';
import { SimulationEditorComponent } from './simulation-editor.component';

describe('SimulationEditorComponent', () => {
  const profile = { cruisingSpeedKmh: 42, speedingSpeedKmh: 95, drivingSeconds: 120,
    speedingSeconds: 60, recoverySeconds: 30, stopSeconds: 210, resumeSeconds: 120 };

  beforeEach(() => TestBed.configureTestingModule({
    imports: [SimulationEditorComponent],
    providers: [provideHttpClient(), provideHttpClientTesting()]
  }));

  it('loads, validates and restarts the confirmation timeout on a new save', fakeAsync(() => {
    const fixture = TestBed.createComponent(SimulationEditorComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('vehicleId', 'vehicle-1');
    fixture.detectChanges();
    http.expectNone(SIMULATION.path('vehicle-1'));
    const component = fixture.componentInstance;
    component.load();
    http.expectOne(SIMULATION.path('vehicle-1')).flush(profile);
    fixture.detectChanges();

    component.form.controls.stopSeconds.setValue(1.5);
    component.save();
    http.expectNone(SIMULATION.path('vehicle-1'));
    expect(component.error()).toBe(SIMULATION.messages.invalid);

    component.form.controls.stopSeconds.setValue(20);
    component.save();
    component.save();
    const request = http.expectOne(SIMULATION.path('vehicle-1'));
    expect(request.request.method).toBe('PUT');
    expect(request.request.body.stopSeconds).toBe(20);
    request.flush({ ...profile, stopSeconds: 20 });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(SIMULATION.messages.saved);
    expect(component.form.pristine).toBeTrue();
    tick(10_000);
    component.save();
    http.expectOne(SIMULATION.path('vehicle-1')).flush(profile);
    tick(FEEDBACK.savedDurationMs - 1);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(SIMULATION.messages.saved);
    tick(1);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain(SIMULATION.messages.saved);
    fixture.destroy();
    http.verify();
  }));

  it('retains edits on failure and cancels pending work when the backend session changes', () => {
    const fixture = TestBed.createComponent(SimulationEditorComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('vehicleId', 'vehicle-1');
    fixture.componentRef.setInput('sessionId', 'first');
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.load();
    http.expectOne(SIMULATION.path('vehicle-1')).flush(profile);
    component.form.controls.cruisingSpeedKmh.setValue(100);
    component.form.markAsDirty();
    component.save();
    http.expectOne(SIMULATION.path('vehicle-1')).flush({}, { status: 503, statusText: 'Unavailable' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(SIMULATION.messages.saveFailed);
    expect(component.form.controls.cruisingSpeedKmh.value).toBe(100);
    expect(component.form.dirty).toBeTrue();

    component.save();
    const pending = http.expectOne(SIMULATION.path('vehicle-1'));
    fixture.componentRef.setInput('sessionId', 'second');
    fixture.detectChanges();
    expect(pending.cancelled).toBeTrue();
    expect(component.loaded()).toBeFalse();
    fixture.destroy();
    http.verify();
  });
});
