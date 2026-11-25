---
id: RP-REG-2026-0001
title: Application observability — use cases and gap analysis
research_area: portfolio-capabilities
discipline:
  - observability
  - architecture
author_agent: anthropic/claude-code
version: 1.0.0
confidence: medium
completion: complete
priority: medium
created: 2026-11-25
updated: 2026-11-25
related_projects:
  - aegis
  - vigila
  - vitium
  - strata
  - unbound
related_documents:
  - registry/systems-v2.json
  - spec/ecosystem-distribution-readiness.md
  - spec/echelon-integration-standard.md
supersedes: []
superseded_by: []
tags: [observability, telemetry, gap-analysis, use-cases, unowned-capability]
keywords: [logging, metrics, tracing, health, alerting, OpenTelemetry, OTLP, Aegis, Vitium]
provenance:
  contributions:
    EXE-20261125T015539652Z-8aaad225:
      operations: [created]
      at: 2026-11-25T01:59:09.743Z
      actor:
        kind: agent
        id: anthropic/claude-code
        provider: anthropic
        model: unknown
        runtime: claude-code
      reason: "Record application-observability use cases and gap analysis; owner deferred building the system"
---

# Research State Snapshot

- **Theory version:** none. This package records an inventory and a gap analysis. It is not a theory.
- **Knowledge-base version:** Registry `main` at `fef1f94` (echelon-current 1.39.0). The default branches of 56 public `kemiller2002` repositories were read on 2026-11-25.
- **Highest-confidence areas:** who uses which Echelon component; what Aegis provides and declines; that no application-observability owner exists.
- **Lowest-confidence areas:** the needs of the five private repositories, which were not read (helix-note-application, clarity-service, ROS-WorkerDaemon, Framework-templates, clarity-framework); production volumes and costs, because no Echelon application is in production with telemetry.
- **Largest remaining unknown:** which first production workload (Unbound, Signal, Causa, Chrona or Summa) will need operational telemetry, and when.
- **Active research streams:** none. The owner deferred building the system (see Decision).
- **Recently invalidated ideas:** that Vigila is the observability system. It is the follow-up and task organizer (section "Failed assumptions").
- **Priority changes:** none.

# Executive Summary

No Echelon system owns application logging, metrics, distributed tracing, health monitoring or alerting. Nothing in the portfolio uses a substitute either.

| Stage | Owner | State (2026-11-25) |
|---|---|---|
| Capture and classify unexpected operational failure | **Aegis** | Released 1.0.0 (NuGet); 17 repositories use it. Source is at 2.0.0, which is unreleased |
| Logs, metrics, traces, health, alerting, incident view, telemetry export | **none** | Gap: no owner, no implementation, no third-party library in use |
| Triage confirmed problems into defects | **Vitium** | Proposed. Machine intake is not deployed, and Vitium is not in the Registry |
| Observe *engineering* activity (commits, CI, deployments) | **Specula** (unregistered), **Praxis** (`engineering.metrics`) | Out of scope here: engineering observation, not application operation |

**Decision (owner, 2026-11-25):** do not build an application-observability system yet. Keep it as an option. Record the use cases and gaps, and close the gaps that do not depend on such a system. This package is that record. Building the system remains **Option A** in "Options".

**Main findings:**

1. **Aegis is the producer that a future system should consume.**
   - It already emits schema-versioned fault events (`aegis/fault/v1`, `aegis/event/v1`) with classification, redaction, lifecycle, escalation, dependency grouping and agent guidance.
   - It declares itself "not a logging framework, a metrics platform, a tracing product, an analytics system or a monitoring tool". Telemetry systems consume its events (aegis `docs/aegis/README.md:337-342`).
2. **Aegis events cannot yet be joined into distributed traces or scoped to a tenant.**
   - The `Fault` record has no trace, span, causation or tenant field (aegis `src/Aegis.Core/Types.fs:214-242`).
   - Correlation exists only within one process scope (`Capture.fs:181-191`).
3. **No repository depends on OpenTelemetry, Serilog or Microsoft.Extensions.Logging.**
   - That leaves the door open to adopt OpenTelemetry cleanly when the option is taken.
   - It also means there is no operational telemetry today beyond Aegis fault events.
4. **Several of the original assignment's premises are wrong** (section "Failed assumptions"):
   - Vigila is a task organizer.
   - Strata is a SQL/schema developer tool with no runtime consumers.
   - Vitium is unregistered.

# Original Objective

The original objective was to implement "Vigila" as Echelon Foundry's production logging, monitoring, observability and diagnostics system.

