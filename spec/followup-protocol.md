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

Provenance travels in the execution envelope, never in the payload; this payload contract (`contracts/followup-create.v1.schema.json`) is unchanged. With `echelon.execution-envelope/v2`, the provider follows `spec/provenance-propagation.md`: it classifies the carried Praxis block, rejects a malformed one, stores an unsupported major verbatim, and for a supported (or absent) block appends the invoking actor's `created` contribution keyed by `envelope.execution` or `EXT-op.<operationId>` without replacing an existing originator (REG-PROV-005..REG-PROV-010). The follow-up's lineage is the block's `derivedFrom`. v1 envelopes are mapped as REG-PROV-008 defines. A caller whose provider does not declare provenance support still creates the follow-up and reports that provenance may not be preserved (REG-PROV-013, REG-PROV-014).

A caller that cannot resolve `followup.create` MUST continue normal core behavior. It MAY surface that deferred-follow-up capture is unavailable.
