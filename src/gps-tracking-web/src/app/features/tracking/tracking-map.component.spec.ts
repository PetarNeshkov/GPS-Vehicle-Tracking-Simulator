import { ComponentFixture, TestBed } from '@angular/core/testing';
import * as L from 'leaflet/dist/leaflet-src.esm.js';
import { MAP_TILES } from '../../core/constants/map.constants';
import { VehicleSnapshot } from '../../core/models/vehicle-snapshot';
import { TrackingMapComponent } from './tracking-map.component';
import { TEST_MAP_TILES } from './tracking-map.test-config';

describe('TrackingMapComponent', () => {
  let fixture: ComponentFixture<TrackingMapComponent>;
  const point = (latitude: number, longitude: number, heading = 90) => ({
    latitude, longitude, heading, speedKmh: 42, timestamp: '2026-10-27T12:00:00Z'
  });
  const vehicle = (overrides: Partial<VehicleSnapshot> = {}): VehicleSnapshot => ({
    id: 'one', name: 'First', latestSample: point(42.697, 23.318),
    lastReceivedAt: null, sampleCount: 1, revision: 1, ...overrides
  });

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [TrackingMapComponent],
      providers: [{ provide: MAP_TILES, useValue: TEST_MAP_TILES }] });
    fixture = TestBed.createComponent(TrackingMapComponent);
  });

  afterEach(() => fixture.destroy());

  it('uses geographic coordinates and keeps stop-separated routes independent', () => {
    fixture.componentRef.setInput('vehicles', [vehicle({ segments: [
      { id: 'a', startedAt: '2026-10-27T12:00:00Z', endedAt: '2026-10-27T12:01:00Z',
        points: [point(42.697, 23.312), point(42.697, 23.314)] },
      { id: 'b', startedAt: '2026-10-27T12:02:00Z', endedAt: null,
        points: [point(42.698, 23.320), point(42.698, 23.322)] },
      { id: 'c', startedAt: '2026-10-27T12:03:00Z', endedAt: null, points: [point(42.699, 23.330)] }
    ] })]);
    fixture.componentRef.setInput('geofences', [{ id: 'west', name: 'West zone',
      minLatitude: 42.695, maxLatitude: 42.700, minLongitude: 23.315, maxLongitude: 23.320 }]);
    fixture.detectChanges();
    const layers = fixture.componentInstance['overlays']!.getLayers();
    const fence = layers.find(layer => layer instanceof L.Rectangle) as L.Rectangle;
    expect(fence.getBounds().getSouthWest()).toEqual(L.latLng(42.695, 23.315));
    expect(fence.getBounds().getNorthEast()).toEqual(L.latLng(42.700, 23.320));
    const routes = layers.filter(layer => layer instanceof L.Polyline && !(layer instanceof L.Polygon)) as L.Polyline[];
    expect(routes.length).toBe(2);
    expect(routes[0].getLatLngs()).toEqual([L.latLng(42.697, 23.312), L.latLng(42.697, 23.314)]);
    expect(routes[1].getLatLngs()).toEqual([L.latLng(42.698, 23.320), L.latLng(42.698, 23.322)]);
    const marker = layers.find(layer => layer instanceof L.Marker) as L.Marker;
    expect(marker.getLatLng()).toEqual(L.latLng(42.697, 23.318));
    expect(fixture.nativeElement.querySelector('.tracking-map-heading').style.transform).toBe('rotate(90deg)');
  });

  it('preserves the user viewport on live updates and clears old overlays on a session reset', () => {
    fixture.componentRef.setInput('vehicles', [vehicle()]);
    fixture.detectChanges();
    const map = fixture.componentInstance['map']!;
    map.setView([42.72, 23.35], 16, { animate: false });
    fixture.componentRef.setInput('vehicles', [vehicle({ latestSample: point(42.698, 23.321, 180), revision: 2 })]);
    fixture.detectChanges();
    expect(map.getCenter().lat).toBeCloseTo(42.72, 4);
    expect(map.getCenter().lng).toBeCloseTo(23.35, 4);
    expect(map.getZoom()).toBe(16);
    expect(fixture.nativeElement.querySelectorAll('.tracking-map-marker').length).toBe(1);
    expect(fixture.nativeElement.querySelector('.tracking-map-heading').style.transform).toBe('rotate(180deg)');
    fixture.nativeElement.querySelector('.map-toolbar button').click();
    expect(map.getCenter().lat).toBeCloseTo(42.698, 4);
    expect(map.getZoom()).toBeLessThanOrEqual(15);
    fixture.componentRef.setInput('vehicles', [vehicle({ latestSample: null, segments: [] })]);
    fixture.componentRef.setInput('geofences', []);
    fixture.detectChanges();
    expect(fixture.componentInstance['overlays']!.getLayers().length).toBe(0);
  });

  it('keeps telemetry visible during tile failures and provides a retry', () => {
    fixture.componentRef.setInput('vehicles', [vehicle()]);
    fixture.detectChanges();
    const tiles = fixture.componentInstance['tiles']!;
    const failedTile = document.createElement('img');
    tiles.fire('tileerror', { tile: failedTile });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="status"]').textContent).toContain('Street map unavailable');
    expect(fixture.nativeElement.querySelector('.tracking-map-marker')).not.toBeNull();
    fixture.componentRef.setInput('vehicles', [vehicle({ latestSample: point(42.698, 23.321), revision: 2 })]);
    fixture.detectChanges();
    const marker = fixture.componentInstance['overlays']!.getLayers()[0] as L.Marker;
    expect(marker.getLatLng()).toEqual(L.latLng(42.698, 23.321));
    const redraw = spyOn(tiles, 'redraw').and.callThrough();
    fixture.nativeElement.querySelector('.map-error button').click();
    fixture.detectChanges();
    expect(redraw).toHaveBeenCalled();
    expect(fixture.nativeElement.querySelector('.map-error')).toBeNull();
    tiles.fire('tileerror', { tile: failedTile });
    tiles.fire('tileload', { tile: document.createElement('img') });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.map-error')).not.toBeNull();
    tiles.fire('tileunload', { tile: failedTile });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.map-error')).toBeNull();
  });

  it('renders names as text and releases the map when destroyed', () => {
    fixture.componentRef.setInput('vehicles', [vehicle({ name: '<img src=x onerror=alert(1)>' })]);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.leaflet-tooltip').textContent).toContain('<img src=x');
    expect(fixture.nativeElement.querySelector('.leaflet-tooltip img')).toBeNull();
    const map = fixture.componentInstance['map']!;
    const remove = spyOn(map, 'remove').and.callThrough();
    const disconnect = spyOn(fixture.componentInstance['resizeObserver']!, 'disconnect').and.callThrough();
    fixture.destroy();
    expect(remove).toHaveBeenCalledTimes(1);
    expect(disconnect).toHaveBeenCalledTimes(1);
  });
});
