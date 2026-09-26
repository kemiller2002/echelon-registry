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
| create with an absent block | PASS | available | new block; invoker recorded once as `created` |
| create with an unsupported-major source block | PASS | available | new block for the record; source stored verbatim, never interpreted |
| update/relay of an existing record whose block has an originator | PASS | available | block preserved; originator unchanged; invoker recorded once as `transformed` |
| update with an unsupported major | PASS | available | block stored verbatim; nothing appended |
| v2 envelope with a malformed block or a credential | PASS | request rejected with a structured error | nothing stored |
| v1 envelope | PASS | available | actor and key mapped by REG-PROV-008 |
| replay of the same `operationId` (create or update) | PASS | available | same record; no duplicate record or contribution |
| relay through a transport | PASS | available | original actor and block unchanged |

The registry's own harness runs these rows against the reference receiver: `npm ci && npm test`.
