# Echelon Distribution Readiness Matrix

Status: draft
Observed: 2026-10-06 (first observed 2026-09-30)
Owner: Echelon Registry
Related: `distribution-catalog-requirements.md`, `distribution-resilience-requirements.md`, `ecosystem-release-workflow-requirements.md`
Consumer: Conditor

## Purpose

This document classifies the active Echelon ecosystem by distribution role and records what must be true before a system may enter a stable Conditor profile.

It is intentionally broader than "what has a repository." A repository is not automatically an installable system. Research corpora, predecessor repositories, experiments, websites, planning repositories, and example consumers must not leak into workstation or project profiles merely because they are part of the Echelon portfolio.

Readiness states:

- **current**: a stable `echelon.release/v2` record exists under `releases/` and the `echelon-current` channel selects that release (see "Current channel" below).
- **eligible**: the observed release/lifecycle shape is already close enough to the standard contract that integration work, rather than a redesign, is the main remaining step.
- **partial**: a usable package or lifecycle exists, but Registry release metadata, immutable artifact publication, platform coverage, or Conditor semantics remain incomplete.
- **not-ready**: the repository does not yet expose a stable distributable artifact/lifecycle appropriate to its role.
- **not-installable**: this repository is intentionally not a Conditor installation target.
- **predecessor**: a newer canonical Echelon identity supersedes this repository for installation/discovery purposes.

The readiness state is not a product-quality judgment. It is only distribution-contract readiness.

## Current channel

`profiles/echelon-current.profile.json` **1.13.0** is the moving stable channel. Resolved against `snapshots/echelon-current.catalog.json`, `channels/echelon-current/` selects the same release set of required components on all five supported platforms (linux-x64, linux-arm64, osx-x64, osx-arm64, win-x64). An optional component (`required: false`) appears only on the platforms its selected release ships for (REG-REL-031): Strata has no linux-arm64 build, so linux-arm64's set omits it. Arca, Fides, Summa Contracts and Limen F# are optional project bindings: they ship platform-neutral packages, so every platform's set carries them, and only a repository that declares one installs it.

