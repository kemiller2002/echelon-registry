# Echelon Provenance Propagation v1

Status: normative. Applies to every Echelon system that sends, relays, or receives an integration invocation.

## 1. Authority

Agent identity and provenance are defined by Praxis, not by this registry. The authoritative contract is:

- Praxis decision `DF-ROS-2026-A037` (Praxis provenance crosses Echelon system boundaries as one versioned interchange contract), building on `DF-ROS-2026-A036`;
- Praxis requirements `RQ-ROS-2026-A013` (foreign execution keys), `A014` (role operations), `A015` (versioned interchange and receiving rules), `A016` (identity propagation), `A017` (no credentials), `A018` (cross-system conformance), and `A019` (provenance is not authority);
- Praxis `docs/agent-provenance.md` (including "Contract revision 1.1") and `docs/echelon-provenance-architecture.md` (the per-system inventory, present from Praxis commit `69738c9` on the same branch), `schemas/provenance-actor.schema.json`, `schemas/provenance-interchange.schema.json`, `lib/provenance-interchange.mjs`, and the fixtures in `tests/fixtures/provenance-interchange/`.

The contract files (schemas, reference library, fixtures, including the 56 conformance cases and `identity-environment.json` of contract revision 1.1) are pinned at Praxis commit `c2657efb4d54f11d0fd0617cc1bcd5b8418601d5` of `kemiller2002/praxis`. The schemas, reference library, and fixtures are vendored unchanged in this repository (`schemas/vendor/praxis/`, `lib/vendor/praxis/`, `tests/fixtures/praxis-provenance/`), each with a `SOURCE.json` that records the commit and SHA-256 of every file.

Terms used below (actor, execution, contribution, operation, lineage, `unknown`, `supported`/`unsupported`/`malformed`) have exactly the Praxis meaning. Where this document and the Praxis contract appear to differ, Praxis wins, and this document is defective.

The key words MUST, MUST NOT, SHOULD, and MAY are normative.

## 2. Requirements

### REG-PROV-001 The registry owns routing and transport, not identity semantics

The registry MUST define only how provenance is routed and transported between systems: the execution envelope, capability manifests, discovery, and idempotency. It MUST NOT define what an agent, actor, execution, contribution, operation, or unknown value means, and it MUST NOT copy and modify a Praxis schema. Registry schemas reference the vendored Praxis schemas by their `$id`.

Traces to: DF-ROS-2026-A037 (decision 1 and "registry-owned identity" rejected), RQ-ROS-2026-A015.

### REG-PROV-002 One identity model

The only actor representation in a new registry contract is the Praxis provenance actor (`{kind, id, provider?, model?, runtime?}`). The `knownValue` actor of `echelon.execution-envelope/v1` is a legacy transport form: it remains accepted (REG-PROV-008) but MUST NOT be extended, reused in new contracts, or treated as a second identity model.

Traces to: DF-ROS-2026-A037 (why: the v1 actor had started to become a second model), RQ-ROS-2026-A015.

### REG-PROV-003 Identity is not authentication or authorization

An envelope actor and every contribution actor are self-reported provenance. A system MUST NOT authenticate, authorize, grant, deny, rate-limit, prioritize, or weight a request or record because of an actor's `kind`, `id`, `provider`, `model`, or `runtime`. Authentication remains a transport concern (Integration Standard section 6), and a provider's result MUST NOT differ because of who the actor claims to be.

Traces to: RQ-ROS-2026-A019.

### REG-PROV-004 Execution envelope v2

Every integration invocation that carries provenance MUST use `echelon.execution-envelope/v2` (`schemas/execution-envelope.v2.schema.json`):

