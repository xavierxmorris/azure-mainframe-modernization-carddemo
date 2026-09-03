---
applyTo: "infra/**/*.bicep,azure.yaml,Dockerfile,.dockerignore,scripts/*.ps1,.github/workflows/*.yml"
---

# Azure platform and delivery

- Use Azure Container Apps and Azure Developer CLI conventions in this phase.
- Use stable resource API versions and run a Bicep build after changes.
- Keep ACR admin access disabled; use managed identity with least-privilege roles.
- Keep the container non-root, Linux/amd64 compatible, and bound to port 8080.
- Keep startup, readiness, and liveness probes on unauthenticated `/health`.
- Keep ingress HTTPS-only and preserve scale-to-zero unless an availability
  requirement explicitly changes the cost tradeoff.
- Pin GitHub Actions to immutable commit SHAs with release comments.
- Use OIDC for GitHub-to-Azure deployment; never add client secrets.
- Do not put deployment outputs, `.azure/`, generated ARM JSON, or credentials
  under source control.

