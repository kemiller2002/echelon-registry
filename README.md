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
```

Cross-system writes MUST use the receiving system's integration boundary when one exists. Consumers MUST NOT depend on another system's internal storage layout.

## Bootstrap

See `spec/` for normative requirements, `schemas/` for machine-readable contracts, and `examples/` for reference manifests.

## Installation inventory and release metadata

The registry describes what systems exist and what they provide. It never
records where a system is installed: `project-administration` owns that
history through `installation.register`, `installation.query` and
`installation.remove` (see `spec/installation-protocol.md` and
`contracts/installation-*.v1.schema.json`).

Each release of a system may publish an `echelon.release/v1` document
(`schemas/release-manifest.schema.json`) carrying the version, tag,
distribution channels, executable and artifact digests.
`examples/ordo.release.json` uses the published Ordo v1.4.0
`native-checksums.txt` digests.


## Distribution catalog foundation

The distribution catalog is evolving without breaking the original integration contracts.

Current compatibility contracts remain available:

- `echelon.system/v1`
- `echelon.release/v1`

The distribution foundation adds:

- `echelon.system/v2` — canonical product identity, aliases and distribution classes without embedding a release version;
- `echelon.release/v2` — immutable release facts with release stage separated from transport mechanism;
- `echelon.profile/v1` — versioned desired environment composition;
- `echelon.catalog-snapshot/v1` — bounded digest-addressable catalog input;
- `echelon.resolved-release-set/v1` — exact platform-specific release/artifact selection;
- `echelon.readiness/v1` — machine-readable distribution-readiness projection.

`registry/systems-v2.json` is the canonical-identity seed for the currently identified Echelon products. Its `plannedCapabilities` list names portfolio capabilities that have a planned owner but no implementation (canonical time primitives, planned owner Chrona; ledger/invoicing/receivables/payments, planned owner Summa; QDI-079 amended 2026-10-05). Ownership is a commitment, not a capability: their capability ids MUST NOT appear in any system's `provides`, including the planned owner's, until they are implemented.

The first Registry-owned host profile is `profiles/echelon-engineering.profile.json`. Version `0.1.0` pins the real published native releases Praxis `3.6.0` and Ordo `1.4.0`. The checked-in Linux x64 resolved release set is a conformance fixture and the seed for Conditor's Registry-consumption implementation.

## Repository lifecycle contract

`spec/repository-lifecycle-contract.md` defines `echelon.repository-lifecycle`
v1: the standard `version`/`status`/`init`/`verify`/`doctor`/`upgrade
--root <repository>` boundary between a Registry-driven installer and a
component that owns its own repository state. A release opts in by declaring
the capability in `provides`; the resolver copies it into the resolved
component as `repositoryLifecycle`. Releases that do not declare it resolve
exactly as before. `conformance/validate-repository-lifecycle.fsx` proves the
contract with synthetic fixtures.

Run the dependency-free distribution proof with:

```bash
dotnet fsi conformance/validate-distribution-fixtures.fsx
```
