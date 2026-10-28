# GPS Vehicle Tracking Simulator

A local demo of two vehicles moving around Sofia. Watch their positions and
headings on a real map, receive speeding and geofence alerts, and inspect routes
separated by stops. The application includes an Angular dashboard, an ASP.NET Core
API and a separate GPS simulator.

## Run the project

You need the .NET SDK, Node.js, npm and a terminal. You can use any editor or IDE.
No database, Docker, map API key or global Angular CLI is required. Internet access
is needed to download dependencies and display the street map.

> The repository currently enforces .NET SDK **10.0.402** in `global.json`,
> Node.js **24.19.x** and npm **11.17.x** in the frontend configuration.
> Use those versions for this checkout. Angular and TypeScript are installed
> automatically with the project dependencies.

From the repository root, install the dependencies:

```sh
dotnet restore GPS-Vehicle-Tracking-Simulator.sln --locked-mode
npm --prefix src/gps-tracking-web ci
```

Then open three terminals at the repository root and run one command in each:

```sh
# Terminal 1: API
dotnet run --project src/GpsTracking.Api --launch-profile http
```

```sh
# Terminal 2: GPS simulator
dotnet run --project src/GpsTracking.Simulator --launch-profile Simulator
```

```sh
# Terminal 3: dashboard
npm --prefix src/gps-tracking-web start
```

Wait for the frontend to finish compiling, then open **http://localhost:4200**.
The two vehicles should appear after the simulator sends its first samples.
For subsequent launches, run the same three startup commands. Repeat `npm ci`
when frontend dependencies change. Stop each process with **Ctrl+C**; restart
all three for a fresh demo.

### Optional Rider shortcut

If you use Rider, open `GPS-Vehicle-Tracking-Simulator.sln` after installing the
dependencies above, select **GPS Tracking — All**, and click **Run**. This shared
configuration starts the API, dashboard and simulator together. Open
**http://localhost:4200** manually and stop all three applications in Rider when done.
If the configuration is missing, reload the solution and check that `.run/` is present.

## What to try

- **Live map:** follow both vehicles, pan and zoom, or click
  **Show all vehicles and routes** to return to the overview.
- **Alerts:** watch speeding and geofence entry/exit events appear automatically.
  Positions update approximately every 10 seconds; the first default speeding
  alert appears around 2½ minutes after simulator startup.
- **Settings:** change each vehicle's speed limit and timing thresholds, or the
  shared stop radius. Simulator controls change the simulated speeds and durations.
- **Routes and history:** expand a vehicle's sections to inspect its observations
  and routes. A confirmed stop separates consecutive routes.

The [demo walkthrough](docs/demo.md) covers the full default scenario in about
11 minutes, including both vehicles stopping and resuming.

## Assumptions to keep in mind

- Data and settings are held in API memory. Refreshing the browser preserves them;
  restarting the API clears them. No database setup is needed.
- Vehicle paths are synthetic and may cross buildings or leave roads. The map
  provides geographic context; it does not calculate driving directions.
- The map background requires internet access. If tiles cannot load, tracking
  continues and the dashboard offers **Retry map**.
- Rules use received GPS observations and their timestamps. Failed submissions
  are skipped rather than replayed; missing data does not imply a vehicle stopped.
- Geofence membership uses two of the latest three observations, so it can lag
  behind the marker. Stops use distance from a fixed anchor, not reported speed.
- This is a local demonstration without authentication or durable storage.
  History is retained for the current session without a size limit.

## Troubleshooting

| Problem | What to check |
| --- | --- |
| SDK or Node version error | Run `dotnet --version`, `node --version` and `npm --version` from the repository root and compare with the requirements above. |
| Port already in use | Stop previous API/frontend instances before starting again. Ports 5078 and 4200 must be available. |
| Waiting for GPS data | Check that the simulator is running and inspect its terminal output for errors. |
| Connection error | Check the API terminal output. Its health endpoint is `http://localhost:5078/api/health`. |
| Street map unavailable | Check browser access to `https://tile.openstreetmap.org`, then click **Retry map**. |

## Technical reference

- [Detailed setup, configuration and test commands](docs/setup.md)
- [Architecture and rule decisions](docs/architecture.md)
- [Checks and verification notes](docs/setup.md#checks-and-verification)
- [OpenAPI document](http://localhost:5078/openapi/v1.json), available while the API runs in Development
