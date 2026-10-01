# Distribution Catalog and Profile Requirements

Status: draft
Owner: Echelon Registry
Consumers: Conditor, release workflows, Project Administration, Praxis diagnostics

## Purpose

Define the machine-readable contracts needed to discover Echelon systems, immutable releases, compatible artifacts, and versioned environment profiles without turning Registry into an installation-instance database or package manager.

## Boundaries

- Registry describes what exists.
- Conditor decides and performs installation.
- Project Administration records where installations exist.
- Release workflows publish release facts.
- Registry metadata contains no credentials and no host-specific identity.

## Pass 1: catalog foundations

**REG-DIST-001** Registry SHALL define a canonical system identity contract with one stable system id per Echelon system.

**REG-DIST-002** System metadata SHALL include canonical repository, human name, executable identities where applicable, provided capabilities, and compatibility aliases.

**REG-DIST-003** Registry SHALL define immutable release metadata for each published system version.

**REG-DIST-004** Release metadata SHALL include system id, semantic version, immutable tag/release identity, source commit when available, distributions, artifacts, platform identifiers, and SHA-256 digests.

**REG-DIST-005** Release metadata SHALL distinguish release stage/channel (for example stable, preview, nightly) from transport/distribution mechanism (for example GitHub Release, NuGet, npm).

**REG-DIST-006** A release record SHALL NOT be mutable after publication except through an explicitly versioned correction mechanism that preserves the prior record and explains the correction.

**REG-DIST-007** Registry SHALL define a versioned profile contract for named Echelon environment release sets.

**REG-DIST-008** A profile SHALL identify its own id/version and the component constraints or exact versions that form the desired environment.

**REG-DIST-009** Profiles SHALL support composition/extension while resolving to one deterministic flattened release set.

**REG-DIST-010** Profile resolution SHALL never depend on unordered search results or latest-at-runtime semantics when reproducibility is requested.

**REG-DIST-011** Registry SHALL define how optional components and feature-selected components are represented.

**REG-DIST-012** Registry SHALL define compatibility constraints that can express platform support and system-to-system version compatibility without embedding installer implementation logic.

**REG-DIST-013** Registry SHALL provide machine-readable validation schemas for system, release, profile, and resolved-release-set documents.

**REG-DIST-014** Release workflows SHALL be able to validate release metadata against Registry schemas before publication.

**REG-DIST-015** Conditor SHALL be able to consume Registry contracts without requiring access to Registry's source repository layout.

**REG-DIST-016** Registry SHALL support exporting a bounded catalog snapshot suitable for verified offline installation.

**REG-DIST-017** Registry SHALL not store installation instances, hostnames, usernames, absolute local paths, device serials, credentials, or secrets.

## Initial acceptance criteria

Given a system id and profile version, a consumer can determine the canonical system identity, available immutable releases, supported distribution artifacts by platform, and the deterministic desired release set without reading another repository's implementation files.
