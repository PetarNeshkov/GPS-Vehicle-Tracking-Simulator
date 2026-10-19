import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { SettingsEditorComponent } from './settings-editor.component';
import { TrackingStore } from '../../core/services/tracking-store.service';
import { VehicleUpdatedEvent } from '../../core/models/tracking-events';

describe('SettingsEditorComponent', () => {
  const settings = { speedLimitKmh: 80, speedingDurationSeconds: 30, stopDurationSeconds: 180 };
  const vehicle = { id: 'vehicle-1', name: 'First', latestSample: null, lastReceivedAt: null,
    sampleCount: 0, revision: 0, settings };

  it('validates input, preserves dirty edits during live updates and prevents duplicate saves', () => {
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
    fixture.destroy();
  });
});
