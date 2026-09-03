# Modernization backlog

## Phase 0 — foundation (implemented)

- Preserve upstream mainframe assets and provenance.
- Parse account, card, customer, cross-reference, and transaction records.
- Expose pseudonymized, masked read-only web/API views.
- Add tests, CI, security automation, containerization, Azure IaC, and Copilot context.

## Phase 1 — behavioral coverage

- Inventory every CICS transaction and batch job.
- Generate behavior specifications tied to program paragraphs and copybooks.
- Add golden datasets and differential tests for account, card, customer,
  transaction, payment, reporting, interest, and statement workflows.
- Add a machine-readable traceability matrix from legacy artifact to test and
  modern endpoint.
- Add an automated accessibility and responsive-viewport gate (for example
  Playwright with axe-core) so the UI claims in the documentation are enforced
  by CI rather than asserted.

## Phase 2 — identity and inquiry services

- Add Microsoft Entra ID authentication and application roles.
- Replace account/customer/card inquiry reads with Azure SQL or PostgreSQL.
- Add private networking, Key Vault references, Application Insights tracing,
  rate limits, and audit events.
- Import synthetic data through a repeatable reconciliation pipeline.

## Phase 3 — asynchronous authorization

- Define versioned authorization request/response contracts.
- Replace IBM MQ queues with Azure Service Bus queues or topics.
- Add duplicate detection, dead-letter handling, retries, correlation IDs,
  idempotency, and replay tooling.
- Preserve approval/decline rules with differential tests.

## Phase 4 — batch decomposition

- Map JCL dependencies to explicit DAGs.
- Implement posting, interest, backup, combine, index, statement, and purge
  outcomes as Container Apps Jobs or Durable Functions.
- Replace fixed sleeps with completion signals and durable checkpoints.
- Add rerun, compensation, reconciliation, and operational dashboards.

## Phase 5 — controlled write cutover

- Introduce command APIs behind feature flags.
- Run dual-read and shadow-write comparisons.
- Prove financial balancing, authorization, auditing, and rollback.
- Cut over one bounded transaction at a time and retire only verified legacy
  resources.

