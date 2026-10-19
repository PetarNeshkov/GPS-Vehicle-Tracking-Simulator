import { fakeAsync, flushMicrotasks, TestBed, tick } from '@angular/core/testing';
import { HubConnection } from '@microsoft/signalr';
import { TRACKING_CONNECTION_FACTORY, TrackingConnectionService } from './tracking-connection.service';
import { REALTIME } from '../constants/realtime.constants';

describe('TrackingConnectionService', () => {
  let service: TrackingConnectionService;
  let hub: jasmine.SpyObj<HubConnection>;
  beforeEach(() => {
    hub = jasmine.createSpyObj<HubConnection>('hub', ['start', 'stop', 'on', 'off', 'onreconnecting', 'onreconnected', 'onclose']);
    hub.start.and.returnValue(Promise.resolve());
    hub.stop.and.returnValue(Promise.resolve());
    TestBed.configureTestingModule({ providers: [
      { provide: TRACKING_CONNECTION_FACTORY, useValue: () => hub }
    ] });
    service = TestBed.inject(TrackingConnectionService);
  });
  afterEach(() => service.stop());

  it('retries initial failure and registers the event handler before connecting only once', fakeAsync(() => {
    hub.start.and.returnValues(Promise.reject(new Error('offline')), Promise.resolve());
    const states: string[] = [];
    service.state$.subscribe(state => states.push(state));
    service.start(); service.start();
    flushMicrotasks();
    expect(hub.on.calls.count()).toBe(1);
    expect(hub.on).toHaveBeenCalledBefore(hub.start);
    expect(states.at(-1)).toBe('reconnecting');
    tick(REALTIME.retryDelayMs);
    expect(hub.start.calls.count()).toBe(2);
    expect(states.at(-1)).toBe('connected');
    service.stop();
    expect(hub.off.calls.mostRecent().args[0]).toBe(REALTIME.vehicleUpdatedEvent);
    expect(hub.stop).toHaveBeenCalled();
  }));

  it('ignores late start completion after stop and forwards reconnect lifecycle', fakeAsync(() => {
    let resolveStart!: () => void;
    hub.start.and.returnValue(new Promise<void>(resolve => resolveStart = resolve));
    const states: string[] = [];
    service.state$.subscribe(state => states.push(state));
    service.start();
    service.stop();
    resolveStart(); flushMicrotasks();
    expect(states.at(-1)).toBe('stopped');
    hub.start.and.returnValue(Promise.resolve());
    service.start(); flushMicrotasks();
    hub.onreconnecting.calls.mostRecent().args[0](new Error('offline'));
    expect(states.at(-1)).toBe('reconnecting');
    hub.onreconnected.calls.mostRecent().args[0]('new-connection');
    expect(states.at(-1)).toBe('connected');
    service.stop();
  }));
});
