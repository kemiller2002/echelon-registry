---
id: POL-ORG-TRN-001
title: Organizational Transparency Policy
status: proposed
version: 0.1.0
classification: public
owner: organization-governance (accountable human: Echelon Foundry owner)
created: 2026-11-25
updated: 2026-11-25
review_cycle: semi-annual
public_projection: kemiller2002/echelon-registry portfolio/governance/policies/organizational-transparency-policy.md
related:
  - STD-ORG-CLS-001 (policies/information-classification/information-classification-standard.md)
  - STD-ORG-PUB-001 (policies/disclosure/public-disclosure-standard.md)
  - POL-ORG-VDP-001 (policies/security/vulnerability-disclosure-policy.md)
  - Secure Construction Standard v0.1 (policies/security/secure-construction-standard.md)
---

# Organizational Transparency Policy

**Status: proposed.** Not yet formally adopted by the accountable owner. Until adopted it is the
working default for agents and contributors, and nothing in it authorizes changing repository
visibility (see §9).

## 1. Principle

> **Public by default. Confidential by necessity. Secure by construction. Verifiable by evidence.**

Echelon Foundry publishes what it knows, decides, builds and verifies unless a specific, documented
reason justifies restricting it. Security comes from construction, verification and operational
control, never from organizational obscurity, small size or secrecy about architecture.

Transparency is a means, not an end. Published information creates value only when it is accurate,
current, scoped and supported by evidence. A stale or overstated public claim is worse than silence,
so every publication obligation in this policy carries a matching accuracy and freshness obligation.

## 2. Scope

All Echelon Foundry information in any medium: repositories (including Git history, issues, pull
requests, CI logs, build artifacts, generated documentation and releases), websites, reports,
questionnaire answers, agent outputs and knowledge-store records. It binds people and autonomous
agents equally.

## 3. Default-public categories

The following are classified **Public** (STD-ORG-CLS-001) unless an item-specific restriction under
§5 is recorded:

| Area | Examples |
|---|---|
| Principles and standards | organizational principles, engineering standards, Praxis governance, secure construction standard |
| Architecture | architectural decisions and rationale, system responsibilities and non-responsibilities, security architecture (design level) |
| Research | findings, methodologies, limitations, negative results, superseded conclusions |
| Direction | strategic initiatives, product roadmaps, technical requirements, priorities |
| Security and assurance | security policies, verification methodologies, scanner validation results, benchmark results, compliance objectives, control implementation status, non-sensitive capability gaps |
| Accountability | decision rationale, known limitations, corrected mistakes, publicly appropriate incident summaries (POL-ORG-VDP-001 §8) |
| Supply chain and accessibility | dependency and provenance practices, SBOM policy, accessibility commitments and conformance status |
| Due diligence | canonical questions, target answers, approved current answers, claim status (§6) |

## 4. Confidential categories

These are never published. Their classification (Confidential or Restricted) is set by
STD-ORG-CLS-001:

- credentials, secrets, tokens and cryptographic private keys;
- customer, donor, employee and contractor personal information; protected health information;
- confidential financial account information;
- production access information and sensitive operational configuration (addresses, topology that
  enables targeting, IAM bindings, break-glass procedures);
- unremediated exploitable vulnerabilities and exploit-relevant control weaknesses;
- contractually restricted information, information under nondisclosure agreements, and third-party
  information without publication rights (including licensed questionnaire content);
- privileged legal communications;
- sensitive incident investigation evidence;
- information about private individuals, including client identities, without their consent;
- **intellectual property before a protection decision**: an invention whose patentability has not yet
  been decided (publication can defeat novelty; the European Patent Convention has no general grace
  period) and genuine trade-secret candidates (disclosure ends trade-secret status irreversibly). The
  restriction ends when the protection decision is recorded; publishing deliberately as defensive prior
  art is a legitimate outcome of that decision.

## 5. Restricting disclosure requires a justification

1. Restriction is an exception. Each restricted item or class records a **confidentiality
   justification**: the §4 category (or another legitimate basis), the harm disclosure would cause,
   who decided, when, and a review date. The machine-readable field set is defined in STD-ORG-CLS-001 §7.
