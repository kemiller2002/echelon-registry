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

## Shared Echelon capability boundaries

Registry resolution states are domain outcomes. A provider being `unavailable`, or a known provider declaration being `misconfigured`, MUST remain explicit registry/integration outcomes and MUST NOT be disguised as Aegis faults.

An implementation that owns a .NET/F# provider-discovery or provider-invocation boundary MUST use Aegis for **unexpected operational failure** such as network failure, filesystem failure, process-launch failure, unreadable external data, or an integration transport failure not already represented by the registry contract.

Aegis MUST NOT change the registry invariant that optional-provider absence is normal and cannot disable unrelated core behavior.

The Registry itself does not require Forma or Folio merely for being a registry specification. If a registry administration UI is implemented, it MUST use Forma. If printable/PDF/paginated registry diagnostics, topology reports, or interoperability evidence are implemented, they MUST use Folio.

Shared capability versions used by implementations MUST be pinned to releases or immutable artifacts, and implementations MUST NOT fork shared Aegis/Forma/Folio capability locally without a recorded gap.
