import {Component, inject, Input, OnChanges, OnDestroy, signal} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule} from '@angular/forms';
import {Subscription, timeout, timer} from 'rxjs';
import { FEEDBACK } from '../../core/constants/feedback.constants';
import {SIMULATION} from '../../core/constants/simulation.constants';
import {TRACKING} from '../../core/constants/tracking.constants';
import {SimulationProfile} from '../../core/models/simulation-profile';
import {ApiService} from '../../core/services/api.service';
import {positiveDuration, positiveNumber} from './settings-editor.component';

@Component({
  selector: 'app-simulation-editor',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './simulation-editor.component.html',
  styleUrl: './simulation-editor.component.css'
})
export class SimulationEditorComponent implements OnChanges, OnDestroy {
  @Input({required: true}) vehicleId = '';
  @Input() sessionId?: string;

  private readonly api = inject(ApiService);
  private request?: Subscription;
  private savedTimer?: Subscription;

  readonly constants = SIMULATION;
  readonly loaded = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly saved = signal(false);
  readonly form = new FormGroup({
    cruisingSpeedKmh: new FormControl(0, {nonNullable: true, validators: positiveNumber}),
    speedingSpeedKmh: new FormControl(0, {nonNullable: true, validators: positiveNumber}),
    drivingSeconds: new FormControl(0, {nonNullable: true, validators: positiveDuration}),
    speedingSeconds: new FormControl(0, {nonNullable: true, validators: positiveDuration}),
    recoverySeconds: new FormControl(0, {nonNullable: true, validators: positiveDuration}),
    stopSeconds: new FormControl(0, {nonNullable: true, validators: positiveDuration}),
    resumeSeconds: new FormControl(0, {nonNullable: true, validators: positiveDuration})
  });

  ngOnChanges(): void {
    this.request?.unsubscribe();
    this.savedTimer?.unsubscribe();
    this.loaded.set(false);
    this.busy.set(false);
    this.error.set(null);
    this.saved.set(false);
    this.form.reset();
  }

  load(): void {
    if (this.busy()) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    this.request = this.api.get<SimulationProfile>(SIMULATION.path(this.vehicleId))
      .pipe(timeout(TRACKING.requestTimeoutMs)).subscribe({
        next: profile => {
          this.form.reset(profile);
          this.loaded.set(true);
          this.busy.set(false);
        },
        error: () => this.fail(SIMULATION.messages.loadFailed)
      });
  }

  save(): void {
    if (this.busy() || !this.loaded()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.error.set(SIMULATION.messages.invalid);
      return;
    }

    const profile = this.form.getRawValue();
    this.savedTimer?.unsubscribe();
    this.busy.set(true);
    this.error.set(null);
    this.saved.set(false);
    this.request = this.api.put<SimulationProfile>(SIMULATION.path(this.vehicleId), profile)
      .pipe(timeout(TRACKING.requestTimeoutMs)).subscribe({
        next: response => {
          this.form.reset(response);
          this.saved.set(true);
          this.savedTimer = timer(FEEDBACK.savedDurationMs).subscribe(() => this.saved.set(false));
          this.busy.set(false);
        },
        error: () => this.fail(SIMULATION.messages.saveFailed)
      });
  }

  private fail(message: string): void {
    this.busy.set(false);
    this.error.set(message);
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
    this.savedTimer?.unsubscribe();
  }
}
