# Echelon Registry

Echelon Registry is the discovery and interoperability specification for independently installable Echelon systems.

## Architectural invariant

An Echelon system MUST remain installable, initializable, upgradeable, validatable, and able to perform its core functions when any optional Echelon provider is absent or inaccessible.

Systems integrate through capabilities, not hard dependencies.

## Resolution states

Capability discovery returns exactly one of:

- `available` — a compatible provider was discovered and can be invoked.
- `unavailable` — no provider is installed/discoverable. This is a normal state and MUST NOT fail the caller.
- `misconfigured` — a provider is declared but cannot be used. This produces a diagnostic but MUST NOT disable unrelated core behavior.

## Registry responsibilities

The registry describes system identity, canonical repository, provided capabilities, optionally consumed capabilities, executable discovery, and supported contract versions.

The registry MUST NOT contain credentials, destination-system business logic, or another system's persistence layout.

## Integration boundary

Receiving systems own their integration executables. Callers submit semantic requests; the executable resolves the provider and owns transformation into the receiving system's canonical representation.

Examples:

```text
vigila follow-up add
chrona time record
summa billing record
```

Cross-system writes MUST use the receiving system's integration boundary when one exists. Consumers MUST NOT depend on another system's internal storage layout.

## Bootstrap

See `spec/` for normative requirements, `schemas/` for machine-readable contracts, and `examples/` for reference manifests.