Discovery showed that Vigila is something else and that observability has no owner. The owner then narrowed the objective to three tasks:

- write the use cases and gap analysis for an application-observability system;
- leave building it as an option;
- complete the work that does not depend on it.

# Scope

**In scope:**
- runtime observability of Echelon *applications*: logs, metrics, traces, health, alerting and incident correlation;
- AI-assisted diagnosis over that telemetry;
- its boundaries with Aegis, Vitium, Fides, Arca, Summa, Praxis and Specula.

**Out of scope:**
- engineering-execution observation, owned by Praxis and Specula;
- defect management, owned by Vitium;
- business audit records and financial ledgers, owned by the consuming applications and Summa;
- security evidence, owned by Tutela;
- choosing a vendor or buying infrastructure.

# Repository Context

The method used for each source:

| Source | What was read | Method |
|---|---|---|
| `registry/systems-v2.json`, `profiles/*`, `examples/*.system.json`, `spec/*` | System identities, `provides`/`optionalConsumes`, profile components, readiness | Parsed JSON; grep |
| 56 public `kemiller2002` repositories | `conditor.json`, `*.fsproj`/`*.csproj`, `Directory.Packages.props`, `package.json`, excluding vendored and generated paths | Blobless shallow clone, read via `git show` |
| `kemiller2002/aegis` | README, `docs/aegis/*`, decisions, `src/Aegis.Core/*.fs` | Read |
| `kemiller2002/vitium` | README, `docs/requirements/VITIUM-REQUIREMENTS.md`, `VITIUM-BUILD-SYSTEM-REPORTING.md`, `src/Vitium.Contracts` | Read |
| `kemiller2002/vigila` | README, `docs/requirements/SCOPE.md`, `SECURITY.md`, source tree | Read |
| `kemiller2002/unbound` | Requirements (see "Unbound traceability") | Read |
| nuget.org | `EchelonFoundry.Aegis.Core` versions (only `1.0.0`); `EchelonFoundry.Vitium.Contracts` (404, not published) | HTTP |

# Current Understanding

## Who uses what (2026-11-25)

The counts below are repositories that declare the component in `conditor.json`, a project file or `package.json`. A system's own repository is not counted.

| Component | Consumers | Note |
|---|---|---|
| Ordo | 30 | 23 pin 1.5.0–1.8.2; four are on 1.20.0 (Causa, Mercatus, Signal, gate-proof-r10) |
| Praxis | 29 | Most pin 3.7.2; the same four are on 3.25.0 |
| Visual Engineering | 20 | |
| Forma | 18 | 0.2.0–1.0.1; only Mercatus and Signal are on 1.x |
| **Aegis** | **17** | All on 1.0.0: Arca, Causa, Chrona, Conditor, Dokimos, Fides, Forma Studio, Percepta, Praxis, Project Administration, Signal, Strata, Summa, Tekmerion, Vigila, gate-proof-r10, indy-init-rehearsal-001 |
| Limen (web) | 14 | |
| Folio | 13 | Dokimos and Percepta pin raw commits |
| Communication Engineering | 8 | |
| Limen F# | 6 | |
| Arca | 4 | Chrona and Summa on 0.5.0; Signal on 0.7.0; Fides on 0.1.0 |
| Percepta | 4 | |
| Fides | 3 | Chrona, Signal, Summa |
| Strata | **0** | |
| Vigila | **0** package consumers | Praxis lists it as an integration (`FileInstanceProjection.fs:27`, availability `Unknown`) and optionally consumes `followup.create` |
| Vitium | **0** | |
| OpenTelemetry, Serilog, Microsoft.Extensions.Logging | **0** | Searched across all of the above |

## Capability contracts declared in the Registry

| Consumer | Capability | Provider |
|---|---|---|
| Praxis | `followup.create` | Vigila |
| Praxis | `workitem.query` | **none registered** |
| Praxis, Ordo, Conditor | `installation.register` (Conditor also `installation.remove`) | Project Administration |
| Chrona | `billing.record` | Summa (planned, not implemented) |

## What Aegis already covers

Most of what an observability system would need for *failures* already exists in Aegis. A future system must consume these, not rebuild them. Paths are in `kemiller2002/aegis`.

