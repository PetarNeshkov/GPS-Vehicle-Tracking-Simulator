# GPS Vehicle Tracking Simulator

M4 adds geofence detection and visualization, alongside speeding alerts and live settings, to the simulator, API and Angular dashboard. Two vehicles independently
follow deterministic routes, supplying speed and heading at intervals of at least
10 seconds. Angular receives live vehicle snapshots and displays a simplified
coordinate map. REST loads initial/reconnected state; regular polling is removed.
Tracking state lives in the backend process.

## Prerequisites

| Tool | Selected version |
| --- | --- |
| .NET SDK | 10.0.402, selected by `global.json` |
| Node.js | 24.19.0, recorded in `src/gps-tracking-web/.nvmrc` |
| npm | 11.17.0 |
| Angular / CLI / build tooling | 22.2.3, installed locally by npm |
| TypeScript | 6.0.3, installed locally by npm |
| Chrome | Required only for the existing headless frontend test |

No global Angular CLI, database, Docker, map key or development certificate is
needed. Use the exact SDK above: preview SDK selection and roll-forward are disabled.

## Restore and build

From the repository root:

```powershell
dotnet --version
dotnet restore GPS-Vehicle-Tracking-Simulator.sln --locked-mode
dotnet build GPS-Vehicle-Tracking-Simulator.sln --no-restore
dotnet test GPS-Vehicle-Tracking-Simulator.sln --no-restore
cd src/gps-tracking-web
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
cd ../..
```

The .NET build does not install or build the frontend. Run both build commands.
NuGet and npm lockfiles are committed; generated output and IDE state are ignored.

## Run in three terminals

Run these from the repository root, one command per terminal:

```powershell
# Terminal 1: backend
dotnet run --project src/GpsTracking.Api --launch-profile http
```

```powershell
# Terminal 2: simulator
dotnet run --project src/GpsTracking.Simulator --launch-profile Simulator
```

```powershell
# Terminal 3: frontend
npm --prefix src/gps-tracking-web start
```

Open `http://localhost:4200`. The page shows **GPS Vehicle Tracking Simulator**.
The API listens at `http://localhost:5078`. Stop each process with Ctrl+C.

Both hosts validate configuration at startup and emit standard lifecycle/error
logs. The simulator sends an initial sample, then waits at least 10 seconds between
reports. Failed deliveries are logged and skipped; later samples continue normally.
The dashboard retains its latest data during connection failures and recovers on
reconnection and REST synchronization. Connection status does not indicate whether
the simulator is running; stopping it leaves the latest positions visible.

Verify the API directly and through the frontend proxy:

```powershell
Invoke-RestMethod http://localhost:5078/api/health
Invoke-RestMethod http://localhost:4200/api/health
```

Both return `status: healthy`. The development OpenAPI document is available at
`http://localhost:5078/openapi/v1.json`. `/hubs/tracking` carries live updates through
the frontend proxy. `GET /api/vehicles` now returns `{ sessionId, vehicles }` rather
than a bare array; both REST snapshots and events identify the backend session.

## Rider

1. Open `GPS-Vehicle-Tracking-Simulator.sln` and allow project loading/restore.
2. Ensure Rider uses the .NET CLI from the installed SDK location and Node
   24.19.0. The repository SDK file selects 10.0.402.
3. Run `npm ci` in `src/gps-tracking-web` once before the first launch.
4. Select **GPS Tracking — All** in the Run configuration dropdown and
   click Run. The shared compound configuration starts the API's `http` profile
   and Angular's `npm start`, plus the simulator's `Simulator` profile in parallel.
   Open `http://localhost:4200` manually.
5. Vehicles appear after the simulator successfully delivers its first samples,
   then update approximately every 10 seconds.
6. Use Rider's Stop controls for **GPS Tracking API** and **GPS Tracking Frontend**
   and **GPS Tracking Simulator** to stop all three processes.

The shared configurations live in `.run/` and use project-relative paths and
Rider's configured Node interpreter. If they do not appear immediately, reload
the solution. The individual API, frontend and simulator configurations remain available.
No startup ordering is required: frontend connection attempts retry until the API is ready,
and a failed initial simulator delivery is followed by a fresh sample on its next interval.
Avoid starting a second copy while ports 5078 or 4200 are already in use.

### Verify movement and live synchronization in Rider

