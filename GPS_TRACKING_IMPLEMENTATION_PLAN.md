# GPS Vehicle Tracking Simulator — implementation handoff

## Provenance and scope

Reconstructed on 18 October 2026 from the handoff previously read in this session
and the evaluator clarifications. The original Downloads file was no longer
available when M0 began. This is a maintained project handoff, not a verbatim copy
of that file. Later evaluator answers take precedence over these assumptions.

Build a practical two-week assessment in ASP.NET Core and Angular. Work on one
milestone at a time, verify it, record actual outcomes, and stop for review.
Do not implement subsequent milestones without a request. Preserve unrelated work.

## Confirmed requirements

| Topic | Confirmed behavior |
| --- | --- |
| Vehicles | Two independently moving vehicles in real time. |
| Sampling | No more frequently than every 10 seconds, with rare exceptions allowed. Use 10 seconds by default. |
| Telemetry | Simulator supplies vehicle ID, coordinates, generation timestamp, speed in km/h and heading from 0 to 360 degrees. |
| Authority | Supplied speed drives speeding alerts; heading is primarily visual. Do not replace speed with a coordinate-derived value. |
| Stops | Detect remaining within a globally configurable radius, default 10 metres, not speed equal to zero. |
| Speeding | Require full elapsed duration with all observations strictly above the threshold: 00, 10, 20 and 30 seconds for a 30-second window. One alert per continuous episode, rearmed after speeding breaks. Defaults: 80 km/h and 30 seconds. |
| Segments | Split after a sufficiently long stop, default 180 seconds. A stop beginning at 12:00 and confirmed at 12:03 ends the previous route at the 12:00 sample; movement resuming at 12:05 starts the next route at the 12:05 sample. Preserve stationary samples in raw history. A line needs at least two points. |
| Geofences | Predefined rectangles; observed GPS points only. Evaluator suggested confirmation by two of the latest three positions. No interpolated crossings required. |
| Time | GPS generation timestamps drive rules. Assume strictly newer timestamps arrive in order. |
| Settings | Speed limit, speeding duration and stop duration are per vehicle. Stop radius is global. Retain live editing and immediate reevaluation of retained observations. |
| Example | After 20 seconds of speeding, changing duration from 30 to 15 seconds immediately alerts with the latest GPS timestamp. |
| UI | Two vehicles, movement and tracking details, history, alerts and configurable thresholds. A simplified visualization is acceptable. |
| Demo | Reliably demonstrate geofencing, speeding and stops/segmentation. |
| Architecture | ASP.NET Core, Angular, a separate .NET simulator, HTTP ingestion and SignalR updates are accepted. Component design remains part of the evaluation. |
| Delivery | Local application is sufficient; external services are permitted but not required. |
| Persistence | Optional. Docker and LocalDB are acceptable; Docker would require startup instructions. |
| Simulator outages | Buffering and replay of failed deliveries are explicitly out of scope. |

Persistence and automated tests are optional in the assessment. Clean Architecture
and CQRS are considerations, not requirements to add frameworks. Focused rule tests
are planned because timestamp and state-transition behavior matters.

## Implementation defaults — distinct from evaluator confirmations

The three previously pending questions are resolved in the requirements above.
The following details remain documented implementation choices:

- Equality: speed equal to the limit is not speeding; distance equal to the stop
  radius is within it; rectangle edges count as inside.
- Stop candidates use a fixed anchor, resetting when the vehicle leaves its
  radius. Do not infer a stop by comparing only consecutive positions.
- Geofences use a rolling majority of three observations. Establish initial
  membership after three samples without an alert; timestamp a transition with
  the confirming sample.
- Preserve historical alerts and completed segments after settings changes.
  Reevaluate the pending stop/current unfinished segment and speeding suffix.
  If new settings invalidate an active speeding episode's qualifying state,
  rearm it; otherwise preserve the latch and avoid duplicate alerts.
- Apply live editing to the global radius as well: reevaluate both vehicles'
  pending stop state in M5. Serialize the update with sample processing, preserve
  completed segments and publish updated snapshots after state changes complete.
- Insufficient retained time coverage cannot produce a duration alert.
- Mark stale after 30 seconds of receipt silence, separately from movement.
  GPS time drives business durations; server receipt time may drive staleness.
- Keep the demo session in memory. Backend restart resets settings, rule state
  and history; browser refresh must not. Session retention is not a production
  memory-management policy.
- Use deterministic routes, realistic progression and dwell durations. Shorten
  configurable rule durations for demos rather than accelerating GPS timestamps.
