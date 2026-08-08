# TMS API Versioning Policy

## Breaking vs Non-Breaking Changes

**Breaking changes require a new version:**
- Removing a field from a response
- Renaming a field in a request or response
- Changing a status code (e.g., 200 → 204, 409 → 400)
- Tightening validation rules on existing fields
- Changing the default sort order of a collection
- Removing an endpoint

**Additive (non-breaking) — no new version needed:**
- Adding a new optional response field
- Adding a new endpoint
- Adding a new optional query parameter with a safe default
- Relaxing validation rules

## Versioning Scheme

URL-segment versioning: `/api/v1/...`, `/api/v2/...`

An optional `X-Api-Version` header is accepted as an escape hatch for clients with
cached CDN URLs, but URL segment is primary. All new integrations must use the URL.

## Sunset Window

V1 runs for a minimum of **6 months** after V2 ships. This covers rural training
centres and mobile clients on quarterly maintenance schedules.

V1 sunset date: **31 December 2026**

## Communication

From the day V2 ships, every V1 response carries:
- `Deprecation: true`
- `Sunset: <RFC 7231 date>`
- `Link: </api/v2/...>; rel="successor-version"`

Additionally: a CHANGELOG entry on release day, an email to every team holding
an API key, and a calendar invite for the V1 shutdown date.

## Version Skipping

Clients are not forced to migrate through every intermediate version.
V1 → V3 is fully supported; V2 can be skipped if it does not apply to a client's use case.

## Decision Checklist (30-second test)

Before merging a change, ask: "Would a client written against the current contract
break if this shipped?" If yes → new version. If no → merge as-is.
