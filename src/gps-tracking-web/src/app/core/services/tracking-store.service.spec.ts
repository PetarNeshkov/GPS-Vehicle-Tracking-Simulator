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

  it('restores full state and independently merges reordered global and vehicle captures during reconnect', () => {
    const point = { timestamp: '2026-10-22T12:00:30Z', latitude: 42, longitude: 23, speedKmh: 95, heading: 90 };
    const alert = { id: 'speeding-1', vehicleId: 'vehicle-1', type: 'Speeding', occurredAt: point.timestamp,
      createdAt: point.timestamp, speedKmh: 95, speedLimitKmh: 80, speedingDurationSeconds: 30 };
    const settings = { speedLimitKmh: 80, speedingDurationSeconds: 30, stopDurationSeconds: 180 };
    const zone = { id: 'west', name: 'West', minLatitude: 41, maxLatitude: 43, minLongitude: 22, maxLongitude: 24 };
    const fullVehicle = (id: string, revision: number): VehicleSnapshot => ({
      ...vehicle(revision), id, latestSample: point, lastReceivedAt: point.timestamp, settings,
      alerts: id === 'vehicle-1' ? [alert] : [],
      geofenceMemberships: [{ geofenceId: 'west', state: 'Inside' }],
      movement: { state: 'Stopped', stopStartedAt: point.timestamp, stopElapsedSeconds: 180 },
      segments: [{ id: `${id}-route`, startedAt: point.timestamp, endedAt: point.timestamp, points: [point] }]
    });

    store.start();
    http.expectOne(TRACKING.vehiclesPath).flush({ sessionId: 'first', vehicles: [vehicle(1), { ...vehicle(1), id: 'vehicle-2' }] });
    connection.state$.next('reconnecting');
    connection.state$.next('connected');
    const pending = http.expectOne(TRACKING.vehiclesPath);
    connection.globalUpdates$.next({ sessionId: 'first', settings: { stopRadiusMeters: 30, revision: 3 },
      vehicles: [fullVehicle('vehicle-1', 6), fullVehicle('vehicle-2', 6)] });
    // An identical settings save keeps the global revision but captures newer vehicle state.
    connection.globalUpdates$.next({ sessionId: 'first', settings: { stopRadiusMeters: 30, revision: 3 },
      vehicles: [fullVehicle('vehicle-1', 9), fullVehicle('vehicle-2', 9)] });
    connection.globalUpdates$.next({ sessionId: 'first', settings: { stopRadiusMeters: 20, revision: 2 },
      vehicles: [fullVehicle('vehicle-1', 5), fullVehicle('vehicle-2', 5)] });
    const moving = { ...fullVehicle('vehicle-1', 10), movement: { state: 'Moving' as const, stopStartedAt: null, stopElapsedSeconds: 0 } };
    connection.updates$.next({ sessionId: 'first', vehicle: moving });
    pending.flush({ sessionId: 'first', globalSettings: { stopRadiusMeters: 10, revision: 1 },
      geofences: [zone], vehicles: [fullVehicle('vehicle-1', 4), fullVehicle('vehicle-2', 4)] });

    expect(store.vehicles()).toEqual([moving, fullVehicle('vehicle-2', 9)]);
    expect(store.globalSettings()).toEqual({ stopRadiusMeters: 30, revision: 3 });
    expect(store.geofences()).toEqual([zone]);
    expect(store.alerts()).toEqual([alert]);
    connection.updates$.next({ sessionId: 'first', vehicle: moving });
    connection.updates$.next({ sessionId: 'first', vehicle: fullVehicle('vehicle-1', 5) });
    expect(store.vehicles()[0]).toEqual(moving);
    expect(store.alerts()).toEqual([alert]);

    connection.state$.next('reconnecting');
    connection.state$.next('connected');
    connection.globalUpdates$.next({ sessionId: 'first', settings: { stopRadiusMeters: 50, revision: 99 }, vehicles: [moving] });
    http.expectOne(TRACKING.vehiclesPath).flush({ sessionId: 'new', vehicles: [vehicle(0)],
      geofences: [], globalSettings: { stopRadiusMeters: 10, revision: 0 } });
    expect(store.vehicles()).toEqual([vehicle(0)]);
    expect(store.alerts()).toEqual([]);
    expect(store.geofences()).toEqual([]);
    expect(store.globalSettings()?.stopRadiusMeters).toBe(10);
  });

  it('applies both movement snapshots from radius updates without overwriting newer vehicle revisions', () => {
    store.start();
    http.expectOne(TRACKING.vehiclesPath).flush({ sessionId: 'first', vehicles: [vehicle(1), { ...vehicle(1), id: 'vehicle-2' }] });
    const stopped = { state: 'Stopped' as const, stopStartedAt: '2026-10-20T12:00:00Z', stopElapsedSeconds: 180 };
    connection.globalUpdates$.next({ sessionId: 'first', settings: { stopRadiusMeters: 20, revision: 1 },
      vehicles: [{ ...vehicle(2), movement: stopped, segments: [] }, { ...vehicle(2), id: 'vehicle-2', movement: stopped, segments: [] }] });
    expect(store.vehicles().every(item => item.movement?.state === 'Stopped')).toBeTrue();
    connection.updates$.next({ sessionId: 'first', vehicle: { ...vehicle(3), movement: { ...stopped, state: 'Moving' } } });
    connection.globalUpdates$.next({ sessionId: 'first', settings: { stopRadiusMeters: 20, revision: 1 },
      vehicles: [{ ...vehicle(2), movement: stopped }] });
    expect(store.vehicles()[0].movement?.state).toBe('Moving');
    connection.state$.next('reconnecting');
    connection.state$.next('connected');
    http.expectOne(TRACKING.vehiclesPath).flush({ ...envelope(0, 'second'), vehicles: [{ ...vehicle(0), segments: [], movement: { ...stopped, state: 'Unknown' } }] });
    expect(store.vehicles()[0].segments).toEqual([]);
    expect(store.vehicles()[0].movement?.state).toBe('Unknown');
  });

  it('recovers geofence membership and alerts on reconnect and replaces definitions after restart', () => {
    const zone = { id: 'west', name: 'West', minLatitude: 1, maxLatitude: 2, minLongitude: 1, maxLongitude: 2 };
    const alert = { id: 'entry', vehicleId: 'vehicle-1', type: 'GeofenceEntered',
      occurredAt: '2026-10-19T12:00:40Z', createdAt: '2026-10-19T12:00:41Z',
      speedKmh: null, speedLimitKmh: null, speedingDurationSeconds: null, geofenceId: 'west', geofenceName: 'West' };
    store.start();
    http.expectOne(TRACKING.vehiclesPath).flush({ ...envelope(0), geofences: [zone] });
    connection.state$.next('reconnecting');
    connection.state$.next('connected');
    const inside = { ...vehicle(5), alerts: [alert], geofenceMemberships: [{ geofenceId: 'west', state: 'Inside' as const }] };
    connection.updates$.next({ sessionId: 'first', vehicle: inside });
    http.expectOne(TRACKING.vehiclesPath).flush({ sessionId: 'first', vehicles: [inside], geofences: [zone] });
    expect(store.geofences()).toEqual([zone]);
    expect(store.alerts()).toEqual([alert]);
    expect(store.vehicles()[0].geofenceMemberships?.[0].state).toBe('Inside');

    connection.state$.next('reconnecting');
    connection.state$.next('connected');
    const unknown = { ...vehicle(0), geofenceMemberships: [{ geofenceId: 'east', state: 'Unknown' }] };
    http.expectOne(TRACKING.vehiclesPath).flush({ sessionId: 'second', vehicles: [unknown], geofences: [{ ...zone, id: 'east' }] });
    connection.updates$.next({ sessionId: 'first', vehicle: inside });
    expect(store.geofences()[0].id).toBe('east');
    expect(store.alerts()).toEqual([]);
    expect(store.vehicles()[0].geofenceMemberships?.[0].state).toBe('Unknown');
  });

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
