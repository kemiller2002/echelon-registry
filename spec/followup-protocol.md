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

These rules apply when the provider declares a provenance capability (`spec/echelon-integration-standard.md` §5.8) or receives an execution envelope v2. They apply the Praxis contract (`RQ-ROS-2026-A013`–`A015`, `DF-ROS-2026-A037`) and do not redefine it. A provider without a provenance capability remains a valid v1 provider. It resolves as `misconfigured` only for callers that require provenance (§5.8.4).

1. **Store the invocation verbatim.** The provider MUST store the following with the follow-up, verbatim, including unknown fields:
   - the envelope's `actor`: the actor that invoked `followup.create`;
   - `execution`, when present;
   - `operationId`;
   - any `provenance` record.

   A later invocation MUST NOT replace them.
2. **Derive; do not append.** The follow-up is a new subject. The provider MUST create a new `praxis.provenance-record` for it:
   - `subject`: the provider's own namespaced reference, for example `vigila:item/ITEM-42`;
   - `contributions`: exactly one `created` contribution by the provider system acting as `automation`, keyed by the provider's own run `EXE-<system>.<run>`, or by the propagated `ROS_EXECUTION_ID` when the provider runs inside that execution (§5.3);
   - `derivedFrom`: the envelope record's `subject` when it has one, or else the caller-supplied source reference;
   - `sources`: the envelope's `provenance` record, verbatim, keyed by that same reference.

   When no source reference is known, `derivedFrom` and `sources` are omitted, because `sources` keys must appear in `derivedFrom`. The record stored under item 1 still keeps the source's provenance. The provider's actor `id` is its stable system identity, for example `echelon/vigila`, and the provider MUST NOT use the invoking actor as its own.

   The Praxis conformance fixture `e2e/06-vigila-followup.json` shows this shape.
3. **Keep the discovering actor.** The provider MUST NOT copy the source's contributors into the follow-up's `contributions`. It MUST NOT overwrite, re-key, or drop the discovering actor, meaning the actor recorded in the carried record, or the invoking `actor` when no record was carried. The invoking actor and the provider's own `created` contribution are distinct facts (§5.5).
4. **Later actions append.** An update, handling, resolution, or validation of the follow-up appends a contribution by the acting actor, keyed by that actor's execution. It uses the interchange operations `x-handled`, `x-resolved`, `x-validated`, or `x-dismissed`, or `modified`. It MUST NOT alter an existing contribution.
5. **Return provenance.** The structured result of `followup.create`, including an idempotent replay, MUST include:
   - the follow-up's provenance record;
   - the stored invoking `actor` and `execution`, returned unchanged.

   A replay MUST NOT add a second `created` contribution.
6. **Reject rather than strip.** The provider MUST reject malformed provenance, or a credential-shaped value, with a structured validation result. It MUST NOT drop the provenance and create the follow-up without it. A record of an unsupported major version is stored and returned verbatim, and is not interpreted.
7. **No authorization.** The recorded actors MUST NOT be used to authorize the operation (§5.6).

A caller that cannot resolve `followup.create` MUST continue normal core behavior. It MAY surface that deferred-follow-up capture is unavailable.
