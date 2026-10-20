import { CommonModule } from '@angular/common';
import { Component, inject, Input, OnChanges, OnDestroy, signal } from '@angular/core';
import { Subscription, timeout } from 'rxjs';
import { MOVEMENT } from '../../core/constants/movement.constants';
import { TRACKING } from '../../core/constants/tracking.constants';
import { HistorySnapshot } from '../../core/models/movement';
import { TrackingApiService } from '../../core/services/tracking-api.service';

@Component({
  selector: 'app-history-viewer', standalone: true, imports: [CommonModule],
  templateUrl: './history-viewer.component.html',
  styleUrl: './history-viewer.component.css'
})
export class HistoryViewerComponent implements OnChanges, OnDestroy {
  @Input() vehicleId = '';
  @Input() sessionId?: string;
  private readonly api = inject(TrackingApiService);
  private request?: Subscription;
  readonly history = signal<HistorySnapshot | null>(null);
  readonly error = signal<string | null>(null);
  readonly loading = signal(false);

  ngOnChanges(): void {
    this.request?.unsubscribe();
    this.history.set(null);
    this.error.set(null);
    this.loading.set(false);
  }

  load(): void {
    if (this.loading() || !this.sessionId) {
      return;
    }
    this.loading.set(true);
    this.error.set(null);
    this.request = this.api.getHistory(this.vehicleId).pipe(timeout(TRACKING.requestTimeoutMs)).subscribe({
      next: history => {
        this.loading.set(false);

        if (history.sessionId !== this.sessionId) {
          this.error.set(MOVEMENT.historySessionChanged);
          this.history.set(null);
          return;
        }
        this.history.set(history);
      },
      error: () => { this.loading.set(false); this.error.set(MOVEMENT.historyFailed); }
    });
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
  }
}
