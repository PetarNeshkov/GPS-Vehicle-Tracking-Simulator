import { CommonModule } from '@angular/common';
import { Component, inject, Input, OnChanges, OnDestroy, SimpleChanges } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors } from '@angular/forms';
import { Subscription } from 'rxjs';
import { SETTINGS } from '../../core/constants/settings.constants';
import { GlobalSettings } from '../../core/models/tracking-settings';
import { VehicleSnapshot } from '../../core/models/vehicle-snapshot';
import { TrackingStore } from '../../core/services/tracking-store.service';

export function positiveNumber(control: AbstractControl): ValidationErrors | null {
  const value: unknown = control.value;
  return typeof value === 'number' && Number.isFinite(value) && value > 0 ? null : { positive: true };
}

export function positiveDuration(control: AbstractControl): ValidationErrors | null {
  const value: number = control.value;
  return !positiveNumber(control) && Number.isInteger(value) && value <= SETTINGS.maximumDurationSeconds
    ? null : { duration: true };
}

@Component({
  selector: 'app-settings-editor',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './settings-editor.component.html',
  styleUrl: './settings-editor.component.css'
})
export class SettingsEditorComponent implements OnChanges, OnDestroy {
  @Input() vehicle: VehicleSnapshot | null = null;
  @Input() global: GlobalSettings | null = null;
  @Input() sessionId?: string;

  private readonly store = inject(TrackingStore);
  private request?: Subscription;

  readonly constants = SETTINGS;
  readonly vehicleForm = new FormGroup({
    speedLimitKmh: new FormControl<number | null>(null, positiveNumber),
    speedingDurationSeconds: new FormControl<number | null>(null, positiveDuration),
    stopDurationSeconds: new FormControl<number | null>(null, positiveDuration)
  });
  readonly globalForm = new FormGroup({
    stopRadiusMeters: new FormControl<number | null>(null, positiveNumber)
  });

  saving = false;
  error: string | null = null;
  saved = false;

  ngOnChanges(changes: SimpleChanges): void {
    const sessionChanged = changes['sessionId'] !== undefined;
    if (sessionChanged) {
      this.request?.unsubscribe();
      this.saving = false;
      this.error = null;
      this.saved = false;
    }

    if (this.vehicle?.settings && (sessionChanged || (!this.vehicleForm.dirty && !this.saving))){
      this.vehicleForm.reset(this.vehicle.settings);
    }

    if (this.global && (sessionChanged || (!this.globalForm.dirty && !this.saving))) {
      this.globalForm.reset({stopRadiusMeters: this.global.stopRadiusMeters});
    }
  }

  saveVehicle(): void {
    if (!this.vehicle || this.saving || this.vehicleForm.invalid) {
      this.vehicleForm.markAllAsTouched();

      return;
    }

    const values = this.vehicleForm.getRawValue();
    const settings = {
      speedLimitKmh: values.speedLimitKmh!,
      speedingDurationSeconds: values.speedingDurationSeconds!,
      stopDurationSeconds: values.stopDurationSeconds!
    };

    this.beginSave();
    this.request = this.store.saveVehicleSettings(this.vehicle.id, settings).subscribe({
      next: response => {
        const current = this.store.vehicles().find(vehicle => vehicle.id === response.vehicle.id);
        const latest = current && current.revision > response.vehicle.revision ? current : response.vehicle;

        this.vehicleForm.reset(latest.settings ?? settings);
        this.saving = false;
        this.saved = true;
      },
      error: () => this.saveFailed()
    });
  }

  saveGlobal(): void {
    if (this.saving || this.globalForm.invalid) {
      this.globalForm.markAllAsTouched();

      return;
    }

    const radius = this.globalForm.controls.stopRadiusMeters.value!;
    this.beginSave();
    this.request = this.store.saveGlobalSettings(radius).subscribe({
      next: response => {
        const current = this.store.globalSettings();
        const latest = current && current.revision > response.settings.revision ? current : response.settings;

        this.globalForm.reset({ stopRadiusMeters: latest.stopRadiusMeters });
        this.saving = false;
        this.saved = true;
      },
      error: () => this.saveFailed()
    });
  }

  private beginSave(): void {
    this.saving = true;
    this.error = null;
    this.saved = false;
  }

  private saveFailed(): void {
    this.saving = false;
    this.error = SETTINGS.messages.saveFailed;
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
  }
}
