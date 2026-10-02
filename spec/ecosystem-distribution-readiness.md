# Echelon Distribution Readiness Matrix

Status: draft
Observed: 2026-09-30
Owner: Echelon Registry
Related: `distribution-catalog-requirements.md`, `distribution-resilience-requirements.md`, `ecosystem-release-workflow-requirements.md`
Consumer: Conditor

## Purpose

This document classifies the active Echelon ecosystem by distribution role and records what must be true before a system may enter a stable Conditor profile.

It is intentionally broader than "what has a repository." A repository is not automatically an installable system. Research corpora, predecessor repositories, experiments, websites, planning repositories, and example consumers must not leak into workstation or project profiles merely because they are part of the Echelon portfolio.

Readiness states:

- **eligible**: the observed release/lifecycle shape is already close enough to the standard contract that integration work, rather than a redesign, is the main remaining step.
- **partial**: a usable package or lifecycle exists, but Registry release metadata, immutable artifact publication, platform coverage, or Conditor semantics remain incomplete.
- **not-ready**: the repository does not yet expose a stable distributable artifact/lifecycle appropriate to its role.
- **not-installable**: this repository is intentionally not a Conditor installation target.
- **predecessor**: a newer canonical Echelon identity supersedes this repository for installation/discovery purposes.

The readiness state is not a product-quality judgment. It is only distribution-contract readiness.

## A. Establishment, governance, and host tooling

| Canonical system | Repository | Distribution role | Observed state | Readiness | Required next step |
|---|---|---|---|---|---|
| Conditor | `kemiller2002/conditor` | self-contained host/bootstrap CLI | native multi-platform releases, SHA verification, attestations, workstation profiles, receipts, rollback, Indy Init preset | eligible | consume external Registry catalog/profile release sets; finish offline bundle/all-supported profile |
| Echelon Registry | `kemiller2002/echelon-registry` | catalog/specification, not a host tool | system/release schema and installation protocol exist | eligible | add profile, resolved-release-set, channel/stage, offline snapshot and readiness projection contracts |
| Praxis | `kemiller2002/praxis` | self-contained native governance CLI | native release distribution, side-by-side activation, doctor/inventory, repository lifecycle | eligible | publish/verify standard Registry release metadata and complete canonical `praxis` identity migration while retaining `ros` alias |
| Ordo | `kemiller2002/ordo` | self-contained native methodology/lifecycle CLI | native artifacts and `echelon.release/v1` publication are documented | eligible | register standard release/profile compatibility and use canonical release metadata from Conditor |
| Project Administration | `kemiller2002/project-administration` | installation-inventory provider + administration CLI/application | canonical installation inventory, typed `administration` capability, local executable and GitHub workflow transports | partial | publish immutable executable/application release metadata; distinguish remote provider deployment from optional local CLI installation |
| ROS Worker Daemon | `kemiller2002/ROS-WorkerDaemon` | host daemon/execution supervisor | npm launcher currently requires Node 20 + .NET 10; standalone build exists locally | partial | publish self-contained native artifacts, standard release manifest, machine-readable health/version contract; keep npm as optional compatibility transport |
| Dokimos | `kemiller2002/dokimos` | engineering executable/repository capability | `dokimos-v0.2.0` (commit `e641048`) publishes self-contained native artifacts for six platforms and a shared-contract `echelon.release/v2` manifest; declares `echelon.repository-lifecycle` v1; cataloged as `releases/dokimos/0.2.0.release.json`; Conditor clean-host proof (linux-x64, `dokimos-proof` 0.1.0) installed, initialized, verified and re-applied with zero drift — kemiller2002/conditor Actions run 37003356848 (2026-10-02) | eligible | member of stable profile `echelon-engineering` 0.2.0 (owner decision 2026-10-02); the full 0.2.0 linux-x64 set (Praxis + Ordo + Dokimos) passed Conditor's clean-host proof with zero drift on re-application (kemiller2002/conditor Actions run 37019901126); add macOS clean-host proof when infrastructure is available |
| Tutela | `kemiller2002/tutela` | security lifecycle/tooling capability | F# security authority exists; standard distributable release not proven | not-ready | self-contained/lifecycle release + Registry metadata; canonical id is `tutela`, with historical Tutors naming only as alias if needed |
| Vigila | `kemiller2002/vigila` | application/tool + integration executable | durable integration work exists; host release contract not proven | not-ready | publish executable distribution and separate receiving-system integration contract from application installation |
| Tekmerion | `kemiller2002/tekmerion` | research lifecycle/publishing capability | F# self-contained lifecycle binaries are bundled behind npm; mature init/status/verify/doctor/upgrade contract | partial | canonicalize Tekmerion package/release identity, publish Registry metadata, separate lifecycle artifact from legacy publishing-runtime prerequisites |
| Visual Engineering | `kemiller2002/visual-engineering` | repository lifecycle/evidence-context capability | install/verify lifecycle via npm is established | partial | immutable standard release metadata; preferably self-contained lifecycle distribution so Node is not a universal bootstrap prerequisite |
| Communication Engineering | `kemiller2002/communication-engineering` | repository lifecycle/evidence-context capability | lifecycle CLI exists; Conditor currently supports pinned Git commit transport | partial | publish immutable standard release artifact/metadata and retire pinned-commit fallback from stable profiles |
| Percepta | `kemiller2002/percepta` | semantic verifier/compiler + separate repository lifecycle | repository lifecycle launcher downloads immutable self-contained binary; semantic verifier is separate | partial | publish one Registry release describing both surfaces and explicit optional browser prerequisite for semantic verification |

