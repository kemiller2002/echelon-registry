# Echelon Integration Standard v1

## 1. Independence

1. Every Echelon system MUST define its core behavior independently of optional integrations.
2. Missing optional providers MUST resolve to `unavailable`, not an error.
3. A declared but unusable provider MUST resolve to `misconfigured` with diagnostics.
4. Neither `unavailable` nor `misconfigured` may prevent unrelated core behavior.
5. Installing a new provider SHOULD make its capabilities discoverable without reinstalling consumers.

## 2. Capability model

Systems declare `provides` and `optionalConsumes`. Consumers resolve semantic capability identifiers rather than hard-coded repository names.

A provider owns the executable implementing its capabilities and the transformation into its own storage representation.

## 3. Discovery

Resolution order is:

1. repository-local configuration, when explicitly supplied;
2. GitHub owner/organization registry;
3. optional explicitly configured upstream registry.

Absence of a registry is valid for a standalone component.

## 4. Executables

Integration executables MUST:
- accept semantic domain input;
- identify their integration version;
- resolve destination ownership through registry data rather than caller hard-coding;
- validate input before writing;
- own destination formatting and location;
- support operation/correlation IDs;
- return structured results;
- be idempotent for mutating operations where duplicate execution could create duplicate domain records;
- operate without cloning the destination repository when the selected transport supports remote mutation.

Consumers MUST NOT manipulate another system's internal persistence representation when a provider executable exists.

## 5. Identity and provenance

Every integration invocation MUST carry an execution envelope. Actor identity MUST distinguish known, unknown, and not-applicable values. Agents MUST provide a stable identifying mechanism when available and MUST NOT fabricate unavailable identity fields.

The envelope SHOULD include correlation ID, operation ID, actor kind/provider/identity, run/session identifiers, source repository, branch, commit, work item, and timestamp.

### 5.1 Envelope versions

Execution envelope v2 adds agent provenance without replacing v1. Nothing in §5.1–§5.9 makes provenance mandatory for any system.