| Field | Required | Meaning |
|---|---|---|
| `schema` | yes | `echelon.execution-envelope/v2` |
| `operationId` | yes | idempotency key (REG-PROV-010) |
| `correlationId` | yes | correlation across a flow |
| `timestamp` | yes | RFC 3339 time of the invocation |
| `actor` | yes | the **current invoking** actor, a Praxis actor |
| `execution` | no | the invoking execution: `EXE-...` (Praxis) or `EXT-<system>.<run-id>` (another Echelon system); absent means unknown |
| `source` | no | repository/branch/commit/work item, unchanged from v1 |
| `provenance` | no | a Praxis interchange block describing the payload being carried |
| `x-...` | no | namespaced extensions |

Any other property MUST be rejected. The actor and execution MUST come from explicit declarations (an upstream envelope, CLI flags, or `ROS_ACTOR_KIND`, `ROS_ACTOR`, `ROS_TELEMETRY_PROVIDER`, `ROS_TELEMETRY_MODEL`, `ROS_TELEMETRY_RUNTIME`, `ROS_EXECUTION_ID`), or from `ros provenance identity --json` when Praxis happens to be present; otherwise the values are the literal `unknown` and `execution` is omitted. A sender MUST NOT guess identity from ambient signals and MUST NOT fabricate an `EXE-` id for a run Praxis did not start.

`echelon.execution-envelope/v1` (`schemas/execution-envelope.schema.json`) is unchanged.

Traces to: RQ-ROS-2026-A013, RQ-ROS-2026-A016, DF-ROS-2026-A037 (decisions 3 and 5).

### REG-PROV-005 Receivers classify every carried block

A receiver MUST classify `envelope.provenance` with the Praxis receiving rules as exactly one of `supported`, `unsupported`, or `malformed`, reaching the verdict pinned for every case in the vendored `cases.json`. A `malformed` block MUST cause the whole request to be rejected at the boundary with a structured error (reference code `provenance-malformed` plus the problems), and nothing may be stored. A receiver MUST NOT drop, truncate, or repair a block to make it acceptable.

Traces to: RQ-ROS-2026-A015.

### REG-PROV-006 The invoking actor is always recorded; a new record gets its own block

Every capability contract declares whether it is a **create** capability (it materialises a new domain record) or an **update** capability (it transports, relays, or changes an existing record). Unless the contract says otherwise, capabilities named `*.create` or `*.record` (for example `followup.create`, `time.record`, `billing.record`, `invoice.create`) are create capabilities and all others are update capabilities. In both kinds the invoking actor's contribution uses:

- key: `envelope.execution` when present, otherwise `EXT-op.<operationId>`, with the operation id escaped injectively as Praxis contract 1.1 defines (every character outside `[A-Za-z0-9.-]`, including `_`, becomes `_xx` per UTF-8 byte, lowercase hex; `op 1` becomes `EXT-op.op_201`);
- `at`: the envelope `timestamp` normalized to UTC.

**Create.** The carried `provenance` describes the **source** of the request (for example an Aegis finding), not the new record. The receiver MUST:

1. give the new record a **new** `praxis.provenance/1` block in which the invoking actor is the only `created` contribution;
2. set that block's `derivedFrom` to the received block's `derivedFrom` (only when the received block is `supported`; another major is never interpreted) followed by a reference to the source record when the payload names one, without duplicates;
3. store the received block verbatim beside the new record as source provenance (reference field `receivedProvenance`, with its verdict), whether it is `supported` or an `unsupported` major, and never append to, merge into, or rewrite it.

The source's contributors are never copied into the new record's block: lineage links the records without merging their authors.

**Update.** The carried `provenance` is the existing record's **own** history (an absent block reads as an empty `praxis.provenance/1` block). The receiver MUST preserve every contribution, operation, evidence item, lineage reference, and unknown field, and MUST append the invoking actor by the Praxis appending rules with `created` only when the block records no contributions, and otherwise `transformed` (a capability contract MAY name a more specific non-authorship operation, but never `created` when the block already records contributions).

