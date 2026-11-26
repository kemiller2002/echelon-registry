---
id: PF-ANL-004
title: Architectural boundary analysis
classification: public
status: provisional
updated: 2026-11-25
work_item: GOV-TRN-001
---

# Architectural boundary analysis

Scope: the boundaries this governance work touches or depends on. Each boundary states what crosses
it, what must not, the current evidence and the risk.

## 1. Boundaries

| ID | Boundary | May cross | Must not cross | Evidence | Status |
|---|---|---|---|---|---|
| BND-01 | System manifest (`echelon.system/v2`) ↔ repository manifest (`echelon.repository/v1`) | system ids referenced from the repository manifest | capabilities, distribution classes, executable aliases in the repository manifest (REG-REPO-001) | spec/repository-manifest.md; schema closed | defined 2026-11-25 |
| BND-02 | Registry ↔ systems at run time | capability discovery results (`available`/`unavailable`/`misconfigured`) | portfolio or repository-manifest reads at run time (REG-REPO-004) | registry README invariant | holds by design |
| BND-03 | Registry (what exists) ↔ Knowledge Store (representation and retrieval) | generated relationship records, task projections | a second portfolio authority inside the Knowledge Store; Knowledge Store writes into registry records | KSI-* requirements | defined 2026-11-25 |
| BND-04 | Organization administration (authoritative policies, internal register) ↔ Registry (public projections) | allow-listed fields with source digest | `internal` and `publication` fields; private repository contents; unreviewed internal research | tools/portfolio projection + property tests | implemented |
| BND-05 | Praxis (repository governance) ↔ Registry (portfolio) | RM-* obligations: agents evaluate and propose registry updates | Praxis failing a repository's own validation because the Registry is unreachable | praxis requirements/REGISTRY-MAINTENANCE.md (RM-15) | proposed |
| BND-06 | Due diligence (questions, answers, claim status) ↔ Assurance (controls, evidence architecture) | control ids, evidence class and status | restricted evidence into the public due diligence repository | due diligence governance; assurance SECURITY-AND-CONFIDENTIALITY.md | proposed |
| BND-07 | Internal verification ↔ independent assessment | evidence references, scope, verifier identity | presenting internal results as certification (POL-ORG-TRN-001 §6) | claim status vocabulary | policy proposed |
| BND-08 | Public repositories ↔ restricted information | opaque references (ID, class, status) | credentials, personal data, unremediated vulnerabilities, incident evidence | STD-ORG-CLS-001 §5 | **violated once** (credential exposure, OD-GOV-012) |
| BND-09 | Application code ↔ browser (Limen) | typed capability requests | application-authored JavaScript (SEC-UI-002) | Limen; HelixNote vendored Limen copy (2026-10-05 quality inventory HLX-F2) | partially violated (vendored copy) |
| BND-10 | Applications ↔ database (Strata) | typed, parameterized operations | dynamic SQL (SEC-DB-001) | Strata; Census consumes | not yet verified in consumers |

## 2. Boundary risks requiring decisions

1. **BND-05 enforcement strength.** Making registry maintenance a blocking Praxis validation would
   violate optional-provider independence if the check needs the Registry online. The requirement
   therefore makes the *local* manifest check blocking and the *central* reconciliation advisory
   (RM-15, RM-16).
2. **BND-04 manual step.** The projection runs locally because the source is private; CI in the
   public registry can verify schema and digest format but not that the digest matches the private
   source. A reviewer with access must confirm (OD-GOV-019: automate with a private CI job that opens
   a pull request in the registry).
3. **BND-06 visibility conflict.** echelon-assurance was directed private and is public; until resolved
   the assurance repository must follow public rules (it does, per its own confidentiality file).
