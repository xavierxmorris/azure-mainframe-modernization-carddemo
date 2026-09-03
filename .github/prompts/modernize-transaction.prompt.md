---
name: modernize-transaction
description: Implement one approved legacy behavior as an Azure-ready vertical slice.
agent: azure-modernizer
argument-hint: "approved behavior specification and target transaction"
---

Implement `${input:scope:one approved transaction or batch outcome}` from the
existing cited behavior specification. Add characterization tests first,
preserve legacy assets, keep sensitive values out of external contracts, update
the Azure seam and documentation, then run the full repository validation.

