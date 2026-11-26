# Echelon organizational portfolio

This is a descriptive planning and governance layer, **not** a capability discovery or release
contract. Canonical system identities remain in [`registry/systems-v2.json`](../registry/systems-v2.json).
No planned responsibility implies an implemented capability. Entries marked *provisional* summarize
repository documentation and have not been verified against running software.

Classification of everything in this directory: **Public** (proposed policy POL-ORG-TRN-001).
Last full review: 2026-11-25 (work item GOV-TRN-001).

## What is Echelon?

Echelon Foundry builds governed, evidence-producing software and methods for organizations that use
AI agents to do real work. Its principle: **public by default, confidential by necessity, secure by
construction, verifiable by evidence** ([transparency policy](governance/policies/organizational-transparency-policy.md),
proposed). Most engineering is performed by autonomous agents operating under
[Praxis](https://github.com/kemiller2002/praxis), Echelon's repository operating system.

## What are we building, and why?

Four families of work (details in [initiatives](generated/initiatives.md)):

| Family | Examples | Why |
|---|---|---|
| Engineering operating system | Praxis, Ordo, Conditor, Dokimos, Tutela, Echelon Registry | agent work must be durable, verified and attributable |
| Shared application foundations | Aegis, Limen, Forma, Folio, Arca, Fides, Strata, Iter | secure-by-construction F#/WebAssembly applications without re-implementing infrastructure |
| Products | Unbound (AI-native DAF administration), HelixNote, Causa, Specula, Signal, Mercatus, Chrona, Summa, Vigila | demonstrate AI-native design in real domains |
| Research and methods | DAF industry research, Clarity, EDF, AI-native Enterprise Design Framework, Knowledge Store, due diligence and assurance research | test whether the methods work before selling or scaling them |

## What does each system do and how do the systems relate?

- Repository inventory (64 named repositories, 19 private counted in aggregate):
  [`generated/repositories.md`](generated/repositories.md) · data: [`repositories.json`](repositories.json)
- What each system owns and must not own; who owns each capability:
  [`analysis/responsibility-and-capability-matrix.md`](analysis/responsibility-and-capability-matrix.md)
- Dependencies and relationships: [`analysis/dependency-and-relationship-map.md`](analysis/dependency-and-relationship-map.md)
- Boundaries: [`analysis/architectural-boundary-analysis.md`](analysis/architectural-boundary-analysis.md)

## What research are we pursuing, and what problems are we trying to solve?

See initiatives INIT-004 (DAF market), INIT-007/019/020 (business transformation, Clarity, EDF),
INIT-009 (structured knowledge), INIT-014 (autonomous engineering agents), INIT-002/003/006 (due
diligence, continuous compliance, assurance cost), and the evidence review
[`research/transparency-competitive-advantage.md`](research/transparency-competitive-advantage.md).

## What have we established — and what remains unproven?

Established (with released, tested artifacts): Praxis 3.25.0, Ordo 1.20.0, Dokimos 0.11.1, Limen
0.9.1, Forma 1.1.0 (repository tag), Folio 0.4.1, Aegis 2.0.2 and others listed with release records.

Unproven, stated plainly:
- No Echelon product is in production with customers; Unbound is at requirements stage.
- **No independent assurance exists**: no SOC report, no ISO certification, no external penetration
  test. Echelon's verification is first-party.
- Comparative benefits claimed for several methods (Limen, EDF, Clarity) have not been established;
  their own repositories say so.
- Implemented integrity controls (Praxis attested completion) are not yet enabled.
- Known gaps and drift: [`analysis/duplication-and-drift.md`](analysis/duplication-and-drift.md).

## What are our current priorities?

[`governance/roadmap.md`](governance/roadmap.md) — first, correct what is already public (P0/P1),
then adopt policies and safeguards, then register missing systems and adopt repository manifests.
Owner decisions pending: [`governance/open-decisions.md`](governance/open-decisions.md).

## Where is authoritative information?

| Topic | Authoritative location |
|---|---|
| System identity, capabilities, releases | `registry/systems-v2.json`, `releases/` |
| Repository facts | each repository's `.echelon/repository.json` once adopted ([spec](../spec/repository-manifest.md)); until then `repositories.json` (central seed) |
| Initiatives | organization administration register; public projection [`initiatives.json`](initiatives.json) (generated) |
| Policies | organization administration; public copies with digests in [`governance/policies/`](governance/policies/) |
| Repository governance obligations | [Praxis `requirements/REGISTRY-MAINTENANCE.md`](https://github.com/kemiller2002/praxis/blob/main/requirements/REGISTRY-MAINTENANCE.md) (proposed) |
| Due diligence answers | [echelon-due-diligence](https://github.com/kemiller2002/echelon-due-diligence) |
| Assurance controls and evidence architecture | [echelon-assurance](https://github.com/kemiller2002/echelon-assurance) |
| Trust center requirements | [`governance/trust-center-requirements.md`](governance/trust-center-requirements.md) |
| Measures | [`governance/measurement-framework.md`](governance/measurement-framework.md) |
| Adversarial review | [`governance/adversarial-review.md`](governance/adversarial-review.md) |
| Vulnerability reporting | [`governance/policies/vulnerability-disclosure-policy.md`](governance/policies/vulnerability-disclosure-policy.md) |

## Initiative catalog requirements

Maintain stable initiative IDs, objectives, rationale, participating repository references, scope
boundaries, maturity, dependencies, success criteria, next actions, evidence and review dates.
Initiatives are public by default under the proposed transparency policy; the public projection is
generated with restricted fields excluded by construction (`tools/portfolio project-initiatives`).
Internal commercial specifics and private repository identities that have not been evaluated for
disclosure stay in access-controlled organizational records.

## Maintenance

Agents must consult the portfolio before proposing new repositories, systems or initiatives (Praxis
RM-01). Reconcile with canonical manifests, identify overlap and stale entries, and preserve
optional-provider independence. Never modify generated distribution channels or declare new provided
capabilities merely to record a planning initiative. Verify descriptions against repository
documentation before removing *provisional*. Keep confidential information out of public records.

Validate and regenerate:

```bash
dotnet test tools/portfolio/Echelon.Portfolio.slnx -c Release
dotnet run --project tools/portfolio/src/Echelon.Portfolio.Cli -- validate --root .
dotnet run --project tools/portfolio/src/Echelon.Portfolio.Cli -- render --root .
# initiative projection (needs read access to the private source):
dotnet run --project tools/portfolio/src/Echelon.Portfolio.Cli -- project-initiatives \
  --source ../echelon-organization-administration/portfolio/initiatives.json \
  --source-record kemiller2002/echelon-organization-administration:portfolio/initiatives.json \
  --out portfolio/initiatives.json
```
