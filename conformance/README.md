# Echelon Integration Conformance

A component is conformant only if its core behavior survives optional integration loss.

Required scenarios:

| Scenario | Core result | Integration result |
|---|---|---|
| component alone | PASS | unavailable |
| component + empty registry | PASS | unavailable |
| component + compatible provider | PASS | available |
| provider not installed | PASS | unavailable |
| provider declared but inaccessible | PASS | misconfigured |
| provider contract incompatible | PASS | misconfigured |
| full supported ecosystem | PASS | available |

A test suite MUST NOT convert `unavailable` into a failing core-health result.

Financial/time integrations MUST additionally prove idempotency: replaying the same operation ID cannot create a second domain record.

## Provenance propagation

Systems that send, relay, or receive provenance (`spec/provenance-propagation.md`) MUST additionally prove the rows below. Core behavior is PASS in every row: provenance support never makes an optional provider required (REG-PROV-014).

| Scenario | Core result | Integration result | Provenance result |
|---|---|---|---|
| provider absent / no registry | PASS | unavailable | not sent |
| provider available, provenance undeclared (v1 manifest or index) | PASS | available | `undeclared`; caller reports it may not be preserved |
| provider declares `praxis.provenance/1`, `unknownFields: preserve` | PASS | available | `preserved` |
| provider declares a lossy descriptor | PASS | available | `lossy`; caller reports it |
| create (`*.create`, `*.record`) with a supported source block | PASS | available | new record gets its own block: invoker is the only creator; lineage = source `derivedFrom` + named source; source block stored byte-identical as `receivedProvenance` |
| create with an absent block | PASS | available | new block; invoker recorded once as `created`; `receivedProvenance` is `{verdict: absent}` |
| create whose payload names a source (`context.source.ref`) | PASS | available | the reference is added to `derivedFrom` through the checked `addLineage` |
| create whose source is malformed, or whose reference is a credential | PASS | request rejected (`payload-invalid` / `lineage-refused`) | nothing stored; lineage never dropped silently |
| create with an unsupported-major source block | PASS | available | new block for the record; source stored verbatim, never interpreted |
| update/relay of an existing record whose block has an originator | PASS | available | block preserved; originator unchanged; invoker recorded once as `transformed` |
| update of an unattributed record (no block, or no contributions) | PASS | available | invoker recorded with its role (`transformed`, `resolved`), never `created`; origin stays unknown |
| update with an unsupported major | PASS | available | block stored verbatim; nothing appended |
| v2 envelope with a malformed block (including `null`) or a credential | PASS | request rejected with a structured error | nothing stored |
| request text with a repeated member name or an unpaired surrogate | PASS | request rejected (`request-malformed`) | nothing stored |
| envelope with a non-strict timestamp, or an id that cannot form a key | PASS | request rejected (`envelope-invalid`) | nothing stored |
| v1 envelope | PASS | available | actor and key mapped by REG-PROV-008; runs namespaced by a known `source.repository` as `EXT-run.<seg(repository)>.<seg(runId)>`, never equal to an un-namespaced key |
| update where an `unknown` invoker reuses a known actor's execution key | PASS | request rejected (`provenance-conflict`) | nothing stored |
| dispatch on behalf of another actor | PASS | available | every inherited identity variable removed before the actor's declared values are set (REG-PROV-017) |
| replay of the same `operationId` (create or update) | PASS | available | same record; no duplicate record or contribution |
| relay through a transport | PASS | available | original actor and block unchanged |

The registry's own harness runs these rows against the reference receiver: `npm ci && npm test`.
