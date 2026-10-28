import { CommonModule } from '@angular/common';
import { Component, computed, ElementRef, inject, Input, OnChanges, OnDestroy, signal, ViewChild } from '@angular/core';
import { HISTORY } from '../../../../core/constants/history.constants';
import { Subscription, timeout } from 'rxjs';
import { MOVEMENT } from '../../../../core/constants/movement.constants';
import { TRACKING } from '../../../../core/constants/tracking.constants';
import { HistorySnapshot } from '../../../../core/models/movement';
import { TrackingApiService } from '../../../../core/services/tracking-api.service';

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
  readonly page = signal(0);
  readonly total = computed(() => this.history()?.samples.length ?? 0);
  readonly pageCount = computed(() => Math.max(1, Math.ceil(this.total() / HISTORY.pageSize)));
  readonly visibleSamples = computed(() => {
    const samples = this.history()?.samples ?? [];
    const end = Math.max(0, samples.length - this.page() * HISTORY.pageSize);
    const start = Math.max(0, end - HISTORY.pageSize);

    return samples.slice(start, end).reverse();
  });
  @ViewChild('historyTable') private historyTable?: ElementRef<HTMLElement>;

  ngOnChanges(): void {
    this.request?.unsubscribe();
    this.history.set(null);
    this.error.set(null);
    this.loading.set(false);
    this.page.set(0);
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
        this.goToPage(0);
      },
      error: () => { this.loading.set(false); this.error.set(MOVEMENT.historyFailed); }
    });
  }

  ngOnDestroy(): void {
    this.request?.unsubscribe();
  }

  goToPage(page: number): void {
    const lastPage = this.pageCount() - 1;
    this.page.set(Math.max(0, Math.min(page, lastPage)));

    if (this.historyTable) {
      this.historyTable.nativeElement.scrollTop = 0;
    }
  }
}
