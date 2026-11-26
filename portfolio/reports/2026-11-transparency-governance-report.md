---
id: PF-RPT-2026-11
title: Organizational transparency, portfolio governance and continuous assurance — report and progress ledger
classification: public
updated: 2026-11-25
work_item: GOV-TRN-001 (echelon-registry); WI-0110 (praxis)
status: iteration 1 complete; owner decisions outstanding; not "done"
---

# Report: transparency, portfolio governance and continuous assurance (iteration 1)

## 1. Outcome in one paragraph

Echelon now has proposed policies for transparency, classification, disclosure and vulnerability
disclosure; the first complete, validated inventory of its repositories (64 named, 19 private counted
in aggregate) and initiatives (29, INIT-001..012 preserved; the transparency initiative was renumbered INIT-029 on 2026-11-26 because INIT-012 was already assigned to continuous availability); a repository manifest contract that
extends the Registry without changing existing contracts; an F# validator that enforces the public
portfolio's disclosure rules by construction; and requirements for Praxis, the Knowledge Store, due
diligence and continuous assurance. The most important findings are not documents: **a credential is
exposed in a public repository**, no public repository has a `SECURITY.md`, and public records
contradict each other in ways that would undermine, not build, trust. Nothing was adopted, merged,
archived or made public/private; those are owner decisions.

## 2. Deliverable index

| # | Deliverable | Location | Status |
|---|---|---|---|
| 1 | Organizational transparency policy | org-admin `policies/transparency/organizational-transparency-policy.md`; public copy `portfolio/governance/policies/` | proposed |
| 2 | Information classification standard (+ machine-readable model) | org-admin `policies/information-classification/` | proposed |
| 3 | Public disclosure standard | org-admin `policies/disclosure/public-disclosure-standard.md` | proposed |
| 4 | Vulnerability disclosure policy | org-admin `policies/security/vulnerability-disclosure-policy.md`; template `portfolio/governance/templates/SECURITY.md` | proposed |
| 5 | Comprehensive repository inventory | `portfolio/repositories.json` (public); org-admin `portfolio/repository-inventory-internal.md` (private) | provisional seed, validated |
| 6 | Public organizational portfolio | `portfolio/README.md`, `portfolio/generated/` | published on branch |
| 7 | Initiative register | org-admin `portfolio/initiatives.json` (authoritative), `portfolio/initiatives.json` (projection) | v1.0.0 |
| 8–9 | System responsibility and capability ownership matrices | `portfolio/analysis/responsibility-and-capability-matrix.md` | provisional |
| 10 | Dependency and relationship map | `portfolio/analysis/dependency-and-relationship-map.md` | provisional |
| 11 | Duplication analysis | `portfolio/analysis/duplication-and-drift.md` | provisional |
| 12 | Architectural boundary analysis | `portfolio/analysis/architectural-boundary-analysis.md` | provisional |
| 13 | Repository manifest specification | `spec/repository-manifest.md`, `schemas/repository-manifest-v1.schema.json` | proposed |
| 14 | Praxis registry maintenance requirements | praxis `requirements/REGISTRY-MAINTENANCE.md` (WI-0110, blocked on approval) | proposed |
| 15 | Knowledge Store integration requirements | echelon-knowledge-store `research/portfolio-integration/requirements.md` | proposed |
| 16 | Due diligence governance updates | echelon-due-diligence `governance/TRANSPARENCY-ALIGNMENT.md` | proposed |
| 17 | Continuous assurance requirements | echelon-assurance `requirements/continuous-assurance-requirements.md` | proposed |
| 18 | Publication safeguards requirements | STD-ORG-PUB-001 §6 (PUB-SG-01..16); validator `tools/portfolio` | partly implemented |
| 19 | Transparency competitive advantage research | `portfolio/research/transparency-competitive-advantage.md` | complete (iteration 1) |
| 20 | Public trust center requirements | `portfolio/governance/trust-center-requirements.md` | proposed |
| 21 | Measurement framework | `portfolio/governance/measurement-framework.md` | proposed, first baselines |
| 22 | Adversarial review | `portfolio/governance/adversarial-review.md` | complete (iteration 1) |
| 23 | Roadmap | `portfolio/governance/roadmap.md` | proposed |
| 24 | Open decisions register | `portfolio/governance/open-decisions.md` | 26 decisions, 3 questions |

## 3. Key findings

1. **Accidental disclosure (P0).** A live-looking AI provider key is committed in a public research
   repository (since 2026-10-13). Details are Restricted and were reported to the owner directly
   (OD-GOV-012). Deleting it from Git does not contain it; revocation does.
