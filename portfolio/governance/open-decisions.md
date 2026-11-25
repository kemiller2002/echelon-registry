---
id: PF-GOV-OD
title: Open decisions and unresolved questions register (organizational governance)
classification: public
updated: 2026-11-25
work_item: GOV-TRN-001
---

# Open decisions and unresolved questions

Decisions belong to the accountable owner unless noted. Agents may prepare options; they do not decide
these. "Default if undecided" is what agents do meanwhile (always the reversible option).

| ID | Decision or question | Options | Recommendation | Default if undecided | Blocks |
|---|---|---|---|---|---|
| OD-GOV-001 | Adopt POL-ORG-TRN-001, STD-ORG-CLS-001, STD-ORG-PUB-001, POL-ORG-VDP-001 | adopt as is · adopt with changes · reject | adopt with review of §4 (confidential list) and VDP safe harbour | treat as working defaults; publish nothing previously restricted | trust center, VDP |
| OD-GOV-002 | echelon-assurance visibility: owner earlier directed **private**; repository is **public**; new policy makes assurance research public by default | keep public (consistent with the policy) · make private (prior directive) | keep public; the repository's own rules already exclude restricted material | follow public-repository rules; no visibility change | assurance publication |
| OD-GOV-003 | Praxis rule "work item id = branch name" vs platform-assigned branch names for cross-repository sessions | amend Praxis rule for hosted sessions · keep and reconcile post hoc | amend (record branch↔work item mapping in the execution record) | reconcile post hoc | Praxis adoption of RM-* |
| OD-GOV-004 | Where are Public-classified policies authored? Currently authoritative in private organization administration with public projections in the registry | keep (projection) · move authority to a public governance repository | move authority to a public repository once adopted; removes the projection step | projections with source digest | duplication of policy text |
| OD-GOV-005 | Approved AI providers per classification level | — | list providers whose terms exclude training on submitted data | Internal and above: no new providers | STD-ORG-CLS-001 §2 |
| OD-GOV-006 | Retention periods (legal review) | — | counsel review | defaults in STD-ORG-CLS-001 | evidence retention |
| OD-GOV-007 | Restricted evidence store and access-controlled document store | GitHub private advisories + encrypted object storage · compliance platform | decide with the assurance evidence architecture | no restricted evidence is collected | continuous assurance, confidential answers |
| OD-GOV-008 | Secret scanner standard | Tutela `scan-secrets` · external (gitleaks) · both | both: external for breadth, Tutela for Echelon patterns | enable GitHub secret scanning + push protection | PUB-SG-01/02 |
| OD-GOV-009 | Publication approver when the owner is unavailable | — | name a delegate or accept delay | delay | publication |
| OD-GOV-010 | Security contact address and `security.txt` | — | dedicated role address | GitHub private vulnerability reporting only | VDP-T3 |
| OD-GOV-011 | Legal review of VDP safe harbour | — | review before publishing the VDP as adopted | publish as proposed with "not yet adopted" | VDP adoption |
| OD-GOV-012 | **Accidental credential exposure in a public repository** (Restricted; details out of band) | — | revoke the credential immediately; then remove from tip; history rewrite optional | — | PUB-SG-03 baseline |
| OD-GOV-013 | Publish internal EDF validation result (negative results are default-public) | publish · keep internal with justification | publish with methodology | not published | INIT-020 credibility |
| OD-GOV-014 | Causa test deployment key embedded in a public site (owner decision ODR-COMP-P13) vs POL-ORG-TRN-001 §11 "no exceptions" for credentials | keep exception (scoped, short-lived, test data only) and amend §11 · remove exception | remove the exception; use a backend token exchange or an unauthenticated read-only demo | exception unused (secrets unset) | Causa demo |
| OD-GOV-015 | Review the internal commercial investigation for adoption and publication | — | review; publish the opportunity inventory without founder-specific material | internal | INIT-027 |
| OD-GOV-016 | Is `census` an Echelon repository? | — | owner states | excluded from the Echelon inventory | inventory completeness |
| OD-GOV-017 | Should Praxis provenance record e-mail addresses in public repositories? | keep · replace with stable actor ids | stable ids | keep | PUB-SG-04 noise |
| OD-GOV-018 | Licence policy per repository class (PolyForm Noncommercial vs MIT vs none) | — | decide per class; "none" is not acceptable for public research | unchanged | publication of research |
| OD-GOV-019 | Automate the initiative projection with a private CI job that opens a registry pull request | — | yes, once projections are routine | manual run of `project-initiatives` | BND-04 |
| OD-GOV-020 | Reconcile three overlapping artefacts: assurance trust center specification + control catalog, due diligence answers, registry trust center requirements | — | registry owns information architecture and claim provenance; assurance owns controls and tiers; due diligence owns answers | keep separate with cross-references | trust center |
| OD-GOV-021 | Duplication findings DUP-01..DUP-11 (consolidate, separate or accept) | per finding | see duplication analysis | no consolidation | — |
| OD-GOV-022 | Dispositions ABN-01..05 (dormant, superseded, completed work) | per item | see duplication analysis | no archival | — |
| OD-GOV-023 | Register Vitium, Causa, Specula; classify EDF (system or framework bundle) | — | register identities without `provides` | unregistered | GAP-02/03 |
| OD-GOV-024 | Owner of runtime monitoring and production logging | existing system · new system | do not create a new system until Unbound's monitoring requirements are confirmed | unowned | GAP-05 |
| OD-GOV-025 | Security rule registry owner: Aegis (per standard) or Tutela | — | Tutela | unchanged standard | DUP-03 |
| OD-GOV-026 | Intellectual-property protection decision before publication (who decides, by when) | — | owner decides at research milestone reviews; default is publish | publish | POL-ORG-TRN-001 §4 |

## Unresolved questions (no decision yet framed)

- Q-GOV-01 Does publishing the full initiative register (including dormant and rejected work) help or
  hurt the commercial position? Measure enquiries citing public material (metric M-20) before judging.
- Q-GOV-02 Which enterprise buyers will Unbound face first, and which questionnaires (needed to scope the
  trust center).
- Q-GOV-03 Is a single accountable owner a SOC 2 and vendor-risk blocker (separation of duties)? See the
  adversarial review.