| Need | Aegis provides | Evidence |
|---|---|---|
| Typed, versioned failure events | `Fault` and the append-only `AegisEvent` stream; `aegis/fault/v1`, `aegis/event/v1`; sortable ids | `src/Aegis.Core/Types.fs:8-13,214-242,340-361` |
| Error classification | Category, severity, impact, persistence, recovery policy, failure domain, blast radius, retention intent; stable codes | `Types.fs:47-159` |
| Retry and recovery semantics | Retry eligibility, automatic-recovery permission, attempt budgets, refusal of non-idempotent repeats, default authority `denyAll` | `Catalog.fs:31-41`, `Recovery.fs:36-41,166-228` |
| Redaction before leaving the process | Classification at creation (`Public`/`Internal`/`Sensitive`/`Secret`); key rules that can only be added; redaction inside serialization before any sink | `Redaction.fs:24-75`, `Capture.fs:261-265` |
| Bounded, loss-visible delivery | Required/Preferred/Optional sinks; per-sink isolation; a single non-recursive fallback; a bounded offline queue that counts drops; `flush`; delivery reports | `Sinks.fs:13-138`, `Offline.fs:20-105`, `Capture.fs:47-160` |
| Self-health of the pipeline | Healthy, Degraded, PersistenceUnavailable, QueueBacklogged, BootstrapOnly, CriticalFailure | `Health.fs:11-64` |
| Deduplication, recurrence, escalation | Fingerprints, lifecycle projection, escalation rules that only raise severity | `Lifecycle.fs:15-200`, `Escalation.fs:11-54` |
| First-level incident grouping | `byDependency`, `likelyRootCause` | `Escalation.fs:71-97` |
| Agent-readable diagnosis | Catalog guidance: likely causes, evidence to inspect, safe checks, prohibited actions; diagnostic bundles; a catalog can forbid an action but never permit one | `Catalog.fs:5-59`, `Diagnostics.fs:64-132` |
| Security-event hand-off | `Tutela.tryProjectFault` maps to `tutela/evidence/v1` | `Tutela.fs:8-61` |

What Aegis does **not** cover:
- trace and span ids, W3C trace context and `System.Diagnostics.Activity`;
- a causation id;
- a tenant field;
- non-failure logs;
- metrics;
- service or dependency health checks;
- alert routing;
- a cross-process query or index;
- export to OpenTelemetry/OTLP;
- browser/WebAssembly instrumentation;
- redaction of `UserMessage`, `TechnicalDetails` and exception messages. They are serialized unredacted (`Serialization.fs:127-139,198-201`); only `Diagnostics.export` strips `TechnicalDetails`.

# Use Cases

Actors:
- **Operator**: the person on call for an Echelon application.
- **Investigator agent**: an authorized AI agent acting as first-line diagnostician.
- **Developer**: instruments an application.
- **Tenant operator**: sponsor or white-label administrator.
- **Security reviewer.**
- **Finance operations.**

Each use case lists what exists today and what is missing. Codes in "Gap" refer to the "Gap Analysis" table.

### UC-OBS-01 — Reconstruct one failed operation end to end
- **Actor:** Operator; Investigator agent.
- **Trigger:** a user-visible failure, for example a grant shows an error.
- **Goal:** see every component, dependency call, retry and fault for that operation, in order, with timing.
- **Today:** Aegis faults share a `CorrelationId` within one scope, so an investigation can collect faults but not successful steps, timings, or calls across a browser→API→worker boundary.
- **Gap:** G-01, G-02, G-03, G-08.

### UC-OBS-02 — Detect an outage before users report it
- **Actor:** Operator.
- **Trigger:** a dependency (payment provider, email service, data import) stops responding.
- **Goal:** an alert that fires once, names the dependency, and lists affected operations and tenants.
- **Today:** Aegis `byDependency` groups active faults inside one process. Nothing evaluates thresholds across processes, routes alerts or suppresses duplicates across instances.
- **Gap:** G-04, G-05, G-06, G-07.

### UC-OBS-03 — Answer "is it up, and is it healthy?"
- **Actor:** Operator; deployment tooling; load balancer.
- **Goal:** separate liveness, readiness and dependency health, with the states healthy, degraded, unavailable, unknown, recovering and disabled.
- **Constraint:** public endpoints must not expose infrastructure details.
- **Today:** Aegis reports only *its own pipeline's* health. There is no shared contract for application health.
- **Gap:** G-05.

### UC-OBS-04 — Know whether retrying is safe
- **Actor:** Investigator agent; Operator.
- **Trigger:** an operation timed out or returned an uncertain result, for example a payment instruction with no provider acknowledgement.
- **Goal:** decide on evidence whether the operation took effect before any retry.
- **Today:** Aegis records retry eligibility and refuses non-idempotent repeats. It deliberately does not infer safety from a timeout. Whether the operation took effect is a *business* fact, so it belongs to the application's records, for example Unbound's payment states.
- **Gap:** this is **mostly covered**. The remaining gap is linking an Aegis fault to the application's authoritative record id (G-09).

