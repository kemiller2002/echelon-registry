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
