---
applyTo: "examples/java/**,examples/contracts/**,tests/**/SharedOverpunchContractTests.cs"
---

# Java/.NET numeric example

- Keep the Java example dependency-free and target JDK 25.
- `examples/contracts/signed-overpunch.txt` is shared with the .NET tests.
  Use synthetic fields only; never add raw customer/card records.
- Trace widths and scales to the source copybook. ASCII trailing overpunch
  is not EBCDIC decoding, leading-sign handling, or packed COMP-3 parsing.
- Use exact decimal arithmetic with explicit bounds. Do not parse via double.
- Reject unsupported formats; do not convert invalid input to zero.
- Run the Java check and `SharedOverpunchContractTests` for any contract change.
- Leave the legacy corpus and the existing read-only production API unchanged.
- Numeric fixture agreement is not full CICS transaction or mainframe parity.
