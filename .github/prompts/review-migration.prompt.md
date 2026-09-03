---
name: review-migration
description: Review a CardDemo modernization diff for behavioral and platform risk.
agent: migration-reviewer
---

Review the current diff against cited legacy behavior. Focus on incorrect field
semantics, invented rules, missing negative paths, sensitive-data leakage,
state/consistency assumptions, and Azure identity, image, ingress, probe,
scaling, and observability failures. Report only high-confidence actionable
findings with files and lines.

