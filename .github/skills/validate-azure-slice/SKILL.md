---
name: validate-azure-slice
description: Validate the CardDemo Azure reference slice locally or after deployment. Use before reviews, pull requests, or releases.
---

# Validate the Azure slice

From the repository root:

1. Run `.\scripts\validate-modern.ps1`.
2. The validation script builds, starts, smoke-tests, inspects, and removes the
   local production image unless `-SkipContainer` is supplied.
3. For Azure, retrieve the FQDN with `az containerapp show` and run the smoke
   script against its HTTPS URL.
4. Confirm the active revision is healthy, the image comes from ACR, the
   registry uses managed identity, ingress disallows HTTP, probes target
   `/health`, and scale remains 0–3.
5. Inspect application and system logs for startup, probe, or parsing errors.
6. Confirm APIs and HTML contain no raw card number, source customer name,
   contact detail, CVV, SSN, government ID, or source record.

Do not treat a successful HTTP 200 alone as migration validation.
