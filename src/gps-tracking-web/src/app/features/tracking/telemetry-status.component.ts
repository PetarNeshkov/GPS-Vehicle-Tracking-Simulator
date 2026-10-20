import { Component, computed, input, OnDestroy, OnInit, signal } from '@angular/core';
import { interval, Subscription } from 'rxjs';
import { FRESHNESS } from '../../core/constants/freshness.constants';

@Component({
  selector: 'app-telemetry-status',
  standalone: true,
  templateUrl: './telemetry-status.component.html',
  styleUrl: './telemetry-status.component.css'
})
export class TelemetryStatusComponent implements OnInit, OnDestroy {
  readonly lastReceivedAt = input<string | null>(null);
  private readonly now = signal(Date.now());
  private clock?: Subscription;

  protected readonly labels = FRESHNESS.labels;
  protected readonly ageSeconds = computed(() => {
    const receivedAt = this.lastReceivedAt();
    if (!receivedAt) {
      return null;
    }

    const receivedTime = Date.parse(receivedAt);
    if (!Number.isFinite(receivedTime)) {
      return null;
    }

    const elapsedMilliseconds = Math.max(0, this.now() - receivedTime);

    return Math.floor(elapsedMilliseconds / FRESHNESS.millisecondsPerSecond);
  });

  protected readonly stale = computed(() => {
    const age = this.ageSeconds();

    return age !== null && age >= FRESHNESS.staleAfterSeconds;
  });

  ngOnInit(): void {
    this.clock = interval(FRESHNESS.clockIntervalMs).subscribe(() => this.now.set(Date.now()));
  }

  ngOnDestroy(): void {
    this.clock?.unsubscribe();
  }
}
