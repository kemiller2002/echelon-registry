---
id: STD-ORG-PUB-001
title: Public Disclosure and Publication Safeguards Standard
status: proposed
version: 0.1.0
classification: public
owner: organization-governance (accountable human: Echelon Foundry owner)
created: 2026-11-25
updated: 2026-11-25
review_cycle: annual
public_projection: kemiller2002/echelon-registry portfolio/governance/policies/public-disclosure-standard.md
related: [POL-ORG-TRN-001, STD-ORG-CLS-001, POL-ORG-VDP-001]
---

# Public Disclosure and Publication Safeguards Standard

**Status: proposed.** Defines how information becomes public (§2–§5), the safeguards that prevent
accidental disclosure (§6, requirements `PUB-SG-*`), and the response when disclosure happens anyway
(§7). Implementation status of each safeguard is recorded honestly in §8; most are **not yet
implemented**.

## 1. Design principles

1. **Prevent rather than detect.** Prefer structures where restricted data cannot reach a public
   surface (allow-listed projections, secrets never in Git, typed records without free-text slots for
   sensitive data) over scanning after the fact.
2. **Scanning is supplementary.** A clean scan is not proof that nothing sensitive exists. No report,
   dashboard or claim may state that a repository "contains no sensitive information" on the basis of
   scanning.
3. **Assume publication is permanent.** Deleting a commit, rewriting history, deleting an issue or
   making a repository private does not remove information from forks, clones, caches, archives,
   package registries, search engines or AI training corpora. Response starts from that assumption.
4. **Every public surface counts.** Source files, Git history, tags, release assets, issues, pull
   requests and review comments, Actions logs and artifacts, Pages sites, package metadata, generated
   documentation, wikis, gists and discussions.

## 2. What may be published without a further decision

Content classified Public (STD-ORG-CLS-001) by its author, in a Public-permitted repository, that
contains no element of POL-ORG-TRN-001 §4.

## 3. Publication decision for previously non-public material

Required when: a private record is projected publicly for the first time; a private repository's
identity or contents would be named publicly; an Internal item is declassified; an incident or
vulnerability summary is published; a due diligence answer moves to `claimable`.

The decision record states: item, source, target surface, classification before/after, the reviewer,
date, and residual risks. Agents may **prepare** a decision and a sanitized projection; the accountable
owner (or a named delegate) **approves** first publication of previously Confidential or Restricted
material. Previously Internal material may be published by its owner role.

## 4. Sanitized projections

- A projection is generated from the authoritative record through an explicit **allow-list** of public
  fields. Fields not on the allow-list never reach the output, regardless of content.
- The public schema of a projection rejects unknown fields (`additionalProperties: false`), so a field
  added upstream cannot leak silently; it fails validation until consciously allow-listed.
- Free-text public fields are reviewed (human or agent with review record) at first publication and on
  material change.
- Each projection cites `sourceRecord` and `sourceDigest` so readers and auditors can verify it came
  from an authoritative record without seeing the record.

## 5. AI-generated content

AI-generated public content is held to the same rules as human content, plus:

- it must not introduce facts, figures, citations or claims that are not traceable to a source record
  (the claim status vocabulary of POL-ORG-TRN-001 §6 applies);
- it must not include material the agent saw in Confidential or Restricted inputs; agents preparing
  public outputs work from Public inputs wherever possible (task-specific projections, see Knowledge
  Store integration requirements);
- agent provenance is recorded where the repository uses Praxis provenance.

## 6. Safeguard requirements

Priority: **P1** = required before the first public trust-center publication; **P2** = required within
two review cycles; **P3** = evaluate.

