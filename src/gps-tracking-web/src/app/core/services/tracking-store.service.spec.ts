import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TRACKING } from '../constants/tracking.constants';
import { REALTIME } from '../constants/realtime.constants';
import { ERROR_MESSAGES } from '../constants/error-messages.constants';
import { VehicleSnapshot } from '../models/vehicle-snapshot';
import { TrackingStore } from './tracking-store.service';
import { TrackingConnectionService } from './tracking-connection.service';
import { TrackingConnectionStub } from './tracking-connection.stub';

describe('TrackingStore live synchronization', () => {
  let store: TrackingStore;
  let http: HttpTestingController;
  let connection: TrackingConnectionStub;
  const snapshot: VehicleSnapshot = {
    id: 'vehicle-1', name: 'Vehicle 1', latestSample: null,
    lastReceivedAt: null, sampleCount: 0, revision: 0
  };
  const vehicle = (revision: number) => ({ ...snapshot, revision, sampleCount: revision });
  const update = (revision: number, sessionId = 'first') => ({ sessionId, vehicle: vehicle(revision) });
  const envelope = (revision: number, sessionId = 'first') => ({ sessionId, vehicles: [vehicle(revision)] });

  beforeEach(() => {
    connection = new TrackingConnectionStub();
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting(),
      { provide: TrackingConnectionService, useValue: connection }] });
    store = TestBed.inject(TrackingStore);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => { store.stop(); http.verify(); });

  it('merges alert IDs independently of vehicle revision and restores global settings on restart', () => {
    store.start();
    const alert = { id: 'alert-1', vehicleId: 'vehicle-1', type: 'Speeding',
      occurredAt: '2026-10-19T12:00:30Z', createdAt: '2026-10-19T12:00:31Z',
      speedKmh: 85, speedLimitKmh: 80, speedingDurationSeconds: 30 };
    connection.globalUpdates$.next({ sessionId: 'first', settings: { stopRadiusMeters: 25, revision: 2 } });
    http.expectOne(TRACKING.vehiclesPath).flush({ ...envelope(5), globalSettings: { stopRadiusMeters: 10, revision: 0 } });
    connection.updates$.next({ sessionId: 'first', vehicle: { ...vehicle(3), alerts: [alert] } });
    connection.updates$.next({ sessionId: 'first', vehicle: { ...vehicle(3), alerts: [alert] } });
    expect(store.alerts()).toEqual([alert]);
    expect(store.vehicles()[0].revision).toBe(5);
    expect(store.globalSettings()?.stopRadiusMeters).toBe(25);

    connection.state$.next('reconnecting');
    connection.state$.next('connected');
    http.expectOne(TRACKING.vehiclesPath).flush({ ...envelope(0, 'second'), globalSettings: { stopRadiusMeters: 10, revision: 0 } });
    expect(store.alerts()).toEqual([]);
    expect(store.globalSettings()?.stopRadiusMeters).toBe(10);
  });

  it('buffers updates during REST loading, rejects older events and does not poll', fakeAsync(() => {
    store.start(); store.start();
    expect(connection.startCalls).toBe(1);
    const pending = http.expectOne(TRACKING.vehiclesPath);
    connection.updates$.next(update(3));
    connection.updates$.next(update(2));
    pending.flush(envelope(1));
    expect(store.vehicles()[0].revision).toBe(3);
    connection.updates$.next(update(3));
    connection.updates$.next(update(1));
    expect(store.vehicles()[0].revision).toBe(3);
    connection.updates$.next(update(4));
    expect(store.vehicles()[0].revision).toBe(4);
    tick(60_000);
    http.expectNone(TRACKING.vehiclesPath);
  }));

  it('retains data through disconnection and resets revisions for a new backend session', () => {
    store.start();
    http.expectOne(TRACKING.vehiclesPath).flush(envelope(50));
    connection.state$.next('reconnecting');
    expect(store.vehicles()[0].revision).toBe(50);
    expect(store.connectionError()).toBe(ERROR_MESSAGES.trackingConnectionFailed);
    connection.state$.next('connected');
    connection.updates$.next(update(1, 'second'));
    http.expectOne(TRACKING.vehiclesPath).flush(envelope(0, 'second'));
    expect(store.vehicles()[0].revision).toBe(1);
    connection.updates$.next(update(100, 'first'));
    expect(store.vehicles()[0].revision).toBe(1);
    expect(store.connectionError()).toBeNull();
  });

  it('cancels stale REST requests on reconnect and resynchronizes missed updates', () => {
    store.start();
    const old = http.expectOne(TRACKING.vehiclesPath);
    connection.state$.next('reconnecting');
    expect(old.cancelled).toBeTrue();
    connection.state$.next('connected');
    http.expectOne(TRACKING.vehiclesPath).flush(envelope(6));
    expect(store.vehicles()[0].revision).toBe(6);
    store.stop();
    connection.updates$.next(update(7));
    expect(store.vehicles()[0].revision).toBe(6);
  });

  it('retries failed synchronization while retaining the newest buffered event', fakeAsync(() => {
    store.start();
    http.expectOne(TRACKING.vehiclesPath).flush('unavailable', { status: 503, statusText: 'Unavailable' });
    connection.updates$.next(update(4));
    expect(store.connectionError()).toBe(ERROR_MESSAGES.trackingSynchronizationFailed);
    tick(REALTIME.retryDelayMs);
    http.expectOne(TRACKING.vehiclesPath).flush(envelope(2));
    expect(store.vehicles()[0].revision).toBe(4);
    expect(store.connectionError()).toBeNull();
  }));

  it('times out hung synchronization and cancels retry timers on stop', fakeAsync(() => {
    store.start();
    const pending = http.expectOne(TRACKING.vehiclesPath);
    tick(TRACKING.requestTimeoutMs);
    expect(pending.cancelled).toBeTrue();
    expect(store.connectionError()).toBe(ERROR_MESSAGES.trackingSynchronizationFailed);
    store.stop();
    tick(REALTIME.retryDelayMs);
    http.expectNone(TRACKING.vehiclesPath);
  }));
});
