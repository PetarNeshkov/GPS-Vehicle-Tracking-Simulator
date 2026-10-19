import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { TRACKING } from '../constants/tracking.constants';
import { TrackingSnapshot } from '../models/tracking-events';
import { VehicleUpdatedEvent } from '../models/tracking-events';
import { GlobalSettingsEvent, VehicleSettings } from '../models/tracking-settings';

@Injectable({ providedIn: 'root' })
export class TrackingApiService {
  private readonly api = inject(ApiService);

  updateVehicleSettings(id: string, settings: VehicleSettings): Observable<VehicleUpdatedEvent> {
    return this.api.put<VehicleUpdatedEvent>(TRACKING.vehicleSettingsPath(id), settings);
  }

  updateGlobalSettings(stopRadiusMeters: number): Observable<GlobalSettingsEvent> {
    return this.api.put<GlobalSettingsEvent>(TRACKING.settingsPath, { stopRadiusMeters });
  }

  getSnapshots(): Observable<TrackingSnapshot> {
    return this.api.get<TrackingSnapshot>(TRACKING.vehiclesPath);
  }
}
