import { inject, Injectable, OnDestroy, signal } from '@angular/core';
import { catchError, EMPTY, exhaustMap, Subscription, timeout, timer } from 'rxjs';
import { TRACKING } from '../constants/tracking.constants';
import { ERROR_MESSAGES } from '../constants/error-messages.constants';
import { VehicleSnapshot } from '../models/vehicle-snapshot';
import { TrackingApiService } from './tracking-api.service';

@Injectable({ providedIn: 'root' })
export class TrackingStore implements OnDestroy {
  private readonly api = inject(TrackingApiService);
  private readonly snapshots = signal<readonly VehicleSnapshot[]>([]);
  private readonly error = signal<string | null>(null);
  private readonly loaded = signal(false);
  private subscription?: Subscription;

  readonly vehicles = this.snapshots.asReadonly();
  readonly connectionError = this.error.asReadonly();
  readonly hasLoaded = this.loaded.asReadonly();

  start(): void {
    if (this.subscription) return;
    this.subscription = timer(0, TRACKING.pollingIntervalMs).pipe(
      exhaustMap(() => this.api.getSnapshots().pipe(
        timeout(TRACKING.requestTimeoutMs),
        catchError(() => {
          this.error.set(ERROR_MESSAGES.trackingConnectionFailed);
          return EMPTY;
        })
      ))
    ).subscribe(vehicles => {
      this.snapshots.set(vehicles);
      this.loaded.set(true);
      this.error.set(null);
    });
  }

  stop(): void {
    this.subscription?.unsubscribe();
    this.subscription = undefined;
  }

  ngOnDestroy(): void {
    this.stop();
  }
}
