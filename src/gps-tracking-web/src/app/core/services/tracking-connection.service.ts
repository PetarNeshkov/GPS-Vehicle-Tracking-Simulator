import { inject, Injectable, InjectionToken, OnDestroy } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { BehaviorSubject, Subject } from 'rxjs';
import { REALTIME } from '../constants/realtime.constants';
import { TrackingConnectionState, VehicleUpdatedEvent } from '../models/tracking-events';

export const TRACKING_CONNECTION_FACTORY = new InjectionToken<() => HubConnection>(
  'Tracking connection factory', {
    providedIn: 'root',
    factory: () => () => new HubConnectionBuilder()
      .withUrl(REALTIME.hubPath)
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: context => REALTIME.reconnectDelaysMs[
          Math.min(context.previousRetryCount, REALTIME.reconnectDelaysMs.length - 1)]
      })
      .build()
  });

@Injectable({ providedIn: 'root' })
export class TrackingConnectionService implements OnDestroy {
  private readonly createConnection = inject(TRACKING_CONNECTION_FACTORY);
  private readonly updates = new Subject<VehicleUpdatedEvent>();
  private readonly state = new BehaviorSubject<TrackingConnectionState>('stopped');
  private connection?: HubConnection;
  private retry?: ReturnType<typeof setTimeout>;

  readonly updates$ = this.updates.asObservable();
  readonly state$ = this.state.asObservable();

  start(): void {
    if (this.connection) return;
    const connection = this.createConnection();
    this.connection = connection;
    connection.on(REALTIME.vehicleUpdatedEvent, (event: VehicleUpdatedEvent) => {
      if (this.connection === connection) this.updates.next(event);
    });
    connection.onreconnecting(() => {
      if (this.connection === connection) this.state.next('reconnecting');
    });
    connection.onreconnected(() => {
      if (this.connection === connection) this.state.next('connected');
    });
    connection.onclose(() => {
      if (this.connection === connection) this.scheduleRetry(connection);
    });
    this.state.next('connecting');
    void this.connect(connection);
  }

  stop(): void {
    const connection = this.connection;
    this.connection = undefined;
    clearTimeout(this.retry);
    this.retry = undefined;
    if (connection) {
      connection.off(REALTIME.vehicleUpdatedEvent);
      // Stop aborts pending starts/reconnects. Old callbacks are guarded by identity.
      void connection.stop().catch(() => undefined);
    }
    this.state.next('stopped');
  }

  private async connect(connection: HubConnection): Promise<void> {
    try {
      await connection.start();
      if (this.connection === connection) this.state.next('connected');
    } catch {
      if (this.connection === connection) this.scheduleRetry(connection);
    }
  }

  private scheduleRetry(connection: HubConnection): void {
    clearTimeout(this.retry);
    this.state.next('reconnecting');
    this.retry = setTimeout(() => {
      if (this.connection === connection) void this.connect(connection);
    }, REALTIME.retryDelayMs);
  }

  ngOnDestroy(): void {
    this.stop();
    this.updates.complete();
    this.state.complete();
  }
}
