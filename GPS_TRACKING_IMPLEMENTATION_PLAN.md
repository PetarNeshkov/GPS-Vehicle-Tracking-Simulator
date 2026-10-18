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
| Stops | Detect remaining within a small distance, not speed equal to zero. |
| Speeding | All observations in the qualifying window must exceed the threshold; one alert per continuous episode, rearmed after speeding breaks. Defaults: 80 km/h and 30 seconds. |
| Segments | Split after a sufficiently long stop, default 180 seconds. Exact boundary remains provisional. A line needs at least two points; current position may be a single point. |
| Geofences | Predefined rectangles; observed GPS points only. Evaluator suggested confirmation by two of the latest three positions. No interpolated crossings required. |
| Time | GPS generation timestamps drive rules. Assume strictly newer timestamps arrive in order. |
| Settings | Per vehicle, editable live, with immediate reevaluation of retained observations under the new settings. |
| Example | After 20 seconds of speeding, changing duration from 30 to 15 seconds immediately alerts with the latest GPS timestamp. |
| UI | Two vehicles, movement and tracking details, history, alerts and configurable thresholds. A simplified visualization is acceptable. |
| Demo | Reliably demonstrate geofencing, speeding and stops/segmentation. |

Persistence and automated tests are optional in the assessment. Clean Architecture
and CQRS are considerations, not requirements to add frameworks. Focused rule tests
are planned because timestamp and state-transition behavior matters.

## Working assumptions — not evaluator-confirmed

Three questions have been sent and remain unanswered:

1. Stop radius: configurable per vehicle, default **10 metres**, measured from the
   fixed location where a potential stop began.
2. Route boundary: if a stop begins at 12:00, qualifies at 12:03 and movement
   resumes at 12:05, end the old segment retrospectively at 12:00 and start the
   new segment at 12:05. Preserve stationary observations in raw GPS history.
3. Speeding coverage: require the full elapsed duration. A 30-second condition
   ordinarily requires observations at 00, 10, 20 and 30 seconds.

Other documented defaults:

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
- Insufficient retained time coverage cannot produce a duration alert.
- Mark stale after 30 seconds of receipt silence, separately from movement.
  GPS time drives business durations; server receipt time may drive staleness.
- Keep the demo session in memory. Backend restart resets settings, rule state
  and history; browser refresh must not. Session retention is not a production
  memory-management policy.
- Use deterministic routes, realistic progression and dwell durations. Shorten
  configurable rule durations for demos rather than accelerating GPS timestamps.

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

Endpoints (only sample ingestion and snapshots are implemented in M1):

- `POST /api/vehicles/{id}/samples`
- `GET /api/vehicles`
- `GET /api/vehicles/{id}/history` and `/segments`
- `GET /api/alerts` and `/api/geofences`
- `GET`/`PUT /api/vehicles/{id}/settings`
- `/hubs/tracking` in M2

Process samples and settings under the same per-vehicle gate. Store accepted
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
| M3 | Speeding rules, alert storage/list, per-vehicle settings and reevaluation. | Continuous-window semantics, one alert per episode, interruption/rearming, immediate 20-to-15-second example. |
| M4 | Geofencing and visualization. | Initial membership without alert; two-of-three confirmed entry/exit; edges defined. |
| M5 | Spatial stops, segments, history APIs and settings reevaluation. | Short stops retain trip; long stop splits once; movement resumes a new segment; slow drift handled. |
| M6 | Deterministic scenarios, validation and UI refinement. | All scenarios repeatable; independent per-vehicle configuration. |
| M7 | Integration, concurrency/reconnect checks and architectural documentation. | Consistent sample/settings processing, clear errors and no duplicate events. |
| M8 | Clean checkout verification and demonstration. | Another developer can run all three apps and demonstrate required behavior. |

Target ten working days: M0 on day 1, M1 days 1–2, M2 day 3, M3 day 4,
M4 day 5, M5 days 6–7, M6 day 8, M7 day 9, M8 day 10. Drop optional map
integration, Docker, persistence and elaborate animation before required behavior.

Tests accompany rules using synthetic timestamps: threshold equality, exact time
coverage, interruptions, episode deduplication, independent vehicles, geofence
majorities/edges, fixed-anchor drift, brief/prolonged stops, resume behavior,
settings reevaluation and completed-history preservation. Later integration
checks cover concurrent settings/samples and reconnect synchronization.

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
- [ ] M2 — Live updates and synchronization
- [ ] M3 — Speeding and runtime settings
- [ ] M4 — Geofencing
- [ ] M5 — Stops and route segments
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
