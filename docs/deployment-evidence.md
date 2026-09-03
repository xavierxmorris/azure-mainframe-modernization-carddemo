# Azure deployment evidence

Deployment verified: 2026-09-03

| Property | Verified value |
|---|---|
| Region | Australia East |
| Resource group | `rg-carddemo-xm-dev` |
| Container App | `ca-carddemo-xm-dev-5gp7yl` |
| Public endpoint | `https://ca-carddemo-xm-dev-5gp7yl.jollysmoke-f0a69d38.australiaeast.azurecontainerapps.io` |
| Runtime state | `Succeeded` / `Running` |
| Active revision | `ca-carddemo-xm-dev-5gp7yl--r0903004653` |
| Revision health | `Healthy` |
| Registry | `crcarddemoxmdev5gp7ylxgkuatg.azurecr.io` |
| Image | `carddemo-web@sha256:6a4b144745da5912a6a084c78c29d664ff42771e8913e0213ee781fa07a19e77` |
| Registry authentication | User-assigned managed identity with `AcrPull` |
| Insecure HTTP | Disabled |
| Scale | Minimum 0, maximum 3 |
| Container resources | 0.25 vCPU, 0.5 GiB |
| Platform | Linux/amd64, non-root user |
| Health route | `/health` |

The live verification against the deployed revision confirmed:

- `/health` returned HTTP 200.
- `/api/summary` returned 50 accounts, 50 cards, 50 customers, and 300
  transactions, and no aggregate monetary values.
- `/api/accounts/00000000001` returned a linked account whose card was masked to
  `•••• •••• •••• 3697` and whose people were pseudonymized to
  `Sample customer 0001`.
- `/api/accounts/4111111111111111` returned HTTP 400 and did not echo the value.
- `/api/accounts?q=4111111111111111` returned HTTP 400 and did not echo the value.
- `/api/accounts/99999999999` returned HTTP 404 without echoing the identifier.
- The live response included CSP, `X-Content-Type-Options`, referrer policy, and
  one-year HSTS headers.
- The active revision used the exact immutable digest built in Azure Container
  Registry.

Deployments are promoted by immutable digest. `scripts/deploy-azure.ps1` provisions
new environments with the bootstrap image on internal ingress, updates the Container
App to the digest built in Azure Container Registry, waits for the named revision to
report `Healthy`/`Running` with that exact digest, and only then enables external
ingress. Endpoint checks retry to tolerate DNS propagation and scale-from-zero
startup.

Local release evidence also included 31 tests, locked restore, `dotnet format`
verification, exact upstream corpus preservation, `az bicep build`, a production
container build, a non-root and `linux/amd64` platform check, and a container smoke
test. Container image scanning runs in CI with Trivy, failing on fixed HIGH and
CRITICAL findings.

No automated accessibility or responsive-viewport gate exists yet; that work is
tracked in [the modernization backlog](modernization-backlog.md).
