# Agent operating guide

## Mission

Preserve CardDemo's mainframe behavior while replacing it through small,
measurable Azure seams. Do not perform a blind line-by-line translation.

## Start here

1. Read `docs/azure-migration-assessment.md`.
2. Read the nearest path-specific file in `.github/instructions/`.
3. Trace the relevant COBOL entry point, copybooks, file definitions, JCL, and
   callers before changing modern code.
4. State the legacy invariant being preserved and add a characterization test.
5. Run `.\scripts\validate-modern.ps1` before completing a change.

## Hard boundaries

- Do not modify legacy source merely to make the modern implementation easier.
- Keep original fixed-width record lengths and offsets traceable to copybooks.
- Never expose or log CVV, SSN, government ID, or raw record values.
- Keep the current reference slice read-only unless a task explicitly includes
  persistence, identity, authorization, audit, and rollback design.
- Use managed identity instead of credentials in code or workflow files.
- Keep Azure resources in Bicep and preserve scale-to-zero defaults.

## Key commands

```powershell
dotnet test .\CardDemo.Azure.slnx -c Release
dotnet format .\CardDemo.Azure.slnx --verify-no-changes --no-restore
az bicep build --file .\infra\main.bicep
docker build --tag carddemo-azure:local .
```