- Retain in-memory storage; no ORM, database, Docker or external service is needed.
  Log and skip failed simulator deliveries, then continue with fresh samples.
  Frontend resynchronization cannot recover samples the backend never received.

## Architecture and contracts

See [architecture decisions](docs/architecture.md) for current boundaries and
versions. Domain has no dependencies; Application references Domain;
Infrastructure references Application/Domain; API composes these plus Contracts;
Simulator references Contracts only. SignalR types stay in API. Introduce real
interfaces only when needed, such as storage and publication in later milestones.

Contracts use UTC `DateTimeOffset`/ISO 8601 timestamps and explicit units.
Validate finite coordinate/speed/heading values, bounds, nonnegative speed,
positive durations/radius and known vehicle IDs. Heading 360 is accepted.
Raw observations are independent from derived route membership.

Endpoints (ingestion, snapshots, alerts, settings and the live hub are implemented
through M5, including history and segments):

- `POST /api/vehicles/{id}/samples`
- `GET /api/vehicles`
- `GET /api/vehicles/{id}/history` and `/segments`
- `GET /api/alerts` and `/api/geofences`
- `GET`/`PUT /api/vehicles/{id}/settings`
- `GET`/`PUT /api/settings` for global settings, initially `StopRadiusMeters`
- `/hubs/tracking` for `VehicleUpdated` and `GlobalSettingsUpdated` events

M2 changes `GET /api/vehicles` from an array to `{ sessionId, vehicles }`. Events
contain `{ sessionId, vehicle }`. The session UUID belongs to one backend lifetime;
frontend revision comparisons reset when REST synchronization identifies a new
session. Simulator POST bodies are unchanged. Update frontend and backend together.

M3 moves `StopRadiusMeters` out of `VehicleSettings` and the two vehicle seed
settings into global tracking settings, defaulting to 10. The radius must be finite
and positive and appears once in a global UI section. Per-vehicle settings retain
speed limit, speeding duration and stop duration. Spatial reevaluation becomes
functional in M5.

Process samples and settings under one shared store gate in M5. Store accepted
observations, rule changes and alerts together in memory, increment a monotonic
vehicle revision, take an immutable snapshot, then publish outside the gate.
Do not expose mutable collections. Settings reevaluation does not wait for a
new sample. Event occurrence uses GPS time; creation time uses server time.

REST is authoritative for refresh/reconnect. Connect and buffer live updates,
then fetch snapshots, apply newer revisions and deduplicate alerts by stable ID.
Resynchronize history/alerts on reconnect. Failed publication must not roll back
accepted telemetry. Do not promise durable delivery or add retry infrastructure.

## Milestones and completion checks

| Milestone | Scope | Completion check |
| --- | --- | --- |
| M0 | Tooling, project references, config, seeds and startup docs. | Backend/frontend build; simulator starts; boundaries valid; run instructions verified. |
| M1 | Telemetry DTOs, in-memory storage, ingestion, simulator routes, snapshots and basic Angular view. | Two independent vehicles update every 10 seconds with supplied speed/heading; browser restart preserves backend session. |
| M2 | SignalR, revisions, snapshot handover and reconnect. | Live updates and resynchronization without stale overwrites. |
| M3 | Speeding rules, alerts, per-vehicle settings and global settings model/API/UI; remove per-vehicle radius. | Full elapsed window, one alert per episode, interruption/rearming, immediate 20-to-15-second example; radius appears once globally. |
| M4 | Geofencing and visualization. | Initial membership without alert; two-of-three confirmed entry/exit; edges defined. |
| M5 | Spatial stops using the global radius, confirmed segment boundaries, history APIs and settings reevaluation. | Stop confirmed at 12:03 closes at the 12:00 sample; next route starts at 12:05 on movement; global radius changes reevaluate both vehicles; completed history preserved. |
| M6 | Deterministic scenarios, validation and UI refinement. | All scenarios repeatable; global radius clearly distinguished from independent per-vehicle thresholds. |
| M7 | Integration, concurrency/reconnect checks and architectural documentation. | Global settings updates coordinate with both vehicles' samples; outage recovery without replay; no duplicate events. |
| M8 | Local clean checkout verification and demonstration. | Another developer can run all three apps locally using Rider or terminal instructions; no hosted deployment required. |

Target ten working days: M0 on day 1, M1 days 1–2, M2 day 3, M3 day 4,
M4 day 5, M5 days 6–7, M6 day 8, M7 day 9, M8 day 10. Drop optional map
integration, Docker, persistence and elaborate animation before required behavior.

