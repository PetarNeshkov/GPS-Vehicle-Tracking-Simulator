import {
  AfterViewInit, Component, ElementRef, inject, Input, NgZone, OnChanges, OnDestroy,
  signal, ViewChild
} from '@angular/core';
import * as L from 'leaflet/dist/leaflet-src.esm.js';
import { MAP_TILES } from '../../../../core/constants/map.constants';
import { MOVEMENT } from '../../../../core/constants/movement.constants';
import { TRACKING } from '../../../../core/constants/tracking.constants';
import { Geofence } from '../../../../core/models/geofence';
import { VehicleSnapshot } from '../../../../core/models/vehicle-snapshot';

@Component({
  selector: 'app-tracking-map',
  standalone: true,
  templateUrl: './tracking-map.component.html',
  styleUrl: './tracking-map.component.css'
})
export class TrackingMapComponent implements AfterViewInit, OnChanges, OnDestroy {
  @Input() vehicles: readonly VehicleSnapshot[] = [];
  @Input() geofences: readonly Geofence[] = [];
  @ViewChild('mapContainer', { static: true }) private container!: ElementRef<HTMLDivElement>;

  protected readonly tilesUnavailable = signal(false);
  private readonly tileConfig = inject(MAP_TILES);
  private readonly zone = inject(NgZone);
  private map?: L.Map;
  private tiles?: L.TileLayer;
  private overlays?: L.FeatureGroup;
  private resizeObserver?: ResizeObserver;
  private readonly failedTiles = new Set<HTMLElement>();

  ngAfterViewInit(): void {
    this.zone.runOutsideAngular(() => {
      this.map = L.map(this.container.nativeElement, { scrollWheelZoom: false });
      this.tiles = L.tileLayer(this.tileConfig.url, {
        attribution: this.tileConfig.attribution,
        maxZoom: this.tileConfig.maxZoom
      });
      this.tiles.on('tileerror', event => this.updateTileStatus(event as L.TileErrorEvent, true));
      this.tiles.on('tileload tileunload', event => this.updateTileStatus(event as L.TileEvent, false));
      this.tiles.addTo(this.map);
      this.overlays = L.featureGroup().addTo(this.map);
      this.renderOverlays();
      this.fitOverview();
      this.resizeObserver = new ResizeObserver(() => this.map?.invalidateSize({ pan: false }));
      this.resizeObserver.observe(this.container.nativeElement);
    });
  }

  ngOnChanges(): void {
    this.zone.runOutsideAngular(() => this.renderOverlays());
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
    this.tiles?.off();
    this.map?.remove();
    this.failedTiles.clear();
    this.map = undefined;
  }

  protected fitOverview(): void {
    const bounds = this.overlays?.getBounds();
    const view = TRACKING.view;
    this.map?.fitBounds(bounds?.isValid() ? bounds : [
      [view.minLatitude, view.minLongitude], [view.maxLatitude, view.maxLongitude]
    ], { padding: [35, 35], maxZoom: 15, animate: false });
  }

  protected retryTiles(): void {
    this.failedTiles.clear();
    this.tilesUnavailable.set(false);
    this.tiles?.redraw();
  }

  private updateTileStatus(event: L.TileEvent, failed: boolean): void {
    if (failed) {
      this.failedTiles.add(event.tile);
    }
    else {
      this.failedTiles.delete(event.tile);
    }

    this.zone.run(() => this.tilesUnavailable.set(this.failedTiles.size > 0));
  }

  private renderOverlays(): void {
    if (!this.overlays) {
      return;
    }

    this.overlays.clearLayers();

    for (const fence of this.geofences) {
      L.rectangle([[fence.minLatitude, fence.minLongitude], [fence.maxLatitude, fence.maxLongitude]], {
        color: '#42794a', weight: 2, dashArray: '6 4', fillOpacity: 0.12,
        className: 'geofence-region'
      }).bindTooltip(this.label(fence.name), { permanent: true, direction: 'center' }).addTo(this.overlays);
    }

    for (const [index, vehicle] of this.vehicles.entries()) {
      const color = TRACKING.colors[index % TRACKING.colors.length];
      for (const segment of vehicle.segments ?? []) {
        if (segment.points.length < MOVEMENT.minimumLinePoints) {
          continue;
        }

        L.polyline(segment.points.map(point => [point.latitude, point.longitude] as L.LatLngTuple), {
          color, weight: 3, opacity: 0.7, className: 'route-line'
        }).bindTooltip(this.label(`${vehicle.name}: ${segment.endedAt ? 'Completed route' : 'Route in progress'}`))
          .addTo(this.overlays);
      }

      const sample = vehicle.latestSample;
      if (!sample) {
        continue;
      }

      const heading = sample.heading % 360;
      const icon = document.createElement('span');
      icon.className = 'tracking-map-marker';
      icon.style.color = color;
      const arrow = document.createElement('span');
      arrow.className = 'tracking-map-heading';
      arrow.style.transform = `rotate(${heading}deg)`;
      icon.append(arrow);
      const description = `${vehicle.name}: ${sample.speedKmh} km/h, heading ${heading}°`;
      L.marker([sample.latitude, sample.longitude], {
        icon: L.divIcon({ html: icon, className: 'tracking-map-icon', iconSize: [38, 38], iconAnchor: [19, 19] }),
        title: description, alt: description, keyboard: true, riseOnHover: true
      }).bindTooltip(this.label(vehicle.name), { permanent: true, direction: 'right', offset: [22, 0] })
        .bindPopup(this.label(description)).addTo(this.overlays);
    }
  }

  // Leaflet treats strings as HTML; names supplied by the API must remain plain text.
  private label(text: string): HTMLElement {
    const element = document.createElement('span');
    element.textContent = text;
    return element;
  }
}
