# CardDemo Azure instructions

- This repository preserves a mainframe CardDemo corpus and adds a read-only
  ASP.NET Core 10 modernization slice deployed to Azure Container Apps.
- Treat `app/`, original shell scripts, and `samples/` as legacy evidence.
  Do not “clean up” or rename legacy artifacts without an explicit parity reason.
- Trace fields to copybooks before changing parser offsets or types.
- Preserve COBOL signed-overpunch, implied decimals, fixed record lengths, and
  date/timestamp semantics; add characterization tests for every change.
- Never return or log CVV, SSN, government ID, raw fixed-width records, or
  unmasked card numbers.
- The modern slice is read-only. Do not add writes without identity,
  authorization, audit, idempotency, persistence, and rollback requirements.
- Build and test with `dotnet test CardDemo.Azure.slnx -c Release`.
- Check formatting with
  `dotnet format CardDemo.Azure.slnx --verify-no-changes --no-restore`.
- Validate Azure IaC with `az bicep build --file infra/main.bicep`.
- Validate the production image and routes with `scripts/validate-modern.ps1`.
- Keep Azure resources declarative in Bicep, use managed identity, avoid
  credentials, retain HTTPS-only ingress and health probes, and scale to zero.
- Prefer one traceable vertical slice over broad mechanical translation.