| System | Role | Selected release | Source | Distribution |
|---|---|---|---|---|
| Praxis | host-tool | **3.7.1** | `kemiller2002/praxis` `v3.7.1` (`26a0a5ff`) | GitHub release, self-contained native CLI `praxis` (alias `ros`) |
| Ordo | host-tool | **1.4.0** | `kemiller2002/ordo` `v1.4.0` (`17467fb3`) | GitHub release, self-contained native CLI `ordo` (alias `sde`) |
| Percepta | repository-lifecycle | **0.1.0** | `kemiller2002/percepta` `percepta-repo-v0.1.0` | GitHub release, self-contained native CLI `percepta-repo` |
| Dokimos | repository-lifecycle | **0.2.0** | `kemiller2002/dokimos` `dokimos-v0.2.0` | GitHub release, self-contained native CLI `dokimos` |
| Visual Engineering | repository-lifecycle | **1.0.0** | `kemiller2002/visual-engineering` `visual-engineering-v1.0.0` | npm `@echelon-foundry/visual-engineering` |
| Communication Engineering | repository-lifecycle | **1.0.0** | `kemiller2002/communication-engineering` `v1.0.0` | GitHub release package `@echelon-foundry/communication-engineering` |
| Tutela | repository-lifecycle | **0.1.0** | `kemiller2002/tutela` `v0.1.0` | GitHub release package `@echelon-foundry/tutela` |
| Aegis | project-binding | **1.0.0** | NuGet `EchelonFoundry.Aegis.Core` 1.0.0 | NuGet library family |
| Limen | project-binding | **0.9.0** | `kemiller2002/limen` `v0.9.0` (`96eb75fb`) | npm `@echelon-foundry/limen` (provenance) |
| Forma | project-binding | **0.5.0** | `kemiller2002/forma` `v0.5.0` (`8a5a5993`) | GitHub release package `@echelon-foundry/design-system` (the same tarball is published to npm as `@echelon-foundry/design-system@0.5.0`) |
| Folio | project-binding | **0.3.0** | `kemiller2002/folio` `v0.3.0` | GitHub release package `@echelon-foundry/print-components` |
| Strata | repository-lifecycle (optional, `>=0.1.1`) | **0.1.1** | `kemiller2002/strata` `v0.1.1` (`f650d91e`) | GitHub release, self-contained native CLI `strata`; linux-x64, osx-x64, osx-arm64, win-x64 |
| Arca | project-binding (optional) | **0.3.0** | `kemiller2002/arca` `v0.3.0` (`6f3acc7b`) | GitHub release NuGet package family `EchelonFoundry.Arca.Core`, `EchelonFoundry.Arca.GitHub` and `EchelonFoundry.Arca.Limen` (attested); installed through Conditor's local NuGet feed until nuget.org publishing exists. `EchelonFoundry.Arca.Limen` (the IndexedDB offline queue and read cache) depends on `EchelonFoundry.Limen.Store` and `.Contract` 0.8.0 or later, so a repository that uses it declares limen-fsharp too; it is proven against limen-fsharp 0.9.0, whose store packages are unchanged from 0.8.0 |
| Fides | project-binding (optional) | **0.2.0** | `kemiller2002/fides` `v0.2.0` (`dea13cef`) | GitHub release NuGet package family `EchelonFoundry.Fides`, `EchelonFoundry.Fides.Client`, `EchelonFoundry.Fides.Arca` and `EchelonFoundry.Fides.Hosting` (attested); installed through Conditor's local NuGet feed until nuget.org publishing exists. `EchelonFoundry.Fides.Arca` depends on `EchelonFoundry.Arca.Core`, so a repository that uses it declares arca too. The release also carries the AWS Lambda package `fides-exchange-linux-arm64.zip` (purpose `application`), which is not a project binding artifact |
| Summa Contracts | project-binding (optional) | **0.1.0** | `kemiller2002/summa` `contracts-v0.1.0` (`e888cfb1`) | GitHub release NuGet package `EchelonFoundry.Summa.Contracts` (attested), the Chrona-to-Summa billing contract (`summa.chrona-billing` 1.x) that Summa owns as the receiving application; installed through Conditor's local NuGet feed. The tag prefix keeps contract releases apart from Summa application releases |
| Limen F# | project-binding (optional) | **0.9.0** | `kemiller2002/limen` `v0.9.0` (`96eb75fb`) | GitHub release NuGet package family `EchelonFoundry.Limen.Contract`, `EchelonFoundry.Limen.Guest`, `EchelonFoundry.Limen.Store` and `EchelonFoundry.Limen.Routing` (attested), released in lockstep with the npm package; installed through Conditor's local NuGet feed. It is a separate system (`limen-fsharp`, `nuget-library`) because Limen's own release is a web package; both records name the same tag |

Older stable records remain for history and pinned profiles: Praxis 3.6.0, Forma 0.3.0 and 0.4.1, Limen 0.6.2 (historical package `@echelon-foundry/typescript-wasm-kernel`) and 0.7.0. The frozen Indy Init release set (`freezes/indy-init-0.1.0.freeze.json`) is unchanged by channel moves.

Governed repositories pin the host tools they expect in `.echelon/toolchain.json` (`"praxis": "3.7.1"`, `"ordo": "1.4.0"` for this channel).

## A. Establishment, governance, and host tooling

