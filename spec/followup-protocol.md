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

A caller that cannot resolve `followup.create` MUST continue normal core behavior. It MAY surface that deferred-follow-up capture is unavailable.
