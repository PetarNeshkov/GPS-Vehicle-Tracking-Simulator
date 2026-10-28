# Presentation walkthrough

## Prepare and start

Follow the [README setup steps](../README.md) first.
The three applications need no database, Docker, map key or hosted service.
Internet access is needed to restore dependencies and load OpenStreetMap tiles.
The API and simulator use local HTTP. The street background uses Leaflet and
OpenStreetMap, with no API key. Synthetic paths are not matched to roads.

Start the API, simulator and dashboard using the README terminal commands, then
open `http://localhost:4200`. If you use Rider, the optional **GPS Tracking — All**
configuration starts the same three applications together. The demonstration
below works with either startup method.

Start with a fresh API and simulator for repeatable timings. Stop all three
processes from an earlier run before launching another copy. API restart clears
all in-memory history, settings and simulation profiles. Simulator restart alone
resets its route position, so restart both for a clean demonstration.

## Default demonstration: about eleven minutes

First check the street-map background and visible OpenStreetMap attribution.
Drag to pan, use the zoom controls, and click **Show all vehicles and routes**
to restore the overview. Telemetry updates must preserve a manually chosen view.
Markers show supplied heading; separate route lines must not join across stops.
The two labeled rectangles are synthetic geofences, not administrative boundaries.

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

For a shorter stop demonstration, lower Vehicle 1's stop duration to 20 seconds
before its stationary phase starts around 3:30. It should confirm a stop around
3:50; the default simulator still resumes around 7:00. Use the full default
scenario above when presenting the original 180-second threshold.

Optionally verify map failure separately from API connectivity by blocking
`https://tile.openstreetmap.org/*` in browser Network tools and panning to
uncached tiles. The map should show a failure message while telemetry continues.
Remove the block and click **Retry map**. Do not disable all networking, which
would also disconnect the API.

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

For repeatable build/test commands and the limits of completed verification,
see [configuration and verification](setup.md#checks-and-verification).
