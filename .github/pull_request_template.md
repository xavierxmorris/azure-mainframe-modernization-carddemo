## Change

Describe the bounded behavior or platform change.

## Legacy evidence

List the CICS transaction, COBOL paragraphs, copybooks, files, SQL/IMS/MQ
objects, JCL, or scheduler definitions that support the change.

## Validation

- [ ] Characterization tests cover the changed behavior and negative paths.
- [ ] Fixed-width offsets, signs, scales, dates, and record links are verified.
- [ ] No CVV, SSN, government ID, raw record, or unmasked card value is exposed.
- [ ] `scripts/validate-modern.ps1` passes.
- [ ] Azure resource, identity, ingress, probe, and scaling changes are documented.
- [ ] The scope and remaining parity gaps are explicit.

