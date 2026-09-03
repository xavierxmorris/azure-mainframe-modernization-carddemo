# Contributing

Contributions should preserve legacy traceability and keep modernization slices
small enough to review.

## Before opening a pull request

1. Link the change to a legacy entry point or an Azure platform requirement.
2. Cite the relevant COBOL paragraphs, copybooks, JCL steps, files, SQL/IMS/MQ
   objects, or existing modern behavior.
3. Add characterization tests before changing observable behavior.
4. Run:

   ```powershell
   .\scripts\validate-modern.ps1
   ```

5. Confirm no CVV, SSN, government ID, raw record, credential, or unmasked card
   number is logged, rendered, or returned.
6. Describe remaining parity gaps and non-goals.

Use the modernization-slice issue form for new scope and the behavior-discrepancy
form for reproducible parity problems.

## Design principles

- Prefer strangler seams over broad rewrites.
- Keep legacy assets as evidence; avoid opportunistic formatting.
- Separate business rules from CICS, VSAM, JCL, Db2, IMS, and MQ mechanics.
- Keep the reference slice read-only unless the full write security and
  consistency model is part of the change.
- Use Bicep, managed identity, OIDC, health probes, and scale controls for Azure.

## Security

Do not report vulnerabilities in a public issue. Follow [SECURITY.md](SECURITY.md).

By contributing, you agree that your contribution is licensed under Apache
License 2.0.