| Canonical system | Repository | Distribution role | Observed state | Readiness | Required next step |
|---|---|---|---|---|---|
| Conditor | `kemiller2002/conditor` | self-contained host/bootstrap CLI | native multi-platform releases, SHA verification, attestations, workstation profiles, receipts, rollback, Indy Init preset | eligible | consume external Registry catalog/profile release sets; finish offline bundle/all-supported profile |
| Echelon Registry | `kemiller2002/echelon-registry` | catalog/specification, not a host tool | system/release schemas, profiles, resolved-release sets, catalog snapshots, the `echelon-current` channel and the Indy Init freeze exist; governed by Praxis 3.7.1 / Ordo 1.4.0 | eligible | publish a versioned offline snapshot bundle and a machine-readable readiness projection |
| Praxis | `kemiller2002/praxis` | self-contained native governance CLI | `releases/praxis/3.7.1.release.json`: native artifacts for linux-x64, linux-musl-x64, linux-arm64, osx-x64, osx-arm64 and win-x64 with checksums and attestations; canonical `praxis` executable with `ros` compatibility alias; `praxis upgrade` installs `./praxis` launchers and keeps `./ros` as an alias | current | keep the `ros` alias as a compatibility surface; move channel only through new release records |
| Ordo | `kemiller2002/ordo` | self-contained native methodology/lifecycle CLI | `releases/ordo/1.4.0.release.json`: native artifacts for six platforms; canonical `ordo` executable with `sde` compatibility alias | current | have `ordo init`/`upgrade` own the `ordo` key of `.echelon/toolchain.json` (Praxis no longer writes it) |
| Project Administration | `kemiller2002/project-administration` | installation-inventory provider + administration CLI/application | canonical installation inventory, typed `administration` capability, local executable and GitHub workflow transports | partial | publish immutable executable/application release metadata; distinguish remote provider deployment from optional local CLI installation |
| ROS Worker Daemon | `kemiller2002/ROS-WorkerDaemon` | host daemon/execution supervisor | npm launcher currently requires Node 20 + .NET 10; standalone build exists locally | partial | publish self-contained native artifacts, standard release manifest, machine-readable health/version contract; keep npm as optional compatibility transport |
| Dokimos | `kemiller2002/dokimos` | engineering executable/repository capability | `dokimos-v0.2.0` (commit `e641048`) publishes self-contained native artifacts for six platforms and a shared-contract `echelon.release/v2` manifest; declares `echelon.repository-lifecycle` v1; cataloged as `releases/dokimos/0.2.0.release.json`; Conditor clean-host proof (linux-x64, `dokimos-proof` 0.1.0) installed, initialized, verified and re-applied with zero drift — kemiller2002/conditor Actions run 37003356848 (2026-10-02) | current | add macOS clean-host proof when infrastructure is available |
| Tutela | `kemiller2002/tutela` | security lifecycle/tooling capability | `releases/tutela/0.1.0.release.json`: GitHub release package `@echelon-foundry/tutela` with checksums | current | canonical id is `tutela`, with historical Tutors naming only as alias if needed; prove a self-contained lifecycle distribution |
| Vigila | `kemiller2002/vigila` | application/tool + integration executable | durable integration work exists; host release contract not proven | not-ready | publish executable distribution and separate receiving-system integration contract from application installation |
| Tekmerion | `kemiller2002/tekmerion` | research lifecycle/publishing capability | F# self-contained lifecycle binaries are bundled behind npm; mature init/status/verify/doctor/upgrade contract | partial | canonicalize Tekmerion package/release identity, publish Registry metadata, separate lifecycle artifact from legacy publishing-runtime prerequisites |
| Visual Engineering | `kemiller2002/visual-engineering` | repository lifecycle/evidence-context capability | `releases/visual-engineering/1.0.0.release.json`: npm `@echelon-foundry/visual-engineering` 1.0.0 | current | preferably a self-contained lifecycle distribution so Node is not a universal bootstrap prerequisite |
| Communication Engineering | `kemiller2002/communication-engineering` | repository lifecycle/evidence-context capability | `releases/communication-engineering/1.0.0.release.json`: GitHub release package `@echelon-foundry/communication-engineering` 1.0.0 | current | retire the pinned-commit fallback from Conditor's stable paths |
| Percepta | `kemiller2002/percepta` | semantic verifier/compiler + separate repository lifecycle | `releases/percepta/0.1.0.release.json`: self-contained native repository lifecycle `percepta-repo` for five platforms; semantic verifier is separate | current (repository lifecycle) | describe the semantic verifier surface and its optional browser prerequisite in a later release |

## B. Project-bound reusable capabilities

These are not global workstation packages merely because they are common Echelon dependencies.