### UC-OBS-05 — Investigate with an AI agent without widening its power
- **Actor:** Investigator agent.
- **Goal:** state what happened, what started it, where it failed and what supports the diagnosis, keeping observations, inferences and uncertainty separate.
- **Constraints:**
  - The agent gains no operational permission from diagnostic access.
  - It treats log text as data, never as instructions.
- **Today:** Aegis catalog guidance and diagnostic bundles serve one process. `Recovery.denyAll` is the default. Nothing indexes events across processes or offers a query contract, and nothing marks free text as untrusted.
- **Gap:** G-08, G-10.

### UC-OBS-06 — Keep one tenant's telemetry away from another
- **Actor:** Tenant operator; Security reviewer.
- **Goal:**
  - A white-label tenant's operators see only their own operations.
  - A cross-tenant outage is visible to platform operators without exposing tenant data.
- **Today:** Aegis has no tenant field. Context values can carry one, but nothing enforces scope.
- **Gap:** G-02, G-11.

### UC-OBS-07 — Instrument a new application in minutes
- **Actor:** Developer.
- **Goal:** add one package and one call at startup to get structured logs, traces, metrics, health and Aegis fault export with safe defaults.
- **Today:** Aegis takes a few lines (`Aegis.scope`, `capture`). There is nothing for the rest.
- **Gap:** G-12.

### UC-OBS-08 — See browser and WebAssembly failures
- **Actor:** Operator.
- **Goal:** capture WebAssembly startup, loading, navigation and API failures correlated to server operations, without keystrokes or form contents. Treat browser input as untrusted.
- **Today:** Aegis has an optional `BrowserVersion` field and no browser package. Limen applications have nothing.
- **Gap:** G-13.

### UC-OBS-09 — Monitor production AI workflows
- **Actor:** Operator; Finance operations (cost).
- **Goal:** record agent identity, model, tokens, cost, tool calls and failures, escalations and policy violations for *application* AI work. Prompts are not captured by default.
- **Today:** Praxis owns *engineering* agent telemetry (`agent.identity`, `engineering.metrics`). Nothing covers *application* AI activity.
- **Gap:** G-14.

### UC-OBS-10 — Promote a recurring failure to a defect, once
- **Actor:** Operator; Investigator agent.
- **Goal:** when evidence shows a defect rather than a transient failure, submit one Vitium observation carrying evidence references, with no duplicates.
- **Today:** Vitium's `observation.detected` contract is proposed, and its intake is not deployed. Aegis fingerprints exist. Vitium requirement VIT-INT-005 requires policy authorization before an Aegis failure becomes a report.
- **Gap:** G-15. Vitium deployment is a non-observability prerequisite (N-02).

### UC-OBS-11 — Notice that observability itself is failing
- **Actor:** Operator.
- **Goal:** alert when export fails, buffers fill, telemetry is dropped or redaction fails, without recursive logging.
- **Today:** Aegis covers this for its own sinks: health states, drop counts and a non-recursive fallback. Nothing covers a wider pipeline, because there is none.
- **Gap:** G-16, which is satisfied by design once G-04 exists if Aegis's pattern is reused.

### UC-OBS-12 — Keep telemetry out of the audit trail
- **Actor:** Finance operations; Security reviewer.
- **Goal:** a financial transaction stays verifiable when every diagnostic record is deleted or sampled away.
- **Today:** this is satisfied by separation. Aegis declares `Retention` as intent only, and applications own audit records. It must stay that way.
- **Gap:** G-17, a documentation and contract rule rather than a build item.

### Not use cases for this system

| Need | Owner |
|---|---|
| Ledger and financial records | Summa / applications |
| Business audit evidence packages | Applications (e.g. Unbound D-006) |
| Defect lifecycle | Vitium |
| Engineering execution telemetry | Praxis |
| Engineering activity timelines | Specula |
| Security evidence | Tutela |
| Identity and authorization | Fides |
| Offline persistence | Arca |
| Failure capture | Aegis |

# Unbound traceability

Unbound (`kemiller2002/unbound`) is the most demanding planned consumer. It has requirements but no code, and most records are proposed or provisional. Its requirements were read on 2026-11-25 at `54dd1bf`. Paths below are relative to `unbound/requirements/`, except `architecture/`, which is at the repository root.

