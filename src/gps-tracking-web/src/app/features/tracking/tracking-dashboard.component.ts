import {Component, inject, OnDestroy, OnInit} from '@angular/core';
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
  protected readonly colors = TRACKING.colors;

  ngOnInit(): void {
    this.store.start();
  }

  ngOnDestroy(): void {
    this.store.stop();
  }

  protected x(sample: GpsSample): number {
    const view = TRACKING.view;

    const longitudeOffset = sample.longitude - view.minLongitude;
    const longitudeRange = view.maxLongitude - view.minLongitude;
    const horizontalRatio = longitudeOffset / longitudeRange;

    return horizontalRatio * view.width;
  }

  protected y(sample: GpsSample): number {
    const view = TRACKING.view;

    const latitudeOffset = view.maxLatitude - sample.latitude;
    const latitudeRange = view.maxLatitude - view.minLatitude;
    const verticalRatio = latitudeOffset / latitudeRange;

    return verticalRatio * view.height;
  }

  protected markerTransform(sample: GpsSample): string {
    const x = this.x(sample);
    const y = this.y(sample);
    const heading = sample.heading % 360;

    return `translate(${x}, ${y}) rotate(${heading})`;
  }
}
