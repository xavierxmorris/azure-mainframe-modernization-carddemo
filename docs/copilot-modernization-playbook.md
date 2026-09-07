# GitHub Copilot modernization playbook

Last reviewed: 2026-09-03

For the **7 September 2026 Java/.NET extension**, see
[target selection, setup, and shared numeric examples](java-dotnet-modernization.md).
It preserves this playbook's read-only scope and separates supported framework
upgrades from AI-assisted COBOL behavior replacement.

## Why the repository is agent-ready

Current GitHub Copilot surfaces can share repository instructions, path-specific
instructions, `AGENTS.md`, custom agents, agent skills, prompt files, and a
cloud-agent setup workflow. This repository assigns one responsibility to each
layer so context remains precise and reusable.

Official references:

- [Repository instructions](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/add-custom-instructions/add-repository-instructions)
- [Agent skills](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills)
- [Custom agents configuration](https://docs.github.com/en/copilot/reference/custom-agents-configuration)
- [Cloud-agent environment setup](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/customize-the-agent-environment)
- [VS Code prompt files](https://code.visualstudio.com/docs/agent-customization/prompt-files)
- [Azure Developer CLI schema](https://learn.microsoft.com/azure/developer/azure-developer-cli/azd-schema)

The Azure Developer CLI schema source was updated on 2026-08-26. The repository
uses Container Apps API `2026-01-01`, which was available in the active Azure
subscription on the assessment date.

## Model-assisted workflow

### 1. Discover

- Identify the transaction ID, program, BMS map, COMMAREA, copybooks, VSAM
  files, Db2/IMS objects, MQ queues, JCL, and scheduler dependencies.
- Use call and data-flow evidence rather than filenames alone.
- Record ambiguity instead of inventing undocumented behavior.

### 2. Specify

- Convert COBOL paragraphs and condition names into observable invariants.
- Capture fixed-width offsets, signed representations, date conventions,
  status codes, pagination rules, and error messages.
- Separate business behavior from CICS, VSAM, JCL, and platform mechanics.

### 3. Characterize

- Add tests against checked-in synthetic records or purpose-built fixtures.
- Include normal, boundary, malformed, and negative-overpunch cases.
- Verify what must never be emitted, not only what should be returned.

### 4. Implement one seam

- Prefer a vertical slice around one inquiry or batch outcome.
- Keep legacy assets immutable and use adapters at the boundary.
- Avoid introducing a shared “legacy utility” dumping ground.

### 5. Compare

- Compare legacy and modern outputs at field and business-rule level.
- Use deterministic fixtures and machine-readable difference reports.
- Treat unexplained differences as failures, not acceptable LLM variation.

### 6. Review

- Run the `migration-reviewer` agent or the `validate-azure-slice` skill.
- Check sensitive-field boundaries, decimal/date fidelity, error semantics,
  accessibility, idempotency, and cloud failure modes.

### 7. Deploy and observe

- Deploy through `azd` and Bicep.
- Verify `/health`, `/api/summary`, one account detail, logs, active revision,
  image identity, and replica settings.
- Capture findings in the relevant ADR or backlog item.

## Good Copilot tasks

- “Trace transaction CCLI from CICS definition through programs and copybooks,
  then produce a behavior specification with citations.”
- “Add characterization tests for COBOL signed-overpunch values used by
  `DALYTRAN-AMT`; do not change production code until the tests are reviewed.”
- “Implement the account inquiry slice from the approved behavior spec and
  compare every returned field to the source copybook.”
- “Review this migration diff for invented business behavior and sensitive-data
  leakage; ignore cosmetic style.”

## Poor Copilot tasks

- “Rewrite the entire mainframe in microservices.”
- “Convert all COBOL to C#.”
- “Make it cloud native.”
- “Use AI to infer missing requirements and proceed.”

Large language models accelerate inventory, explanation, test generation,
mapping, and review. They do not replace business-owner validation, production
data reconciliation, security architecture, or cutover controls.
