# GPS Vehicle Tracking Simulator

The repository currently contains a cleaned application shell. GPS simulation and
tracking features have not been implemented.

## Projects

- `GpsTracking.sln` — open this solution in Rider.
- `GpsTracking.Api/` — ASP.NET Core backend targeting .NET 10.
- `gps-tracking-web/` — Angular 18 frontend.

This cleanup retains the existing framework versions and HTTPS/SPA startup setup.
The planned Angular 22 upgrade belongs to the next setup milestone.

## Run locally

Install the frontend dependencies:

```powershell
cd gps-tracking-web
npm ci
cd ..
```

Trust the development certificate and start the backend:

```powershell
dotnet dev-certs https --trust
dotnet run --project GpsTracking.Api --launch-profile https
```

The existing SPA proxy starts the Angular development server. Open
`https://localhost:4200` to see the application title. The backend uses
`https://localhost:7239`; its development OpenAPI document is at `/openapi/v1.json`.

In Rider, reopen `GpsTracking.sln` after the rename and run the API's `https`
launch profile. The frontend can also be started separately with `npm start`
from `gps-tracking-web`.

## Checks

```powershell
dotnet build GpsTracking.sln
cd gps-tracking-web
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

The frontend test command requires Chrome. Build output, dependency directories,
local certificates, and IDE state are excluded from Git.