The current actor performing a creation, transport, or transformation is therefore always recorded, and never as author of a record that already has an originator: `transformed` is not authorship and never conflicts with the existing `created`. Appending to a key the invoker already holds merges operations (an identical operation is a no-op). The request MUST be rejected with a structured error (reference code `provenance-conflict`), never accepted with the invoker unrecorded, when on update the invoking key is already attributed to a different actor (a contribution is never re-attributed) or the append would leave the history malformed (for example an invocation timestamp earlier than the recorded creation), or the Praxis rules refuse the append for any other reason. In particular an invoker whose identity is `unknown` (id `unknown`, or kind `unknown`) MUST NOT extend an entry held by a known actor, even under the same execution key: `unknown` never contradicts, but it never proves the same run either, so its work would otherwise be attributed to the known actor.

Traces to: RQ-ROS-2026-A015, RQ-ROS-2026-A014, RQ-ROS-2026-A013, DF-ROS-2026-A037 (decisions 2 and 4).

### REG-PROV-007 Unsupported majors are carried verbatim; nothing is stripped silently

A block tagged with another `praxis.provenance/<major>` MUST be stored (on create, as the source provenance beside the new record) or forwarded byte-for-byte equivalent (as JSON) and MUST NOT be interpreted, merged into, or have contributions appended. A receiver or transport MUST NOT silently strip a block of any verdict: it either keeps it, or rejects the request with a structured error. A system that cannot keep provenance at all MUST declare so (REG-PROV-012) and MUST NOT claim preservation.

Traces to: RQ-ROS-2026-A015, DF-ROS-2026-A037 (decision 2).

### REG-PROV-008 Envelope v1 remains accepted with an explicit lossless mapping

Receivers that accept v2 MUST also accept `echelon.execution-envelope/v1`, mapping it exactly as the Praxis reference functions `actorFromEnvelopeV1` and `keyFromEnvelopeV1` do:

| v1 | Praxis |
|---|---|
| `kind: agent`, `human`, `automation` | same kind |
| `kind: system` | `automation` |
| `identity` `known` (non-empty) | `id` = the value |
| `identity` `unknown` or `not-applicable` | `id` = the literal `unknown` |
| non-human `provider` `known` (non-empty) | `provider` = the value |
| non-human `provider` `unknown` or `not-applicable` | `provider` = the literal `unknown` |
| human `provider` (any state) | omitted, as are `model` and `runtime` |
| non-human `model`, `runtime` (not expressible in v1) | `unknown` |
| `runId` `known`, `source.repository` `known` | contribution key `EXT-run.<repository>.<runId>` (see below) |
| `runId` `known`, `source.repository` absent or not `known` | contribution key `EXT-run.<runId>` |
| `runId` absent, `unknown`, or `not-applicable` | contribution key `EXT-op.<operationId>` |

`run` and `op` are reserved pseudo-systems for these keys (REG-PROV-011): `EXT-run.` says only that a run id was declared by an unnamed system, and `EXT-op.` that no execution is known.

**Escaping.** `<runId>` and `<operationId>` use the injective escaping of Praxis contract 1.1 (as `keyFromEnvelopeV1`): every character outside `[A-Za-z0-9.-]`, including `_`, becomes `_xx` per UTF-8 byte in lowercase hex (`gh/99` becomes `gh_2f99`), so two different ids never share a key.

**Run namespace.** A v1 run id is unique only within its sender, so `EXT-run.<runId>` from two senders can collide and merge unrelated runs. When the envelope's `source.repository` is `known`, a receiver MUST therefore key the run as `EXT-run.<repository>.<runId>`, where `<repository>` is escaped like a run id and additionally escapes `.` (as `_2e`), so the first `.` after `EXT-run.` always ends the repository segment (`kemiller2002/aegis` run `7` becomes `EXT-run.kemiller2002_2faegis.7`; `octo/repo.js` run `gh/99` becomes `EXT-run.octo_2frepo_2ejs.gh_2f99`). Otherwise the Praxis reference key is used unchanged. The reference `keyFromEnvelopeV1` is not modified; the namespacing is the registry's routing concern. A namespaced key and an un-namespaced key denote different senders by construction; the un-namespaced form remains only as unique as the sender's run ids.

