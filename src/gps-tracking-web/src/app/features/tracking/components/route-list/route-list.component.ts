import { CommonModule } from '@angular/common';
import { Component, computed, input, OnChanges, signal, SimpleChanges } from '@angular/core';
import { RouteSegment } from '../../../../core/models/movement';

type RouteFilter = 'all' | 'active' | 'completed';

@Component({
  selector: 'app-route-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './route-list.component.html',
  styleUrl: './route-list.component.css'
})
export class RouteListComponent implements OnChanges {
  readonly segments = input<readonly RouteSegment[]>([]);
  readonly sessionId = input<string>();
  private readonly requestedPage = signal(0);
  protected readonly pageSize = 5;
  protected readonly filter = signal<RouteFilter>('all');
  protected readonly activeCount = computed(() => this.segments().filter(route => !route.endedAt).length);
  protected readonly filteredRoutes = computed(() => this.segments()
    .filter(route => this.filter() === 'all' || (this.filter() === 'active' ? !route.endedAt : !!route.endedAt))
    .slice().sort((a, b) => Date.parse(b.startedAt) - Date.parse(a.startedAt)));
  protected readonly pageCount = computed(() => Math.max(1, Math.ceil(this.filteredRoutes().length / this.pageSize)));
  protected readonly currentPage = computed(() => Math.min(this.requestedPage(), this.pageCount() - 1));
  protected readonly firstIndex = computed(() => this.currentPage() * this.pageSize);
  protected readonly visibleRoutes = computed(() => this.filteredRoutes().slice(this.firstIndex(), this.firstIndex() + this.pageSize));

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['sessionId']) {
      this.requestedPage.set(0);
      this.filter.set('all');
    }
  }

  protected setFilter(filter: RouteFilter): void {
    this.filter.set(filter);
    this.requestedPage.set(0);
  }

  protected changePage(offset: number): void {
    this.requestedPage.set(Math.max(0, Math.min(this.pageCount() - 1, this.currentPage() + offset)));
  }
}
