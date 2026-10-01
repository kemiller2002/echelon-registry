# Ecosystem Release Workflow Requirements - Pass 3: Make Every System Installable

Status: draft
Owner: Echelon Registry
Applies to: every Echelon repository that publishes a distributable executable, library, application package, or profile-consumable artifact.

## Standard release workflow

**REG-REL-001** Every distributable Echelon repository SHALL have an automated release workflow that emits Registry-compatible release metadata.

**REG-REL-002** Release metadata generation SHALL derive version, tag, repository, commit, artifact names, platform identities, and digests from the actual release build rather than duplicated hand-maintained literals where feasible.

**REG-REL-003** The workflow SHALL validate the generated release document against the Registry schema before attaching/publishing it.

**REG-REL-004** The workflow SHALL generate cryptographic digests from the exact bytes uploaded as release artifacts.

**REG-REL-005** The workflow SHALL refuse to reuse an existing immutable version/tag with different artifact bytes.

**REG-REL-006** Native executable repositories SHALL smoke-test each build architecture that can execute in available CI and SHALL at minimum verify archive structure and declared executable identity for cross-built targets.

**REG-REL-007** The workflow SHALL publish provenance/attestation evidence when the hosting/build platform supports it.

**REG-REL-008** Release publication SHALL fail when a required Registry metadata field cannot be established truthfully.

## Distribution classes

**REG-REL-010** Registry SHALL define distribution classes at least for:
- self-contained native CLI;
- NuGet library/tool;
- npm/browser package where still intentionally supported;
- contract/data bundle;
- application artifact.

**REG-REL-011** A release SHALL declare its distribution class so Conditor can select installation semantics without repository-specific guessing.

**REG-REL-012** A project-bound library SHALL NOT advertise itself as a workstation-global executable merely because its repository also contains build tools.

**REG-REL-013** A release MAY expose multiple distribution mechanisms, but each artifact SHALL have one explicit purpose/platform identity.

## Ecosystem readiness matrix

**REG-REL-020** Registry SHALL maintain or generate a machine-readable readiness projection for Echelon systems.

**REG-REL-021** For each system, the projection SHALL identify:
- canonical system id;
- canonical repository;
- active aliases/renames;
- license identifier;
- distribution class;
- current stable release;
- release-manifest compliance;
- supported platforms;
- self-contained status where applicable;
- compatibility metadata status;
- Conditor stable-profile eligibility;
- blocking gaps.

**REG-REL-022** A system SHALL NOT be marked stable-profile eligible merely because a repository exists or a build succeeds.

**REG-REL-023** Stable-profile eligibility SHALL require immutable release identity, verified artifact digests, validated release metadata, and enough compatibility/install semantics for Conditor to act without bespoke guesses.

## Profile publication

**REG-REL-030** Versioned environment profiles SHALL be publishable independently from Conditor binaries.

**REG-REL-031** A profile publication workflow SHALL validate that all required systems resolve on every platform the profile claims to support.

**REG-REL-032** Publishing a reproducible profile SHALL also publish or retain its resolved release sets by supported platform.

**REG-REL-033** The Indy Init competition profile SHALL retain the exact resolved set used at the event even after the normal stable profile advances.

**REG-REL-034** An all-Echelon profile SHALL distinguish workstation components from project-bound libraries and SHALL not force global installation semantics onto project libraries.

## Cross-repository rollout

**REG-REL-040** The ecosystem SHALL provide a reusable release-workflow template or equivalent shared contract so repositories do not independently reimplement release metadata rules.

**REG-REL-041** Adopting the shared release contract SHALL require repository-specific inputs only where facts genuinely differ, such as executable name, distribution class, or supported targets.

**REG-REL-042** Shared release automation SHALL be versioned so a repository can pin the release contract it uses.

**REG-REL-043** A release workflow upgrade SHALL not silently change the meaning of existing published metadata schemas.

**REG-REL-044** Release workflows SHALL register or publish the resulting release metadata to the Registry discovery surface through an idempotent operation.

## Licensing and public distribution

**REG-REL-050** Every public Echelon artifact intended for unrestricted installation SHALL declare its license identifier in Registry metadata.

**REG-REL-051** Stable profile publication SHALL fail or mark the system ineligible when the required distributable artifact lacks the intended license declaration.

## Final ecosystem acceptance criterion

A newly created Echelon system can adopt the standard release contract, publish one immutable release, become discoverable through Registry, and be added to a Conditor profile without adding system-specific download/version literals to Conditor source.
