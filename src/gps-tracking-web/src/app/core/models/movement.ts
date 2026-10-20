import { GpsSample } from './vehicle-snapshot';

export type MovementState = 'Unknown' | 'Moving' | 'PossibleStop' | 'Stopped';
export interface Movement {
  readonly state: MovementState;
  readonly stopStartedAt: string | null;
  readonly stopElapsedSeconds: number;
}
export interface RouteSegment {
  readonly id: string;
  readonly startedAt: string;
  readonly endedAt: string | null;
  readonly points: readonly GpsSample[];
}
export interface HistorySnapshot {
  readonly sessionId: string;
  readonly vehicleId: string;
  readonly revision: number;
  readonly samples: readonly GpsSample[];
}
