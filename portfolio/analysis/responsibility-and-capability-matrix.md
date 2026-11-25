---
id: PF-ANL-001
title: System responsibility and capability ownership matrices
classification: public
status: provisional
updated: 2026-11-25
work_item: GOV-TRN-001
sources: registry/systems-v2.json, portfolio/repositories.json, echelon-assurance integration assessment (branch claude/fervent-tesla-ytnxz6 @ 77bf3a8, unmerged), organization administration quality inventory 2026-10-05
---

# System responsibility and capability ownership matrices

**Read this first.** A *system* (`echelon.system/v2`) is not a *repository*, a *product*, a *capability*
or an *initiative*. This document is a provisional view built on 2026-11-25 from repository
documentation read at HEAD. "Implemented" means code exists; it does not mean verified in production.
Registered capabilities (`provides`) are the only implemented capability claims the Registry makes;
everything else below is descriptive and must be verified before it is relied on.

## 1. System responsibility matrix (all 32 registered systems)

Responsibilities and non-responsibilities are quoted or summarized from each repository's own
documentation (file references in `portfolio/repositories.json`). Six registered systems have **no
documented non-responsibilities** in public sources; that is a finding (BND-01), not an omission here.

| System | Repository | Responsibilities (documented) | Non-responsibilities (documented) | Registered `provides` | Maturity (observed) |
|---|---|---|---|---|---|
| `conditor` | conditor | Parse/validate conditor.json and resolve pinned lifecycle components (README.md); Deterministic installation plan, execution and verification; .conditor/lock.json (README.md) | Not specific to the Indy Init competition application (README.md); Does not recreate scaffold files once established; they belong to the project (README.md) | — | released |
| `praxis` | praxis | Work protocol with legal begin/block/resume/complete transitions and completion evidence (README.md); Canonical research records and generated record registries (README.md) | Central project-administration policy, repository registration, portfolio data, reporting belong to the central reporting repository, not Praxis (README.md ownership table); Must remain operational wh | agent.identity, engineering.metrics | released |
| `ordo` | ordo | SDE doctrine, method and templates (PROJECT-CHARTER.md); ordo/sde lifecycle CLI installing .sde/ into repositories (README.md) | No modification to HelixNote (PROJECT-CHARTER.md); Not solving Boundary Change Amplification in this phase (PROJECT-CHARTER.md) | — | released |
| `project-administration` | project-administration | Cross-project FAQs, how-tos, guides, reference (README.md); Append-only installation event history and generated current state (docs/installation-inventory.md) | Not the store for repository-local work execution (docs/installation-inventory.md); Never becomes a catalog; Registry owns that (docs/installation-inventory.md) | installation.register, installation.query, installation.remove | implemented |
| `dokimos` | dokimos | Code-quality measurement and longitudinal evidence (README.md); Immutable snapshots, comparison, trend and finding lifecycle (context/CURRENT-STATE.md) | Security owned by 'Aegis/Tutors' (README.md); Process telemetry owned by Praxis/ROS (README.md) | quality.snapshot, quality.compare, quality.evaluate, quality.history, echelon.repository-lifecycle | released |
| `tutela` | tutela | Security model Asset->Boundary->Threat->Invariant->Control->Evidence->Posture (README.md); Deterministic posture gate (F# CI authority) (context/CURRENT-STATE.md) | Not a vulnerability scanner, compliance badge, pentest replacement or binary secure classifier (PROJECT-CHARTER.md) | — | released |
| `vigila` | vigila | Item capture and lifecycle (create/edit/complete/reopen/cancel/defer) (docs/requirements/SCOPE.md); GitHub repository persistence per user (PROJECT-CHARTER.md) | Not a project-management system; not a replacement for ROS (PROJECT-CHARTER.md, docs/requirements/GOVERNANCE.md) | followup.create, followup.update, followup.resolve, followup.query | implemented |
| `tekmerion` | tekmerion | Typed ingestion of research state (README.md); Versioned /data/v1 machine contracts (README.md) | Inferring claims/evidence from prose (PROJECT-CHARTER.md); Access-controlled publication (PROJECT-CHARTER.md) | — | released |
| `visual-engineering` | visual-engineering | visual-engineering CLI: init/status/verify/doctor/upgrade of .visual-engineering context (README.md, docs/cli.md); UI research context package @kemiller2002/visual-engineering-context (packages/visual-engineering-context, CHANGELOG.md) | **none documented** | — | released |
| `communication-engineering` | communication-engineering | Communication research corpus (research/); Evidence-bounded operational agent context (agent-context/) | Installer does not install research corpus or promote provisional claims to universal rules (README.md); Does not claim validated universal writer rules, quality scores, persuasion optimizers, cultura | — | released |
| `percepta` | percepta | Semantic UI contracts (regions, hierarchy, state projections, action legality) (README.md); percepta semantic verification CLI (docs/PERCEPTA-SEMANTIC-CLI.md) | Not a design system, CSS framework, component library, state-machine engine, screenshot regression or general E2E framework (PROJECT-CHARTER.md); Lifecycle does not own .percepta/contracts or app code | — | released |
| `percepta-semantic` | percepta | Semantic UI contracts (regions, hierarchy, state projections, action legality) (README.md); percepta semantic verification CLI (docs/PERCEPTA-SEMANTIC-CLI.md) | Not a design system, CSS framework, component library, state-machine engine, screenshot regression or general E2E framework (PROJECT-CHARTER.md); Lifecycle does not own .percepta/contracts or app code | percepta.evidence | released |
| `aegis` | aegis | Fault model, capture, redaction, lifecycle, recovery, escalation, presentation, diagnostics (README.md); GitHub storage adapter (README.md) | Database adapters built elsewhere against Store.T (README.md; DF-AEGIS-2026-DBBD) | — | released |
| `limen` | limen | TypeScript browser kernel: DOM bindings, HTTP, storage, clipboard, navigation as typed effects (README.md, docs/ROADMAP.md); JSON-serializable engine/kernel protocol and EngineTransport seam (src/protocol.ts per docs/ROADMAP.md) | Does not make illegal domain states unrepresentable; consumer engine must (README.md, HANDOFF.md); npm package does not ship a WASM application engine (README.md) | — | released |
| `forma` | forma | DTCG design tokens and themes (tokens/, tools/TokenCompiler); Canonical HTML/CSS component patterns and catalog site (patterns/, catalog/, docs/CATALOG-AUTHORING.md) | No JavaScript, WebAssembly, Custom Elements, Shadow DOM or UI runtimes in the design-system runtime (PROJECT-CHARTER.md, ADR-0002); No application-domain logic or persistence/network effects (PROJECT- | — | released |
| `folio` | folio | Light-DOM ef-print-* components (document, title page, header/footer, table, TOC...) (README.md, src/components); Print CSS, page profiles (Letter/A4) and tokens (PROJECT-CHARTER.md) | No JavaScript general-purpose pagination engine (README.md, PROJECT-CHARTER.md); No WYSIWYG editor, CMYK/prepress, pixel-identical output, PDF/UA guarantee (PROJECT-CHARTER.md) | — | released |
| `echelon-theme` | echelon-design-system | Echelon brand manifest, tokens and theme CSS for Forma (theme/, dist/); Owner-approved corporate logos (logos/, brand/application-logos.md) | Not a second UI component library; Forma implements components (README.md); Must use, never define, the Forma namespace (README.md 1.1.0) | — | released |
| `iter` | iter | Typed route parsing/formatting and navigation intent (README.md); Minimality budget enforcement (docs/MINIMALITY.md) | Rendering, DOM, components, lifecycle, DI, state management, forms, domain semantics (README.md); Browser capability access (Limen) (PROJECT-CHARTER.md) | — | implemented |
| `framework-templates` | Framework-templates | *not documented in public sources* | **none documented** | — | unknown |
| `chrona` | chrona | Timer/manual entry in exact minutes, overlap prevention (PROJECT-CHARTER.md); Amend/void/split/merge with append-only audit (PROJECT-CHARTER.md) | Money: rates, invoices, payments belong to Summa (PROJECT-CHARTER.md); No database (Strata) (PROJECT-CHARTER.md) | time.record, time.query, time.approve | implemented |
| `summa` | summa | Pure financial domain: Money, Ledger, Invoicing, Payments, Reports (HANDOFF.md); Chrona-to-Summa contracts package (src/Summa.Contracts) | Hub never edits registered repositories' files (README.md) | — | implemented |
| `strata` | strata | Compile .sql schema to artifact, diff against live DB, judge and apply changes (README.md); Six explicit comparison states (README.md) | **none documented** | echelon.repository-lifecycle | released |
| `forma-studio` | forma-studio | Visual specification authoring across Layout pages and Flow diagrams (PROJECT-CHARTER.md); Authoritative versioned project document; canvas as projection (README.md, schemas/forma-studio.schema.json) | Does not own business-domain state of designed applications (PROJECT-CHARTER.md, RN-008); Does not fork/replace Forma component implementation (PROJECT-CHARTER.md, RN-009) | — | prototype |
| `mercatus` | mercatus | Market/problem hypotheses, evidence and ICP management (README.md core loop); Lead qualification, opportunity and outcome tracking for first slice PRD-25 (PROJECT-CHARTER.md) | Not a conventional CRM, autonomous spam system, social-content factory or set of disconnected agents (README.md); No real outbound sending before WF-09 gate (PROJECT-CHARTER.md) | — | prototype |
| `signal` | signal | Immutable survey template authoring, validation, publication (HANDOFF.md WI-0042); Live URL as canonical respondent state (PROJECT-CHARTER.md) | No PII collection/processing of any kind (PROJECT-CHARTER.md Excluded); No free-form answers during trial (PROJECT-CHARTER.md) | — | implemented |
| `helix-note` | helix-note-application | *not documented in public sources* | **none documented** | — | unknown |
| `clarity-service` | clarity-service | *not documented in public sources* | **none documented** | — | unknown |
| `ros-worker-daemon` | ROS-WorkerDaemon | *not documented in public sources* | **none documented** | — | unknown |
| `arca` | arca | Pure core: record format, versioning, conflict detection/merge, commit/audit, offline queue (README.md); GitHub adapter with effects as data (README.md) | Domain meaning (PROJECT-CHARTER.md); Authentication; no Fides dependency (PROJECT-CHARTER.md) | — | released |
| `fides` | fides | Stateless server-side code-for-token exchange, refresh, revocation (README.md); F# client library for Limen/Forma WASM apps and Arca token provider (README.md) | Nothing is deployed from this repository (README.md); No session service (DF-FIDES-2026-0004 via context/CURRENT-STATE.md) | — | released |
| `summa-contracts` | summa | Pure financial domain: Money, Ledger, Invoicing, Payments, Reports (HANDOFF.md); Chrona-to-Summa contracts package (src/Summa.Contracts) | Hub never edits registered repositories' files (README.md) | — | implemented |
| `limen-fsharp` | limen | TypeScript browser kernel: DOM bindings, HTTP, storage, clipboard, navigation as typed effects (README.md, docs/ROADMAP.md); JSON-serializable engine/kernel protocol and EngineTransport seam (src/protocol.ts per docs/ROADMAP.md) | Does not make illegal domain states unrepresentable; consumer engine must (README.md, HANDOFF.md); npm package does not ship a WASM application engine (README.md) | — | released |
## 2. Capability ownership matrix

Status vocabulary: **registered** = in `systems-v2.json` `provides`; **implemented** = code observed,
not registered; **proposed** = named in a standard or requirement, not implemented; **unowned** = no
system claims it.

| Capability area | Owner (status) | Other systems touching it | Evidence | Finding |
|---|---|---|---|---|
| Work protocol, provenance, execution telemetry | Praxis (registered: `agent.identity`, `engineering.metrics`) | Ordo (method), Specula (observes) | praxis README, systems-v2 | coherent |
| Installation inventory | Project Administration (registered: `installation.*`) | Praxis (optional registration) | spec/installation-protocol.md | coherent |
| Follow-up tracking | Vigila (registered: `followup.*`) | — | systems-v2 | coherent |
| Time recording and approval | Chrona (registered: `time.*`) | time-tracking-application (superseded) | systems-v2 | coherent |
| Code quality measurement and gate | Dokimos (registered: `quality.*`) | Ordo (own complexity/churn tests) | dokimos, ordo tests | overlap DUP-09 |
| Independent verification of completion claims | Dokimos (implemented, not registered) | Praxis (attested completion, implemented, not enabled) | assurance ECH1 assessment | register as capability; enable (GAP-04) |
| Defect reporting and quality intelligence | Vitium (implemented: report form; proposed: machine intake) | Dokimos | vitium README | **system not registered** (GAP-02) |
| Operational fault handling (.NET) | Aegis (implemented, released NuGet) | — | aegis README | coherent |
| Security rule registry and semantic analysis | **Aegis (proposed by Secure Construction Standard)** vs **Tutela (security discipline: invariants, threats, controls, evidence)** | Dokimos (verification) | secure-construction-standard.md; tutela README | **conflicting assignment** (DUP-03) |
| Secret scanning | Tutela (implemented: `scan-secrets`, own CI only) | Aegis (proposed scanner orchestration) | tutela-foundation.yml | adopt portfolio-wide (PUB-SG-01/02) |
| Typed database access | Strata (released 0.1.1, PostgreSQL schema management) | Census (consumer) | strata README | scope expansion proposed (DRF-03) |
| Shared data layer (GitHub-backed records) | Arca (released 0.7.0) | Vigila (own GitHub host), Aegis (`Aegis.Store.GitHub`) | inventory batch A | overlap DUP-06 |
| Identity and SSO | Fides (released 0.2.0) | — | fides README | coherent |
| Browser capability boundary | Limen (released 0.9.1) | Iter (typed web primitives above Limen) | limen, iter | coherent (layered) |
| UI components and tokens | Forma (released 1.1.0, brand-neutral) | echelon-design-system (brand theme), Forma Studio (composition tool) | forma CHANGELOG 1.0.0 | preview/documentation ownership unclear (DUP-05) |
| Print and PDF | Folio (released 0.4.1) | — | folio README | coherent |
| Research publication | Tekmerion (released 0.2.0) | research-publisher npm package (legacy) | tekmerion README | naming migration open |
| Distribution and environment bootstrap | Conditor (tag v0.10.29) | Armamentarium (kit composition, requirements only) | armamentarium requirements | overlap DUP-04 |
| Release catalog and capability discovery | Echelon Registry | Conditor (consumer) | registry README | coherent |
| Organizational portfolio (identity of repositories and initiatives) | Echelon Registry `portfolio/` (this work) | Project Administration (cross-project knowledge), Knowledge Store (representation) | spec/repository-manifest.md | boundary defined in §3 |
| Structured knowledge representation and retrieval | Knowledge Store (research) | Praxis (record registries) | EKS README | coherent if KSI-* requirements are followed |
| Assurance coordination, control catalog | echelon-assurance (research) | Praxis, Dokimos, Tutela (evidence producers) | assurance branch | coherent |
| Due diligence answers | echelon-due-diligence (research) | echelon-assurance (controls) | due-diligence governance | boundary in DD governance doc |
| Canonical time primitives | **Chrona (planned owner, QDI-079)** | Vigila (local Instant/Clock until then) | systems-v2 plannedCapabilities | planned, unimplemented |
| Ledger, invoicing, payments | **Summa (planned owner, QDI-079)** | — | systems-v2 plannedCapabilities | planned, unimplemented |
| Runtime monitoring and production logging | **unowned** (Vigila named "runtime monitoring" in the Secure Construction Standard; Vigila is a follow-up tracker) | — | assurance ECH1 finding 6; vigila charter | **unowned** (GAP-05) |
| Auditor evidence export package | **unowned** | Praxis, Folio (proposals) | assurance ECH1 finding 6 | **unowned** (GAP-06) |
| WORM / tamper-evident evidence storage | **unowned** | Dokimos evidence branch (mutable by pushers) | assurance ECH1 | **unowned** (GAP-07) |
| Publication safeguards (portfolio projection checks) | Registry `tools/portfolio` (implemented 2026-11-25) | Tutela (secret scanning) | this work | coherent |
| Survey and assessment engine | Signal | agile-assessment (historical) | signal | coherent |
| Causal investigation | Causa (**not registered**) | EDF (specialization), Clarity | causa README | GAP-03 |

## 3. Boundary of the new portfolio responsibility

| Question | Owner |
|---|---|
| What repositories, systems and initiatives exist; what each is responsible for | Echelon Registry (`registry/`, `portfolio/`) |
| Where a system is installed | Project Administration |
| How organizational knowledge is represented, linked and retrieved | Echelon Knowledge Store |
| Repository governance obligations (when an agent must update the registry) | Praxis (`requirements/REGISTRY-MAINTENANCE.md`) |
| Authoritative policy text and internal initiative details | Organization administration |
