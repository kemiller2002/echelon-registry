---
id: STD-ORG-CLS-001
title: Information Classification Standard
status: proposed
version: 0.1.0
classification: public
owner: organization-governance (accountable human: Echelon Foundry owner)
created: 2026-11-25
updated: 2026-11-25
review_cycle: annual
machine_readable: policies/information-classification/classification-model.json
public_projection: kemiller2002/echelon-registry portfolio/governance/policies/information-classification-standard.md
related: [POL-ORG-TRN-001, STD-ORG-PUB-001, POL-ORG-VDP-001]
---

# Information Classification Standard

**Status: proposed.** Implements POL-ORG-TRN-001. The machine-readable model
[`classification-model.json`](classification-model.json) is normative for tooling; where it and this
text disagree, this text governs and the JSON must be corrected.

## 1. Levels

Four levels were evaluated and all four are adopted. A three-level model (Public / Internal /
Confidential) was considered and rejected because secrets, keys and sensitive personal or health data
need handling rules (no AI processing by external providers, no repository storage at all) that would
over-restrict ordinary confidential business material if merged.

| Level | Definition | Typical content |
|---|---|---|
| **Public** | Approved for anyone. Disclosure causes no harm, or the benefit clearly exceeds the harm. | policies, standards, architecture, research, roadmaps, control status, approved due diligence answers |
| **Internal** | For Echelon people, contracted agents and tools. Disclosure causes minor harm or is simply not yet decided. Time-boxed: may not be used as a permanent home for information that is not harmful (POL-ORG-TRN-001 §5.3). | working drafts awaiting a publication decision, internal coordination, unreleased plans under active review |
| **Confidential** | Limited to named roles with need-to-know. Disclosure causes material harm to Echelon, a customer or a third party. | contracts and NDA material, negotiated commercial terms, inventions awaiting a patent decision and trade-secret candidates, private repository contents not evaluated for disclosure, non-public financials, licensed third-party content, client identities |
| **Restricted** | Limited to named individuals; handling is enforced technically. Disclosure causes severe harm, enables attack, or breaches law. | credentials, secrets, private keys, production access details, unremediated exploitable vulnerabilities, sensitive incident evidence, personal data, PHI, financial account data, privileged legal communications |

## 2. Handling requirements by level

| Requirement | Public | Internal | Confidential | Restricted |
|---|---|---|---|---|
| **Permitted audiences** | anyone | Echelon people and authorized agents | named roles with need-to-know; counterparties under NDA where the contract allows | named individuals only; recorded access list |
| **Storage** | any Echelon repository or site, including public repositories | private repositories or Echelon-controlled stores | private repositories or access-controlled document stores; never public repositories | secret manager, restricted evidence store or encrypted store with access logging; **never any Git repository, public or private**, never CI logs, issues or chat |
| **Access** | none | authenticated Echelon account | authenticated, least privilege, reviewed at least annually | authenticated with MFA, least privilege, access logged, reviewed at least quarterly |
| **Publication** | allowed; projections cite source and hash | requires a classification decision first (STD-ORG-PUB-001 §3) | only an approved sanitized summary may be published | never; at most an opaque reference (ID, severity class, status) |
| **Retention** | indefinite; superseded versions retained with supersession links | until decided or superseded; reviewed each cycle | per contract or legal requirement; default 7 years for business records | minimum necessary; secrets rotated and destroyed on rotation; personal data per privacy obligations |
| **AI processing** | any provider | approved providers whose terms exclude training on submitted data | approved providers under contractual data-protection terms, minimum necessary content, no persistent agent memory of the content | no external AI processing; local or no processing; agents may handle opaque references only |
| **Evidence handling** | may be committed in public repositories | private repository | access-controlled evidence store; public records may hold digest and reference only | restricted evidence store; public records may hold opaque ID, class, status and digest only if the digest itself discloses nothing |
| **Declassification** | n/a | owner or delegate records a decision; may move to Public | owner decision with recorded justification review; usually via sanitized projection | owner decision only, after remediation or expiry of the harm; usually via summary, never raw material |
| **Incident response** | none for disclosure; correct inaccuracies publicly | assess; usually no external action | containment and assessment under STD-ORG-PUB-001 §7 | immediate containment, rotation or revocation, incident assessment and notification analysis under STD-ORG-PUB-001 §7 and POL-ORG-VDP-001 |

## 3. Classifying

- The creator classifies when creating. Agents must classify every record they create that enters a
  structured register (repository inventory, initiative register, due diligence answers, evidence).
- Classify by **content**, not by location. Moving a record does not change its class.
- An aggregate is classified at least at the level of its most sensitive element **unless** it is a
  projection that structurally excludes the more sensitive fields.
- Aggregation risk: individually Public facts can become sensitive in combination (for example a
  precise inventory of unpatched component versions mapped to deployed hosts). Assessors consider
  aggregation when publishing operational detail.
- When unsure between two levels, choose the higher **and** record a review date no more than one
  review cycle out; do not leave uncertainty permanent.

## 4. Labelling

- Markdown records: front-matter field `classification:`.
- JSON records: field `classification` using the values in `classification-model.json`.
- Repositories: `.echelon/repository.json` field `informationClassification.maximumPermitted` (registry
  spec `spec/repository-manifest.md`).
- Unlabelled content in a public repository is treated as asserted-Public; publishing it is the
  author's classification decision.

## 5. Classification is not visibility

| | Public repository | Private repository |
|---|---|---|
| Public information | correct home | permitted; consider publishing |
| Internal information | **not permitted** | correct home |
| Confidential information | **not permitted** | permitted only with access review; prefer a document store for contracts and NDA material |
| Restricted information | **not permitted, including history, issues, PRs, CI logs, artifacts, generated docs** | **not permitted** (use the secret manager or restricted evidence store) |

A private repository is shared with every collaborator, installed GitHub App, CI integration and AI
provider granted access. Treat private repositories as Confidential at most.

## 6. Repository identity disclosure

The *existence and name* of a repository is itself information. Public portfolio records list public
repositories. Private repositories appear in public projections only after a disclosure evaluation
records that their identity is Public; otherwise public projections report only aggregate counts.
Repositories that are personal and unrelated to Echelon are out of the Echelon inventory and are not
named in any Echelon record.

## 7. Restriction record (machine-readable)

Every Internal-for-more-than-one-cycle, Confidential or Restricted classification of an item that
would otherwise be default-public carries:

```json
{
  "classification": "confidential",
  "justification": {
    "category": "contractual | personal-data | security-exploitable | credential | legal-privilege | third-party-rights | competitive-specific | incident-evidence | intellectual-property | other",
    "harm": "what disclosure would cause",
    "decidedBy": "role or person",
    "decidedOn": "YYYY-MM-DD",
    "reviewBy": "YYYY-MM-DD"
  }
}
```

The JSON Schema for this object is `classification-model.json#/$defs/restriction`.

## 8. Open questions

OD-GOV-005 (approved AI provider list), OD-GOV-006 (retention periods require legal review),
OD-GOV-007 (restricted evidence store selection). See the open decisions register.
