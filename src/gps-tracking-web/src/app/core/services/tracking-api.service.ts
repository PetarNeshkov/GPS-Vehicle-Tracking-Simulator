import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { TRACKING } from '../constants/tracking.constants';
import { VehicleSnapshot } from '../models/vehicle-snapshot';

@Injectable({ providedIn: 'root' })
export class TrackingApiService {
  private readonly api = inject(ApiService);

  getSnapshots(): Observable<readonly VehicleSnapshot[]> {
    return this.api.get<readonly VehicleSnapshot[]>(TRACKING.vehiclesPath);
  }
}