1. Start API and frontend first: both cards should be waiting for GPS data.
   `GET http://localhost:5078/api/vehicles` returns a session ID and two empty snapshots.
2. Start the simulator: two colored markers and cards appear, with speeds of 42
   and 66 km/h. After roughly 10 seconds coordinates, timestamps and sample counts
   change independently. The arrows use supplied heading.
3. Refresh the browser: sample counts and latest positions remain in the backend.
4. Stop the API: the dashboard shows a connection error and retains its last data.
   Restart it: SignalR reconnects and REST replaces the old session; fresh samples
   are accepted even though their revisions restart from zero.
5. Stop the simulator and restart the API to inspect the empty session reliably.
6. Run `GpsTracking.Tests` through Rider's test runner. Run the frontend test
   command above in Rider's terminal; Chrome is required.
7. In browser Network tools, inspect `/hubs/tracking` and its `VehicleUpdated`
   messages. There should be one REST snapshot request after connection, another
   after reconnection, and no periodic two-second snapshot requests. Failed REST
   synchronization retries after two seconds until successful.

Run `npm ci` after pulling M3 to install Angular Forms alongside the SignalR client. Restart the API
and frontend together because the snapshot response shape changed. If a running
Debug process locks build output, stop it before rebuilding, or run tests with
`--configuration Release`.

### Demonstrate M3 speeding and settings

1. Start **GPS Tracking — All** and open the dashboard. Each vehicle card has
   independent speed/duration settings; the global stop radius appears once below.
2. For Vehicle 1 (42 km/h), save a 40 km/h limit and 30-second speeding duration.
   Once retained qualifying observations cover 30 seconds, one alert appears.
   If enough history already exists, reevaluation can alert immediately on save.
3. Continued speeding produces no duplicate alerts. Save a limit above 42 km/h to
   invalidate the current episode, then lower it again to demonstrate reevaluation.
   Previously generated alerts remain visible with their original thresholds.
4. For the exact 20-to-15-second case, start a fresh backend session and set Vehicle
   1's limit to 40 before starting the simulator. At three samples (roughly 20 seconds
   coverage), change duration from 30 to 15. The alert uses the third sample's GPS time.
5. Refresh the browser or disconnect/reconnect: saved settings and alert history
   return from REST. Restarting the backend resets both to a new session.

Settings updates use last-write-wins ordering; newer revisions protect displayed
state. Unsaved form edits survive live updates. Stop duration and global radius
now reevaluate unfinished stop/route state immediately; completed boundaries remain fixed.

### Demonstrate M4 geofencing

1. Rebuild and start **GPS Tracking — All** in Rider. Open `http://localhost:4200`.
   Restart both the API and frontend so their expanded snapshot/alert contracts match.
2. The coordinate map shows the two labeled Sofia rectangles behind the markers.
   Each vehicle card lists its confirmed membership in each zone.
3. Initially membership is **Determining membership**. After three GPS samples,
   the backend establishes Inside/Outside without creating an initial alert.
4. Let the existing routes run. Both vehicles enter and leave both zones during a
   loop; automated route checks cover a ten-minute simulation at ten-second steps.
   No speed settings changes are needed to demonstrate geofencing.
5. Entry/exit requires two of the latest three observed positions to support the
   new state. The confirmed membership may lag behind the displayed marker. An
   edge counts as inside; no crossing is inferred between observed positions.
6. Entry and exit alerts show the zone name and confirming GPS time. Refresh or
   reconnect to recover the same history without duplicates. Restarting the API
   starts a new session with unknown memberships and no alerts.

`GET /api/geofences` returns the session's predefined rectangles. The same definitions
are included in `GET /api/vehicles`, so the dashboard needs no extra polling request.
Geofences are startup configuration, not editable settings. The CQRS handlers in
Application separate reads from writes without MediatR or a separate database.

### Demonstrate M5 stops and separate routes

The normal simulator routes keep moving. For a focused stop demonstration, start
the **GPS Tracking API** and **GPS Tracking Frontend** configurations separately in
Rider with a fresh API session and leave the simulator stopped. Open
`http://localhost:4200`, then run from the repository root:

```powershell
./scripts/Verify-M5.ps1
```

