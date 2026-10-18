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
    return (sample.longitude - view.minLongitude) / (view.maxLongitude - view.minLongitude) * view.width;
  }

  protected y(sample: GpsSample): number {
    const view = TRACKING.view;
    return (view.maxLatitude - sample.latitude) / (view.maxLatitude - view.minLatitude) * view.height;
  }

  protected markerTransform(sample: GpsSample): string {
    return `translate(${this.x(sample)}, ${this.y(sample)}) rotate(${sample.heading % 360})`;
  }
}
