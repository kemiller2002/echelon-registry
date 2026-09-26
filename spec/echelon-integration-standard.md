# Echelon Integration Standard v1.1

Revision 1.1 replaces the locally defined identity model of section 5 with a reference to the Praxis provenance contract (see `spec/provenance-propagation.md`). All other sections are unchanged from v1, and everything valid under v1 remains valid.

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

The registry does not define agent identity or provenance. Actor, execution, contribution, operation, lineage, and unknown values have the meaning given by Praxis (`RQ-ROS-2026-A013`..`RQ-ROS-2026-A019`, `DF-ROS-2026-A037`); the registry only routes and transports them. The normative rules are in `spec/provenance-propagation.md` (`REG-PROV-001`..`REG-PROV-017`). In summary:

1. Every integration invocation MUST carry an execution envelope. Invocations that carry provenance use `echelon.execution-envelope/v2`, whose `actor` is a Praxis provenance actor (the current invoking actor), whose optional `execution` is the invoking `EXE-...` or `EXT-<system>.<run-id>` key, and whose optional `provenance` is a Praxis interchange block describing the payload (REG-PROV-004).
2. `echelon.execution-envelope/v1` remains accepted and is mapped to Praxis losslessly by the rules of `actorFromEnvelopeV1` and `keyFromEnvelopeV1` (REG-PROV-008). Its tri-state `knownValue` actor is a legacy transport form, not a second identity model (REG-PROV-002).
3. Receivers classify carried provenance as `supported`, `unsupported`, or `malformed`, preserve what they receive, never strip it silently, never overwrite the original actor, and append the invoker's contribution idempotently (REG-PROV-005..REG-PROV-010).
4. Identity values come from explicit declarations only. Unknown values are the literal `unknown`; nothing is fabricated or guessed (REG-PROV-004). An executable acting for another actor clears every inherited identity variable first (REG-PROV-017).
5. Identity is self-reported provenance. It is not authentication or authorization (REG-PROV-003), and it never carries credentials (REG-PROV-016).

The envelope SHOULD include correlation ID, operation ID, actor, execution, source repository, branch, commit, work item, and timestamp.

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

The standalone case is permanently required for conformance. A system that carries provenance MUST additionally pass the provenance scenarios in `conformance/README.md` (REG-PROV-014, REG-PROV-015).

## Shared Echelon capability boundaries

Registry resolution states are domain outcomes. A provider being `unavailable`, or a known provider declaration being `misconfigured`, MUST remain explicit registry/integration outcomes and MUST NOT be disguised as Aegis faults.

An implementation that owns a .NET/F# provider-discovery or provider-invocation boundary MUST use Aegis for **unexpected operational failure** such as network failure, filesystem failure, process-launch failure, unreadable external data, or an integration transport failure not already represented by the registry contract.

Aegis MUST NOT change the registry invariant that optional-provider absence is normal and cannot disable unrelated core behavior.

The Registry itself does not require Forma or Folio merely for being a registry specification. If a registry administration UI is implemented, it MUST use Forma. If printable/PDF/paginated registry diagnostics, topology reports, or interoperability evidence are implemented, they MUST use Folio.

Shared capability versions used by implementations MUST be pinned to releases or immutable artifacts, and implementations MUST NOT fork shared Aegis/Forma/Folio capability locally without a recorded gap.
