import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TRACKING } from '../constants/tracking.constants';
import { ERROR_MESSAGES } from '../constants/error-messages.constants';
import { VehicleSnapshot } from '../models/vehicle-snapshot';
import { TrackingStore } from './tracking-store.service';

describe('TrackingStore', () => {
  let store: TrackingStore;
  let http: HttpTestingController;
  const snapshot: VehicleSnapshot = {
    id: 'vehicle-1', name: 'Vehicle 1', latestSample: null,
    lastReceivedAt: null, sampleCount: 0, revision: 0
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    store = TestBed.inject(TrackingStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => { store.stop(); http.verify(); });

  it('does not overlap requests and retains data through failure and recovery', fakeAsync(() => {
    store.start();
    store.start();
    tick(0);
    const first = http.expectOne(TRACKING.vehiclesPath);
    tick(TRACKING.pollingIntervalMs);
    http.expectNone(TRACKING.vehiclesPath);
    first.flush([snapshot]);
    expect(store.hasLoaded()).toBeTrue();

    tick(TRACKING.pollingIntervalMs);
    http.expectOne(TRACKING.vehiclesPath).flush('unavailable', { status: 503, statusText: 'Unavailable' });
    expect(store.vehicles()).toEqual([snapshot]);
    expect(store.connectionError()).toBe(ERROR_MESSAGES.trackingConnectionFailed);

    tick(TRACKING.pollingIntervalMs);
    const updated = { ...snapshot, revision: 1, sampleCount: 1 };
    http.expectOne(TRACKING.vehiclesPath).flush([updated]);
    expect(store.connectionError()).toBeNull();
    expect(store.vehicles()).toEqual([updated]);
    store.stop();
  }));

  it('times out a hung request and tries again on a subsequent poll', fakeAsync(() => {
    store.start();
    tick(0);
    const stalled = http.expectOne(TRACKING.vehiclesPath);
    tick(TRACKING.requestTimeoutMs);
    expect(stalled.cancelled).toBeTrue();
    expect(store.connectionError()).toBe(ERROR_MESSAGES.trackingConnectionFailed);
    tick(1_000);
    http.expectOne(TRACKING.vehiclesPath).flush([snapshot]);
    expect(store.connectionError()).toBeNull();
    store.stop();
  }));
});