Tests accompany rules using synthetic timestamps: threshold equality, exact time
coverage, interruptions, episode deduplication, independent vehicles, geofence
majorities/edges, fixed-anchor drift, brief/prolonged stops, resume behavior,
settings reevaluation and completed-history preservation. Later integration
checks cover concurrent settings/samples and reconnect synchronization.

Explicit clarification acceptance checks:

- No speeding alert at 20 seconds; alert at 30 seconds if all four samples exceed
  the limit. A sample equal to or below the limit interrupts the episode.
- Confirmation at 12:03 closes the previous route at the 12:00 sample. The next
  route starts at the 12:05 resumption sample, without a stationary connecting
  line between routes. Stationary observations remain in raw history.
- One global radius applies to both vehicles; speed and duration settings stay
  independent. Updating settings leaves completed historical segments unchanged.
- Simulator delivery failures produce no replay backlog; fresh samples resume
  normally when the backend becomes available.

## Progress

- [x] M0 — Foundation
  - [x] M0.1 — Inspect repository; pin tooling; establish project boundaries.
  - [x] M0.2 — Seed definitions, startup configuration and simulator host.
  - [x] M0.3 — Angular 22 and independent local startup.
  - [x] M0.4 — Focused verification and documentation.
- [x] M1 — Simulator to backend to Angular
  - [x] M1.1 — Immutable telemetry, transport contracts and validation.
  - [x] M1.2 — Per-vehicle in-memory storage and application operations.
  - [x] M1.3 — Ingestion and snapshot HTTP endpoints.
  - [x] M1.4 — Independent simulator loops and closed demo routes.
  - [x] M1.5 — Angular polling, coordinate map and tracking cards.
  - [x] M1.6 — Focused tests, live checks and startup documentation.
- [x] M2 — Live updates and synchronization
  - [x] M2.1 — Session-aware snapshot/event contracts and atomic snapshot capture.
  - [x] M2.2 — Application publication boundary and API SignalR adapter/hub.
  - [x] M2.3 — Frontend connection lifecycle, buffered synchronization and revision merging.
  - [x] M2.4 — Failure/reconnect tests, live restart check and Rider documentation.
- [x] M3 — Speeding, per-vehicle settings and global radius settings
  - [x] Plain C# speeding rule, elapsed windows and episode rearming
  - [x] Atomic vehicle settings reevaluation and retained alert history
  - [x] Global radius model, validation and settings endpoints
  - [x] Live settings forms, alerts and revision/session synchronization
  - [x] Focused backend/frontend checks and startup/demo documentation
- [x] CQRS prerequisite — Separate command/query handlers; preserve M3 behavior
- [x] M4 — Geofencing
  - [x] Three-observation rule and independent membership per vehicle/zone
  - [x] Atomic processing, membership snapshots and entry/exit alert history
  - [x] Geofence definitions API and frontend synchronization
  - [x] SVG rectangles, membership display and type-specific alerts
  - [x] Focused tests, real SignalR verification and Rider demo documentation
- [x] M5 — Global-radius stops and confirmed route boundaries
  - [x] Fixed-anchor distance rule and GPS duration confirmation
  - [x] Stable route IDs, onset/resumption boundaries and preserved raw samples
  - [x] Atomic radius reevaluation for both vehicles and duration reevaluation
  - [x] CQRS history/segment APIs and expanded synchronization snapshots
  - [x] Separate route lines, movement status and raw history inspection
  - [x] Focused checks and real-time Rider demonstration script
- [ ] M6 — Scenarios and UI completion
- [ ] M7 — Integration and documentation
- [ ] M8 — Clean-run verification and demo

## Handoff log

### 18 October 2026 — M0 completed; awaiting review

The earlier template cleanup was committed before this milestone; M0 started
from a clean working tree. The current solution name,
`GPS-Vehicle-Tracking-Simulator.sln`, was preserved.

Implemented:

- SDK and dependency pinning, NuGet/npm lockfiles, common C# build settings and
  Domain/Application/Infrastructure/Contracts/Simulator project references.
- Three pure domain seed records, API startup options/validation, two independent
  vehicle defaults and two rectangular geofences; `GET /api/health`.
- Independent API/simulator/frontend startup with explicit local configuration.
  The simulator validates settings and runs an idle generic host; it sends no data.
- Angular 22.2.3/TypeScript 6.0.3 upgrade with current build tooling, explicit
  NgModule change detection, HTTP development proxy and retained creation test.