## B. Project-bound reusable capabilities

These are not global workstation packages merely because they are common Echelon dependencies.

| Canonical system | Repository | Distribution role | Observed state | Readiness | Conditor rule |
|---|---|---|---|---|---|
| Aegis | `kemiller2002/aegis` | .NET/NuGet library family | NuGet packages are documented and MIT licensed | partial | bind exact packages only to an explicit .NET project/scaffold; publish Registry release metadata for the package family |
| Limen | `kemiller2002/limen` | browser/runtime package + repository lifecycle capability | npm browser package and F# lifecycle exist; product/package naming differs | partial | explicit web target only; canonical id `limen`; historical package name is distribution identity, not system identity |
| Forma | `kemiller2002/forma` | zero-runtime design-system package | pinned package consumption and release tarball workflow are documented | partial | bind to explicit web project; never global-install or copy source/CSS into consumer |
| Folio | `kemiller2002/folio` | print/web-component package | public package/component surface exists | partial | bind to explicit project; renderer capabilities are separate explicit prerequisites |
| Iter | `kemiller2002/iter` | optional .NET/NuGet application library | F# routing library exists; release contract not proven | not-ready | bind only to an explicit requesting .NET target; Limen must never depend on Iter |
| Framework Templates | `kemiller2002/Framework-templates` | reusable framework scaffolding/template bundle | framework-agnostic starter artifacts exist; no immutable distribution contract observed | not-ready | publish a versioned template bundle and bind it only when explicitly scaffolding a framework repository; do not global-install it |

## C. Echelon applications and developer products

Applications are not automatically members of an "all tools on PATH" profile. Their developer environment and end-user deployment are separate desired states.

| Canonical system | Repository | Role | Readiness | Distribution requirement |
|---|---|---|---|---|
| Chrona | `kemiller2002/chrona` | time-entry application | not-ready | declare application artifact/deployment class and separate dev profile from end-user deployment |
| Summa | `kemiller2002/summa` | ledger/invoicing/receivables/payments application/service | not-ready | publish explicit application/service artifacts; never infer payment/database credentials or silently deploy live services |
| Strata | `kemiller2002/strata` | SQL/schema developer tool/library | not-ready | explicitly separate native CLI from any reusable .NET library and database-provider prerequisites |
| Forma Studio | `kemiller2002/forma-studio` | design/workflow application | not-ready | declare runtime/deployment class and separate development from end-user installation |
| Mercatus | `kemiller2002/mercatus` | sales/marketing application | not-ready | publish application/developer distribution contract; predecessor `sales-and-marketing` must not be independently installable |
| Signal | `kemiller2002/signal` | F#/WASM survey and assessment application | not-ready | publish immutable application/static-deployment artifact and developer profile; preserve no-PII constraints in release evidence |
| HelixNote | `kemiller2002/helix-note-application` | evidence/reasoning application | not-ready | replace source-oriented MVP bootstrap with declared developer/deployment artifacts and current Echelon stack bindings |
| Clarity Service | `kemiller2002/clarity-service` | repository-analysis CLI/service | not-ready | publish self-contained native CLI and Registry metadata; consuming users should not need the .NET SDK |
| Echelon Culinary | `kemiller2002/echelon-culinary` | static web application/site | partial | treat GitHub Pages/static build as deployment, not workstation installation; only add to Conditor if an explicit developer/deploy profile needs it |
| Recipe Formatter | `kemiller2002/recipe-formatter` | standalone static application/pilot | partial | keep outside core Echelon profiles unless promoted to a canonical Echelon application with an explicit release identity |

