# ADR 0001: Start with a read-only strangler slice

- Status: Accepted
- Date: 2026-09-03

## Context

The upstream repository contains a broad mainframe sample but no executable
cloud application, automated tests, or production-grade behavioral
specification. A full rewrite would combine business-rule discovery, platform
replacement, data migration, identity, and cutover into one unreviewable risk.

## Decision

Preserve all legacy assets and add a stateless ASP.NET Core data-exploration
slice that parses selected supplied fixed-width records. Deploy it to Azure
Container Apps and keep it read-only. Pseudonymize, mask, or omit
sensitive-shaped values at the domain boundary. Do not claim CICS transaction
or authorization parity until separately specified and proven.

## Consequences

- The repository becomes runnable and testable without claiming parity for
  unimplemented transactions.
- Copybook interpretation becomes executable evidence.
- Container-local data is safe because it is immutable.
- Production writes, authentication, external state, and asynchronous workflows
  remain explicit future phases.