- Root README with terminal/Rider instructions, architecture notes and this
  reconstructed handoff. No future service classes or rule placeholders were added.

Checks actually executed:

| Check | Result |
| --- | --- |
| `dotnet --version` | 10.0.402 selected, rather than installed .NET 11 RC. |
| `dotnet build GPS-Vehicle-Tracking-Simulator.sln` | Passed, 0 warnings and 0 errors. |
| `dotnet restore GPS-Vehicle-Tracking-Simulator.sln --locked-mode` | Passed for all six C# projects. |
| `npm ci --no-audit --no-fund` | Passed from regenerated Angular 22 lockfile. |
| `npm run build` | Passed using `@angular/build`. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | Passed, 1 existing component test. |
| API `http` profile startup | Loaded 2 vehicles and 2 geofences; listened on localhost:5078. |
| `GET /api/health` and development OpenAPI document | HTTP success, healthy response, documented health path. |
| CORS origin checks | Allowed localhost:4200; did not authorize an unrelated origin. |
| Simulator `Simulator` profile startup | Logged matching IDs, backend URL and 10-second interval; host remained running. |
| `npm start`, frontend HTTP response, proxied `/api/health` | Passed at localhost:4200 with healthy backend response through the proxy. |
| Invalid API configuration | Rejected zero stop radius, duplicate vehicle IDs, invalid latitude bounds and invalid frontend origin. |
| Invalid simulator configuration | Rejected interval below 10 seconds, invalid backend URL and duplicate IDs. |
| Seed alignment and project-reference inspection | API/simulator IDs match; all six C# dependency boundaries match the design. |
| Git whitespace and ignored-file checks | Passed; no generated/ignored files tracked. |

Test processes were stopped after verification. These checks cover startup and
configuration, not telemetry or rules that do not yet exist. No Rider GUI run was
automated; its documented launch profiles were exercised with `dotnet run`.

Known limitations: the three evaluator questions remain unresolved. npm reports
upstream Karma dependency deprecations and optional native-script notices;
installation, build, tests and development startup succeeded on the selected
Windows tooling without enabling additional scripts. Full cross-platform and
clean-machine verification remains M8.

Next milestone: **M1**, only after review/request. Add actual telemetry contracts,
storage, ingestion, deterministic vehicle progression, snapshots and the basic
tracking view. No M1 work, commits, pushes or deployment were performed in M0.

### 18 October 2026 — Clean Architecture layout refinement

Moved API and Angular projects under `src/` alongside the existing layers and
simulator. Updated solution paths/nesting, API project references and all current
README startup/configuration paths. The solution filename remains unchanged.

Documented the Clean Architecture variant explicitly: Domain contains business
concepts/rules; Application owns use cases and required abstractions;
Infrastructure implements them; API handles HTTP and composition. Contracts
contains transport DTOs; the simulator and Angular application are independent
clients. Domain/Application must not depend on API or Infrastructure.

Verification after relocation: locked NuGet restore passed; solution build passed
with zero warnings/errors; Angular production build passed; the existing headless
component test passed (1 test); Git whitespace check passed. No rule logic or
M1 functionality changed. Rider should reload the solution and any existing npm
run configuration should use `src/gps-tracking-web/package.json`.

### 18 October 2026 — Host registration extensions and constants

Both API and Simulator now register services through their own
`ServiceCollectionExtensions.AddServiceCollection(IServiceCollection, IConfiguration)`.
Program files retain host/pipeline setup. Removed custom startup summary logs;
standard framework lifecycle/error logs remain. `ValidateOnStart` validates
configuration without resolving options just to log them.

API constants hold the CORS policy name, frontend-origin configuration key and
health route. Frontend `core/constants/app.constants.ts` holds the displayed app
name. Environment URLs and configurable vehicle settings remain configuration.
No ORM or database was introduced; in-memory storage is still planned for M1.

Checks: solution build passed with zero warnings/errors; Angular production build
and existing headless component test passed; API startup, health/OpenAPI and
allowed/denied CORS checks passed; invalid frontend origin, stop radius and
simulator interval were rejected. An obsolete static-asset manifest from the
earlier project move was regenerated with `dotnet clean` and rebuild before the
successful startup check. M1 remains untouched.

### 18 October 2026 — M1 completed; awaiting review

Implemented the approved telemetry slice through the existing Clean Architecture
boundaries. Domain owns validated immutable samples; Application owns operations
and the storage abstraction; Infrastructure keeps raw history and atomic
per-vehicle snapshots; API maps HTTP contracts. Both hosts retain their
`AddServiceCollection` composition extensions. No ORM was introduced.

