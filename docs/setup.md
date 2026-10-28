# Configuration and verification

Use the [README](../README.md) to install dependencies and start the application
from a terminal. Any editor or IDE can be used. The optional **GPS Tracking — All**
configuration starts the same three applications in Rider.

## Runtime configuration

Paths below are relative to the repository root.

| File | What to configure |
| --- | --- |
| `src/GpsTracking.Api/appsettings.json` | Vehicle IDs/names, initial thresholds, global stop radius, rectangular geofences and allowed `Frontend:Origin`. |
| `src/GpsTracking.Api/Properties/launchSettings.json` | API address and Development environment for the `http` launch profile. |
| `src/GpsTracking.Simulator/appsettings.json` | `Simulator:BackendBaseUrl`, two matching `VehicleIds` and `TickIntervalSeconds`. |
| `src/gps-tracking-web/src/proxy.conf.cjs` | API and SignalR forwarding; backend override via `GPS_TRACKING_API_URL`. |
| `src/gps-tracking-web/src/app/core/constants/map.constants.ts` | Street-map tile URL, provider attribution and maximum zoom. Rebuild the frontend after changes. |
| `src/GpsTracking.Simulator/Movement/DemoRoutes.cs` | Synthetic route coordinates. |
| `src/GpsTracking.Contracts/Tracking/SimulationProfile.cs` | Default simulation speeds and phase durations. |

Defaults are **80 km/h**, **30 seconds of speeding**, **180 seconds stopped**,
and a **global 10-metre stop radius**. The API seeds two vehicles and two Sofia
rectangles. Dashboard changes apply to the current API session; restarting the
API restores startup defaults.

Keep vehicle IDs aligned between the API and simulator. If changing API port
5078, update its launch profile, simulator URL and frontend proxy together.
If changing frontend port 4200, also update the API's `Frontend:Origin`.
Restart affected processes after changing startup configuration.

Both .NET hosts accept standard environment overrides (`__` separates keys) and
command-line configuration, for example:

```sh
dotnet run --project src/GpsTracking.Simulator --launch-profile Simulator -- --Simulator:TickIntervalSeconds=15
```

Intervals below 10 seconds fail validation. Built-in phases support 10–15-second
intervals; longer intervals need longer phases. The simulator loads its copied
output configuration, so rebuild after editing its appsettings before using
`--no-build`. Invalid IDs, thresholds, bounds and URLs fail startup validation.

Simulation settings in the dashboard are separate from tracking thresholds.
A saved profile is applied on the next successful simulator reporting cycle.
A changed profile preserves position and restarts the current phase timer; it
does not send an extra sample. Failed profile reads retain the current profile.

## Health and API access

Open these URLs in a browser, or request them with an HTTP client:

| URL | Expected result |
| --- | --- |
| `http://localhost:5078/api/health` | API returns `{"status":"healthy"}`. |
| `http://localhost:4200/api/health` | The same response through the frontend proxy. |
| `http://localhost:5078/api/vehicles` | Session ID, two vehicle snapshots, global settings and geofences. |
| `http://localhost:5078/openapi/v1.json` | OpenAPI document; available only in Development. |

Live frontend updates use SignalR at `/hubs/tracking`. The dashboard reloads
REST snapshots after connecting/reconnecting. It does not poll vehicle positions.
Health checks verify the API, not the simulator or external tile service.

## Checks and verification

Run from the repository root. These checks are optional for viewing the demo.
Chrome is required only for frontend headless tests. If it is not discovered,
set `CHROME_BIN` to the Chrome executable path.

```sh
dotnet restore GPS-Vehicle-Tracking-Simulator.sln --locked-mode
dotnet build GPS-Vehicle-Tracking-Simulator.sln --configuration Release --no-restore
dotnet test GPS-Vehicle-Tracking-Simulator.sln --configuration Release --no-build --no-restore
npm --prefix src/gps-tracking-web ci
npm --prefix src/gps-tracking-web run build
npm --prefix src/gps-tracking-web test -- --watch=false --browsers=ChromeHeadless
```

The solution builds the .NET projects and tests. It also includes a frontend
project entry for IDE navigation, using the Visual Studio JavaScript SDK.
Angular dependencies and builds are managed separately by npm; the solution
does not install or build them automatically. Terminal startup requires no IDE.
NuGet and npm lockfiles preserve dependency versions. If a running process
locks build output, stop it before rebuilding.

Backend tests cover rule boundaries, settings reevaluation, API validation,
concurrency, service lifetimes and simulator failures. Frontend tests cover
snapshot merging, reconnects, forms, history and map presentation. Map tests
use a local image and do not request public tiles.

Verification used Windows, .NET SDK 10.0.402, Node 24.19.0,
npm 11.17.0 and Chrome Headless 156. The backend and frontend builds passed;
86 backend and 30 frontend tests passed. Browser checks verified real street tiles,
mobile layout, viewport preservation, tile failure/retry, live updates, browser
refresh, stale telemetry, API session reset and separate route lines.
The eleven-minute default scenario was not repeated end to end in that browser
pass; automated tests cover its rules. Fresh-machine, cross-platform and Rider
Run-button operation were not verified. Use the [demo guide](demo.md) for a
visible walkthrough on the presentation machine.

With the frontend IDE project entry restored, locked .NET
restore and both builds passed without warnings; all 86 backend and 30 frontend
tests passed again. Local documentation links were also checked. The frontend
remains part of the solution and can also be run independently through npm.

Dependency checks can be repeated with:

```sh
npm --prefix src/gps-tracking-web audit
npm --prefix src/gps-tracking-web audit --omit=dev
```

The recorded audit reported four findings in development/build/test
dependencies (two high, one moderate, one low) and zero with `--omit=dev`.
Installation also emitted existing dependency deprecation and optional native
install-script notices; installation, builds and tests completed successfully.

## External service and delivery scope

Street tiles load directly from OpenStreetMap in the browser. Leaflet JavaScript
and CSS are bundled locally. Visible attribution and normal browser caching are
retained. Follow the [tile usage policy](https://operations.osmfoundation.org/policies/tiles/);
public tiles have no availability guarantee. There is no bulk/offline download.
For a larger deployment, configure a suitable provider and its attribution.

This delivery is intended for short local sessions: HTTP, no authentication,
no database and no multi-instance coordination. State and history are unbounded
in API memory and disappear on API restart. Frontend pagination limits rendered
rows, not retained data or snapshot size. Browser/API clocks are assumed to be
synchronized for the 30-second freshness indicator. Failed GPS deliveries are
skipped, not replayed. See [architecture](architecture.md) for rule assumptions.

The source handoff consists of the solution, `src/`, `tests/`, documentation,
lockfiles and shared configuration. Generated `bin/`, `obj/`, `node_modules/`,
`dist/`, `.angular/`, coverage output and personal IDE settings are excluded by
`.gitignore`; they are not needed in a source package.
