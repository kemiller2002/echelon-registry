# Vigila Follow-Up Protocol v1

Capability: `followup.create`

The capability exists for work that requires later human attention but does not justify stopping otherwise legal agent work.

## Disposition

- Work the agent can safely resolve itself remains agent work.
- Engineering work belongs in the applicable work-item system.
- Non-blocking human review, decision, approval, information, or investigation may become a Vigila follow-up.
- Blocking human work may become both a Vigila follow-up and a blocked source work item.

## Requirements

The caller supplies the semantic follow-up payload plus the Echelon execution envelope. Vigila owns identifiers, persistence layout, serialization, and status representation.

`operationId` is the idempotency key. Replaying the same operation MUST return the existing logical result and MUST NOT create a duplicate follow-up.

The provider MUST preserve provenance sufficient to trace the follow-up back to the invoking actor and source when those values are known.

## Provenance

Provenance travels in the execution envelope, never in the payload; this payload contract (`contracts/followup-create.v1.schema.json`) is unchanged. With `echelon.execution-envelope/v2`, the provider follows `spec/provenance-propagation.md`. `followup.create` creates a new record, so the carried Praxis block describes the follow-up's **source** (for example an Aegis finding): the provider classifies it and rejects a malformed one; otherwise it gives the follow-up a new `praxis.provenance/1` block whose only creator is the invoking actor, keyed by `envelope.execution` or `EXT-op.<operationId>`, and stores the received block verbatim beside it as `receivedProvenance` (supported or an unsupported major), never appending to or merging it (REG-PROV-005..REG-PROV-010). The follow-up's `derivedFrom` is the source block's `derivedFrom` plus the source record named in the payload's `context.source` (for example `aegis:finding/SF-0001`), when present. `followup.update` and `followup.resolve` carry the follow-up's own block and append the invoking actor as `transformed` (or a more specific non-authorship role), never `created` once the follow-up has an originator. v1 envelopes are mapped as REG-PROV-008 defines. A caller whose provider does not declare provenance support still creates the follow-up and reports that provenance may not be preserved (REG-PROV-013, REG-PROV-014).

A caller that cannot resolve `followup.create` MUST continue normal core behavior. It MAY surface that deferred-follow-up capture is unavailable.