| Canonical system | Repository | Distribution role | Observed state | Readiness | Conditor rule |
|---|---|---|---|---|---|
| Arca | `kemiller2002/arca` | .NET/NuGet library family (pure core + GitHub adapter + Limen IndexedDB bridge) | `releases/arca/0.3.0.release.json` (0.1.0, 0.2.0 and 0.2.1 retained): GitHub release assets `EchelonFoundry.Arca.Core`, `EchelonFoundry.Arca.GitHub` and `EchelonFoundry.Arca.Limen` 0.3.0 (the IndexedDB offline queue, the localStorage-to-IndexedDB move and the read cache), each with a build-provenance attestation; not yet on nuget.org | current | bind exact packages to an explicit .NET project through Conditor's local NuGet feed (`vendor/nuget`, Conditor `docs/nuget-feed-contract.md`); a repository that references `EchelonFoundry.Arca.Limen` also declares limen-fsharp |
| Fides | `kemiller2002/fides` | .NET/NuGet library family (pure single sign-on core, WebAssembly client, Arca bridge, hosting runtime) plus an AWS Lambda package | `releases/fides/0.2.0.release.json` (and 0.1.0): GitHub release assets `EchelonFoundry.Fides`, `EchelonFoundry.Fides.Client`, `EchelonFoundry.Fides.Arca` and `EchelonFoundry.Fides.Hosting` 0.2.0 and `fides-exchange-linux-arm64.zip`, each with a build-provenance attestation; not yet on nuget.org | current | bind exact packages to an explicit .NET project through Conditor's local NuGet feed (`vendor/nuget`, Conditor `docs/nuget-feed-contract.md`) |
| Summa Contracts | `kemiller2002/summa` | .NET/NuGet library (pure contract types, canonical JSON codec, golden vectors) | `releases/summa-contracts/0.1.0.release.json`: GitHub release assets `EchelonFoundry.Summa.Contracts` 0.1.0, `checksums.txt` and `echelon-release.json`, each with a build-provenance attestation from `.github/workflows/contracts-release.yml`; not on nuget.org | current | bind the exact package to an explicit .NET project through Conditor's local NuGet feed (`vendor/nuget`, Conditor `docs/nuget-feed-contract.md`) |
| Limen F# | `kemiller2002/limen` | .NET/NuGet library (generated contract bindings, the engine's handshake, the functional IndexedDB store API and its in-memory fake, and URL-state routing) | `releases/limen-fsharp/0.9.0.release.json` (0.8.0 retained): GitHub release assets `EchelonFoundry.Limen.Contract`, `.Guest`, `.Store` and `.Routing` 0.9.0 and `checksums.txt`, each with a build-provenance attestation from `.github/workflows/publish.yml`; not on nuget.org | current | bind the exact packages to an explicit .NET project through Conditor's local NuGet feed (`vendor/nuget`, Conditor `docs/nuget-feed-contract.md`) |
| Aegis | `kemiller2002/aegis` | .NET/NuGet library family | `releases/aegis/1.0.0.release.json`: NuGet `EchelonFoundry.Aegis.Core` 1.0.0, MIT licensed | current | bind exact packages only to an explicit .NET project/scaffold |
| Limen | `kemiller2002/limen` | browser/runtime package + repository lifecycle capability | `releases/limen/0.9.0.release.json`: npm `@echelon-foundry/limen` 0.9.0, with the `./routing` subpath and the `echelon.routes/v1` schema (0.8.0, 0.7.1, 0.7.0 and historical 0.6.2 also recorded); its F# packages are the separate `limen-fsharp` system; F# lifecycle exists; product and package names agree | current | explicit web target only; canonical id `limen`; current npm distribution identity is `@echelon-foundry/limen`; the deprecated `@echelon-foundry/typescript-wasm-kernel` (last 0.6.2) is a historical distribution identity, not system identity |
| Forma | `kemiller2002/forma` | zero-runtime design-system package | `releases/forma/0.5.0.release.json`: GitHub release package `@echelon-foundry/design-system` 0.5.0, which adds the static icon system and the `icons/*` package exports (0.3.0 and 0.4.1 also recorded) | current | bind to explicit web project; never global-install or copy source/CSS into consumer |
| Folio | `kemiller2002/folio` | print/web-component package | `releases/folio/0.3.0.release.json`: GitHub release package `@echelon-foundry/print-components` 0.3.0 | current | bind to explicit project; renderer capabilities are separate explicit prerequisites |
| Iter | `kemiller2002/iter` | optional .NET/NuGet application library | F# routing library exists; release contract not proven | not-ready | bind only to an explicit requesting .NET target; Limen must never depend on Iter |
| Framework Templates | `kemiller2002/Framework-templates` | reusable framework scaffolding/template bundle | framework-agnostic starter artifacts exist; no immutable distribution contract observed | not-ready | publish a versioned template bundle and bind it only when explicitly scaffolding a framework repository; do not global-install it |

## C. Echelon applications and developer products

Applications are not automatically members of an "all tools on PATH" profile. Their developer environment and end-user deployment are separate desired states.