A system that upgrades a v1 envelope to v2 MUST set `execution` to that run key only when the run id is known and MUST keep the original v1 actor verbatim under `x-envelope-v1-actor`, so `sessionId` and the tri-state values are not lost. A v1 envelope cannot carry `provenance`. Historical v1 records are never rewritten or backfilled.

Traces to: RQ-ROS-2026-A016, RQ-ROS-2026-A013, DF-ROS-2026-A037 (why), Praxis contract revision 1.1 (rule 6).

### REG-PROV-009 Transport never overwrites the original actor

A system that relays a request it did not originate MUST forward the envelope's `actor`, `execution`, `provenance`, and extensions unchanged; it MUST NOT substitute itself as the actor. A system that invokes on its own behalf sets itself as the envelope actor but MUST carry any upstream block verbatim in `provenance`, where the upstream originator and contributors remain; the receiver then records that system as the current actor: as `transformed` when the carried block is the existing record's own history, or as the creator of the new record's own block on a create, where the upstream block is kept as source provenance (REG-PROV-006). A receiver MAY add its own `transformed` contribution to the record's block when it changes the record's representation, as the automation actor `{kind: automation, id: echelon/<system>, provider: echelon, model: unknown, runtime: <system>}` keyed `EXT-<system>.<operationId>`; it MUST NOT attribute that transformation to the invoker or change any other contribution.

Traces to: RQ-ROS-2026-A014, RQ-ROS-2026-A015.

### REG-PROV-010 Replay is idempotent

`operationId` is the idempotency key. Replaying the same operation (the same payload, actor, execution key, and block; a retried `timestamp` or `correlationId` does not make it a new operation) MUST return the existing logical result and MUST NOT create a second record or a second contribution. Appending an identical contribution is a no-op. Reusing an `operationId` for a different request MUST be rejected (reference code `operation-conflict`), never merged.

Traces to: Integration Standard section 4, Follow-Up Protocol, RQ-ROS-2026-A015.

### REG-PROV-011 Registry ids are the `<system>` of foreign execution keys

A registry id MUST match `^[a-z][a-z0-9-]*$` (lower case, no dots) so it can name the system in `EXT-<system>.<run-id>`. The ids `run` and `op` are reserved and MUST NOT be registered.

Traces to: RQ-ROS-2026-A013.

### REG-PROV-012 Manifests may declare provenance support

`echelon.system/v2` (`schemas/system-manifest.v2.schema.json`) adds an optional `provenance` descriptor:

| Field | Meaning |
|---|---|
| `interchange` | Praxis majors the system classifies as `supported`, e.g. `["praxis.provenance/1"]` |
| `envelopes` | optional; envelope versions accepted (absent: only v1 may be assumed) |
| `roles` | any of `producer`, `consumer`, `transport` |
| `propagation` | booleans `actor`, `execution`, `contributions`, `lineage`: what the system keeps |
| `unknownFields` | `preserve` or `discard` |
| `recordsTransformation` | optional; whether it appends its own `transformed` contribution (REG-PROV-009) |

The descriptor declares what the system does today; it MUST NOT be set ahead of the implementation. `echelon.system/v1` is unchanged. The registry index gains the same optional descriptor in `echelon.registry/v2` (`schemas/registry.v2.schema.json`); `echelon.registry/v1` (`schemas/registry.schema.json`, which formalizes the existing `registry/systems.json`) remains valid.

Traces to: RQ-ROS-2026-A018, DF-ROS-2026-A037 (decision 7).

### REG-PROV-013 Undeclared never means preserved