**Unbound already puts telemetry outside the audit record.** RR-UNB-RES-003-r2 (`discovery/2026-11-24-simplification-economics/18-REFINED-REQUIREMENTS.md:802`) sets recovery classes:
- authoritative facts are C0 (RPO 0);
- evidence and issued documents are C1;
- "(C4) telemetry and non-evidentiary logs: best effort".

Two further statements reinforce this:
- `architecture/CONTINUOUS-CONTROL-MONITORING-AND-EVIDENCE-ASSURANCE.md:41`: "Monitoring failure is a degraded-assurance incident, not a healthy state."
- SOC2-027 (`governance/SOC2-CONTROLS-EVIDENCE-AND-ENTERPRISE-DIRECTIVE.md:35`): "A missing expected log or control heartbeat MUST itself create a monitored control exception."

An observability system can therefore never be Unbound's evidence store. It can only *detect* that evidence is missing.

Owner key:
- **Obs**: needs the observability system.
- **App**: Unbound's business logic, which observability may surface but must not decide.
- **Audit**: Unbound's evidence or ledger, which must not live in telemetry.

| Unbound requirement | Source | Need | Owner | Use case / gap |
|---|---|---|---|---|
| UNB-API-STD-003 | `PLATFORM-UX-API-AND-ASSURANCE-STANDARDS.md:20` | Tracing/correlation ids on every API and event contract | Obs | UC-01; G-01 |
| UNB-TEN-009, UNB-TEN-007, UNB-TEN-006 | `PARTNER-SEGREGATION-AND-TENANCY.md:16-19` | Tenant isolation covers logs and observability; shared telemetry needs an explicit cross-tenant classification; partner-scoped monitoring | Obs | UC-06; G-02, G-11 |
| UNB-RES-001, UNB-RES-003 / RR-UNB-RES-003-r2 | `PLATFORM-...:47,49`; `18-REFINED...:799-810` | Failure modes per dependency outage; runbooks, telemetry and incident escalation; telemetry is C4 | Obs (C4) / Audit (C0–C1) | UC-02, UC-03, UC-12; G-05, G-17 |
| SPM-062, SPM-066–SPM-070 | `discovery/2026-11-24-simplification-economics/13-DASHBOARD-AND-REPORTING-REQUIREMENTS.md:248-256` | Per-journey SLOs, outbox age, file-delivery misses, read-model watermark age, AI cost and prompt-injection spike alerts; a single uptime percentage is rejected (SPM-071) | Obs | UC-02, UC-09; G-04, G-06, G-14 |
| SOC2-012, SOC2-017 | `governance/SOC2-...:20,25` | Security monitoring of authentication, authorization, administrator, export, agent and configuration activity; agents attributable, monitored and interruptible | Obs (detection) + Audit (evidence) | UC-05, UC-09; G-14 |
| SOC2-027, CTRL-SEC-01, RR-MON-002-r1, MON-004, MON-018 | `governance/SOC2-...:35,58`; `18-REFINED...:465-474`; `governance/CONTINUOUS-CONTROL-MONITORING-REQUIREMENTS.csv` | A missing log or heartbeat is an exception; zero activity counts only if the collector heartbeat was healthy | Obs (heartbeat) → Audit | UC-11; G-05, G-16 |
| SOC2-003, MON-014, GEX envelope | `governance/SOC2-...:10`; `grants/GRANT-EVENT-CATALOG-AND-EXCEPTION-RESOLUTION.md:8` | No secrets, credentials, unnecessary PII, restricted documents or unrestricted prompts in logs | Obs (redaction) | UC-05, UC-06; G-18 |
| UNB-GEX-020, UNB-GEX-023, UNB-GEX-021 | `grants/GRANT-EVENT-CATALOG-...:126-129` | Bounded retry and quarantine; detection and resolution times; case deduplication to avoid alert storms | App, with Obs metrics | UC-02; G-04 (Aegis already covers quarantine and dead letters) |
| RR-REQ-X-004-r1, UNB-RCN-GRT-008, UNB-PAY-004, UNB-PAY-006, UNB-GEX-040 | `grants/END-TO-END-GRANT-LIFECYCLE-AND-OPERATING-MODEL.md:140-149,244-252`; `DEFERRED-PAYMENTS-AND-REMAINING-GAPS.md:10,12`; `grants/GRANT-EVENT-CATALOG-...:146` | A payment in Unknown blocks redispatch; Unknown is not failure; aging escalation | **App** | UC-04; G-09 only |
| UNB-XIN-BNK-003, UNB-GEX-005, UNB-GEX-006 | `DEFERRED-PAYMENTS-...:93`; `grants/GRANT-EVENT-CATALOG-...:111-112` | Unmatched bank debit becomes a high-severity incident within 1 hour; exception owner, SLA and closure proof; state-divergence detection | **App** (business exceptions) | Not observability; reconciliation is Unbound's |
| UNB-XIN-EVA-004, UNB-XIN-EVA-007 | `discovery/2026-11-23-cross-industry/03-MISSING-REQUIREMENTS-REGISTER.md:110,112` | Late feed or stalled process manager creates a business work item and holds automation | App | UC-02 (Obs may alert on the same signal) |
| RR-UNB-GRANT-030-r2, RR-TVR-OPS-002-r1 | `18-REFINED...:787-798,455-464` | Rule monitoring signals that auto-suspend a rule; kill-switch notification failure raises an incident | App (business control) | Obs supplies the signal only |
| AIN-013, AIN-019, AIN-033, UNB-RCN-PLT-004, RR-UNB-AI-STD-003-r1 | `ai/AI-NATIVE-INTERACTION-AND-AGENT-COLLABORATION.md:24,30,44`; `PLATFORM-...:118-124,169-174` | Agent identity, version and tool actions recorded; a missing agent trace is a deficiency; retrieval and denials logged; an agent cannot close its own alert | Audit (records), with Obs (telemetry) | UC-09; G-14 |
| UNB-XIN-EVA-016, UNB-XIN-EVA-003 | cross-industry `data/gap-requirements.jsonl:148` | Physically separate authority tables; reports never read event streams for financial figures | **Audit** | UC-12; G-17 |
| RR-REQ-X-036-r1, SOC2-023, SOC2-032 | `charities/JURISDICTION-AWARE-REGULATORY-RULES-AND-ELIGIBILITY.md:168-175`; `governance/SOC2-...:31,40` | One retention class per record; no invented universal retention; append-only logs are not exempt from privacy deletion | Audit; Obs must support deletion | G-17 |

