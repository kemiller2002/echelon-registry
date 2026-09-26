# 0001. Envelope v2 adopts the Praxis actor; manifests v2 declare provenance capability

- Status: accepted
- Date: 2026-09-26
- Related: kemiller2002/praxis `DF-ROS-2026-A037`, `RQ-ROS-2026-A013`–`A015`; `spec/echelon-integration-standard.md` §5.1–§5.9

## Context

The v1 execution envelope defined its own actor:
- kinds `agent`, `human`, `automation`, and `system`;
- `identity` instead of `id`;
- `{state, value}` objects;
- no model or runtime;
- no execution;
- no place to carry lineage.

That actor is a second identity model, incompatible with the Praxis actor that other Echelon systems are adopting. Manifests had no way to state whether a provider keeps provenance, so a caller could not tell whether a provider would strip it.

## Decision

1. Add `echelon.execution-envelope/v2` side by side with the frozen v1. Its `actor` is the Praxis provenance actor. It adds `execution` and an optional verbatim `praxis.provenance-record`. The Praxis schemas are vendored unchanged, with commit and digests, and referenced by `$id`. Their semantics are not copied.
2. Specify v1 → v2 as a total, non-fabricating mapping, with two documented collapses:
   - `system` becomes `automation`;
   - not-applicable becomes `"unknown"` where the Praxis actor says a value applies.

   Specify v2 → v1 as lossy.
3. Add `echelon.system/v2`, with optional `envelopes.accepts` and a `provenance` capability descriptor (contract, accepted and emitted majors, `preserves`, `propagates`). A consumer may mark a consumed capability `requiresProvenance`. A provider that cannot meet that requirement resolves as `misconfigured` for that consumer, rather than receiving a stripped v1 envelope.
4. Add a schema for `echelon.registry/v1` entries, and an `agent.identity` v1 contract that describes `ros provenance identity --json`.

## Consequences

- Nothing becomes mandatory. v1 envelopes and v1 manifests remain valid, and systems stay independently installable with no dependency on Praxis.
- Vendored Praxis files must be refreshed, never edited, when Praxis changes. The conformance runner verifies their digests.
- Semantic provenance rules, such as a single `created` or credential detection, are not expressible in JSON Schema. They remain a codec obligation of each implementing system.