A system without a descriptor (every v1 manifest and v1 index entry) has provenance support `undeclared`. A caller MUST NOT assume an undeclared system preserves provenance. A descriptor with `unknownFields: discard`, or with `propagation.contributions` or `propagation.lineage` false, declares a lossy receiver. A caller that sends provenance to an undeclared or lossy system, or downgrades a v2 envelope to v1 for a provider that accepts only v1, MUST report that provenance may not be preserved (for example in its structured result or diagnostics); it MUST NOT report it as preserved.

Traces to: RQ-ROS-2026-A015, RQ-ROS-2026-A018.

### REG-PROV-014 Optional providers stay optional

Provenance support never changes the registry invariant. A provider that is absent, `unavailable`, `misconfigured`, undeclared, or lossy for provenance MUST NOT cause the caller's core behavior to fail, and a caller MUST NOT refuse to invoke a compatible provider only because it does not declare provenance support. No system may require Praxis to be installed, reachable, or registered to send, relay, or receive provenance.

Traces to: RQ-ROS-2026-A016, RQ-ROS-2026-A018, Integration Standard section 1.

### REG-PROV-015 Conformance against the shared fixtures

The registry's conformance harness MUST test the reference receiver against every case in the vendored `cases.json` (same verdict and warning count) and replay the vendored `echelon-chain.json` through v2 envelopes, and MUST verify the SHA-256 of every vendored file against its `SOURCE.json`. A vendored file MUST NOT be edited locally; it is replaced only by re-vendoring from a Praxis commit.

Traces to: RQ-ROS-2026-A018, DF-ROS-2026-A037 (decision 7).

### REG-PROV-016 Secrets never travel as provenance

Envelopes, manifests, and registry data MUST NOT contain credentials. A receiver MUST reject an envelope whose actor, execution, source, extensions, or v1 actor fields contain a credential-like value (reference code `credential-in-envelope`), and MUST treat a credential-like value anywhere in `provenance` (of any major) as `malformed`. Execution, run, session, and operation identifiers are identifiers, not credentials.

Traces to: RQ-ROS-2026-A017, Integration Standard section 6.

### REG-PROV-017 Integration executables never leak their own identity into another actor's run

An integration executable, hub, worker, or launcher that starts or dispatches a process on behalf of **another** actor MUST first remove every environment variable listed in the vendored Praxis `identity-environment.json` (the `ROS_*` and `ROS_TELEMETRY_*` identity variables, `CLAUDE_CODE_SESSION_ID`, `CODEX_SESSION_ID`, `CODEX_THREAD_ID`, `GEMINI_SESSION_ID`, `COPILOT_SESSION_ID`, `GITHUB_ACTIONS`, `GITHUB_RUN_ID`, `OLLAMA_HOST`), and then set only that actor's explicitly declared values (for example from the envelope's `actor` and `execution`); the Praxis reference is `identityEnvironment`. Otherwise its own identity, session, or run keys leak into the other actor's execution and runs are merged. `ROS_EXECUTION_ID` taken from the environment is honoured only when the process also declares an identity (`ROS_ACTOR_KIND` or `ROS_ACTOR`); a process without a declared identity MUST NOT inherit a run from its environment and records its execution as unknown.

Traces to: RQ-ROS-2026-A016, Praxis contract revision 1.1 (rules 7 and 8).

## 3. Capability contracts

`followup.create` v1 (`contracts/followup-create.v1.schema.json`) is unchanged: provenance travels in the envelope, never in the payload. `followup.create` is a create capability (REG-PROV-006): the carried block describes the source the follow-up came from. The follow-up gets its own new block whose only creator is the invoking actor; the source block is stored verbatim beside it as `receivedProvenance`. The follow-up's `derivedFrom` is the source block's `derivedFrom` plus the source record the payload names in its free-form `context.source` (a namespaced reference such as `aegis:finding/SF-0001`), when present. `followup.update` and `followup.resolve` are update capabilities. The same rule makes `time.record` (Chrona) and `billing.record`, `invoice.create`, and `payment.record` (Summa) create capabilities.

## 4. Reference implementation

