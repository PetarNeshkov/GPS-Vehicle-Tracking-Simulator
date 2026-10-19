import {inject, Injectable, OnDestroy, signal} from '@angular/core';
import {Subscription, timeout} from 'rxjs';
import {TRACKING} from '../constants/tracking.constants';
import {ERROR_MESSAGES} from '../constants/error-messages.constants';
import {VehicleSnapshot} from '../models/vehicle-snapshot';
import {TrackingApiService} from './tracking-api.service';
import {TrackingConnectionService} from './tracking-connection.service';
import {REALTIME} from '../constants/realtime.constants';
import {TrackingConnectionState, VehicleUpdatedEvent} from '../models/tracking-events';

@Injectable({providedIn: 'root'})
export class TrackingStore implements OnDestroy {
  private readonly api = inject(TrackingApiService);
  private readonly connection = inject(TrackingConnectionService);

  private readonly snapshots = signal<readonly VehicleSnapshot[]>([]);
  private readonly error = signal<string | null>(null);
  private readonly loaded = signal(false);

  private subscription?: Subscription;
  private request?: Subscription;
  private retry?: ReturnType<typeof setTimeout>;

  private sessionId?: string;
  private synchronizing = false;
  private generation = 0;

  private readonly buffered = new Map<string, VehicleUpdatedEvent>();
  private readonly status = signal<string>(REALTIME.status.stopped);

  readonly vehicles = this.snapshots.asReadonly();
  readonly connectionError = this.error.asReadonly();
  readonly hasLoaded = this.loaded.asReadonly();
  readonly connectionStatus = this.status.asReadonly();

  start(): void {
    if (this.subscription) {
      return;
    }

    this.subscription = new Subscription();

    const updatesSubscription = this.connection.updates$.subscribe(event => this.receive(event));
    this.subscription.add(updatesSubscription);

    const stateSubscription = this.connection.state$.subscribe(state => this.onConnectionState(state));
    this.subscription.add(stateSubscription);

    this.connection.start();
  }

  stop(): void {
    this.subscription?.unsubscribe();
    this.subscription = undefined;

    this.cancelSynchronization();
    this.buffered.clear();

    this.connection.stop();
    this.status.set(REALTIME.status.stopped);
  }

  private onConnectionState(state: TrackingConnectionState): void {
    this.cancelSynchronization();
    this.buffered.clear();
    this.status.set(REALTIME.status[state]);

    if (state === 'connected') {
      this.synchronizing = true;
      this.synchronize();
    } else {
      this.synchronizing = true;
      if (state === 'reconnecting') {
        this.error.set(ERROR_MESSAGES.trackingConnectionFailed);
      }
    }
  }

  private synchronize(): void {
    const generation = this.generation;

    this.status.set(REALTIME.status.synchronizing);

    const snapshotRequest = this.api.getSnapshots().pipe(timeout(TRACKING.requestTimeoutMs));

    this.request = snapshotRequest.subscribe({
      next: snapshot => {
        if (generation !== this.generation) {
          return;
        }

        const sameSession = this.sessionId === snapshot.sessionId;
        const previousEntries = this.snapshots().map(vehicle => [vehicle.id, vehicle] as const);
        const previousVehicles = new Map(previousEntries);

        const mergedVehicles = snapshot.vehicles.map(vehicle => {
          const current = sameSession ? previousVehicles.get(vehicle.id) : undefined;
          const hasNewerSnapshot = current !== undefined && current.revision > vehicle.revision;

          return hasNewerSnapshot ? current : vehicle;
        });

        this.sessionId = snapshot.sessionId;
        this.snapshots.set(mergedVehicles);

        for (const event of this.buffered.values()) {
          this.apply(event);
        }

        this.buffered.clear();
        this.synchronizing = false;
        this.loaded.set(true);
        this.error.set(null);
        this.status.set(REALTIME.status.connected);
      },
      error: () => {
        if (generation !== this.generation) {
          return;
        }

        this.error.set(ERROR_MESSAGES.trackingSynchronizationFailed);
        this.retry = setTimeout(() => this.synchronize(), REALTIME.retryDelayMs);
      }
    });
  }

  private receive(event: VehicleUpdatedEvent): void {
    if (this.synchronizing) {
      const key = `${event.sessionId}/${event.vehicle.id}`;
      const previous = this.buffered.get(key);

      if (!previous || previous.vehicle.revision < event.vehicle.revision) {
        this.buffered.set(key, event);
      }
    } else {
      this.apply(event);
    }
  }

  private apply(event: VehicleUpdatedEvent): void {
    if (event.sessionId !== this.sessionId) {
      return;
    }

    this.snapshots.update(vehicles => {
      return vehicles.map(vehicle => {
        const isMatchingVehicle = vehicle.id === event.vehicle.id;
        const isNewerRevision = event.vehicle.revision > vehicle.revision;

        return isMatchingVehicle && isNewerRevision ? event.vehicle : vehicle;
      });
    });
  }

  private cancelSynchronization(): void {
    this.generation++;

    this.request?.unsubscribe();
    this.request = undefined;

    clearTimeout(this.retry);
    this.retry = undefined;
  }

  ngOnDestroy(): void {
    this.stop();
  }
}