2. These are **not** legitimate justifications on their own: the information is embarrassing; it shows
   an incomplete or immature capability; it contradicts an earlier decision or claim; it would help a
   competitor understand general strategy; it is unpolished; nobody has reviewed it yet.
3. "Not yet reviewed for publication" is a temporary *state*, not a classification. It may last no
   longer than one review cycle (§7) before a decision is recorded.
4. Intellectual-property restrictions (§4) must name the invention or asset class and a decision date;
   "it might be patentable someday" is not sufficient.
5. Commercial strategy that is genuinely competitively sensitive (for example pricing negotiated with a
   named counterparty, or pre-announcement partnership terms) may be restricted, but the
   justification must identify the specific harm, and the general strategic direction remains public.
6. Where an item mixes public and confidential content, publish a **sanitized projection** (§8)
   rather than restricting the whole.

## 6. Accuracy and claim discipline

Every public statement about Echelon capability, security or compliance states which of these it is:

| Claim status | Meaning |
|---|---|
| `intended` | Echelon plans or aims to do it |
| `implemented` | Code, configuration or procedure exists |
| `verified-internal` | Echelon's own repeatable verification shows it works, with an evidence reference |
| `assessed-independent` | An independent party assessed it, with scope and date |
| `claimable` | Approved for use in customer-facing statements, with scope and expiry |

Internal verification is never presented as independent certification. Compliance progress is never
presented as certification or attestation. Material assurance claims carry source, scope,
verification status, effective date, evidence reference and freshness requirement (see the trust
center requirements in the registry portfolio).

## 7. Review of confidentiality decisions

- Every restriction carries a review date no more than 12 months out (Restricted: 6 months).
- At review the owner either renews the restriction with a refreshed justification, declassifies, or
  publishes a sanitized projection. Expired restrictions without a decision are reported as stale by
  the measurement framework (metric M-17).
- Remediated vulnerabilities, closed incidents and superseded strategies are re-evaluated for
  publication at closure.

## 8. Publication mechanics

- Authoritative records stay in their authoritative repository. Public versions are published as
  **projections** that cite their source record and content hash.
- Projections generated from structured records must exclude restricted fields **by construction**
  (an allow-list of public fields), not by post-generation scanning. Scanning is a supplementary control.
- Publication safeguards, approval and accidental-disclosure response are defined in STD-ORG-PUB-001.

## 9. Repository visibility

Information classification is distinct from repository visibility (STD-ORG-CLS-001 §5). A public
repository must contain only Public information in all of its surfaces, including Git history. A
private repository is not automatically an acceptable home for Restricted information.

**Repository visibility is changed only by the accountable owner, with an explicit recorded
decision.** Agents never change visibility, and never act on this policy by publishing the contents
or identity of a private repository without a recorded disclosure evaluation.

## 10. Responsibilities

| Role | Responsibility |
|---|---|
| Owner (accountable human) | adopts this policy, decides restrictions and declassification, approves first publication of previously restricted material, decides visibility |
| Contributors and agents | classify what they create, keep restricted material out of public surfaces, record justifications, never fabricate claims |
| Praxis | carries the registry-maintenance and classification requirements into repository work (praxis `requirements/REGISTRY-MAINTENANCE.md`) |
| Echelon Registry | hosts the public portfolio and public policy projections |

## 11. Exceptions

Exceptions to §3 are restrictions under §5. Exceptions to §4 do not exist.

## 12. Public security and compliance statements

Public statements about security controls create legal exposure when they are inaccurate (for example
FTC action over encryption claims in 2020; claims based on a public security statement survived a
motion to dismiss in SEC v. SolarWinds in 2024). Therefore every public security or compliance
statement is generated from, or linked to, a record with claim status and evidence (§6), carries an
effective date, and is re-reviewed at its freshness interval. Prefer linking to evidence over
describing controls in prose. Evidence summary: registry
`portfolio/research/transparency-competitive-advantage.md`.

## 13. Measurement

Effectiveness is measured with the organizational measurement framework (registry
`portfolio/governance/measurement-framework.md`), not by volume of published material.

## 14. Open questions

Tracked in the open decisions register (registry `portfolio/governance/open-decisions.md`), in
particular OD-GOV-001 (adoption), OD-GOV-002 (assurance repository visibility conflict) and
OD-GOV-004 (whether public-classified policies should be authored directly in a public repository).
