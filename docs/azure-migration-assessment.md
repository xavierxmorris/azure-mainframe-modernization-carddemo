# Azure migration assessment

Assessment date: 2026-09-03
Upstream baseline: `aws-samples/aws-mainframe-modernization-carddemo` at `59cc6c2`

## Executive summary

CardDemo is a mainframe application source corpus, not an AWS cloud deployment. It contains COBOL, CICS transactions, BMS maps, VSAM file definitions, JCL batch jobs, Db2 SQL, IMS definitions, MQ integrations, scheduler samples, and fixed-width ASCII/EBCDIC data. There is no cloud infrastructure, CI pipeline, automated test suite, or locally runnable end-to-end application.

The recommended first Azure modernization slice is a **strangler-pattern reference application**:

- Preserve the original mainframe assets unchanged under `app/`, `samples/`, and `scripts/`.
- Add a stateless ASP.NET Core 10 web/API application that reads the supplied fixed-width ASCII records.
- Deploy the container to Azure Container Apps through Azure CLI/Bicep, with an `azd` project definition for compatible environments.
- Add automated parser, domain, API, container, and infrastructure checks.
- Add repository instructions, path-specific instructions, agent skills, custom agents, and reusable prompts so GitHub Copilot can perform repeatable mainframe analysis and modernization work with less guesswork.

This phase is intentionally read-only. A production migration would externalize state and add authenticated write workflows before cutover.

## Current-state inventory

| Area | Current technology | Evidence | Azure direction |
|---|---|---|---|
| Online transactions | CICS COBOL and BMS | `app/cbl/CO*.cbl`, `app/bms/` | ASP.NET Core APIs and web UI on Container Apps |
| Primary records | VSAM KSDS/AIX | COBOL `FILE-CONTROL`, `app/jcl/*FILE.jcl` | Azure SQL or PostgreSQL after data reconciliation |
| Batch | JCL and COBOL | `app/jcl/`, `app/cbl/CB*.cbl` | Container Apps Jobs; Durable Functions for orchestrated workflows |
| Relational data | Db2 static SQL | `app/app-transaction-type-db2/` | Azure SQL or Azure Database for PostgreSQL |
| Hierarchical data | IMS DB | `app/app-authorization-ims-db2-mq/ims/` | Relational/document model selected after access-pattern analysis |
| Messaging | IBM MQ | `app/app-vsam-mq/`, authorization extension | Azure Service Bus queues/topics |
| Identity | RACF/VSAM credentials | `COSGN00C.cbl`, `DUSRSECJ.jcl` | Microsoft Entra ID and managed identities |
| Scheduling | JCL, Control-M, CA7 | `app/scheduler/` | Container Apps Jobs schedules or Logic Apps |
| Observability | Job output and CICS responses | scripts and programs | Azure Monitor, Log Analytics, Application Insights |
| Source data | Fixed-width ASCII/EBCDIC | `app/data/` and copybooks | Versioned seed data now; migration pipeline later |

## Compatibility and redesign analysis

### Suitable for the first slice

- The ASCII datasets provide deterministic demo data that can be parsed without a mainframe runtime.
- Account, card, customer, cross-reference, transaction, and reference records have copybooks with explicit record lengths. The checked-in ASCII card cross-reference export intentionally omits its copybook's 14 trailing filler bytes, so its transport contract is explicitly tested as 36 bytes rather than represented as a 50-byte record.
- A stateless container can expose read-only workflows and scale safely.
- The pseudonymized data explorer creates an integration seam for later, separately specified transaction-by-transaction replacement. It does not claim parity with CICS inquiry authorization or transaction behavior.

### Requires redesign before production

- VSAM locking, alternate indexes, and CICS unit-of-work semantics do not map directly to container-local files.
- Db2/IMS two-phase commit must be replaced by explicit consistency, idempotency, and compensating-action designs.
- IBM MQ request/reply and trigger behavior needs a Service Bus contract, dead-letter, duplicate-detection, retry, and correlation design.
- RACF IDs and plaintext sample passwords must not become application credentials. The modern application must use Entra ID.
- Batch dependencies currently rely on submission order and sleeps in shell scripts. Production orchestration needs durable state and job completion signals.
- Card numbers, CVV-like values, names, contact details, SSNs, and government IDs in sample records are sensitive-shaped data. The reference UI pseudonymizes people, masks cards, omits contact/identity fields, and must not log raw records.

## Target architecture for this repository

```text
Browser
  |
  v
Azure Container Apps ingress
  |
  v
ASP.NET Core 10 web + JSON API
  |
  +-- fixed-width parser --> bundled ASCII sample data (read-only demo)
  +-- health endpoints
  +-- structured logs --> Log Analytics

Future production services:
  Entra ID | Azure SQL/PostgreSQL | Service Bus | Container Apps Jobs
  Key Vault | Application Insights | private networking
```

The application has no hardcoded service-discovery hostnames. Configuration is environment-driven, and the current slice has no outbound service dependency.

## Complexity, risk, and controls

Overall migration complexity: **High** for full functional replacement; **Low** for the read-only reference slice.

| Risk | Impact | Control in this phase |
|---|---|---|
| Incorrect fixed-width interpretation | Wrong balances or identities | Parser unit tests against copybook offsets and record lengths |
| Exposure of sensitive-shaped sample fields | Privacy/security issue | Pseudonymize people, mask card numbers, and omit contact, expiry, CVV, SSN, and government-ID fields |
| False equivalence between demo and production | Bad migration decisions | Label the slice read-only and document non-goals |
| Container-local state loss | Data loss | No runtime writes; immutable seed data |
| Legacy behavior drift | Modern result differs from COBOL | Trace every displayed field to copybooks and add golden-record tests |
| Cloud cost sprawl | Unnecessary spend | Single consumption-based Container App environment and ACR; explicit teardown command |

## Validation plan

1. Build the .NET solution in Release mode.
2. Run parser and endpoint tests against the checked-in sample records.
3. Build and run the Linux container locally; verify `/health` and representative API/UI routes.
4. Lint Bicep and run an Azure what-if deployment.
5. Provision with the deployment script, deploy an immutable ACR digest, and smoke-test the public URL.
6. Run a second-model rubber-duck review over the complete diff and address valid findings.
7. Push only after the deployed revision and repository checks pass.

## Non-goals for this phase

- Reimplementing every CICS screen and JCL program.
- Claiming production equivalence or PCI compliance.
- Persisting edits to account, customer, card, or transaction data.
- Migrating real customer data.
- Removing or rewriting the original mainframe source corpus.