## D. Research, methodology, planning, website, and data repositories

The repositories below must **not** become normal Conditor install targets in their present role.

| Repository | Classification | Registry/Conditor posture |
|---|---|---|
| `kemiller2002/echelon-diagnostic-framework` | framework/research corpus + website | not-installable; may later publish an immutable contract/domain-pack artifact, but not a host tool |
| `kemiller2002/clarity-framework` | framework/research corpus | not-installable; may publish versioned framework/contract bundles separately |
| `kemiller2002/framework-engineering` | methodology/research governance | not-installable |
| `kemiller2002/application-visual-language` | research program/style-guide evidence | not-installable until/unless it produces a separately named operational distribution |
| `kemiller2002/AI-Engineering` | research operating-system corpus | not-installable; publication is research projection |
| `kemiller2002/research-documents` | research/data aggregation | not-installable |
| `kemiller2002/Indy-init` | competition planning and immutable governing contracts | not a tool; Conditor consumes a pinned contract bundle/profile input |
| `kemiller2002/echelon-foundry` | company/marketing site | not-installable |
| `kemiller2002/decision-posture` | Clarity static website | not-installable |
| `kemiller2002/echelon-organization-administration` | organization/governance repository | not a workstation install target unless a future distinct executable is explicitly defined |
| `kemiller2002/communication-handlers` | provider/edge handler repository with no canonical product contract observed | not-installable pending explicit promotion/identity |
| `kemiller2002/outreach` | intake/data workspace with no canonical product contract observed | not-installable |
| `kemiller2002/web-component-engineering` | earlier Visual Engineering web-component implementation workspace | predecessor/research posture; Forma is the current canonical shared design-system product |
| `kemiller2002/sales-and-marketing` | predecessor research/product repository | predecessor; canonical application identity is Mercatus |
| `kemiller2002/SDE-Engineering-Trial-1-Greenfield-Construction` | experiment/trial | not-installable |
| `kemiller2002/SDE-Engineering-Trial-2-Greenfield-Construction` | experiment/trial | not-installable |
| `kemiller2002/time-entry-state-machine` | Limen/F# consumer/proof | not-installable as core infrastructure |
| `kemiller2002/time-tracking-application` | trial/application predecessor | not part of core distribution unless explicitly promoted |
| `kemiller2002/time-tracking-data` | trial/data repository | not-installable |
| `kemiller2002/software-engineering` | methodology/research pilot | not-installable unless a future separately named operational capability is promoted |
| `kemiller2002/clarity-framework-training` | training/marketing content site | not-installable |
| `kemiller2002/echelon-consulting-rik-dryfoos` | project-specific consulting/evaluation workspace | not-installable; project evidence is not an ecosystem product |

## E. Canonical identity rules discovered by this inventory

1. `tekmerion` is canonical. `research-publisher` and `@echelon-foundry/research-publisher` are historical/compatibility distribution identities, not second systems.
2. `tutela` is canonical. References to "Tutors" must not create another Registry system id.
3. `mercatus` is the canonical current Sales and Marketing application. `sales-and-marketing` is predecessor research.
4. `forma` is the canonical reusable design-system product. `web-component-engineering` must not appear as a second installable component unless a future decision establishes a separate responsibility.
5. `praxis` is canonical for the repository operating system CLI. `ros` remains a compatibility executable/legacy identity, not an independently installable system.
6. Project Administration is the canonical installation-instance inventory owner. Echelon Registry remains the catalog owner.

## F. Stable-profile eligibility gate

A system may enter a stable Conditor profile only when all applicable items are proven:

1. canonical Registry system identity;
2. license identity appropriate for distribution;
3. semantic release identity and immutable tag/version;
4. validated `echelon.release/v1` (or successor) metadata;
5. exact artifact/package identity and SHA-256 digest;
6. distribution class;
7. supported platform/target contract;
8. compatibility metadata;
9. clean-consumer or clean-host test against the packaged artifact itself;
10. machine-readable version/health/lifecycle contract where executable;
11. explicit Conditor binding semantics;
12. no hidden source checkout or moving-branch dependency;
13. no hidden credential, provider, database, browser, Node, .NET SDK, or external-service installation;
14. idempotent installation/binding behavior;
15. upgrade/uninstall ownership semantics where Conditor mutates host/repository state.

