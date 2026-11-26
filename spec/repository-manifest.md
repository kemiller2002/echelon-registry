# Repository manifest specification (`echelon.repository/v1`)

Status: **proposed** (GOV-TRN-001, 2026-11-25). Additive. Does not change any existing Registry
contract.

## 1. Why a separate manifest

`echelon.system/v2` (`schemas/system-manifest-v2.schema.json`) describes a **system's** identity,
distribution and capabilities and is closed (`additionalProperties: false`). It is consumed by Conditor,
the distribution catalog and capability discovery. Organizational facts (purpose, responsibilities,
initiatives, classification, maturity) do not belong in that contract:

- adding them would break consumers that validate strictly against v2;
- a **repository** is not a **system**: one repository may contain several systems (for example
  `summa` contains `summa` and `summa-contracts`; `percepta` contains `percepta` and
  `percepta-semantic`), one system may span repositories, and many repositories (research,
  governance, websites) contain no installable system at all.

So the repository manifest is a **companion** document. It references system ids; it never restates
system identity, distribution classes, `provides` or `optionalConsumes`. Those remain authoritative in
`registry/systems-v2.json` and the per-system manifests.

| Concept | Authoritative record |
|---|---|
| System identity, aliases, distribution, capabilities | `echelon.system/v2` (Registry) |
| Release facts | `echelon.release/v1`, `echelon.release/v2` (Registry `releases/`) |
| Installations | Project Administration (`installation.*`) |
| **Repository** purpose, boundaries, classification, status | `echelon.repository/v1` at `.echelon/repository.json` in the repository (this spec) |
| **Initiatives** | organization initiative register (organization administration), public projection `portfolio/initiatives.json` |
| Cross-repository analysis (duplication, dependents, drift) | Registry `portfolio/` (central, derived) |

## 2. Location and authority

- File: `.echelon/repository.json` in the repository root. Classification: Public in public
  repositories; private repositories may hold Internal values.
- **Locally authoritative** (the repository owns these facts; the central portfolio copies them):
  `purpose`, `businessJustification`, `responsibilities`, `nonResponsibilities`, `intendedConsumers`,
  `systems`, `initiatives`, `informationClassification`, `developmentStatus`, `maturity`,
  `authoritativeDocuments`, `knownLimitations`, `plannedDirection`, `outstandingDecisions`,
  `governingPolicies`, `lineage`.
- **Centrally maintained** (never written in the local manifest): system identity and capabilities,
  latest verified release, repository visibility as observed, dependents, duplication and drift
  findings, initiative definitions, last verification by the central inventory.
- Where a repository has adopted the manifest, the central entry in `portfolio/repositories.json` is
  **generated** from it (`source: "manifest"`). Until then the central entry is a seed maintained in the
  Registry (`source: "central-seed"`) and must be marked `provisional` wherever it summarizes rather
  than quotes repository documentation.

## 3. Fields

| Field | Req. | Meaning |
|---|---|---|
| `schema` | yes | `"echelon.repository/v1"` |
| `id` | yes | stable repository identifier, `^[a-z][a-z0-9-]*$`; survives renames |
| `repository` | yes | canonical `owner/name` |
| `displayName` | yes | human name |
| `purpose` | yes | why the repository exists (one or two sentences) |
| `businessJustification` | no | why Echelon invests in it |
| `responsibilities` | yes | what it owns (≥ 1) |
| `nonResponsibilities` | yes | what it must not own (may be empty, but should not be for core systems) |
| `intendedConsumers` | no | people, systems or agents it serves |
| `systems` | yes | system ids from `systems-v2.json` that live here (may be empty) |
| `initiatives` | no | `INIT-NNN` ids |
| `informationClassification` | yes | `{ "maximumPermitted": "public" \| "internal" \| "confidential" }` — never `restricted` (STD-ORG-CLS-001) |
| `developmentStatus` | yes | `active` · `maintained` · `experimental` · `dormant` · `archived` · `superseded` |
| `maturity` | yes | `concept` · `requirements` · `prototype` · `implemented` · `verified` · `released` |
| `authoritativeDocuments` | yes | repository-relative paths |
| `knownLimitations` | no | strings |
| `plannedDirection` | no | string |
| `outstandingDecisions` | no | decision or question ids/paths |
| `governingPolicies` | no | policy ids (for example `POL-ORG-TRN-001`, `SEC-*`) |
| `lineage` | no | `{ "previousNames": [], "supersedes": [], "supersededBy": null }` |
| `lastReviewed` | yes | `{ "date": "YYYY-MM-DD", "by": "actor", "method": "manual \| agent \| generated" }` |

The JSON Schema is `schemas/repository-manifest-v1.schema.json` (closed; unknown fields fail).

## 4. Rules

- **REG-REPO-001** The repository manifest MUST NOT declare capabilities, distribution classes or
  executable aliases. Capability ids appear only in `echelon.system/v2`.
- **REG-REPO-002** Every id in `systems` MUST exist in `registry/systems-v2.json`.
- **REG-REPO-003** A repository whose visibility is public MUST declare
  `informationClassification.maximumPermitted = "public"`.
- **REG-REPO-004** Registry unavailability MUST NOT affect a system's core functions: the manifest is
  read by governance tooling, never by a system at runtime, and its absence is a governance finding,
  not a runtime failure (preserves the Registry architectural invariant).
- **REG-REPO-005** Agents creating a repository, introducing a system, or changing responsibilities
  update the manifest in the same change (Praxis `requirements/REGISTRY-MAINTENANCE.md`, RM-*).
- **REG-REPO-006** `maturity` is a claim and follows the claim discipline of POL-ORG-TRN-001 §6:
  `verified` requires an evidence reference in `authoritativeDocuments` or `outstandingDecisions`
  context; `released` requires a release record in the Registry.

## 5. Central projection

`portfolio/repositories.json` (`echelon.portfolio-repositories/v1`, schema
`schemas/portfolio-repositories-v1.schema.json`) is the public, central inventory. It contains only
public repositories, plus at most aggregate counts of private ones, unless a disclosure decision
(`disclosureDecision`) is recorded for a private entry (STD-ORG-PUB-001 PUB-SG-11). The validator
`tools/Echelon.Portfolio` enforces this.

## 6. Compatibility

No existing schema, generated channel, release set or system manifest changes. Consumers that do not
know `echelon.repository/v1` ignore `.echelon/repository.json`.
