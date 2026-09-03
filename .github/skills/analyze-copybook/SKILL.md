---
name: analyze-copybook
description: Analyze CardDemo COBOL copybooks and fixed-width records. Use when mapping fields, offsets, types, sensitive data, or parser tests.
---

# Analyze a CardDemo copybook

1. Locate the complete `01` record and every nested field, `REDEFINES`,
   `OCCURS`, and `DEPENDING ON` clause used by the target path.
2. Calculate zero-based start, storage length, display length, sign, scale,
   encoding, and filler for each field. Do not assume character count equals
   storage length for `COMP`, `COMP-3`, or binary fields.
3. Confirm the declared total against the source file or VSAM/JCL definition.
4. Identify card, CVV, SSN, government ID, credential, contact, and financial
   values. Mark what must be masked, omitted, encrypted, or access-controlled.
5. Find every program that reads or writes the record and note alternate
   interpretations introduced by `REDEFINES`.
6. Add fixtures for positive and negative overpunch, zero, maximum length,
   spaces, malformed length, and date/timestamp edge cases.
7. Return a field table and cite the copybook plus consumers.

Never expose a sensitive source field merely because it is present in a sample.

