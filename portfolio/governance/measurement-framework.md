---
id: PF-GOV-MF
title: Organizational measurement framework
classification: public
status: proposed
updated: 2026-11-25
work_item: GOV-TRN-001
---

# Organizational measurement framework

Rules: **no fabricated baselines.** Where a measurement does not yet exist the baseline is recorded as
`unknown` with its collection method. Metrics measure effectiveness, not documentation volume:
counts of documents, pages or records are never targets.

"Computed 2026-11-25" values come from `tools/portfolio validate` and the 2026-11-25 inventory; they are
first measurements, not trends.

| ID | Metric | Definition | Collection method | Baseline (2026-11-25) | Target (proposed) |
|---|---|---|---|---|---|
| M-01 | Repositories registered | Echelon repositories with a portfolio entry ÷ Echelon repositories identified | `portfolio/repositories.json` vs internal inventory | 64 named + 19 aggregate of 83 identified = 100% counted; **77% named** | 100% named or justified |
| M-02 | Repository descriptions verified | entries not `provisional` and `source: manifest` ÷ entries | validator metrics | 38 non-provisional of 64; **0 manifest-sourced** | ≥ 80% manifest-sourced |
| M-03 | Initiatives with current status | initiatives with `lastReview` ≤ 180 days ÷ initiatives | validator PF010 | 28/28 (all reviewed 2026-11-25) | ≥ 95% sustained |
| M-04 | Capabilities with identified owners | capability areas in the ownership matrix with an owner ÷ areas | ownership matrix | 28 of 31 areas (3 unowned: GAP-05/06/07) | 100% owned or explicitly unowned with decision |
| M-05 | Conflicting responsibilities | open DUP-* findings rated moderate or high confidence | duplication analysis | 8 | decreasing; each resolved by decision, not by deletion |
| M-06 | Duplicate research efforts | research programs with overlapping questions | duplication analysis (DUP-01, DUP-08) | 2 | decreasing |
| M-07 | Unregistered systems | systems observed but not in systems-v2.json | inventory | 4 (Vitium, Causa, Specula, EDF undecided) | 0 |
| M-08 | Stale registry entries | entries with `lastVerified` > 180 days | validator PF010 | 0 (first build) | < 10% |
| M-09 | Policies with verification methods | adopted policies whose requirements each have a stated verification method | policy review | **unknown** (no policy adopted yet) | 100% |
| M-10 | Controls with evidence | controls with at least one current evidence record ÷ controls | assurance control catalog | **unknown** (catalog in research; no production evidence) | rising |
| M-11 | Evidence generated automatically | automated evidence records ÷ all evidence records | evidence ledger | **unknown** (no ledger) | rising |
| M-12 | Due diligence answers supported by current evidence | approved answers with unexpired evidence ÷ approved answers | due diligence records | **unknown** (0 approved answers) | ≥ 90% |
| M-13 | Human effort per questionnaire | hours from receipt to submission, per questionnaire, with question count | time log per questionnaire (Chrona when available) | **unknown** (no questionnaire logged) | measure first; then decide |
| M-14 | Customer assurance turnaround | calendar days from first security request to completed response | request log | **unknown** | measure first |
| M-15 | External assurance expenditure | spend on audits, penetration tests, scanners, compliance tooling | accounting records (Summa when available) | **unknown** | measure first |
| M-16 | Accidental disclosure incidents | confirmed exposures of Confidential/Restricted information in a public surface | incident records (opaque ids) | **1** identified on 2026-11-25 (OD-GOV-012) | 0 new |
| M-17 | Expired confidentiality restrictions | restrictions past `reviewBy` without a decision | classification restriction records; validator PF012 | 0 recorded restrictions | 0 |
| M-18 | Time to locate authoritative knowledge | median minutes (or tokens) for an agent to answer "who owns capability X / what does repository Y do" | scripted agent tasks, Knowledge Store benchmark (HY-KSI-01) | **unknown** | measure first |
| M-19 | Time to onboard an engineering agent | tokens and minutes until an agent in a new repository states purpose, boundaries, initiatives and governing policies correctly | scripted onboarding task with rubric | **unknown** | measure first |
| M-20 | Inbound enquiries citing public material | enquiries (customers, candidates, researchers) that cite public records | enquiry log field | **unknown** | observe; no target |
| M-21 | Strategic decisions with recorded rationale | material decisions with a DF-/OD- record including rationale ÷ material decisions | decision registers | **unknown** (no single register across repositories) | 100% |
| M-22 | Public-repository safeguard coverage | public repositories with secret scanning + push protection + SECURITY.md ÷ public repositories | GitHub settings + repository scan | SECURITY.md **0 of 57**; scanning **unknown** | 100% |

## Anti-gaming notes

- M-02 rewards manifests, not text length; a manifest that marks everything `provisional` still counts
  as unverified.
- M-05/M-06 are reduced only by recorded decisions; removing a finding without a decision is a defect.
- M-16 must not be suppressed by under-reporting; any exposure found by a safeguard counts.
- M-13/M-14 are measured before any target is set: published vendor claims are not baselines
  (research memo §3.2).
