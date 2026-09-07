---
description: 'Compare one copybook numeric boundary in Java and .NET using shared synthetic fixtures.'
mode: agent
---

# Compare a numeric modernization boundary

Read `AGENTS.md`, `docs/java-dotnet-modernization.md`, the relevant copybook,
`src/CardDemo.Modern/Services/FixedWidth.cs`,
`examples/java/OverpunchContract.java`, and the shared contract fixtures.

Trace one field's offset, width, sign representation, scale, valid range, and
invalid-input behavior. State which claims come from the copybook and which
come only from authored fixtures or the current implementation.

For an uncovered boundary, add the smallest synthetic fixture and, only if
required, change the Java example. Do not silently redefine the shared oracle.
Do not modify legacy assets, the production parser/API, or sensitive-field
handling unless the user explicitly requests that behavioral change.

Run:

```powershell
.\scripts\check-java-contract.ps1
dotnet test .\CardDemo.Azure.slnx --configuration Release `
  --filter "FullyQualifiedName~SharedOverpunchContractTests"
```

Report exact outcomes, field mapping, and coverage limits. If a runtime is
unavailable, report the missing evidence. Do not claim CICS parity or production
readiness, add writes, deploy resources, or commit/push changes.
