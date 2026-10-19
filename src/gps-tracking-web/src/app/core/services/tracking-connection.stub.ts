import { BehaviorSubject, Subject } from 'rxjs';
import { TrackingConnectionState, VehicleUpdatedEvent } from '../models/tracking-events';

export class TrackingConnectionStub {
  readonly updates$ = new Subject<VehicleUpdatedEvent>();
  readonly state$ = new BehaviorSubject<TrackingConnectionState>('stopped');
  startCalls = 0;

  start(): void {
    this.startCalls++; this.state$.next('connected');
  }
  stop(): void {
    this.state$.next('stopped');
  }
}
