# Security policy

## Supported code

Security fixes target the `main` branch and the Azure reference implementation.
The preserved legacy assets are retained for analysis and parity; findings in
those assets should still be reported when they affect migration decisions.

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability. Use GitHub private
vulnerability reporting for this repository.

Include the affected path, impact, reproduction steps, and any evidence that
sample sensitive-shaped data can leave the intended masking boundary. Do not
include real credentials, customer data, or payment data.

## Data-handling boundary

The modern application must not return or log:

- CVV values
- Social security numbers
- Government-issued identifiers
- Unmasked card numbers
- Raw fixed-width source records

Future write operations require authentication, authorization, audit events,
idempotency, and a documented data-retention policy before they can be enabled.