Sample ingestion returns 204, with ProblemDetails for invalid input (400), unknown
vehicles (404) and non-newer timestamps (409). Snapshots include latest supplied
speed/heading, receipt time, sample count and revision. Backend restart resets the
session; reconnecting HTTP clients read existing state without resetting it.

Simulator routes run independently at 42 and 66 km/h with elapsed-time progression,
overshoot carried across vertices, initial delivery and at least ten seconds
between reports. Failed deliveries are skipped; subsequent observations continue.
Angular polls every two seconds without overlapping requests, shows colored SVG
heading markers and telemetry cards, and retains data during connection failures.

Checks actually executed:

| Check | Result |
| --- | --- |
| Locked solution restore | Passed, including the new test project. |
| `dotnet test GPS-Vehicle-Tracking-Simulator.sln --verbosity minimal` | Passed, 24 tests; domain validation, atomic/concurrent storage, route progression and HTTP integration. |
| `npm run build` | Passed, production build. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | Passed, 4 tests; app creation, marker/card rendering, polling failure/recovery and request timeout. |
| Live API, simulator and Angular proxy | Both vehicles accepted telemetry; four consecutive observations per vehicle changed positions with generation intervals at least 10 seconds. |
| New HTTP client against existing backend | Integration test confirmed sample count and latest sample persist across client replacement. |
| Backend outage and restart through frontend proxy | Unavailability observed, then successful recovery with two empty snapshots while simulator was stopped. |
| Git whitespace check | Passed. |

Browser rendering and polling recovery were verified by ChromeHeadless component
tests. A separate live-page DOM capture was inconclusive; no manual Rider GUI or
full browser reload/outage walkthrough was automated. README provides those steps.
Fixed map bounds suit the demo routes; raw history is unbounded within this short
in-memory session. The three pending evaluator decisions remain assumptions and
are not implemented as rules in M1.

Next milestone: **M2**, only after review/request. No SignalR, alert rules, stop
detection, segmentation, settings editing, commits or deployment were added.

### 18 October 2026 — Shared Rider API/frontend startup

