# Migration status

| Phase | Status | Notes |
|---|---|---|
| Source assessment | Complete | Mainframe corpus inventoried at upstream commit `59cc6c2`. |
| Azure target design | Complete | Read-only strangler slice on Azure Container Apps selected. |
| Read-only reference slice | Complete | Copybook-backed ASP.NET Core UI and pseudonymized JSON APIs over five ASCII data files. |
| Local validation | Complete | 31 tests, `dotnet format`, locked restore, corpus preservation, Bicep build, container build, and container smoke checks pass. Trivy image scanning runs in CI. |
| Azure deployment | Complete | Healthy immutable-digest revision running through managed identity in Australia East. |
| Independent review | Complete | Second-model rubber-duck review completed; findings triaged, fixed, and re-validated. |
| Publication | Complete | Published as a public repository under `xavierxmorris`. |
| CICS transaction migration | Not started | No online transaction, screen flow, or BMS map behavior has been migrated. |
| Batch and JCL migration | Not started | No batch job, scheduler, or GDG behavior has been migrated. |
| Db2, IMS, and MQ migration | Not started | Optional modules remain mainframe-only. |
| Write path, identity, and cutover | Not started | The slice is read-only; no persistence, authorization, audit, or rollback design exists. |

Only the read-only data-exploration slice has been migrated. The application as a whole
has **not** been migrated, and this repository does not claim CICS parity.
