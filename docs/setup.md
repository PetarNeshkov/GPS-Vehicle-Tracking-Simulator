# Technical setup and configuration

Local vehicle-tracking demonstration: an ASP.NET Core API, Angular dashboard and
separate .NET GPS simulator. Two vehicles send positions, speed and heading at
real-time intervals of at least 10 seconds. The dashboard displays them on an
OpenStreetMap street map, with speeding alerts, geofences and stop-separated routes.

For a quick start from any terminal, follow the [project README](../README.md).
This reference covers detailed setup, checks and configuration. An IDE is optional.

## Prerequisites

| Tool | Version |
| --- | --- |
| .NET SDK | 10.0.402, required by `global.json` (roll-forward and previews disabled) |
| Node.js | 24.19.0, recorded in `src/gps-tracking-web/.nvmrc` |
| npm | 11.17.0 |
| Angular / CLI | 22.2.3, installed locally by npm |
| TypeScript | 6.0.3, installed locally by npm |
| Chrome | Needed for the frontend headless tests |

No database, Docker, global Angular CLI, development certificate or map API key
is needed. Internet access is needed for dependency restoration and street-map
tiles. The API and simulator run locally; their processing does not depend on the
map service. The verified platform is Windows; see [verification](verification.md).

## Install and verify

Run from the repository root. Restore commands use the committed lockfiles.

```powershell
dotnet --version
node --version
npm --version
dotnet restore GPS-Vehicle-Tracking-Simulator.sln --locked-mode
dotnet build GPS-Vehicle-Tracking-Simulator.sln --configuration Release --no-restore
dotnet test GPS-Vehicle-Tracking-Simulator.sln --configuration Release --no-build --no-restore
npm --prefix src/gps-tracking-web ci
npm --prefix src/gps-tracking-web run build
npm --prefix src/gps-tracking-web test -- --watch=false --browsers=ChromeHeadless
```

The .NET build does not install or build the frontend. Run both build commands.
Frontend map tests use a local image, so they do not contact OpenStreetMap.

## Start in three terminals

Run one command per terminal, from the repository root:

```powershell
# Terminal 1: API
dotnet run --project src/GpsTracking.Api --launch-profile http
```

```powershell
# Terminal 2: simulator
dotnet run --project src/GpsTracking.Simulator --launch-profile Simulator
```

```powershell
# Terminal 3: dashboard
npm --prefix src/gps-tracking-web start
```

Open **http://localhost:4200**. The API listens at **http://localhost:5078**.
Stop each process with Ctrl+C. The hosts log startup, configuration failures and
HTTP failures in their terminals. Startup ordering is not required: the frontend
retries connection and the simulator retries with a fresh sample on its next cycle.

Alternatively, open `GPS-Vehicle-Tracking-Simulator.sln` in Rider, configure the
SDK/Node versions above, run `npm ci` once in `src/gps-tracking-web`, and select
**GPS Tracking — All**. The shared `.run/` compound starts the API, frontend and
simulator. Open the browser manually. Stop all three child configurations afterward.
Reload the solution if the shared configurations are not listed.

## Verify the running application

```powershell
Invoke-RestMethod http://localhost:5078/api/health
Invoke-RestMethod http://localhost:4200/api/health
Invoke-RestMethod http://localhost:5078/api/vehicles
```

Both health requests return `status: healthy`. The snapshot contains a `sessionId`,
two vehicles, global settings and geofences. After the simulator starts, both
vehicles acquire samples and update approximately every 10 seconds.

- Check the street background, two named markers, heading arrows and two labeled
  geofences. Drag or zoom the map; live updates preserve your chosen view.
  **Show all vehicles and routes** fits the current overlays into view.
- Stop only the simulator: after 30 seconds telemetry becomes stale, while the
  last positions remain visible. Connection status describes the API connection.
- Refresh the browser: the current API session retains settings, alerts and routes.
  Restart the API: reconnection loads a new session with default settings and
  clears old overlays. Restart API and simulator together for a repeatable demo.
- See [demonstration scenarios](demo.md) for speeding, geofence transitions,
  stops, separate routes, settings and reconnect checks (about eleven minutes).