**Unbound decisions that the observability option depends on:**
- XID-SAS-004, UNB-XIN-SAS-008 and UNB-XIN-EVA-013: SLO, RPO and RTO values.
- XID-EVA-007 and ADR-006: PII vault and erasure.
- RCD-007: the retention catalog, pending counsel.
- GEX-ADR-009: the boundary between a security incident and a business exception.
- `discovery/2026-11-24-simplification-economics/17-ARCHITECTURAL-IMPLICATIONS.md:21,54`: no message broker, workflow engine or change-data capture is justified for release 1, so telemetry must not assume one.

**Unbound states no expectations of Aegis, Vigila, Vitium, Fides or Arca.** They appear only as "candidate systems to investigate" (`CHARTER.md:40`). Unbound records that Strata provides schema governance only, with no data API (`reconciliation/2026-11-23/13-ARCHITECTURE-DEPENDENCY-ANALYSIS.md:28`). That is consistent with N-04.

**Unbound's adoption requirement**, to be recorded in Unbound when it chooses its stack: adopt Aegis for unexpected operational failure, as `spec/echelon-integration-standard.md:73` requires. Keep C0 and C1 evidence in Unbound-owned stores.

# Gap Analysis

The class column means:
- **O**: needs the observability system (deferred, Option A).
- **A**: can be closed in Aegis without a new system, subject to Aegis's own governance.
- **D**: documentation or contract only.

