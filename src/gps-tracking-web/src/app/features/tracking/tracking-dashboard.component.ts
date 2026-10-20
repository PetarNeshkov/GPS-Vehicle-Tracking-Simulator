import {Component, inject, OnDestroy, OnInit} from '@angular/core';
import { MOVEMENT } from '../../core/constants/movement.constants';
import { RouteSegment } from '../../core/models/movement';
import { GEOFENCE } from '../../core/constants/geofence.constants';
import {TRACKING} from '../../core/constants/tracking.constants';
import {GpsSample} from '../../core/models/vehicle-snapshot';
import {TrackingStore} from '../../core/services/tracking-store.service';

@Component({
  selector: 'app-tracking-dashboard',
  standalone: false,
  templateUrl: './tracking-dashboard.component.html',
  styleUrl: './tracking-dashboard.component.css'
})
export class TrackingDashboardComponent implements OnInit, OnDestroy {
  protected readonly store = inject(TrackingStore);
  protected readonly movement = MOVEMENT;
  protected readonly geofence = GEOFENCE;
  protected readonly colors = TRACKING.colors;

  ngOnInit(): void {
    this.store.start();
  }

  ngOnDestroy(): void {
    this.store.stop();
  }

  protected x(longitude: number): number {
    const view = TRACKING.view;

    const longitudeOffset = longitude - view.minLongitude;
    const longitudeRange = view.maxLongitude - view.minLongitude;
    const horizontalRatio = longitudeOffset / longitudeRange;

    return horizontalRatio * view.width;
  }

  protected y(latitude: number): number {
    const view = TRACKING.view;

    const latitudeOffset = view.maxLatitude - latitude;
    const latitudeRange = view.maxLatitude - view.minLatitude;
    const verticalRatio = latitudeOffset / latitudeRange;

    return verticalRatio * view.height;
  }

  protected segmentPoints(segment: RouteSegment): string {
    return segment.points.map(point => `${this.x(point.longitude)},${this.y(point.latitude)}`).join(' ');
  }

  protected geofenceName(id: string): string {
    return this.store.geofences().find(definition => definition.id === id)?.name ?? id;
  }

  protected markerTransform(sample: GpsSample): string {
    const x = this.x(sample.longitude);
    const y = this.y(sample.latitude);
    const heading = sample.heading % 360;

    return `translate(${x}, ${y}) rotate(${heading})`;
  }
}