The script sets Vehicle 1's stop duration to 20 seconds and sends seven observations
at real ten-second intervals (about one minute). It moves, stays at one position,
then resumes. Watch Possible stop become Stopped. The completed route ends at the
first stationary point; the next route starts at the resumption point. Its line
appears only after a second point arrives. The script verifies two routes and seven
raw observations. Expand **Routes** and **Raw GPS history** in the vehicle card;
history loads on demand and labels its captured revision. Refresh it for newer data.

Defaults remain 180 seconds per vehicle and a global 10-metre radius. GPS time and
distance from a fixed anchor determine stops; reported speed does not. Settings
changes reevaluate the latest unfinished candidate. Completed routes never reopen.
If shrinking the radius excludes the latest point of a confirmed stop, movement
resumes at that latest observed point. An initially stationary vehicle has no
completed driven route. A candidate stop keeps an open route until confirmation.

`GET /api/vehicles/{id}/history` includes every accepted sample; `/segments` returns
separate route point lists. Both responses include session ID and vehicle revision.
Unknown vehicles return 404. Refresh/reconnect preserves routes; API restart clears
the session. The map remains a simplified coordinate view. Full simulator scenarios
and further UI refinement remain M6.

## Configuration

Demo routes and speeds are defined in
`src/GpsTracking.Simulator/Movement/DemoRoutes.cs`. Each loop advances by actual
elapsed time, carrying distance across vertices of its closed local path.

| Location | Settings |
| --- | --- |
| `src/GpsTracking.Api/appsettings.json` | Two vehicle seeds and their initial settings; two rectangular Sofia demo zones; permitted frontend origin. |
| `src/GpsTracking.Api/Properties/launchSettings.json` | Development API address, port 5078. |
| `src/GpsTracking.Simulator/appsettings.json` | Backend URL, two matching vehicle IDs and reporting interval. |
| `src/gps-tracking-web/src/proxy.conf.cjs` | `/api/**` and `/hubs/**` forwarding; backend override via `GPS_TRACKING_API_URL`. |

Each vehicle starts with 80 km/h, 30-second speeding duration and 180-second stop
duration. The evaluator confirmed a **global** 10-metre default stop radius.
Radius is configured under `Tracking:GlobalSettings:StopRadiusMeters`; it is no
longer a vehicle setting. M5 uses it for spatial stop detection. The synthetic geofences are
Sofia West (latitude 42.695–42.700, longitude 23.315–23.320) and Sofia East
(latitude 42.695–42.700, longitude 23.325–23.330). M4 evaluates these rectangles using the documented three-observation majority.

ASP.NET Core and simulator configuration support standard .NET environment
variables (`__` separates keys) and command-line overrides. For example:

```powershell
dotnet run --project src/GpsTracking.Simulator -- --Simulator:TickIntervalSeconds=15
```

The simulator loads configuration from its output directory, so project-root and
Rider launches use the same copied file. Edit its source appsettings and rebuild
before using `--no-build`. Keep simulator IDs aligned with API seed IDs.

Invalid configuration fails startup with a specific message. This example must
exit with an error rather than start (it does not modify the configuration file):

```powershell
dotnet run --project src/GpsTracking.Simulator -- --Simulator:TickIntervalSeconds=1
```

If changing the API port, update its launch profile, simulator backend URL and
frontend proxy target together. If changing the frontend port, update the API's
`Frontend:Origin` as well. Restart processes after startup configuration changes;
the dashboard's live settings update the current in-memory session immediately.
The startup configuration supplies defaults again after backend restart.

## Architecture, status and limitations

- [Project boundaries and decisions](docs/architecture.md)
- [Requirements, assumptions, milestones and actual verification log](GPS_TRACKING_IMPLEMENTATION_PLAN.md)

The evaluator confirmed the global stop radius, routes ending at the start of a
confirmed stop and restarting when movement resumes, and full elapsed speeding
windows (four observations at 00/10/20/30 seconds). Speeding, geofences and stop/route
rules are implemented. The fixed stop anchor and geofence initialization details remain
documented implementation defaults. Delivery is local; simulator buffering and
replay after failed deliveries are explicitly out of scope.
Tracking state is session-only and local to one backend instance. Raw history is
retained without a cap for this short demo. No
persistence, broker, authentication, hosted deployment or map provider is included.

`npm ci` currently reports upstream deprecation notices from the retained Karma
stack and notices for optional native dependency scripts. The selected Windows
setup passed installation, production build, headless tests and dev-server startup
without enabling additional install scripts. These notices are not suppressed.