The Development-only OpenAPI document is at
[http://localhost:5078/openapi/v1.json](http://localhost:5078/openapi/v1.json).
Live updates use SignalR at `/hubs/tracking`. REST supplies initial/reconnected
snapshots; the dashboard does not periodically poll positions. Vehicle history
and simulator profiles load on demand. The simulator reads its profile each cycle.

## Configuration

| Location | Purpose |
| --- | --- |
| `src/GpsTracking.Api/appsettings.json` | Two vehicle seeds, initial thresholds, global stop radius, rectangular geofences and `Frontend:Origin`. |
| `src/GpsTracking.Api/Properties/launchSettings.json` | Development API URL/port. |
| `src/GpsTracking.Simulator/appsettings.json` | `Simulator:BackendBaseUrl`, two matching `VehicleIds`, `TickIntervalSeconds`. |
| `src/gps-tracking-web/src/proxy.conf.cjs` | API and SignalR proxy target; override with `GPS_TRACKING_API_URL`. |
| `src/gps-tracking-web/src/app/core/constants/map.constants.ts` | Tile URL, provider attribution and maximum zoom; rebuild frontend after changes. |
| `src/GpsTracking.Simulator/Movement/DemoRoutes.cs` | Synthetic route coordinates. |
| `src/GpsTracking.Contracts/Tracking/SimulationProfile.cs` | Default per-vehicle simulation speeds and phase durations. |

Defaults: 80 km/h speed limit, 30-second speeding duration, 180-second stop duration,
and a **global** 10-metre stop radius. Dashboard edits apply to the current API
session. API restart restores startup defaults. Simulator controls edit speeds and
phase durations; a saved profile applies on the next successful simulator cycle.
The active phase restarts its timer while preserving position.

Both .NET hosts support standard environment overrides (`__` separates keys) and
command-line configuration, for example:

```powershell
dotnet run --project src/GpsTracking.Simulator --launch-profile Simulator -- --Simulator:TickIntervalSeconds=15
```

Built-in phases support 10–15-second intervals. Longer intervals require longer
phases; intervals below 10 seconds fail startup validation. The simulator reads
its copied output configuration, so rebuild after editing its source appsettings
before using `--no-build`. Keep vehicle IDs aligned between the two hosts.

When changing the API port, update its launch profile, the simulator backend URL
and the frontend proxy together. When changing the frontend port, also update
`Frontend:Origin`. Restart affected processes after changing startup configuration.

## Assumptions and limitations

- **Session-only storage:** history, alerts, settings and simulation profiles live
  in one API process. Browser refresh preserves them; API restart loses them.
  History and cumulative snapshots are unbounded, suitable for short local demos.
  UI pagination limits displayed rows, not retained data or network payloads.
- **Synthetic movement:** paths follow predefined coordinates and straight lines,
  without road routing or map matching. They can cross buildings on the street map.
  Speeds change instantly; the supplied speed is authoritative for speeding rules.
- **Observed time:** rules use accepted GPS timestamps. Four ten-second observations
  at 00/10/20/30 cover a 30-second speeding window. Missing observations are skipped,
  never buffered or replayed. A gap does not prove uninterrupted physical movement.
- **Stops and routes:** stops use distance from a fixed anchor, not reported speed.
  A completed route ends at stop onset; the next begins when movement resumes.
  Stationary samples remain in raw history. Changing settings reevaluates unfinished
  state; completed boundaries remain fixed.
- **Geofences:** rectangular boundaries include their edges. Three observations
  establish initial membership without an alert; two of the latest three confirm
  subsequent transitions. Membership can lag behind the current marker.
- **Clocks and restarts:** freshness uses backend receipt time and the browser clock,
  assuming synchronized clocks. Restarting only the simulator resets its scenario
  and can produce a jump in the existing API history.
- **Map service:** Leaflet 1.9.4 uses OpenStreetMap raster tiles directly from the
  browser, with visible attribution and normal browser caching. Internet access is
  needed for uncached tiles. Failure displays a retry message while overlays and
  telemetry keep updating. Public tiles have no availability guarantee; follow the
  [provider's usage policy](https://operations.osmfoundation.org/policies/tiles/).
  No bulk/offline download or tile proxy is implemented. A larger deployment should
  configure a suitable provider and its attribution.
- **Deployment scope:** local HTTP, no authentication, durable event queue or
  multi-instance coordination. Concurrent settings edits use last-write-wins.
  Hosted deployment is outside this delivery.

## Troubleshooting

| Symptom | Check |
| --- | --- |
| SDK not found | Install exactly 10.0.402; check `dotnet --version` from the repository root. |
| Port already in use | Stop the previous API/frontend process before starting another copy. |
| Waiting for GPS data | Start the simulator; inspect its logs, backend URL and vehicle IDs. |
| Connection error | Check API health, frontend proxy target, allowed origin and `/hubs/tracking` WebSocket traffic. |
| Street map unavailable | Allow browser access to `https://tile.openstreetmap.org`, then click **Retry map**. API health does not test the tile service. |
| ChromeHeadless cannot start | Install Chrome or set `CHROME_BIN` to its executable path. |
| Locked .NET build output | Stop the running Debug process, or build/test Release. |
| Frontend dependencies out of sync | Stop the frontend and rerun `npm --prefix src/gps-tracking-web ci`. |

## Further technical documentation

- [Architecture and rule decisions](architecture.md)
- [Demonstration scenarios](demo.md)
- [Current verification results and limits](verification.md)
- [Original requirements and historical implementation log](../GPS_TRACKING_IMPLEMENTATION_PLAN.md)

Optional local material (not included in this checkout):
[Подробно лично ръководство на български — PDF, HTML, Markdown и ZIP пакет](bg/README.md).
