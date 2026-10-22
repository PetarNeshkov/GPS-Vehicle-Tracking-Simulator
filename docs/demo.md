# Assessment demonstration

## Prepare and start

Follow the [README prerequisites and restore/build commands](../README.md) first.
The three applications need no database, Docker, map key or hosted service.
Internet access is needed to restore dependencies; the running demo uses local
HTTP and a simplified coordinate map.

In Rider, open `GPS-Vehicle-Tracking-Simulator.sln`, select **GPS Tracking — All**
and press Run. This compound starts **GPS Tracking API**, **GPS Tracking Frontend**
and **GPS Tracking Simulator**. Open `http://localhost:4200` manually. If the
configuration is missing, reload the solution and check that the shared `.run/`
files are present. Set Rider's Node interpreter to the README version and run
`npm ci` before the first launch. Terminal alternatives are in the README.

Start with a fresh API and simulator for repeatable timings. Stop all three
processes from an earlier run before launching another copy. API restart clears
all in-memory history, settings and simulation profiles. Simulator restart alone
resets its route position, so restart both for a clean demonstration.

## Default demonstration: about eleven minutes

Leave the defaults unchanged: 80 km/h limit, 30 seconds of speeding, 180 seconds
stopped and a global 10-metre stop radius. Timings below are approximate, measured
from simulator startup; delivery delays and ten-second sampling affect them.

| When | Show and explain |
| --- | --- |
| First 30 seconds | Two independently updating markers, supplied speed/heading, observation counts and GPS timestamps. Initial geofence membership settles after three observations without an entry alert. |
| During driving | Rectangles and entry/exit alerts. Two of the latest three observations confirm membership, so it may lag behind the marker. |
| Around 2:30 | Vehicle 1's speeding alert: observations at 00, 10, 20 and 30 seconds of the episode must all exceed the limit. Continuing the episode does not repeat the alert. |
| Around 3:30–6:30 | Vehicle 1 becomes a possible stop, then a confirmed stop while Vehicle 2 continues moving. Stop detection uses distance from a fixed anchor, not reported speed. |
| Around 6:00 | Vehicle 2's independent speeding alert. |
| Around 7:10 | Vehicle 1 resumes and starts a new route. The completed route ends at the stop-onset sample, not three minutes later. A new route line needs two points. |
| Around 10:00–10:40 | Vehicle 2 confirms its stop and resumes into its next route. |

Expand **Routes** and **Raw GPS history**. Load history, move between its pages
and refresh it after more samples arrive. Stationary observations remain in raw
history; they do not connect separate driven routes. The history table displays
20 rows per page in a bounded scroll region. Alert history also scrolls and
paginates rather than extending the page indefinitely.

## Settings and recovery: after the default demonstration

1. Change Vehicle 1's speed limit while its current observations qualify. Saving
   reevaluates retained GPS data immediately; alert times use the latest GPS time,
   not the time the Save button was clicked. Vehicle 2 retains its own thresholds.
2. Change the global stop radius and save. This setting applies to both vehicles;
   completed routes remain fixed. Restore 10 metres afterward.
3. Expand Vehicle 1's **Simulator controls**, load its settings and change cruising
   speed. The API stores the profile immediately; the simulator applies it on its
   next successful reporting cycle. The current phase restarts its timer while
   preserving position. If currently stopped or speeding, cruising speed becomes
   visible when a cruising phase runs. Vehicle 2 remains independent.
4. Each successful save displays a confirmation for 15 seconds. Unsaved edits
   remain intact while telemetry arrives. A simulator-profile confirmation means
   stored by the API, not acknowledged by an offline simulator.
5. Refresh the browser: current settings, alerts and routes return. Stop only the
   simulator: after 30 seconds, telemetry becomes stale while the API connection
   stays connected. Missing samples do not imply a new stop.
6. Restart the API with the browser open. The connection recovers into a new,
   empty session with default settings. Start the simulator again for fresh data.
   Failed deliveries are skipped, not replayed. Stop all three apps when finished.

For a shorter stop-only demonstration, the optional `scripts/Verify-M5.ps1`
walkthrough in README uses a fresh API and frontend with the simulator stopped.
It changes the stop duration to 20 seconds; it is not the default 180-second demo.

## Explain the design

- Domain contains plain C# rules. Application contains command/query handlers and
  interfaces; Infrastructure supplies in-memory storage; API composes them and
  handles HTTP/SignalR. Controllers delegate business decisions.
- Lightweight CQRS separates reads and writes without MediatR, a broker or separate
  databases. Shared stores coordinate atomic snapshots; scoped handlers use them.
- Simulator-to-API communication is HTTP. SignalR sends frontend updates, with REST
  snapshots for initial load and reconnect. Session IDs and revisions prevent old
  captures from replacing newer state.
- Local in-memory storage keeps the assessment simple. It resets on API restart;
  there is no ORM, authentication or durable history. Pagination bounds rendered
  rows, not retained data or cumulative network payloads. Gaps in observations do
  not prove continuous physical movement. See [architecture](architecture.md).

## Delivery evidence

The M8 entry in the [progress log](../GPS_TRACKING_IMPLEMENTATION_PLAN.md) records
the actual clean-source build, test and runtime checks. A clean source copy on the
existing Windows machine does not establish fresh-machine or cross-platform
compatibility. Rider configuration inspection and terminal startup do not establish
that Rider's Run button was exercised; perform that final IDE smoke check locally.
