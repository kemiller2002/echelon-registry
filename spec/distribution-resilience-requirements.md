# Distribution Catalog Requirements - Pass 2: Publishing, Trust, Compatibility, and Offline Use

Status: draft
Owner: Echelon Registry

## Release publication contract

**REG-DIST-100** Every independently distributable Echelon system SHALL publish release metadata conforming to the Registry release contract.

**REG-DIST-101** A native executable release SHALL publish platform-specific immutable assets and digests for every platform it claims to support.

**REG-DIST-102** Release metadata SHALL identify the exact source commit when the publishing system can provide it.

**REG-DIST-103** Release workflows SHALL fail before publication when generated release metadata does not validate.

**REG-DIST-104** Release workflows SHALL verify that every artifact named by release metadata actually exists and matches its declared digest.

**REG-DIST-105** Release workflows SHALL smoke-test a published or staged executable sufficiently to prove that its version identity matches the release being published.

**REG-DIST-106** Release metadata SHALL support provenance/attestation references without making a specific hosting provider mandatory.

**REG-DIST-107** A version/tag already published SHALL be immutable. Re-running a release SHALL not replace assets with different bytes.

## Compatibility model

**REG-DIST-110** Registry SHALL define a compatibility representation that can express:
- supported runtime/platform identifiers;
- minimum/maximum compatible versions of other Echelon systems;
- required capabilities and contract versions;
- incompatible combinations;
- deprecation and supersession relationships.

**REG-DIST-111** Compatibility declarations SHALL describe semantic compatibility, not installer implementation steps.

**REG-DIST-112** Registry SHALL validate that profile constraints can resolve to at least one coherent release set for each platform the profile claims to support.

**REG-DIST-113** Registry SHALL detect cycles or contradictions in required system relationships during validation.

**REG-DIST-114** Compatibility aliases SHALL map to one canonical system id and SHALL NOT create independent release histories.

## Release stage/channel

**REG-DIST-120** Registry SHALL model release stage/channel separately from distribution transport.

**REG-DIST-121** The initial release stages SHALL support at least stable, preview, and nightly semantics.

**REG-DIST-122** A release record SHALL state its stage explicitly; absence SHALL NOT be interpreted as stable.

**REG-DIST-123** A profile SHALL declare which release stages are permitted for each component or for the profile as a whole.

## Resolved release sets

**REG-DIST-130** Registry SHALL define a resolved-release-set document that records the exact immutable releases selected for a profile version and platform.

**REG-DIST-131** A resolved release set SHALL include the profile id/version, resolver/schema version, platform, every selected system version, artifact identity/digest, and the compatibility facts necessary to audit the resolution.

**REG-DIST-132** Resolved release sets SHALL be content-addressable by digest.

**REG-DIST-133** A resolved release set used for a reproducible event SHALL remain retrievable or exportable as historical evidence even after newer releases appear.

## Offline catalog snapshots

**REG-DIST-140** Registry SHALL define a portable catalog snapshot format containing the bounded system/release/profile metadata needed to resolve or verify a selected profile offline.

**REG-DIST-141** A catalog snapshot SHALL declare its schema version and digest identity.

**REG-DIST-142** Offline snapshots SHALL contain no secrets, credentials, installation-instance data, or host identity.

**REG-DIST-143** A consumer SHALL be able to validate snapshot structure and internal references without network access.

## Naming and migration

**REG-DIST-150** Registry SHALL make system renames explicit through canonical id plus aliases/supersession rather than by creating unrelated identities.

**REG-DIST-151** Registry SHALL provide validation that detects two active system definitions claiming the same executable or compatibility alias unless an explicit coexistence rule permits it.

**REG-DIST-152** Historical release metadata SHALL retain the canonical identity valid for that release while current discovery can map recognized historical aliases to the current canonical system.

## Pass 2 acceptance criteria

A conforming release workflow can publish one immutable release document, profiles can resolve against compatibility and release-stage policy, consumers can audit the exact selected set, and the metadata required to install or verify that set can be exported for offline use.


## Release lifecycle, SBOM, and platform trust

**REG-DIST-160** Registry SHALL model release lifecycle state separately from release stage/channel. At minimum it SHALL distinguish active, deprecated, withdrawn, and security-revoked releases.

**REG-DIST-161** Withdrawal or revocation SHALL NOT delete or mutate the historical release identity, artifact digests, or provenance record. The state change itself SHALL be durable and attributable.

**REG-DIST-162** Normal stable resolution SHALL exclude withdrawn and security-revoked releases even when they otherwise satisfy semantic-version constraints.

**REG-DIST-163** A deprecated release MAY remain resolvable according to profile policy, but consumers SHALL be able to report the deprecation reason and successor when one is declared.

**REG-DIST-164** Registry release metadata SHALL support references/digests for an SBOM or equivalent dependency inventory appropriate to the distribution class.

**REG-DIST-165** Registry release metadata SHALL support third-party-license/notice evidence where the released artifact contains distributable third-party material.

**REG-DIST-166** Native release metadata SHALL support platform code-signing/notarization evidence independently from SHA-256 integrity and CI provenance.

**REG-DIST-167** Registry SHALL define a stable publication/discovery surface for catalog snapshots, profiles, and resolved release sets so consumers do not depend on Registry repository source layout.

**REG-DIST-168** Published catalog/profile snapshots SHALL be versioned and digest-bound, and the publication process SHALL preserve prior snapshots needed for reproducibility.

**REG-DIST-169** The reusable Echelon release workflow/template SHALL itself be versioned. Every generated release document SHALL identify the release-contract/workflow version that produced or validated it.

**REG-DIST-170** Registry validation SHALL fail when a stable-profile-eligible native release claims required signing, provenance, SBOM, or license evidence but the referenced evidence is missing or digest-mismatched.
