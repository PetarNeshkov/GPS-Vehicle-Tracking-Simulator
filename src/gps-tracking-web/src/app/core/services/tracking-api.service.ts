import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { TRACKING } from '../constants/tracking.constants';
import { TrackingSnapshot } from '../models/tracking-events';

@Injectable({ providedIn: 'root' })
export class TrackingApiService {
  private readonly api = inject(ApiService);

  getSnapshots(): Observable<TrackingSnapshot> {
    return this.api.get<TrackingSnapshot>(TRACKING.vehiclesPath);
  }
}