2. **No vulnerability disclosure path:** 0 of 57 public Echelon repositories have `SECURITY.md`.
   Only Tutela runs a secret scan, on itself.
3. **Drift in public records** (DRF-01..09): version facts disagree, registry release records lag
   tags, unfilled templates read as charters.
4. **Unregistered systems** (Vitium, Causa, Specula; EDF undecided) and **unowned capabilities**
   (runtime monitoring, auditor evidence export, tamper-evident storage).
5. **Conflicting assignment** of security rule ownership (Aegis in the standard vs Tutela's charter).
6. **Overlaps** worth a decision, each with a counterargument recorded (DUP-01..11). No consolidation
   is recommended on functional overlap alone.
7. **Implemented but unused integrity controls**: Praxis attested completion and Dokimos completion
   verification exist and are not enabled (from the echelon-assurance integration assessment).
8. **Evidence on transparency**: strong for coordinated disclosure and verifiable designs; vendor-only
   for sales and questionnaire benefits; strong legal risks for IP and public security statements.
   The policy was amended accordingly (IP category, §12).
9. **Conflicts with earlier directions**, preserved not resolved: echelon-assurance visibility
   (OD-GOV-002); Causa's approved public test key (OD-GOV-014); Praxis branch-name rule (OD-GOV-003).

## 4. Decisions and assumptions made autonomously (all reversible)

| Decision | Rationale | Alternative |
|---|---|---|
| Authoritative policies in private org-admin; public byte-identical copies with digests in the registry | brief: keep policies in their repository, publish approved versions | move authority to a public repository (OD-GOV-004) |
| Repository manifest as a companion (`echelon.repository/v1`), not a v3 system manifest | v2 is closed and consumed by distribution; a repository ≠ a system | extend systems-v2 (rejected: breaking) |
| Initiative register authoritative as JSON in org-admin, projected by allow-list | restricted fields excluded by construction | hand-maintained public copy (rejected: drift, leakage) |
| Private repositories named publicly only where already public (DD-001); 19 counted in aggregate | brief: do not expose private identities without evaluation | name all (rejected) |
| Personal and third-party client repositories excluded from Echelon records | classification standard §6 | — |
| Restricted incident details kept out of Git, including private repositories | STD-ORG-CLS-001 §5 | record in org-admin (rejected) |
| New initiatives only where distinct evidence exists; themes mapped otherwise | avoid inflating the register | one initiative per theme (rejected) |
| Praxis WI-0110 blocked, not completed | requirements await approval; no "requirements" completion class | complete as research (rejected: misclassification) |

## 5. Verification performed

| Check | Command | Result |
|---|---|---|
| Portfolio tool unit, property and acceptance tests | `dotnet test tools/portfolio/Echelon.Portfolio.slnx -c Release` | 38 passed, 0 failed |
| Public portfolio validation | `echelon-portfolio validate --root . --today 2026-11-25` | 0 errors, 0 warnings |
| Generated views current | `echelon-portfolio render --root . --check` | current |
| No internal fields in projection | grep for `publication`/`internal`/internal notes text in `portfolio/initiatives.json` | none |
| Praxis validation (praxis repository) | `./praxis validate` | passed |
| Organization administration tests | `npm test` | passed (business plan build, quality inventory 223 findings / 42 repositories) |

Not verified: GitHub repository settings (secret scanning, private vulnerability reporting) are not
observable from repository contents; dependency graph not built from package references; private
repository contents not read (except organization administration).

## 6. Continuation instructions

1. Read `portfolio/governance/open-decisions.md`; do nothing that depends on an undecided item.
2. P0: confirm with the owner that OD-GOV-012 is contained before any other publication work.
3. When the owner adopts or amends a policy in org-admin, copy it byte-for-byte into
   `portfolio/governance/policies/` and update the digest table.
4. When the internal initiative register changes, run `project-initiatives`, then `render`, then
   `validate`; commit both repositories together.
5. Next agent work items in priority order: roadmap R-04, R-05, R-08, R-09, R-11, R-12, R-14, R-15.
6. Re-run the inventory brief (kept with the session; reproducible: for each repository read README,
   AGENTS, charter, handoff, ros.json, project files, workflows, CHANGELOG, SECURITY, LICENSE; record
   with file references) at least every 180 days or after R-09 makes it unnecessary.
7. Pull requests for this work are drafts in each repository on branch
   `claude/echelon-transparency-governance-rvt82z`; the echelon-due-diligence and echelon-assurance
   repositories each have a parallel research branch — reconcile on merge, do not overwrite.
