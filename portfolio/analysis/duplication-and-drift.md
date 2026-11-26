---
id: PF-ANL-003
title: Organizational duplication and drift analysis
classification: public
status: provisional
updated: 2026-11-25
work_item: GOV-TRN-001
methods: EDF v1.0 draft core grammar (echelon-diagnostic-framework docs/edf/specification-v1-draft.md) for the portfolio diagnosis; Clarity claim review (claim, support, assumptions, confidence low/moderate/high, correctability, what would change the decision) for each recommendation
evidence: portfolio/repositories.json and the per-repository files it cites; registry/systems-v2.json and releases/; organization administration quality inventory 2026-10-05; echelon-assurance integration assessment (unmerged branch @ 77bf3a8)
---

# Organizational duplication and drift analysis

Nothing here authorizes consolidation, retirement or repository changes. Every recommendation is a
proposal for an owner decision (open decisions register). Consolidation is **not** recommended merely
because two systems share functionality: each finding weighs cohesion, independent deployment,
maintenance cost, organizational complexity and long-term consequences, and states the counterargument.

## 1. Portfolio diagnosis (EDF grammar)

**System context.** One accountable owner, ~64 Echelon repositories (57 public), 32 registered
systems, 29 initiatives, most work performed by autonomous agents under Praxis.

**Manifestations.**
- Version facts disagree across records: `ros.json` `rosVersion` differs from `.echelon/ros.json`
  `installedVersion` in most inspected repositories; registry release records lag repository tags
  (Conditor 0.10.19 vs tag v0.10.29; Forma 1.0.1 vs v1.1.0).
- Bootstrap templates remain unfilled in charters and handoffs of released systems (Limen, Conditor,
  Aegis, Strata, Percepta, Visual Engineering, Communication Engineering, Forma, Folio, Echelon Foundry).
- Systems exist that the registry does not know (Vitium, Causa, Specula, EDF).
- Overlapping responsibilities (§2).
- Zero `SECURITY.md` files; secret scanning in one repository; a credential exposed in a public repository.

**Origin network (diagnosis, moderate confidence).**
1. *Creation is cheap, reconciliation is not.* Agents create repositories, systems and documents
   faster than any process updates shared records. No rule required registry updates (Praxis had none
   before RM-*).
2. *Facts are maintained by hand in several places* (pins in `ros.json`, `.echelon/ros.json`,
   foundations manifests, conditor.json, registry releases), so each copy drifts independently.
3. *Templates are installed before purpose exists*, and nothing distinguishes "template" from
   "authoritative" for a reader or agent.

**Propagation.** Stale records mislead agents, which then build on wrong assumptions (for example
pinning pre-1.0 Forma, or naming the wrong security owner), which creates more drift. Public stale
records reduce the credibility the transparency policy is meant to create.

**Control points.** (a) the moment a repository or system is created; (b) release publication; (c)
Praxis validation; (d) generation of central projections from local manifests.

**Calibration.** Moderate confidence. Alternative diagnosis: drift is a normal, cheap by-product of a
fast research phase and will be removed when products stabilize. Evidence against relying on that:
drift is present in released, depended-on foundations, not only in experiments.

## 2. Duplication findings

