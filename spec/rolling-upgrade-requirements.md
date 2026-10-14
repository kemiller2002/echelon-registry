# Rolling Upgrade Requirements

Status: draft
Owner: Echelon Registry
Consumers: Conditor, application and library repositories that run `conditor upgrade --current`

The `REG-RU` series covers how the `echelon-current` channel moves so that
consumers can follow it. REG-RU-001 to REG-RU-006 came from Conditor's
`docs/requirements/rolling-upgrade-notes.md` and were captured here on
2026-10-13; their status notes say what is implemented. REG-RU-007 is
accepted and enforced. The program lead directed it on 2026-10-13 after
`echelon-current` 1.21.0 (Ordo 1.6.0) was prepared ahead of a Conditor
release that qualifies Ordo 1.6.0.

## Registration, release metadata and channel shape

REG-RU-001 to REG-RU-006 are captured from Conditor's
`docs/requirements/rolling-upgrade-notes.md` (conditor `b61c3d9e`), where
Conditor routed them to the Registry. The program lead directed their capture
on 2026-10-13. This section records them only; none is implemented by this
change. The status notes give each one's state on the date of capture.

**REG-RU-001** Each library's release workflow SHALL open a Registry
registration pull request automatically when it publishes a stable release.
The pull request records the release manifest, adds the release to the
catalog snapshot, regenerates the channel sets and index, and bumps the
channel's profile version.

Rationale: the gap between release and registration is what left Forma 0.6.1
(released 2026-10-10) invisible to Chrona, Summa and Signal until
2026-10-13.

Status: not implemented. Registration is a manual pull request today (for
example #65).

**REG-RU-002** `echelon.release/v2` (or a successor) SHOULD carry:

- `releasedAt`, the publication time;
- `migrationNotes`, a URL to the notes for consumers moving to that release.

Conditor already prefers `migrationNotes` when present. Without it, Conditor
links the GitHub release page. Without `releasedAt`, Conditor derives
registration time from Registry Git history, which a shallow clone does not
have.

Status: not implemented. Neither field exists in
`schemas/release-manifest-v2.schema.json`, so adding them is a schema change.

**REG-RU-003** The Registry SHOULD keep one channel bump per registered
release line (no two minor lines of one system in a single bump). It MAY
instead publish a "stepping-stone" resolved set per registered
release line. Either way, every intermediate step that a stepwise upgrade
needs then exists as a Registry-produced set, and Conditor never has to
report a gap or compose a set (`DF-CON-2026-A003` decision 5).

Status: followed by practice, not enforced. For example, Ordo 1.6.0 was
selected in `echelon-current` 1.21.0 and Ordo 1.7.0 in 1.22.0. No
conformance check enforces it.

**REG-RU-004** The Registry MAY declare a `stalenessPolicy` in each channel's
resolved sets, using the shape Conditor reads (Conditor
`docs/drift-advisory.md`). The portfolio's default thresholds are then
Registry data, not Conditor's built-in default.

Status: not implemented. Resolved sets carry no `stalenessPolicy`.

**REG-RU-005** A profile's `version.range` lower bound is Conditor's
"minimum supported" line (CON-318). The Registry SHOULD raise those lower
bounds deliberately, as releases leave support, and record each change in the
profile's version history.

Status: partly followed, not as a support policy. `echelon-current` lower
bounds have been raised when a release was breaking or defective. Arca was
raised in channels 1.6.0, 1.9.0, 1.12.0, 1.16.0 and 1.18.0, and Strata in
the Strata 0.1.1 move. Each change is recorded in the profile's Git history.
No bound has yet been raised because a release left support, and there is no
stated support window.

**REG-RU-006** (`DF-CON-2026-A004` decision 2, accepted) Release manifests of
project-binding systems SHOULD declare the consumer contract they honour, for
example `consumerContracts: ["echelon.web-package-binding/v1"]`. Registry
review SHALL check the claim when it records the release, and SHALL backfill
the claim on active web-package and NuGet-library releases. The claim takes
effect in Conditor only inside a signed release record (REG-SIG-001..008,
where REG-SIG-002 and REG-SIG-003 carry the reviewed claim in the signed
statement; see `spec/release-attestation-requirements.md`).

Status: not implemented. No release record declares `consumerContracts`.
Until the signed release records (REG-SIG-001..008) exist, Conditor keeps
exact per-version qualification (CON-346), and REG-RU-007 continues to apply
unchanged.

## Channel selection and the released Conditor

**REG-RU-007** `echelon-current` SHALL NOT select a version of a
consumer-declared component that the latest released Conditor refuses.

- A *consumer-declared component* is one that consumer repositories list in
  their `conditor.json`. The Registry keeps that list in
  `conformance/conditor-declared-components.json`.
- *Refuses* means `conditor upgrade --current` would refuse the selected
  version. For a component that Conditor's compatibility graph
  (`conditor compatibility --json`) lists, the selected version must be one of
  the listed versions. A NuGet-library component (`distributionClass`
  `nuget-library`) that the graph does not list is accepted at the version the
  set selects. Any other unlisted component is refused.
- *Latest released Conditor* means the binary attached to the newest
  `kemiller2002/conditor` GitHub release, not a branch build or an unreleased
  pull request.

Rationale: every consumer declares Ordo, Praxis and Visual Engineering. If the
channel selects a version Conditor refuses, `conditor upgrade --current`
refuses the whole plan (exit 9) for every consumer. That blocks unrelated
moves, such as Forma, Folio or Arca, until Conditor is released. So the order
is: release a Conditor that qualifies the version first, then select the
version in the channel.

Enforcement: the distribution-conformance workflow downloads the latest
Conditor release for linux-x64. It checks the binary against the release's
`checksums.txt` and its build-provenance attestation from
`.github/workflows/release.yml`. It then runs `conditor compatibility --json`,
and `conformance/validate-conditor-qualification.fsx` checks every platform's
`echelon-current` resolved set against that output. A pull request that moves
the channel early fails this check. The same check can turn `main` red when a
later Conditor release stops qualifying a selected version, which is the
intended signal.

Maintaining the declared list: add a component when any consumer's
`conditor.json` starts declaring it, and remove one only when no consumer
declares it. Components that consumers pin outside Conditor are not on the
list. Today that is Limen, which is pinned through `package.json` and
`.echelon/foundations.json`. For it the channel may move before Conditor
qualifies a version, and the apps adopt by changing their own pins. If the
apps declare Limen in `conditor.json`, add `limen` to the list in the same
cycle.

Forma and Folio joined the list on 2026-10-13. Summa was the first app to
declare them, through Conditor 0.9.0's opt-in (summa#86). Every later Forma
or Folio selection in `echelon-current` must therefore be qualified by the
latest released Conditor first.

## Signed release attestations

Contract-and-line qualification of project bindings, which removes the need
for a Conditor release per library release, depends on the Registry signing
release attestations. Those requirements are REG-SIG-001..008 and the
conformance checks REG-SIG-CT-001..008, in
`spec/release-attestation-requirements.md`.