| Id | Gap | Use cases | Today | Class | What closing it means |
|---|---|---|---|---|---|
| G-01 | No distributed trace context | 01, 05, 08 | None in any repository | O (+A for the fields) | Adopt W3C Trace Context via .NET `Activity`/OpenTelemetry; Aegis faults carry `traceId`/`spanId` when an ambient `Activity` exists |
| G-02 | No tenant or causation identifiers on telemetry | 01, 06 | `Fault` has neither | A | Optional `TenantRef` and `CausationId` on `Fault`, additive under `aegis/fault/v1` or a v2 schema; never free-text tenant names |
| G-03 | No non-failure structured logging | 01 | None | O | Use `Microsoft.Extensions.Logging` with typed F# event ids, not a proprietary logger |
| G-04 | No metrics, cardinality control or export | 02, 11 | None | O | OpenTelemetry metrics with an allow-list of attribute keys per instrument; no per-donor, per-request or per-transaction identifiers |
| G-05 | No application health contract | 02, 03 | Aegis pipeline health only | O | Typed health states and checks (startup, liveness, readiness, dependency) with separate public and private views |
| G-06 | No alert evaluation, deduplication, suppression or routing | 02 | Aegis escalation, per process | O | Prefer the managed backend's alerting over building alert evaluation; Echelon supplies typed alert-rule definitions |
| G-07 | No cross-process dependency view | 02 | Aegis `byDependency`, per process | O | Derived from traces (G-01) and Aegis `Dependencies` |
| G-08 | No cross-process telemetry query for operators or agents | 01, 05 | Aegis bundles, per process | O | Query the chosen backend through a typed, read-only, tenant-scoped diagnostic query contract |
| G-09 | No link from fault to authoritative business record | 04 | Context map only | A/D | A documented context key convention, e.g. `record.ref`, classified `Internal`, carrying an opaque id, never business content |
| G-10 | Free text in telemetry is not marked untrusted for agents | 05 | Not addressed | O/D | The diagnostic query contract labels every message, exception text and external response as untrusted data; agents treat it as evidence, never as instructions |
| G-11 | No tenant-scoped access enforcement | 06 | None | O | Enforced at the query contract through Fides claims |
| G-12 | No one-call adoption package | 07 | Aegis setup only | O | One F# package: OpenTelemetry SDK setup, Aegis export bridge, health and redaction defaults |
| G-13 | No browser/WebAssembly instrumentation | 08 | None | O | Limen-hosted instrumentation; validated server-side ingestion; no keystrokes or form contents |
| G-14 | No application-AI telemetry contract | 09 | Praxis covers engineering only | O | OpenTelemetry GenAI semantic conventions, with prompts off by default |
| G-15 | No path from evidence to Vitium | 10 | Vitium intake undeployed | O (after N-02) | Fingerprint-deduplicated, policy-authorized `observation.detected` submission |
| G-16 | Pipeline self-monitoring | 11 | Aegis pattern exists | O | Reuse Aegis's health, drop-count and fallback pattern |
| G-17 | Audit and telemetry separation is not written down as a portfolio rule | 12 | Implicit | D | State it in `spec/echelon-integration-standard.md` when the option is taken |
| G-18 | Aegis serializes `UserMessage`, `TechnicalDetails` and exception messages unredacted to sinks | 05, 06 | Only `export` strips `TechnicalDetails` | A | Aegis decides whether sink payloads apply key-rule redaction to free text, or documents the risk to consumers |

## Gaps that do not depend on the observability system

These were found during discovery. They can be closed now, each by its owning repository.

