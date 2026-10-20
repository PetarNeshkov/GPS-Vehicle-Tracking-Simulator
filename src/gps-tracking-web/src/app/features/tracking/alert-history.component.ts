import { CommonModule } from '@angular/common';
import { Component, computed, input, OnChanges, signal, SimpleChanges } from '@angular/core';
import { ALERTS } from '../../core/constants/alerts.constants';
import { GEOFENCE } from '../../core/constants/geofence.constants';
import { TrackingAlert } from '../../core/models/tracking-settings';
import { VehicleSnapshot } from '../../core/models/vehicle-snapshot';

@Component({
  selector: 'app-alert-history',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './alert-history.component.html',
  styleUrl: './alert-history.component.css'
})
export class AlertHistoryComponent implements OnChanges {
  readonly alerts = input<readonly TrackingAlert[]>([]);
  readonly sessionId = input<string>();
  readonly vehicles = input<readonly VehicleSnapshot[]>([]);

  private readonly requestedPage = signal(0);

  protected readonly geofence = GEOFENCE;
  protected readonly labels = ALERTS.labels;
  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.alerts().length / ALERTS.pageSize)));
  protected readonly currentPage = computed(() => Math.min(this.requestedPage(), this.pageCount() - 1));
  protected readonly visibleAlerts = computed(() => {
    const start = this.currentPage() * ALERTS.pageSize;

    return this.alerts().slice(start, start + ALERTS.pageSize);
  });

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['sessionId']) {
      this.requestedPage.set(0);
    }
  }

  protected previousPage(): void {
    this.requestedPage.set(Math.max(0, this.currentPage() - 1));
  }

  protected nextPage(): void {
    this.requestedPage.set(Math.min(this.pageCount() - 1, this.currentPage() + 1));
  }

  protected vehicleName(id: string): string {
    const vehicle = this.vehicles().find(item => item.id === id);

    return vehicle?.name ?? id;
  }
}
