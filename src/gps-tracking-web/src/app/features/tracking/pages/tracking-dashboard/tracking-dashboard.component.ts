import {Component, inject, OnDestroy, OnInit} from '@angular/core';
import { MOVEMENT } from '../../../../core/constants/movement.constants';
import { GEOFENCE } from '../../../../core/constants/geofence.constants';
import {TRACKING} from '../../../../core/constants/tracking.constants';
import {TrackingStore} from '../../../../core/services/tracking-store.service';

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

  protected geofenceName(id: string): string {
    return this.store.geofences().find(definition => definition.id === id)?.name ?? id;
  }
}