| Id | Gap | Evidence | Owner action |
|---|---|---|---|
| N-01 | **Closed 2026-11-25:** Aegis 2.0.0 was published to nuget.org (aegis#24, publish run 45531833809) and registered as `releases/aegis/2.0.0.release.json` (WI-0083). Original finding: **Aegis 2.0.0 is in source but unreleased.** The repository has no tags; NuGet and the Registry have only 1.0.0; every consumer is on 1.0.0 | aegis `src/Aegis.Core/Aegis.Core.fsproj` `<Version>2.0.0</Version>`; nuget.org index lists only `1.0.0`; `releases/aegis/1.0.0.release.json` only | Aegis: release 2.0.0 through the normal release workflow, or record why it is held. Registry: register the release when it exists |
| N-02 | **Vitium is not in the Registry** although it is active and Aegis, Praxis, Ordo, Conditor, Dokimos and Tutela are named producers for it | `registry/systems-v2.json` (no `vitium`); vitium `docs/requirements/VITIUM-REQUIREMENTS.md` (VIT-INT-005, VIT-INT-014) | Owner: decide its canonical identity and distribution class; Registry: add it |
| N-03 | **`workitem.query` has no provider.** Praxis declares it in `optionalConsumes` | `examples/praxis.system.json` | Owner: name a provider or drop the declaration |
| N-04 | **Strata is not a runtime database layer.** It ships a CLI only; no library is distributed and nothing depends on it | `spec/ecosystem-distribution-readiness.md` Strata row; usage scan | Recorded so that no plan assumes Strata instrumentation. Database telemetry, when needed, comes from OpenTelemetry's ADO.NET/Npgsql instrumentation, not Strata |
| N-05 | **Active Echelon repositories are missing from the Registry:** Causa, Specula, Vitium, Census, Armamentarium, echelon-knowledge-store, echelon-diagnostic-framework | `registry/systems-v2.json`; repository READMEs | Owner: decide which are canonical systems |
| N-06 | **An empty `observer` repository exists.** It has no commits | `git clone` of `kemiller2002/observer` | Owner: delete it, or reserve it for Option A and record that |
| N-07 | **Consumers lag the current set.** 23 of 30 Ordo consumers pin 1.5.0–1.8.2, and most Praxis consumers pin 3.7.2, against 1.20.0 and 3.25.0 in echelon-current 1.39.0 | usage scan | Each repository: upgrade through Conditor. This matters for any future shared package's adoption speed |

# Options for the observability system

| Option | Description | For | Against |
|---|---|---|---|
| **A** | New Echelon system (name to be decided; not Vigila) that wraps OpenTelemetry: typed F# telemetry contracts, a one-call setup package, an Aegis→OpenTelemetry bridge, a health contract, a diagnostic query contract, and OTLP export to a managed backend | Standards-based; no proprietary telemetry database; Aegis stays focused; one owner | A new repository to maintain; needs a managed backend decision |
| B | Extend Aegis into an observability product | One fewer system | Contradicts Aegis's declared non-goals (`docs/aegis/README.md:337-342`; AEG-CORE-038, AEG-LOG-042 "Minimal core dependencies") |
| C | Each application adopts the OpenTelemetry SDK directly, with no Echelon system | Least code | Every application repeats redaction, cardinality and tenancy rules; no shared AI-diagnostic contract |
| D | Do nothing until a production workload exists | No cost now | Gaps G-01, G-02 and G-18 are cheaper to fix before consumers multiply |

**Recommendation when the option is taken:** Option A, as a thin layer over OpenTelemetry.

**Do now, without building it:**
- the class-A gaps in Aegis (G-02, G-18; G-01's fields are optional), proposed to Aegis's owner;
- the non-observability gaps N-01 to N-07.

**Triggers for taking the option:** the first of the following.
- An Echelon application with real users is deployed with a server component. Unbound, Signal and Summa are candidates.
- Two applications ask for the same telemetry plumbing.
- An incident takes longer to diagnose than the agreed target because evidence is spread across processes.

# Key Discoveries

1. The canonical Vigila is a task and follow-up organizer. Its own requirements prohibit third-party telemetry (vigila `docs/requirements/SECURITY.md`).
2. Aegis already covers most of the *failure* semantics that an observability brief usually asks for: classification, retry safety, redaction, bounded delivery, self-health, escalation and agent guidance.
3. Nothing in the portfolio produces non-failure telemetry.
4. Aegis 2.0.0 exists only in source.

# Evidence Registry

The evidence is the file references cited inline. No `EV-` records were created, because every claim cites a stable path in a public repository and the Registry state at `fef1f94`.

# Hypothesis Registry

| Id | Hypothesis | How to test |
|---|---|---|
| H-1 | A thin OpenTelemetry layer plus an Aegis bridge meets UC-OBS-01 to UC-OBS-08 with no proprietary storage | Prototype against one application (Signal or Causa) with an OTLP collector exporting to a local backend |
| H-2 | Adding trace and tenant fields to Aegis is additive and needs no consumer change | Build the 17 consumers against a candidate package |

# Failed Assumptions

| Assumption (from the original brief) | Actual |
|---|---|
| Vigila owns observability | Vigila provides `followup.*` (Registry) and is a task organizer |
| Strata is the database interaction layer | Strata is a SQL/schema developer CLI with no runtime library and no consumers |
| Vitium is an integrated Echelon system | Vitium is proposed and unregistered, and its intake is undeployed |
| Aegis is at 1.0.0 | Released at 1.0.0; source at 2.0.0 is unreleased |

# Open Questions

1. Which managed telemetry backend is acceptable (AWS CloudWatch/X-Ray via the AWS Distro for OpenTelemetry, Grafana Cloud, Honeycomb or another)? This involves cost and data residency, so it is the owner's decision.
2. Should Aegis carry optional `TraceId`/`SpanId`/`TenantRef`/`CausationId` fields? This is the Aegis owner's decision.
3. What is the name and repository for Option A when taken?
4. What is the license? The portfolio has moved to PolyForm Noncommercial 1.0.0, while the original brief asked for MIT.
5. What do the five private repositories (including Helix Note) need? They were not read.

# Recommended Next Research

- Read the private repositories listed above.
- When a production candidate is chosen, measure real event volumes to replace projections.

# Research Backlog

- G-01, G-02 and G-18: propose to Aegis.
- N-01 to N-07: owner actions, recorded in the Praxis backlog of this repository.

# Suggested Specialized Research Agents

None needed until Option A is taken.

# Parallel Research Opportunities

N-01 to N-07 are independent of each other and of the observability option.