## G. Indy Init inclusion

The Indy Init profile should include only the exact capabilities necessary for the competition environment. It must not mean "install every Echelon repository."

Expected categories are:

- host/governance: Conditor, Praxis, Ordo, and only the engineering/security tools required by the frozen competition profile;
- repository context: Visual Engineering, Communication Engineering, Percepta lifecycle where required;
- project bindings: Aegis, Limen, Forma, Folio, Iter only when the competition scaffold requires them;
- governing input: the immutable Indy Init contract bundle;
- application implementation: generated at kickoff, not shipped inside Conditor or the Indy Init planning repository.

The exact competition release set is frozen by resolved-release-set digest and may be reproduced online or from an offline bundle.


## H. Omission-audit coverage

The 2026 repository audit compared this matrix against every repository created under the owner account during 2026 and then widened the search to older repositories whose names or contents indicate Echelon/framework/engineering responsibilities.

The following recent repositories were explicitly reviewed and are intentionally outside the Echelon distribution catalog unless a later owner decision promotes them:

| Repository | Reason excluded from distribution |
|---|---|
| `kemiller2002/-kevin-m-miller` | personal writing/voice material, not a software capability |
| `kemiller2002/catering-events` | event/catering content |
| `kemiller2002/culinary-arts-2026-state-fair` | competition/content workspace |
| `kemiller2002/ewing-house-construction-data` | project data/application material, not Echelon infrastructure |
| `kemiller2002/Games` | unrelated application workspace |
| `kemiller2002/hackney-power` | unrelated/project-specific repository; no Echelon product contract observed |
| `kemiller2002/health-samantha` | private-domain data-processing project, not an Echelon distributable system |
| `kemiller2002/highschool` | educational/personal site |
| `kemiller2002/house-design` | personal design content |
| `kemiller2002/HVAC` | personal/project data |
| `kemiller2002/learning-programming` | learning material |
| `kemiller2002/personal-administration` | personal workspace; no Echelon product contract observed |
| `kemiller2002/scratch` | scratch repository |
| `kemiller2002/travel` | personal travel workspace |
| `kemiller2002/travel-itineraries` | personal travel data/content |

An excluded repository must not become installable merely because it later adopts Praxis/Ordo, Forma, Folio, Limen, or another Echelon capability. Promotion into the catalog requires an explicit canonical identity, distribution class, owner decision, and stable-profile eligibility review.

## I. Remaining cross-cutting gaps found by the omission audit

The repository inventory exposed requirements that are not owned by one application repository:

1. **Stable Registry publication surface.** Registry needs a versioned, digest-bound catalog/profile snapshot that Conditor can discover without reading Registry source layout.
2. **Reusable release workflow.** Distributable repositories need one versioned shared release workflow or generator for release manifests, artifact digests, clean-consumer tests, provenance and readiness evidence.
3. **Release withdrawal/revocation.** Immutable release records need a non-destructive way to mark a release deprecated, withdrawn or security-revoked so new resolution refuses it while historical evidence remains intact.
4. **Software bill of materials.** Stable distributable releases need an SBOM/dependency inventory appropriate to their distribution class, plus third-party-license evidence when applicable.
5. **Platform trust.** Native public releases should carry platform-appropriate signing/notarization when practical, separately from SHA-256 integrity and CI provenance attestations.
6. **Bootstrap current-session usability.** A successful first bootstrap must leave Conditor invocable immediately, without requiring a manual PATH edit/relogin before the documented next command.
7. **Conditor version lifecycle.** There must be an explicit, atomic way to install an exact Conditor version and move between approved Conditor releases; rerunning an unpinned latest installer is not a reproducibility contract.
8. **Platform discrimination.** Runtime/platform resolution must distinguish materially incompatible Linux/runtime variants such as glibc vs musl instead of mapping all machines to a filename by OS/CPU alone.
9. **Concurrent mutation exclusion.** Two Conditor mutating operations against the same host or target must not execute concurrently without a deterministic lock/reconciliation protocol.
10. **Preflight capacity.** Before irreversible mutation, Conditor should establish required disk space, artifact/source reachability where online, authentication prerequisites, and write permissions to the extent they can be known.
11. **Canonical standard profiles.** Registry must own/version the definitions for standard environment roles, while resolved release sets freeze exact artifacts by platform.
12. **Indy Init competition profile.** The Indy Init repository must own a machine-readable event profile that names the exact capabilities/contracts and the clean-host baseline; Conditor executes that profile rather than inventing competition scope.