| Canonical system | Repository | Role | Readiness | Distribution requirement |
|---|---|---|---|---|
| Chrona | `kemiller2002/chrona` | time-entry/time-tracking application (pre-implementation); also the **planned owner** of canonical time primitives, to be built as a separately packaged library in the chrona repository (working name `Chrona.Time`), not inside the time-entry application; Vigila is the first consumer. Not implemented (see "Planned portfolio capabilities") | not-ready | declare application artifact/deployment class and separate dev profile from end-user deployment |
| Summa | `kemiller2002/summa` | project-administration hub (cross-repository work coordination); also the **planned owner** of ledger/invoicing/receivables/payments, gated on Summa replacing its copied Node-era tooling with a real foundation (QDI-071). Not implemented (see "Planned portfolio capabilities") | not-ready | declare application artifact/deployment class; must not advertise billing, invoice or payment capabilities until they are implemented |
| Strata | `kemiller2002/strata` | SQL/schema developer tool | current | `releases/strata/0.1.1.release.json` (0.1.0 kept): self-contained native CLI (no .NET runtime) for linux-x64, osx-x64, osx-arm64 and win-x64 with checksums and attestations; declares `echelon.repository-lifecycle` v1; no linux-arm64/musl build until its PostgreSQL parser ships one. A reusable .NET library is not distributed |
| Forma Studio | `kemiller2002/forma-studio` | design/workflow application | not-ready | declare runtime/deployment class and separate development from end-user installation |
| Mercatus | `kemiller2002/mercatus` | sales/marketing application | not-ready | publish application/developer distribution contract; predecessor `sales-and-marketing` must not be independently installable |
| Signal | `kemiller2002/signal` | F#/WASM survey and assessment application | not-ready | publish immutable application/static-deployment artifact and developer profile; preserve no-PII constraints in release evidence |
| HelixNote | `kemiller2002/helix-note-application` | evidence/reasoning application | not-ready | replace source-oriented MVP bootstrap with declared developer/deployment artifacts and current Echelon stack bindings |
| Clarity Service | `kemiller2002/clarity-service` | repository-analysis CLI/service | not-ready | publish self-contained native CLI and Registry metadata; consuming users should not need the .NET SDK |
| Echelon Culinary | `kemiller2002/echelon-culinary` | static web application/site | partial | treat GitHub Pages/static build as deployment, not workstation installation; only add to Conditor if an explicit developer/deploy profile needs it |
| Recipe Formatter | `kemiller2002/recipe-formatter` | standalone static application/pilot | partial | keep outside core Echelon profiles unless promoted to a canonical Echelon application with an explicit release identity |

### Planned portfolio capabilities

Two portfolio capabilities have a **planned owner** but **no implementation**. They are recorded in `registry/systems-v2.json` under `plannedCapabilities` with `status: "planned"` (owner decision: `kemiller2002/echelon-organization-administration` QDI-079, amended 2026-10-05; the original QDI-079 had recorded both as unowned). Ownership is a commitment, not a capability.

| Capability | Planned owner | Prerequisites | Actual state | Next action |
|---|---|---|---|---|
| Canonical time primitives (Instant, Clock, date-only/calendar semantics) | Chrona | separately packaged library in chrona (working name `Chrona.Time`); Vigila first consumer | no library or contract exists; Chrona's time-entry application does not provide it; Vigila defines its own local time types | Chrona builds `Chrona.Time` as a separately packaged library; Vigila adopts it first |
| Ledger, invoicing, receivables and payments (`billing.record`, `invoice.create`, `payment.record`) | Summa | Summa foundation replaces copied Node tooling (QDI-071) | requirements exist only as Summa input documents; no implementation | Summa completes QDI-071, then implements the capability |

Until a planned capability is implemented, no system — including its planned owner — may list its capability ids under `provides`. A consumer may still declare them in `optionalConsumes`; resolution returns `unavailable`, which MUST NOT fail the consumer's core behavior.

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
7. `limen` is canonical. The repository was formerly `typescript-wasm-kernel`, which remains a compatibility alias of `limen`, not a second system. From 0.7.0 the npm distribution identity is `@echelon-foundry/limen`. `@echelon-foundry/typescript-wasm-kernel` is deprecated; its releases through 0.6.2 keep that package name as the historical distribution identity valid for those releases (REG-DIST-152).

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

1. **Stable Registry publication surface.** Partly met: `channels/echelon-current/` publishes digest-bound resolved sets and a channel index over a catalog snapshot. A versioned offline snapshot bundle that Conditor can discover without reading Registry source layout is still open.
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
