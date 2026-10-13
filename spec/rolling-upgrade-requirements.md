# Rolling Upgrade Requirements

Status: draft
Owner: Echelon Registry
Consumers: Conditor, application and library repositories that run `conditor upgrade --current`

The `REG-RU` series covers how the `echelon-current` channel moves so that
consumers can follow it. REG-RU-001 to REG-RU-006 are proposed to the Registry
in Conditor's `docs/requirements/rolling-upgrade-notes.md` and are not yet
accepted here. REG-RU-007 is accepted. The program lead directed it on
2026-10-13 after `echelon-current` 1.21.0 (Ordo 1.6.0) was prepared ahead of a
Conditor release that qualifies Ordo 1.6.0.

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
