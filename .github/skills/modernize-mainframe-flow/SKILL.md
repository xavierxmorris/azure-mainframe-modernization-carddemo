---
name: modernize-mainframe-flow
description: Modernize one CardDemo CICS transaction or JCL batch flow into a tested Azure seam. Use for bounded implementation tasks.
---

# Modernize one flow

1. Invoke the equivalent of the Mainframe Analyst workflow and obtain a cited
   behavior specification.
2. Define scope and non-goals. One CICS transaction, inquiry, command, report,
   or batch outcome is the normal maximum.
3. Add characterization tests for inputs, outputs, side effects, status/error
   behavior, sensitive fields, and malformed data.
4. Select the Azure mapping:
   - HTTP inquiry/command: ASP.NET Core on Container Apps
   - Queue/topic: Azure Service Bus
   - Scheduled/run-to-completion batch: Container Apps Jobs
   - Durable multi-step orchestration: Durable Functions
   - Relational state: Azure SQL or Azure Database for PostgreSQL
   - Secrets/keys: Key Vault through managed identity
5. Implement an adapter boundary rather than translating platform calls inline.
6. Compare deterministic legacy and modern outputs.
7. Run `scripts/validate-modern.ps1` and update migration documentation.

Do not enable writes without authentication, authorization, auditing,
idempotency, persistence, reconciliation, and rollback behavior.