| ID | Finding | Evidence | Assessment | Recommendation | Counterargument | Confidence |
|---|---|---|---|---|---|---|
| DUP-01 | Three places model an AI-native DAF enterprise: `unbound` (product requirements), the EEF DAF case study, and `daf-landscape/research/ai-native-daf-enterprise` (own F# models) | inventory records for the three repositories | Different purposes (product requirements vs framework case study vs market research), but the F# models in daf-landscape duplicate EEF modelling | Keep the three purposes; designate EEF as the only enterprise-model implementation and make daf-landscape cite it; Unbound consumes both as sources | The daf-landscape models may predate EEF and carry evidence links EEF lacks; moving them risks losing provenance | moderate |
| DUP-02 | Four repositories hold Causa test data and targets (`causa-testing-repo`, `causa-write-test` ~1,000 undocumented investigations, `causa-data` demo workspaces, Indy-init rehearsal repositories) | inventory batch C | Test fixtures without documentation are maintenance and disclosure risk | Document each as a test fixture in its manifest; archive `causa-write-test` after confirming no consumer (owner decision ABN-02) | Real-provider acceptance tests may need isolated repositories by design (write scopes) | moderate |
| DUP-03 | Security rule ownership: the Secure Construction Standard assigns the rule registry and semantic analysis to **Aegis**; **Tutela** is the security engineering discipline (invariants, threats, controls, evidence) and already ships secret scanning; Dokimos README names "Aegis/Tutors" (Tutela alias) | secure-construction-standard.md; tutela, aegis, dokimos READMEs | Aegis is an operational-fault library; Tutela matches the rule-registry responsibility more closely | Amend the standard: Tutela owns security rules and evidence; Aegis keeps operational fault handling; correct Dokimos README | Aegis has the F# semantic analysis surface in-process; splitting may need an analyzer boundary | moderate |
| DUP-04 | Armamentarium (signed, platform-specific kit composition) vs Conditor (governed environment bootstrap from manifests, lock files, install plans) | armamentarium requirements/component-inventory.md asks whether Conditor already owns this | Armamentarium is requirements-only; Conditor is released | Decide before building Armamentarium: extend Conditor with a kit output, or define Armamentarium strictly as an offline bundle format Conditor produces | Separate deployment cadence and offline/air-gapped use may justify a separate artifact | moderate |
| DUP-05 | Design system preview and documentation: Forma (ships its own workflow editor, ADR-0006), Forma Studio (composition tool), echelon-design-system (brand theme, planned components and print contracts) | inventory batch B | Forma 1.0 made Forma brand-neutral and moved the brand to echelon-design-system: a sound separation. The overlap is in preview/editor tooling and in echelon-design-system's planned `components/` and `print/` directories, which would duplicate Forma and Folio | Record non-responsibilities: echelon-design-system owns brand tokens only; components stay in Forma, print in Folio; one preview surface (Forma Studio) | Brand-specific components may legitimately need a home outside a neutral system | moderate |
| DUP-06 | GitHub-backed persistence implemented three times: Arca (shared data layer), Vigila (`Vigila.Host.GitHub`), Aegis (`Aegis.Store.GitHub`) | inventory batch A | Arca is the designated shared layer; Aegis is a dependency *of* Arca-using applications, so Aegis depending on Arca could create a cycle | Vigila migrates to Arca; Aegis keeps a minimal store only if Arca would create a cycle (document the reason in Aegis non-responsibilities) | Independent deployability of Aegis (a low-level library) argues against depending on Arca | moderate |
| DUP-07 | Four copies of the init/verify/upgrade lifecycle CLI pattern: Praxis, Limen, Visual Engineering, Communication Engineering | inventory batch B | Pattern duplication, not code sharing evidence | Evaluate a shared repository-lifecycle library (Conditor already resolves `repository-lifecycle` components) | Each lifecycle is small and independently released; a shared library adds coupling across four release trains | low |
| DUP-08 | Research repositories share structure and near-identical `knowledge-platform/` documents (AI-Engineering, visual-engineering) | inventory batch B | Copy-paste research scaffolding | Move shared method text to one source (Framework Engineering or Tekmerion) and reference it | Research programs may need frozen copies for reproducibility | low |
| DUP-09 | Complexity and churn measurement in Ordo tests overlaps Dokimos churn and hotspot measurement | inventory batch A | Ordo uses them for method validation | Ordo consumes Dokimos quality snapshots where possible | Ordo's research needs may require metric definitions Dokimos does not offer | low |
| DUP-10 | Administration across projects: Summa README describes a cross-project hub while its HANDOFF and CURRENT-STATE describe invoicing/ledger; Project Administration owns installation inventory and cross-project knowledge | summa README vs HANDOFF | Documentation drift rather than real duplication | Correct Summa README (DRF-04) | — | high |
| DUP-11 | Three "Clarity" repositories plus decision-posture: clarity-framework (content), clarity-service (software), clarity-framework-training (site), decision-posture (public site) | inventory batch C; 2026-10-05 quality inventory CLR-F4 | Naming ambiguity; distinct artifacts | Record each as a separate repository with explicit relation in manifests; keep one public definition of Clarity | — | high |

## 3. Drift findings

| ID | Finding | Evidence | Recommendation |
|---|---|---|---|
| DRF-01 | Legacy `rosVersion` in `ros.json` disagrees with `.echelon/ros.json` `installedVersion` in most repositories (examples: registry 3.7.1/3.7.2; gate-proof-r10 3.12.0 vs conditor.json 3.25.0; praxis root 1.0.0 vs release 3.25.0) | inventory records | Praxis upgrade should rewrite or remove the legacy field; validation should flag disagreement |
| DRF-02 | Registry release records lag repository tags (Conditor, Forma) | registry `releases/` vs `git ls-remote --tags` | Release publication must update the registry (Praxis RM-05) |
| DRF-03 | Strata's scope widening from PostgreSQL schema management to data interaction virtualization across Echelon (STRATA-DIV-001, proposed 2026-11-23) | strata docs/requirements/data-interaction-virtualization.md | Treat as a responsibility change requiring a registry update and boundary review (RM-04) before adoption |
| DRF-04 | Summa README purpose contradicts its handoff | summa | Correct README |
| DRF-05 | Unfilled bootstrap templates presented as charters in released systems | inventory batches A–C | Praxis should mark template content machine-readably as `template` until filled; portfolio treats it as not documented |
| DRF-06 | Research repositories depend on the retired npm research-publisher package (patched on postinstall) | framework-engineering package.json | Migrate to Tekmerion |
| DRF-07 | Consumers pin pre-1.0 Forma (forma-studio 0.10.0, echelon-engineering-system 0.9.0) | inventory batch B | Upgrade or record the reason |
| DRF-08 | Indy-init README lists ROS and Praxis as separate stack items | Indy-init README | Correct naming |
| DRF-09 | Licences inconsistent: PolyForm Noncommercial (most systems), MIT (registry, project-administration and several sites), none (EEF, unbound, daf-landscape, Indy-init) | inventory records | Licensing decision per repository class (OD-GOV-018) |

## 4. Gaps (unowned or unregistered)

| ID | Gap | Recommendation |
|---|---|---|
| GAP-01 | No `SECURITY.md` in any inspected repository | POL-ORG-VDP-001 VDP-T2 |
| GAP-02 | Vitium is a system with releases planned but not in `systems-v2.json` | register (identity only; no `provides` until implemented) |
| GAP-03 | Causa, Specula and EDF are not registered; EDF's distribution class is undecided (EDF issue #24) | decide system vs framework bundle; register or record as non-system repositories |
| GAP-04 | Dokimos completion verification and Praxis attested completion are implemented but neither registered as capabilities nor enabled | register capability ids; pilot enablement |
| GAP-05 | Runtime monitoring/production logging is unowned; the Secure Construction Standard assigns it to Vigila, whose charter is follow-up tracking | owner decision; do not create a new system before confirming no existing owner fits |
| GAP-06 | Auditor evidence export is unowned | assign to Praxis (export) + Folio (presentation) per continuous assurance requirements |
| GAP-07 | Tamper-evident evidence storage is unowned | see continuous assurance requirements CA-PROV-* |

## 5. Abandoned or dormant work (not silently abandoned)

| ID | Item | Proposed disposition |
|---|---|---|
| ABN-01 | AI-Engineering, software-engineering (dormant research) | continue under INIT-014, or close with a findings note |
| ABN-02 | Indy Init rehearsal and test repositories | record outcomes in INIT-021; archive after confirming no consumer |
| ABN-03 | agile-assessment | archive, or move redesign research to Signal |
| ABN-04 | time-tracking-application, sales-and-marketing | mark `superseded` with successors Chrona and Mercatus |
| ABN-05 | INIT-007 umbrella | confirm it still adds coordination value over INIT-019/020 |

## 6. Systems whose responsibilities should be separated (not merged)

| System | Why separation may be warranted |
|---|---|
| Strata | Schema management (released) and data virtualization (proposed) have different consumers and risk profiles; a single system would couple a security-critical access path to an experimental layer |
| echelon-design-system | Brand tokens vs planned components/print contracts (see DUP-05) |
| Praxis | Repository operating system and run-time web hub share a release train; the run-time hub brings Forma/Limen/Aegis dependencies into the governance tool (CYC-01) |
