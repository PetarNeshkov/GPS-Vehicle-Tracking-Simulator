# Street-map update verification

Checked on 27 October 2026 on the existing Windows development machine, using
.NET SDK 10.0.402, Node 24.19.0, npm 11.17.0 and Chrome Headless 156.

## Build and automated checks

| Check | Result |
| --- | --- |
| `dotnet restore GPS-Vehicle-Tracking-Simulator.sln --locked-mode` | Passed. |
| Release solution build with `--no-restore` | Passed, zero warnings/errors. |
| Release tests with `--no-build --no-restore` | 86 passed, zero failed/skipped. |
| `npm --prefix src/gps-tracking-web ci` | Passed with the updated lockfile. |
| `npm --prefix src/gps-tracking-web run build` | Passed without warnings. |
| ChromeHeadless frontend tests, `--watch=false` | 30 passed. |

The new map tests use actual Leaflet layers and a local tile image. They cover
latitude/longitude ordering, geofence bounds, supplied heading, separate route
segments, omission of single-point lines, viewport preservation, empty-session
cleanup, tile failure/retry, safe text labels and lifecycle cleanup. Dashboard
tests continue to cover telemetry, membership, alerts and stop presentation.

Leaflet's ESM distribution is bundled with the application. The initial production
output is 525.88 kB (136.67 kB estimated transfer). The warning budget increased
from 500 to 550 kB to account for the street-map library and CSS; the 1 MB error
budget is unchanged. No CDN script is required at runtime.

## Browser and process checks

The three terminal commands now documented in the [setup guide](setup.md) started
the API, simulator and frontend.
A separate headless Chrome session exercised the running dashboard through the
Chrome DevTools Protocol. Desktop and 390-pixel viewport screenshots were inspected.

- API health and frontend-proxied health both returned `healthy`; the Development
  OpenAPI document loaded successfully.
- Two independent simulator vehicles supplied live samples. Street tiles loaded
  from OpenStreetMap; two markers, heading arrows, named geofences, independent
  route lines and provider attribution were visible. Speeding and geofence
  entry/exit alerts appeared during the real-time run.
- A manually selected center and zoom remained unchanged after a new sample.
  The overview button restored the bounds. At 390 pixels, the map resized to
  333 × 360 pixels and the page had no horizontal overflow.
- Blocking only the tile domain displayed the map failure message. GPS revisions
  continued advancing and both markers remained. Unblocking and clicking
  **Retry map** restored loaded tiles.
- Browser reload preserved the API session and restored vehicle data.
- Stopping only the simulator made both vehicles stale after the freshness window;
  their last positions remained visible. Restarting the API while the browser
  stayed open reconnected to an empty session, removed old markers/routes and
  restored the configured geofences.
- With a fresh API and the simulator stopped, `scripts/Verify-M5.ps1` passed:
  seven real-time observations produced two stop-separated routes. The running
  browser rendered exactly two Leaflet polylines matching the API's point lists,
  with no connecting line across the stop.

## Dependency notices and verification limits

On this date, `npm audit` reported four findings in development/build/test
dependencies: two high, one moderate and one low. `npm audit --omit=dev` reported
zero findings. Existing dependency versions were retained; the only added
packages are Leaflet, its TypeScript declarations and the transitive GeoJSON
declarations. No automatic force-upgrade was applied.

`npm ci` also emitted existing Karma dependency deprecation notices and notices
about optional native install scripts. Installation, build and tests completed
without approving additional scripts.

This verifies the current checkout on this machine, including reinstalling npm
dependencies from the lockfile. It does not establish fresh-machine or
cross-platform compatibility. Rider's Run button was not exercised in this pass;
the shared configurations and equivalent terminal commands were checked. The
full eleven-minute default demo was not repeated end to end; rule/integration
tests cover default scenarios. Public tile availability is external and may vary.

Use [README](../README.md) to repeat setup and [demo scenarios](demo.md) for the
visible acceptance walkthrough. Earlier milestone evidence remains in the
[historical implementation log](../GPS_TRACKING_IMPLEMENTATION_PLAN.md).
