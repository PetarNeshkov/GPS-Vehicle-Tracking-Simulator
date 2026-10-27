import { InjectionToken } from '@angular/core';

export const MAP_TILES = new InjectionToken('Map tile configuration', {
  providedIn: 'root',
  factory: () => ({
    url: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
    maxZoom: 19
  })
});
