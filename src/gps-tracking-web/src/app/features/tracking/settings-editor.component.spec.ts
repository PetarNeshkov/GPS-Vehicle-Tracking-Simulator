import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { FEEDBACK } from '../../core/constants/feedback.constants';
import { Subject } from 'rxjs';
import { SettingsEditorComponent } from './settings-editor.component';
import { TrackingStore } from '../../core/services/tracking-store.service';
import { VehicleUpdatedEvent } from '../../core/models/tracking-events';
import { SETTINGS } from '../../core/constants/settings.constants';
import { GlobalSettingsEvent } from '../../core/models/tracking-settings';

describe('SettingsEditorComponent', () => {
  it('shows global save confirmation for 15 seconds and removes its layout space', fakeAsync(() => {
    const result = new Subject<GlobalSettingsEvent>();
    const store = jasmine.createSpyObj<TrackingStore>('store', ['saveGlobalSettings', 'globalSettings']);
    const global = { stopRadiusMeters: 10, revision: 0 };
    store.globalSettings.and.returnValue(global);
    store.saveGlobalSettings.and.returnValue(result);
    TestBed.configureTestingModule({ imports: [SettingsEditorComponent], providers: [{ provide: TrackingStore, useValue: store }] });
    const fixture = TestBed.createComponent(SettingsEditorComponent);
    fixture.componentRef.setInput('global', global);
    fixture.detectChanges();

    fixture.componentInstance.saveGlobal();
    result.next({ sessionId: 'session', settings: global });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.save-confirmation')?.textContent).toContain(SETTINGS.messages.saved);
    tick(FEEDBACK.savedDurationMs - 1);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.save-confirmation')).not.toBeNull();
    tick(1);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.save-confirmation')).toBeNull();
    fixture.destroy();
  }));

  const settings = { speedLimitKmh: 80, speedingDurationSeconds: 30, stopDurationSeconds: 180 };
  const vehicle = { id: 'vehicle-1', name: 'First', latestSample: null, lastReceivedAt: null,
    sampleCount: 0, revision: 0, settings };

  it('renders a failed save and retains the draft for retry', () => {
    const result = new Subject<VehicleUpdatedEvent>();
    const store = jasmine.createSpyObj<TrackingStore>('store', ['saveVehicleSettings']);
    store.saveVehicleSettings.and.returnValue(result);
    TestBed.configureTestingModule({ imports: [SettingsEditorComponent], providers: [{ provide: TrackingStore, useValue: store }] });
    const fixture = TestBed.createComponent(SettingsEditorComponent);
    fixture.componentRef.setInput('vehicle', vehicle);
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.vehicleForm.controls.speedLimitKmh.setValue(90);
    component.vehicleForm.markAsDirty();
    const root: HTMLElement = fixture.nativeElement;
    const button = root.querySelector<HTMLButtonElement>('button')!;
    button.click();
    fixture.detectChanges();
    expect(button.disabled).toBeTrue();

    result.error(new Error('Unavailable'));
    fixture.detectChanges();
    expect(root.textContent).toContain(SETTINGS.messages.saveFailed);
    expect(button.disabled).toBeFalse();
    expect(component.vehicleForm.controls.speedLimitKmh.value).toBe(90);
    expect(component.vehicleForm.dirty).toBeTrue();
    fixture.destroy();
  });

  it('validates input, preserves dirty edits and dismisses successful save feedback', fakeAsync(() => {
    const result = new Subject<VehicleUpdatedEvent>();
    const store = jasmine.createSpyObj<TrackingStore>('store', ['saveVehicleSettings', 'saveGlobalSettings', 'vehicles']);
    store.vehicles.and.returnValue([]);
    store.saveVehicleSettings.and.returnValue(result);
    TestBed.configureTestingModule({ imports: [SettingsEditorComponent], providers: [{ provide: TrackingStore, useValue: store }] });
    const fixture = TestBed.createComponent(SettingsEditorComponent);
    fixture.componentRef.setInput('vehicle', vehicle);
    fixture.componentRef.setInput('sessionId', 'session');
    fixture.detectChanges();
    const component = fixture.componentInstance;

    component.vehicleForm.controls.speedingDurationSeconds.setValue(1.5);
    component.saveVehicle();
    expect(store.saveVehicleSettings).not.toHaveBeenCalled();
    component.vehicleForm.controls.speedingDurationSeconds.setValue(15);
    component.vehicleForm.markAsDirty();
    fixture.componentRef.setInput('vehicle', { ...vehicle, revision: 1 });
    fixture.detectChanges();
    expect(component.vehicleForm.controls.speedingDurationSeconds.value).toBe(15);

    component.saveVehicle(); component.saveVehicle();
    expect(store.saveVehicleSettings.calls.count()).toBe(1);
    result.next({ sessionId: 'session', vehicle: { ...vehicle, settings: { ...settings, speedingDurationSeconds: 15 } } });
    expect(component.saved).toBeTrue();
    expect(component.vehicleForm.pristine).toBeTrue();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(SETTINGS.messages.saved);
    tick(FEEDBACK.savedDurationMs - 1);
    expect(component.saved).toBeTrue();
    tick(1);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain(SETTINGS.messages.saved);
    fixture.destroy();
  }));
});