| ID | Safeguard | Requirement | Type | Priority |
|---|---|---|---|---|
| PUB-SG-01 | Secret scanning with push protection | Every public Echelon repository enables GitHub secret scanning **and push protection** (available free for public repositories). Private repositories enable it where the plan allows. | preventive | P1 |
| PUB-SG-02 | Pre-commit secret scanning | Repositories that use Praxis run a local secret scan (for example gitleaks or an equivalent with a pinned version) as part of the commit or validation path. A finding blocks the commit unless a reviewed allow-list entry exists. | preventive | P2 |
| PUB-SG-03 | Git history scanning | Each public repository's full history is scanned once (baseline) and after any history rewrite. Findings are triaged as incidents under §7, not silently rewritten. | detective | P1 |
| PUB-SG-04 | Sensitive and personal information detection | Public projections and generated documentation are checked for personal data patterns (e-mail addresses other than published role addresses, phone numbers, government IDs, account numbers, absolute home-directory paths that disclose personal names). | detective | P2 |
| PUB-SG-05 | Generated artifact scanning | Release assets, packages, generated sites and SBOMs are scanned before publication; builds embed no environment variables, local paths or credentials. | detective | P2 |
| PUB-SG-06 | CI log hygiene | Workflows never echo secrets, use masked secrets only, avoid `set -x` around credentials, and set the shortest practical artifact and log retention for public repositories. | preventive | P1 |
| PUB-SG-07 | Documentation scanning | Documentation and research outputs are included in secret and PII scans (not only source code). | detective | P2 |
| PUB-SG-08 | Dependency metadata scanning | Lockfiles, package manifests and registry metadata contain no private registry tokens, internal hostnames or private repository URLs. | detective | P2 |
| PUB-SG-09 | Research source licensing | Research that quotes or stores third-party content records the source licence or the fair-use basis; licensed questionnaire content (for example SIG, CAIQ) is not stored in public repositories without confirmed rights. | preventive | P1 |
| PUB-SG-10 | Classification checks | Structured records in public repositories carry `classification`; validators reject `internal`, `confidential` or `restricted` values in a public repository and reject unknown fields in public projections. | preventive | P1 |
| PUB-SG-11 | Publication approval | First publication of previously non-public material (§3) requires a recorded decision; registry validation checks that every `private` repository named in a public record has a disclosure decision reference. | preventive | P1 |
| PUB-SG-12 | Projection by construction | Public portfolio, trust center and due diligence projections are generated through allow-listed fields (§4). Hand-maintained public copies of private records are prohibited. | preventive | P1 |
| PUB-SG-13 | Responsible vulnerability disclosure | POL-ORG-VDP-001 is in force and every public repository has a `SECURITY.md` pointing to it. | preventive | P1 |
| PUB-SG-14 | AI-generated content review | Agent-generated public text passes claim-traceability review (§5) before merge; Praxis provenance records the generating agent. | preventive | P2 |
| PUB-SG-15 | Visibility-change guard | Before any repository becomes public, its full history and all surfaces are scanned (PUB-SG-03/04/05) and the owner records a decision. Agents never change visibility. | preventive | P1 |
| PUB-SG-16 | Issue and PR hygiene | Issue and PR templates of public repositories warn against posting secrets, personal data and vulnerability details, and point to the private reporting channel. | preventive | P2 |

## 7. Accidental disclosure procedure

1. **Contain** (minutes): revoke or rotate any exposed credential *first*; rotation is mandatory even if
   the exposure window seems short. Remove the content from the current tip, close or edit the issue or
   comment, delete exposed Actions artifacts and logs, unpublish a package version where the registry
   allows.
2. **Assume copies exist.** Do not treat deletion or history rewriting as remediation. Request cache
   removal (GitHub Support for sensitive data in pull request refs and cached views; search engine
   removal tools) as damage limitation only.
3. **Assess** (hours): what was exposed, its classification, exposure window, forks and clones observed,
   whether the data concerns third parties or personal data, and whether notification duties arise
   (privacy law, contracts, regulators). Record the assessment as a Restricted incident record.
4. **Remediate**: fix the root cause (which safeguard failed or was missing), add a regression check,
   and decide whether a history rewrite is still worth doing for hygiene (it is optional and never a
   substitute for rotation).
5. **Notify** affected parties and authorities where required, within any legal or contractual deadline.
6. **Disclose** a publicly appropriate summary once safe (POL-ORG-VDP-001 §8), including the systemic
   improvement.

## 8. Current implementation status (2026-11-25)

| Safeguard | Status | Basis |
|---|---|---|
| PUB-SG-01 | unknown | repository settings are not observable from repository contents; to verify through GitHub settings |
| PUB-SG-02, -04, -05, -07, -08 | not implemented portfolio-wide | of the repositories inspected on 2026-11-25, only Tutela runs a secret scan in CI (`tutela scan-secrets`, Tutela sensitive-data catalog `TUTELA-SD-V*`, in `tutela/.github/workflows/tutela-foundation.yml`), and only on itself; the echelon-assurance working branch has a minimal pattern validator. A reusable capability therefore exists but is not adopted |
| PUB-SG-09 | partially | due diligence and assurance research record source registers; licence checks not systematic |
| PUB-SG-10, -11, -12 | partially implemented | the registry portfolio validator added on 2026-11-25 enforces public-projection schemas and the disclosure-decision rule for named private repositories |
| PUB-SG-13 | proposed | POL-ORG-VDP-001 drafted; **no** inspected repository (0 of 63 cloned on 2026-11-25) has a `SECURITY.md` |
| PUB-SG-03, -06, -14, -15, -16 | not implemented | — |

## 9. Open questions

OD-GOV-008 (whether to standardize on Tutela's existing secret scanner, a mature external scanner such as gitleaks, or both — Tutela catalog for Echelon-specific patterns, external scanner for breadth — and whether Praxis invokes it as a validation step), OD-GOV-009
(who may approve first publication when the owner is unavailable).
