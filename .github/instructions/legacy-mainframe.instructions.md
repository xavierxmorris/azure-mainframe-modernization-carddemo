---
applyTo: "app/**/*.{cbl,CBL,cpy,CPY,jcl,JCL,bms,asm,mac,prc,ctl,ddl,dcl,dbd,psb,PSB,csd},samples/**/*"
---

# Legacy mainframe assets

- Treat these files as the behavioral source, not as style-refactoring targets.
- Preserve sequence areas, column significance, record lengths, dataset names,
  transaction IDs, program IDs, return codes, and copybook layouts.
- Before changing a program, identify its CICS/JCL entry point, called programs,
  copybooks, files/tables/queues, and downstream jobs.
- Distinguish platform mechanics from business rules in explanations.
- Never infer a requirement from a field name alone; cite the paragraph,
  condition name, SQL, file operation, or JCL step that supports it.
- If a legacy change is necessary, add or update a modern characterization test
  and document why parity requires the legacy edit.