Added three shared project-relative configurations in `.run/`: **GPS Tracking
API** (existing `http` profile with build before launch), **GPS Tracking Frontend**
(`npm start` using Rider's project Node interpreter), and **GPS Tracking — API +
Frontend** (compound parallel launch). Simulator startup remains separate. README
now documents one-click startup, initial `npm ci`, individual launches and stopping
both processes. No application code, dependencies or milestone scope changed.

Verification: all three XML files parse, compound references resolve, referenced
project/package files exist, and `.run/` files are not ignored. Rider's run
configuration API recognized all three configurations. Existing processes already
occupied ports 5078 and 4200; their API health, frontend page and proxied health
checks passed. The IDE automation could not start the compound configuration
(reported "The process has failed to start"). No existing sessions were stopped.
One-click launch and shutdown therefore still require a manual Rider check with
those ports free. Git whitespace check passed. M2 remains untouched.

### 18 October 2026 — Evaluator clarifications incorporated into the plan

All three pending questions are resolved: stop radius is global with a 10-metre
default; the old route ends at the stop-onset sample and the next starts at the
movement-resumption sample; speeding requires full elapsed coverage, including
00/10/20/30-second samples for the default 30-second duration.

The proposed architecture and local delivery are accepted. External services,
LocalDB and Docker are permitted, not required. Keep the current in-memory design.
Simulator buffering and replay after outages are explicitly out of scope.

Updated current requirements, defaults, planned settings endpoints, milestone
scopes and acceptance checks, plus README and architecture notes. M3 now includes
the global settings model/API/UI and removal of per-vehicle radius; M5 includes
shared-radius stop processing and reevaluation. Live global-radius editing follows
the approved plan; fixed anchoring and geofence initialization remain implementation
choices, not additional evaluator confirmations.

Historical log entries above describe what was known at their respective stages;
their references to pending questions are superseded by this entry. M0 and M1
remain complete; M2-M8 remain incomplete. Current code still contains per-vehicle
radius seeds, explicitly scheduled for correction in M3. No application code,
configuration, dependencies or runtime behavior changed in this documentation
update. Verification: documentation consistency review and Git whitespace check;
no builds or tests required for this documentation-only change.

### 18 October 2026 — Thin controllers and tracking constants

Moved sample creation/validation handling into an application operation accepting
`SubmitSampleCommand` and returning `SubmitSampleResult`. Controllers delegate to
that operation; API extensions handle DTO and HTTP result mapping. Domain sample
validation bounds/messages, tracking problem messages and health status now use
named constants. Existing routes, payloads, HTTP status codes and storage behavior
are preserved. The supplied `TaskExtensions` helper remains unchanged because this
in-memory operation is synchronous. No new milestone functionality was added.

Verification: `dotnet test tests/GpsTracking.Tests/GpsTracking.Tests.csproj
--no-restore --verbosity minimal` passed all 24 tests, including HTTP validation,
unknown/non-newer sample rejection and snapshot compatibility.

### 18 October 2026 — Consistent OK results and startup validation constants

Added a synchronous `ToOkResult` overload alongside the existing task overload.
Health and snapshot GET responses now use the helper without introducing fake
asynchronous work. Sample ingestion still returns 204 and errors keep their
existing status codes. Extracted all `TrackingOptionsValidator` messages into
API-owned constants and named its count/positive-threshold limits; coordinate
validation reuses domain bounds. No settings scope or validation behavior changed.

Verification: all 24 backend tests passed with `dotnet test
tests/GpsTracking.Tests/GpsTracking.Tests.csproj --no-restore --verbosity minimal`.

### 18 October 2026 — Built-in global exception handling

Enabled `UseExceptionHandler` before CORS/controller endpoints, using the existing
ProblemDetails registration. Added an integration test with a failing test-only
tracking store in Development and Production. Both cases verify a 500 problem
response without internal exception details. Existing expected rejection statuses
remain covered. No custom middleware, extra packages or business rules were added.

Verification: all 26 backend tests passed after correcting the test environment
data to use MemberData (framework environment names are not attribute constants).

### 19 October 2026 — M2 completed; awaiting review

Added SignalR at `/hubs/tracking` and the `VehicleUpdated` event. Accepted samples
capture their immutable snapshot under the vehicle gate; Application publishes
outside that gate through `ITrackingPublisher`. The API adapter isolates/logs
transport failures with a bounded cancellation timeout, preserving accepted state.
Server SignalR comes from ASP.NET Core; only the JavaScript client was added,
pinned to 10.0.11 with its npm lockfile. No broker or database was introduced.

REST snapshots now return a session envelope rather than a bare array. Events
carry the same session identity. Angular registers listeners and connects before
loading REST state, buffers newest updates during loading, merges by revision and
replaces state on backend session changes. Initial connection failure, automatic
reconnect, failed REST synchronization and stop/restart cleanup are handled.
Regular two-second polling was removed; explicit connection status is displayed.
Vehicle freshness/stop classification remains separate from connectivity.

Checks actually executed:

| Check | Result |
| --- | --- |
| Backend Release test run | 29 tests passed, including prior M1 checks, session lifetime, hub negotiation and publication failure/lock isolation. |
| Frontend ChromeHeadless tests | 9 tests passed: rendering, event buffering/order/duplicates, reset to lower revisions in a new session, request cancellation, REST retry/timeout and connection lifecycle. |
| Frontend production build | Passed, approximately 271 kB initial raw bundle. |
| Real SignalR client against isolated API on port 5088 | Received supplied telemetry with matching REST session/revision. |
| Real API restart with live client | Observed interruption, automatic reconnection, a new REST session with empty vehicles and successful revision-1 live delivery. |

The real transport smoke check used the installed SignalR JavaScript client over
WebSockets. A second Angular dev-server launch with a temporary proxy environment
override was rejected by automatic approval review with only "blocked by policy";
the live frontend-proxy and Rider GUI restart walkthrough were not automated.
README supplies those manual verification steps. Existing user-run processes and
unrelated edits were preserved. Historical M1 polling descriptions above record
the prior milestone and are superseded by this entry.

Next milestone: **M3**, only after review/request. Alert rules, global/per-vehicle
settings editing, geofencing and stop/segment rules were not implemented.

### 19 October 2026 — M3 completed; awaiting review

Implemented plain C# speeding evaluation using supplied speed and full GPS elapsed
windows. Equality/below-limit samples rearm the rule; a continuous episode produces
one alert. Vehicle settings changes reevaluate the latest qualifying history suffix
under the same gate as ingestion. Immediate alerts use the latest GPS timestamp;
historical alerts retain their original thresholds and separate creation time.

Added per-vehicle GET/PUT settings, GET alerts and global GET/PUT settings. Moved
stop radius from vehicle seeds to one global setting, default 10 metres. Controllers
delegate to Application and use shared response mapping. Validation/default/error
constants belong to their respective layers. No ORM or other infrastructure added.

Vehicle snapshots now include settings and cumulative alerts. Global settings have
their own revision and SignalR event. Angular merges alerts by ID, compares revisions
within a session and resets on backend session replacement. Reactive settings forms
preserve dirty drafts during live updates, validate finite positive values/integer
durations, prevent duplicate submissions and retain input after failed saves.
Added only `@angular/forms` 22.2.3 and updated its npm lockfile.

Checks actually executed:

| Check | Result |
| --- | --- |
| `dotnet restore --locked-mode` | Passed. |
| Backend Release tests | 40 passed, including full-window boundaries, equality/rearming, settings reevaluation, concurrent ingestion/settings, validation, HTTP behavior and transport failure isolation. |
| Frontend ChromeHeadless tests | 11 passed, including prior M2 cases, alert deduplication, global revision/session handling and dirty settings forms. |
| Frontend production build | Passed; approximately 333 kB initial raw bundle. |
| Real API and SignalR client on isolated port 5090 | Verified 20-to-15-second immediate alert at the latest GPS timestamp, no duplicate on identical save, independent vehicle settings and live global settings revision delivery. |
| `git diff --check` | Passed; Git reported only automatic LF/CRLF conversion notices. |

README documents Rider startup, the required `npm ci`, and a speeding demonstration
with the existing simulator. Architecture notes describe cumulative snapshot alert
delivery, last-write-wins edits and in-memory lifetime. GUI verification in Rider
was not automated. Stop duration and global radius can be saved, but spatial stop
detection remains M5. Geofencing remains M4. No subsequent milestone implemented.

Next milestone: **M4**, only after review/request.

### CQRS refactoring before M4 — completed

Following the user's architecture preference, lightweight CQRS was implemented in
Application before M4 functionality. The scope below records the agreed refactoring;
it was verified independently against all 40 existing M3 backend tests.

- Separate write use cases into command handlers: submit a sample, update vehicle
  settings and update global settings.
- Separate read use cases into query handlers: retrieve vehicle snapshots, one
  vehicle's settings, alert history and global settings.
- Move orchestration out of the combined `TrackingService` into these handlers.
  Controllers inject the relevant handlers directly and retain transport mapping
  and existing `ToOkResult`/error response conventions.
- Keep business rules in Domain and storage implementations in Infrastructure.
  Use the same in-memory store for reads and writes; CQRS does not require separate
  databases, event sourcing, a broker or MediatR. Add no dispatch framework.
- Preserve atomic sample/settings processing, session identities, revision ordering,
  alert history and publication after releasing storage locks. Commands may return
  the existing result snapshots needed by clients.
- Preserve HTTP contracts and frontend behavior. Run existing backend/frontend
  regression checks and adapt focused handler tests before declaring the refactor
  complete. Add future handlers only when their milestone is implemented.

### 19 October 2026 — CQRS added to the architecture plan

Recorded the user's preference for CQRS as a focused refactoring step before M4.
The current code still uses `TrackingService`; no application code or runtime
behavior changed in this update. M3 remains complete and M4 remains pending.

### 19 October 2026 — CQRS and M4 completed; awaiting review

Replaced the combined TrackingService with directly injected command/query handlers
in Application. Existing HTTP contracts and result mapping were preserved during
the refactoring, and all 40 M3 backend tests passed before geofencing was added.
No MediatR, dispatch framework or separate storage introduced.

Added a plain C# geofence rule per vehicle/rectangle. Membership starts Unknown;
three accepted observations establish the rolling majority without an alert.
Later majority changes generate entry/exit alerts at the confirming GPS timestamp.
Edges count as inside. Rejected samples do not affect the rule; overlapping zones
and vehicles remain independent. No crossing interpolation is performed.

Geofencing runs alongside speeding inside the same vehicle gate. A sample may
produce several alerts but increments revision once. Snapshots include membership
and cumulative alerts; publication remains outside the lock. GET /api/geofences
and the existing synchronization snapshot expose startup definitions. Geofence
alerts add zone ID/name and use null speeding-specific fields; speeding alerts
retain existing fields and values. No separate event stream was added.

Angular displays labeled SVG rectangles, confirmed membership and type-specific
alert descriptions. Definitions load through the existing REST synchronization;
membership and history reuse revision/session handling. A visible note explains
that confirmed membership may lag behind the latest marker. Existing simulator
routes already demonstrate both zone entries/exits, so their movement is unchanged.

Checks actually executed:

| Check | Result |
| --- | --- |
| CQRS prerequisite regression | 40 existing backend tests passed. |
| Backend Release tests after M4 | 55 passed, covering boundaries, initialization, majority changes, no interpolation, independent/overlapping zones, combined speeding/geofence alerts, rejected samples, HTTP refresh and backend reset. |
| Deterministic simulator verification | Both existing routes produce entries and exits for both zones within 600 simulated seconds at 10-second steps. Included in backend tests. |
| Frontend ChromeHeadless tests | 13 passed, including SVG projection, alert rendering, reconnect recovery and new-session definition/membership reset. |
| Frontend production build | Passed; approximately 335 kB initial raw bundle. |
| Real API and SignalR client, isolated port 5091 | Received eight revisions with entry/exit alerts and confirmed memberships matching REST history. Temporary API stopped afterward. |
| `git diff --check` | Passed. |

README includes Rider startup and M4 demonstration steps; architecture notes explain
CQRS, rule state, contract additions and confirmation behavior. Rider GUI execution
was not automated. The documented geofence defaults remain implementation choices.
Stop/segment processing and history APIs remain M5. No later milestone implemented.

Next milestone: **M5**, only after review/request.

### 20 October 2026 — M5 completed; awaiting review

Added Domain MovementRule and Haversine distance calculation. A fixed anchor and
GPS elapsed time distinguish Unknown, Moving, PossibleStop and Stopped without
using supplied speed. Confirmed stops close the current route at the anchor sample;
resumption outside the radius opens a new route at that observed position. Raw
history retains stationary observations. Initially stationary points do not become
completed driven routes; route IDs remain stable as routes grow and close.

Stop-duration edits reevaluate that vehicle's latest unfinished candidate. Global
radius edits reevaluate both vehicles under a single shared store gate, replacing
the earlier independent locks. Completed routes and historical alerts remain fixed.
Already confirmed stops do not reopen when duration increases. A narrower radius
excluding the latest sample resumes at that sample without guessing an earlier
crossing. These are implementation defaults, not additional evaluator answers.

Synchronization now captures both vehicles and global settings atomically. Global
settings events and PUT responses include both vehicle snapshots, merged by vehicle
revision independently of global revision. Publication stays outside the gate.
Added CQRS history/segments reads with session/revision envelopes and 404 handling.
Vehicle snapshots carry movement and cumulative segments for reconnect recovery.

Angular draws separate route lines only for routes with two or more points, displays
movement state and recorded stop coverage, and exposes route boundaries and raw
history inspection. The history viewer loads on demand, labels its captured revision,
uses signals for response rendering and cancels obsolete session requests. Settings
help text now describes active stop processing. The existing GeofenceRule edit was
preserved; simulator routes were not changed and M6 remains unimplemented.

Checks actually executed:

| Check | Result |
| --- | --- |
| Backend Release tests | 63 passed, including prior speeding/geofence checks, fixed-anchor drift, exact radius, 180-second boundary, stop/resume route boundaries, settings reevaluation, concurrent radius/sample updates, raw history, HTTP rejection and backend reset. |
| Frontend ChromeHeadless tests | 16 passed, including separate route lines, one-point rendering, radius-update revisions, session reset, raw history rendering and request cancellation. |
| Frontend production build | Passed; approximately 340 kB initial raw bundle. |
| Real-time `scripts/Verify-M5.ps1`, isolated API port 5092 | Passed with real ten-second reporting: seven samples, two routes, preserved raw history. |
| Actual SignalR global settings update | Both captured vehicle revisions delivered; route end equals stop-onset sample and next route starts at resumption sample. |
| `git diff --check` | Passed. |

README documents the fresh API + Frontend Rider launch and focused demonstration
script. The temporary API was stopped after checks. Rider GUI interaction was not
automated. State and history remain in memory, and cumulative snapshots grow with
session length. No persistence or extra messaging infrastructure introduced.

Next milestone: **M6**, only after review/request.

### 20 October 2026 — M5 final review after component extraction

Verified the separate VehicleSession class and the history viewer's external HTML
and CSS against the complete M5 implementation. Fixed one settings-reevaluation
edge case: a known resumption with only one point in its new route now retains
Moving instead of reverting to Unknown. Initial single-point observations still
remain Unknown. Added a regression test preserving the route ID and start time.

Verification on the current source: 64 backend Release tests passed, 16 frontend
ChromeHeadless tests passed, and the frontend production build passed (approximately
340 kB initial bundle). Existing staged changes were preserved; no commit or staging
operation performed. M5 remains complete. M6 was not started.