1. `echelon.execution-envelope/v1` (`schemas/execution-envelope.schema.json`) is frozen. It MUST NOT be changed, and a v1 envelope that was valid remains valid.
2. `echelon.execution-envelope/v2` (`schemas/execution-envelope.v2.schema.json`) is defined side by side with v1. A provider declares which versions it accepts (§5.8). A caller SHOULD send v2 to a provider that accepts it.
3. A provider that accepts v2 SHOULD continue to accept v1 while any caller may still send it.
4. Envelope v2 adopts the Praxis provenance contract. It MUST NOT redefine it. The normative sources are:
   - requirements `RQ-ROS-2026-A013`, `RQ-ROS-2026-A014`, and `RQ-ROS-2026-A015`, and decision `DF-ROS-2026-A037`, in kemiller2002/praxis;
   - [`provenance-actor.schema.json`](https://github.com/kemiller2002/praxis/blob/main/schemas/provenance-actor.schema.json), [`artifact-provenance.schema.json`](https://github.com/kemiller2002/praxis/blob/main/schemas/artifact-provenance.schema.json), and [`provenance-record.schema.json`](https://github.com/kemiller2002/praxis/blob/main/schemas/provenance-record.schema.json).
5. This repository vendors those schemas unchanged in `schemas/vendor/praxis/`. `SOURCE.json` there records the Praxis commit and each file's SHA-256. When a vendored file and Praxis disagree, Praxis is authoritative and the vendored copy MUST be refreshed, not edited.

### 5.2 Actor

1. The v2 `actor` IS the Praxis provenance actor: `kind`, `id`, `provider`, `model`, and `runtime`, with exactly the Praxis meaning of each. Envelope v2 defines no other actor model.
2. The `actor` MUST identify the actor performing the current invocation (the **current actor**).
3. A value that applies but is not known MUST be the literal `"unknown"`. A value that does not apply (for a human: `provider`, `model`, and `runtime`) MUST be omitted. An implementation MUST NOT fabricate, infer, or guess a value. In particular, it MUST NOT derive one from Git authorship, free-text fields, or any environment variable outside the discovery that Praxis specifies: the whitelisted non-secret identity variables and the known agent runtimes (`RQ-ROS-2026-A014`).
4. An `agent` actor MUST carry `provider`, `model`, and `runtime`.
5. A receiver MUST preserve actor fields it does not model.
6. Runtime session, conversation, and run identifiers MAY be carried in `runIdentifiers`, following the same unknown/omitted convention. They are not part of the actor's stable identity.

### 5.3 Execution

1. `execution` identifies the run performing the invocation.
2. When `ROS_EXECUTION_ID` is present in the caller's environment, as exported by `ros provenance identity --env`, the caller MUST use that value.
3. Otherwise, a system acting within its own run MUST use its own namespaced key `EXE-<system>.<run>`, as `RQ-ROS-2026-A014` specifies.
4. A caller MUST NOT mint a Praxis-shaped `EXE-<timestamp>-<random>` identifier.
5. A caller MUST NOT name an execution it is not running.
6. `execution` MAY be absent, which means the run is not known. Examples: a human acting outside any run, or a v1 envelope mapped to v2. An absent execution MUST NOT be filled in by a receiver.
7. A receiver that records a contribution for an agent actor MUST key it by an `EXE-` execution. With no execution, it MUST NOT record an agent contribution (`RQ-ROS-2026-A014`).

### 5.4 Provenance record

1. `provenance` is OPTIONAL. When present, it is the provenance of the subject the invocation acts upon, for example the finding a follow-up is raised for. It is one of:
   - a `praxis.provenance-record` of major version 1, which MUST validate against the Praxis record schema;
   - a record of another major version, carried verbatim;
   - a legacy unversioned `{"contributions": …}` block, read as version 1.
2. Every system that forwards, stores, or returns the envelope MUST carry `provenance` verbatim. This includes unknown fields and unsupported major versions. A system MUST NOT strip, re-key, or rewrite any part of it (`RQ-ROS-2026-A015`).
3. A receiver MUST reject an envelope whose `provenance` is malformed. It MUST NOT silently drop the provenance and proceed. A rejection is a structured validation result for that invocation. It is not a provider failure.
4. A receiver MUST NOT append the current actor to the carried record. A receiver that produces a new subject records its own contribution in a new, derived record (`RQ-ROS-2026-A015`; for follow-ups, see `spec/followup-protocol.md`).

### 5.5 Current actor and original actor

1. The current actor (`actor`, `execution`) and the actors inside `provenance` are distinct. The original actor is the one that holds the `created` contribution of the carried record or of its `sources`.
2. A receiver MUST NOT replace, merge, or re-attribute either one with the other. In particular, the actor that transports or acts upon a subject MUST NOT overwrite the actor that discovered or created it.

### 5.6 Identity is not authentication, authorization, or evidence

1. Actor, execution, and provenance are self-reported provenance. A receiver MUST NOT use them:
   - to authenticate a caller;
   - to authorize an operation;
   - as evidence, or as a weight on evidence (`RQ-ROS-2026-A015`).
2. Authentication remains a transport concern (§6).
3. No envelope field, including `actor`, `execution`, `runIdentifiers`, `source`, `provenance`, and `x-` extensions, may carry a secret, token, key, or credential. A receiver MUST reject an envelope in which it detects a credential-shaped value. JSON Schema cannot detect every such value, so this is enforced by implementations.

### 5.7 Mapping between v1 and v2

1. **v1 → v2 is total and non-fabricating.** A receiver that accepts both versions MUST interpret a v1 envelope as the v2 envelope produced by this mapping:

   | v1 | v2 |
   |---|---|
   | `schema`, `operationId`, `correlationId`, `timestamp` | unchanged, with `schema` set to `…/v2` |
   | `actor.kind` `agent`/`human`/`automation` | the same kind |
   | `actor.kind` `system` | `automation` |
   | `actor.identity` known | `actor.id` = the value |
   | `actor.identity` unknown, not-applicable, or known without a value | `actor.id` = `"unknown"` |
   | `actor.provider` known, non-human | `actor.provider` = the value |
   | `actor.provider` unknown, not-applicable, or known without a value, non-human | `actor.provider` = `"unknown"` |
   | `actor.provider` known, human | `actor.provider` = the value |
   | `actor.provider` unknown or not-applicable, human | omitted |
   | (no v1 field) `model`, `runtime`, non-human | `"unknown"` |
   | `actor.sessionId`, `actor.runId` | `runIdentifiers.sessionId`, `runIdentifiers.runId` |
   | `source.*` | `source.*` |
   | (no v1 field) `execution`, `provenance` | omitted |

   For `runIdentifiers` and `source`, a known value is carried unchanged. Unknown, or known without a value, becomes `"unknown"`. Not-applicable is omitted.

   Every known v1 value survives unchanged, and no value that v1 did not state is invented. Two v1 distinctions have no Praxis representation, and the mapping collapses them:
   - `system` becomes `automation`;
   - not-applicable, unknown, and "known without a value" become `"unknown"` wherever Praxis says the value applies.

   A v1 envelope that uses neither distinction round-trips exactly (v1 → v2 → v1). `conformance/lib/envelope.mjs` is the executable reference, and `conformance/fixtures/envelope-mapping/` holds the expected results.
2. **v2 → v1 is lossy.** v1 cannot represent:
   - `actor.model` or `actor.runtime`;
   - `actor.kind` `unknown` or `x-…`;
   - unknown actor fields;
   - `execution`;
   - `runIdentifiers.conversationId`;
   - `provenance`;
   - `x-` extensions.

   An implementation that down-converts MUST report every field it could not carry.
3. A caller MUST NOT down-convert a v2 envelope carrying provenance, execution, or a known model or runtime for a provider that accepts only v1, when the caller requires provenance. That provider MUST resolve as `misconfigured` for that caller (§5.8). The caller's core behavior continues (§1).
4. A caller that does not require provenance MAY send such a provider a v1 envelope.

### 5.8 Provenance capability declaration and resolution

1. A system declares provenance support in an `echelon.system/v2` manifest (`schemas/system-manifest.v2.schema.json`), or in its registry entry (`schemas/registry.schema.json`). Declarations use these fields:
   - `envelopes.accepts`: the envelope versions its executable accepts;
   - `provenance`: the provenance capability descriptor. It names `contract` `praxis.provenance-record`, with `versions.accepts` and `versions.emits` as major versions. `preserves` is true when received records are stored and returned verbatim. `propagates.actor`, `propagates.execution`, and `propagates.lineage` are true when the system carries each onward.

   Every one of these fields is OPTIONAL.
2. A v1 manifest, and a registry entry without these fields, declares envelope v1 only and no provenance capability. `echelon.system/v1` is frozen and remains valid.
3. A system MUST NOT declare `preserves: true` unless it satisfies §5.4 and `RQ-ROS-2026-A015`. It MUST NOT declare a propagation it does not perform.
4. A consumer MAY state that it requires provenance from a consumed capability. It does so with `{"id": "<capability>", "requiresProvenance": true}` in `optionalConsumes`, or with equivalent runtime policy. A provider resolves as `misconfigured` for such a consumer, with a diagnostic naming the gap, when any of these holds:
   - it does not accept envelope v2;
   - it declares no provenance capability;
   - it does not accept `praxis.provenance-record` major 1;
   - it does not preserve provenance;
   - it does not propagate the actor, the execution, or lineage.
5. Absence of the provider still resolves as `unavailable`. Neither outcome may affect the consumer's core behavior or core health (§1, §7).
6. The registry describes provenance capability. It does not own identity. Identity semantics belong to the Praxis contract (`DF-ROS-2026-A037`).
7. No system gains a dependency on Praxis by declaring or consuming provenance. The contract is a set of JSON shapes.

### 5.9 `agent.identity`

1. The `agent.identity` capability returns the result described by `contracts/agent-identity.v1.schema.json`: the output of `ros provenance identity --json`. Its assurance is always `self-reported`.
2. A caller MAY use the result to populate the v2 `actor` and `execution`. It MUST NOT treat the result as authentication.

## 6. Authentication

Registry data MUST NOT contain secrets. Authentication is a transport concern. Implementations MAY resolve credentials from GitHub Actions installation tokens, existing authenticated GitHub tooling, fine-grained tokens, or future credential providers.

## 7. Health

Core health and integration health MUST be reported separately. A healthy standalone installation remains healthy when optional providers are not installed.

## 8. Conformance

At minimum every consumer MUST be tested:
- standalone;
- with registry but no optional providers;
- with each supported provider;
- with provider absent;
- with declared provider inaccessible;
- with incompatible provider contract;
- with the full supported ecosystem.

The standalone case is permanently required for conformance.

A consumer that requires provenance (§5.8.4) MUST additionally be tested with a v1-only provider. That provider MUST resolve as `misconfigured` while core behavior still passes. See `conformance/README.md`.

## Shared Echelon capability boundaries

Registry resolution states are domain outcomes. A provider being `unavailable`, or a known provider declaration being `misconfigured`, MUST remain explicit registry/integration outcomes and MUST NOT be disguised as Aegis faults.

An implementation that owns a .NET/F# provider-discovery or provider-invocation boundary MUST use Aegis for **unexpected operational failure** such as network failure, filesystem failure, process-launch failure, unreadable external data, or an integration transport failure not already represented by the registry contract.

Aegis MUST NOT change the registry invariant that optional-provider absence is normal and cannot disable unrelated core behavior.

The Registry itself does not require Forma or Folio merely for being a registry specification. If a registry administration UI is implemented, it MUST use Forma. If printable/PDF/paginated registry diagnostics, topology reports, or interoperability evidence are implemented, they MUST use Folio.

Shared capability versions used by implementations MUST be pinned to releases or immutable artifacts, and implementations MUST NOT fork shared Aegis/Forma/Folio capability locally without a recorded gap.