`lib/echelon-provenance.mjs` is a dependency-free, pure reference receiver built on the vendored Praxis library: `envelopeProblems`, `invocation`, `upgradeEnvelopeV1`, `relay`, `capabilityKind`, `receive`, `systemActor`, and `provenanceExpectation`. It is conformance tooling, not a runtime dependency: systems implement the rules in their own language and may use it as an oracle.

## 5. Traceability

| Requirement | Implementation | Tests |
|---|---|---|
| REG-PROV-001 | `schemas/execution-envelope.v2.schema.json` (`$ref` to vendored Praxis `$id`s), `schemas/vendor/praxis/` | `tests/schemas.test.mjs` (schemas compile), `tests/vendored.test.mjs` |
| REG-PROV-002 | envelope v2 `actor`; Integration Standard section 5 | `tests/schemas.test.mjs` (v2 requires a Praxis actor) |
| REG-PROV-003 | `lib/echelon-provenance.mjs` `receive` (no actor-dependent branch) | `tests/receiver.test.mjs` (outcome does not depend on the actor) |
| REG-PROV-004 | `schemas/execution-envelope.v2.schema.json`, `envelopeProblems` | `tests/schemas.test.mjs` (extra properties, execution keys), `tests/receiver.test.mjs` (structural problems) |
| REG-PROV-005 | `receive` | `tests/receiver.test.mjs` (malformed fixture cases), `tests/vendored.test.mjs` (56 cases) |
| REG-PROV-006 | `capabilityKind`, `receive` (create and update paths) | `tests/receiver.test.mjs` (capability kind; create: source byte-identical, one creator, lineage from source, replay; update: supported cases, transformed when an originator exists, no-op merge, conflict, unknown invoker cannot extend a known entry, out-of-order rejection, replay) |
| REG-PROV-007 | `receive`, envelope v2 `provenance` if/then/else | `tests/receiver.test.mjs` (unsupported verbatim, absent block), `tests/schemas.test.mjs` |
| REG-PROV-008 | `invocation`, `upgradeEnvelopeV1`, `keyFromEnvelopeV1Namespaced`, `escapeKeySegment`, vendored `actorFromEnvelopeV1`/`keyFromEnvelopeV1` | `tests/receiver.test.mjs` (v1 mapping, upgrade, injective escaping, repository run namespace), `tests/schemas.test.mjs` (v1 examples) |
| REG-PROV-009 | `relay`, `systemActor`, `receive` | `tests/receiver.test.mjs` (never overwritten, relay, transformed) |
| REG-PROV-010 | `receive` | `tests/receiver.test.mjs` (replay, retry, operation conflict, chain replayed twice) |
| REG-PROV-011 | `schemas/system-manifest.v2.schema.json` `systemId` | `tests/schemas.test.mjs` (reserved pseudo-systems) |
| REG-PROV-012 | `schemas/system-manifest.v2.schema.json`, `schemas/registry.schema.json`, `schemas/registry.v2.schema.json`, `examples/*.v2.system.json` | `tests/schemas.test.mjs` (descriptor forms, examples, registry) |
| REG-PROV-013 | `provenanceExpectation` | `tests/receiver.test.mjs` (expectations, undeclared scenarios) |
| REG-PROV-014 | conformance scenarios | `tests/receiver.test.mjs` (conformance rows), `conformance/README.md` |
| REG-PROV-015 | `*/SOURCE.json`, `tests/fixtures/praxis-provenance/` | `tests/vendored.test.mjs`, `tests/receiver.test.mjs` (echelon-chain) |
| REG-PROV-016 | `receive` (credential tripwire from the vendored library) | `tests/receiver.test.mjs` (credentials rejected), vendored `credential-*` cases |
| REG-PROV-017 | vendored `identityEnvironment`, `tests/fixtures/praxis-provenance/identity-environment.json` | `tests/receiver.test.mjs` (identity environment list and stripping) |

Run the harness with `npm ci && npm test` (Node.js 22).
