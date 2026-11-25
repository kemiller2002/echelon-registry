---
id: PF-GOV-TC
title: Public trust center requirements (information architecture)
classification: public
status: proposed — requirements only; no application is to be built until these are reviewed (OD-GOV-001, OD-GOV-020)
updated: 2026-11-25
work_item: GOV-TRN-001
related:
  - echelon-assurance trust-center/customer-trust-center-specification.md (research branch, unmerged) — disclosure tiers and questionnaire automation
  - echelon-due-diligence governance/TRANSPARENCY-ALIGNMENT.md — answers and claim status
  - POL-ORG-TRN-001 §6 and §12 — claim discipline and public security statements
---

# Public trust center requirements

## 1. Purpose and boundary

A public view of how Echelon approaches security, reliability, governance, research and
accountability, **generated from authoritative records**, not a separately written marketing version.

| Concern | Owner |
|---|---|
| Information architecture, claim provenance, generation from records (this document) | Echelon Registry portfolio |
| Disclosure tiers beyond Public (prospect, NDA, customer, bank partner), questionnaire automation | echelon-assurance trust center specification |
| Question and answer content | echelon-due-diligence |
| Presentation | Forma (interactive), Folio (printable) — no new system |

The public tier is the only tier in scope here.

## 2. Content (public tier)

| ID | Section | Source record (authoritative) | Exists today? |
|---|---|---|---|
| TC-R-01 | Organizational principles | POL-ORG-TRN-001 projection | proposed policy |
| TC-R-02 | Security architecture (design level) | Secure Construction Standard; SEC-* rules; boundary analysis | proposed standard |
| TC-R-03 | Engineering standards | Praxis governance, Secure Construction Standard | yes |
| TC-R-04 | System portfolio | `portfolio/repositories.json`, `registry/systems-v2.json` | yes (2026-11-25) |
| TC-R-05 | Strategic initiatives | `portfolio/initiatives.json` | yes (2026-11-25) |
| TC-R-06 | Public policies | `portfolio/governance/policies/` | proposed |
| TC-R-07 | Compliance objectives | echelon-assurance regulatory applicability | research |
| TC-R-08 | Verified control status | assurance control catalog with evidence status | **no evidence yet** |
| TC-R-09 | Research findings | research repositories, Tekmerion | partial |
| TC-R-10 | Scanner validation results | Secure Construction Standard deliverable 1 | **not produced** |
| TC-R-11 | Accessibility commitments and conformance status | Forma/Visual Engineering conformance evidence | partial |
| TC-R-12 | Software supply-chain information | registry release records, attestations | partial |
| TC-R-13 | Approved due diligence answers | due diligence answers with `approval` (DDT-03) | **none approved** |
| TC-R-14 | Public incident summaries | POL-ORG-VDP-001 §8 records | none |
| TC-R-15 | Known limitations | `knownLimitations` fields; claim status | yes |
| TC-R-16 | Independent assurance reports or approved summaries | external reports (SOC 3 is general-use; SOC 1/2 are restricted-use) | **none exist** |
| TC-R-17 | Vulnerability disclosure policy and `security.txt` | POL-ORG-VDP-001 | proposed |

## 3. Claim record (every material assurance statement)

**TC-R-20** Each material claim displayed is rendered from a claim record:

```json
{
  "id": "CLM-...",
  "statement": "plain-language claim",
  "source": "record id and path of the authoritative record",
  "scope": "systems, environments, period",
  "verificationStatus": "intended | implemented | verified-internal | assessed-independent",
  "assuranceIndependence": "first-party | second-party | independent-third-party | regulatory-examination | none",
  "effectiveDate": "YYYY-MM-DD",
  "evidence": ["evidence id (class, not content)"],
  "freshness": { "reviewBy": "YYYY-MM-DD", "interval": "P90D" },
  "classification": "public"
}
```

- **TC-R-21** A claim past `reviewBy` is shown as *stale* (or withdrawn); it is never silently shown as current.
- **TC-R-22** No wording implies certification, attestation or audit unless `verificationStatus` is
  `assessed-independent` for exactly that scope. Compliance progress is shown as progress
  ("objective", "in progress", "not started"), never as compliance.
- **TC-R-23** Unfavourable statuses (`unknown`, `not implemented`, `unsupported`) are displayed, not
  filtered (POL-ORG-TRN-001 §5.2).
- **TC-R-24** Prefer linking to evidence over describing controls in prose (legal exposure; research
  §3.9).

## 4. Generation

- **TC-R-30** Generated deterministically from records with an allow-list projection (STD-ORG-PUB-001 §4);
  the generator refuses non-Public records.
- **TC-R-31** Each page shows its generation time, source commit and record ids.
- **TC-R-32** Static output, hosted like other Echelon sites (no new infrastructure); F# generator,
  Forma presentation, Folio printable export. Reuse `tools/portfolio` projections first.
- **TC-R-33** A build fails when a referenced record is missing, stale beyond its interval, or not Public.

## 5. Preconditions before building anything

1. OD-GOV-001 policies adopted.
2. OD-GOV-020: the three overlapping specifications reconciled.
3. P1 publication safeguards implemented (STD-ORG-PUB-001 §6).
4. At least one claim class with real evidence (otherwise the trust center would display only
   intentions, which the adversarial review identifies as a credibility risk).

## 6. Measures

Metrics M-12, M-14, M-15, M-16 in the measurement framework.
