# Capability pairing constraints (REG-PAIR-001)

Status: normative. Source: intake `.praxis/inbox/documents/2026-10-24-REG-PAIR-001-capability-pairing-constraints.md`.

Some capabilities need two components at compatible versions. Praxis 3.17.0 and later issue attested completion
claims, and only Dokimos 0.7.0 and later can verify them (`completion.verify-attested`). Praxis 3.20.0 with Dokimos
0.6.0 refuses with `VERIFIER_ATTESTATION_UNSUPPORTED` (kemiller2002/indy-init-rehearsal-003
`research/rehearsal-003-engineering-hardening.md` §7, OBSERVED). Before this requirement the Registry had no way to
say so, and echelon-current 1.26.0 selected Praxis 3.20.0 with Dokimos 0.6.0.

## REG-PAIR-001-R1: declared pairings

- A profile declares pairings in `capabilityPairings` (`schemas/profile.schema.json`). Each pairing has an `id`, a
  `host` side and a `requires` side. Each side names a profile component (`systemId`) and a semantic version `range`.
  A host selection inside `host.range` enables the capability. The paired selection must then be inside
  `requires.range`.
- A catalog snapshot can record a paired capability as disabled in `disabledCapabilities`
  (`schemas/catalog-snapshot.schema.json`), with a reason. A snapshot must not disable a capability the profile does
  not declare.
- `tools/resolve-profile.fsx` evaluates every pairing on every platform after it selects releases:
  - **enabled:** both selections satisfy their ranges;
  - **inactive:** the host selection does not enable the capability, or the host is not in this platform's set;
  - **disabled:** the snapshot disables the capability;
  - otherwise the host side is enabled without its paired side. Resolution **fails** with `REG-PAIR-001` and writes
    no set.
- A resolved set of a profile with pairings carries `capabilities` (`schemas/resolved-release-set.schema.json`): one
  entry per pairing, with its state, both ranges and both selected versions. A set of a profile without pairings has
  no `capabilities` property, so those sets are unchanged byte for byte.
- `conformance/validate-capability-pairing.fsx` proves the rule with the fixtures in `examples/capability-pairing/`,
  which use the real Registry records:

  | Fixture snapshot | Selection | Expected |
  |---|---|---|
  | `violation.catalog.json` | Praxis 3.20.0, Dokimos 0.6.0 | refused |
  | `disabled.catalog.json` | Praxis 3.20.0, Dokimos 0.6.0, capability disabled | `disabled` |
  | `paired.catalog.json` | Praxis 3.20.0, Dokimos 0.7.0 | `enabled` |
  | `inactive.catalog.json` | Praxis 3.16.0, Dokimos 0.6.0 | `inactive` |
  | `undeclared-disabled.catalog.json` | disables a capability the profile does not pair | refused |

  It also checks that each echelon-current channel set carries exactly one state per pairing that the profile
  declares.

The pairing is directional. Dokimos 0.7.0 with a Praxis older than 3.17.0 is harmless: nothing issues attested
claims. Pairing does not replace Conditor qualification (REG-RU-007). It states a constraint between two
components, and REG-RU-007 states what one Conditor release accepts.

## REG-PAIR-001-R2: Dokimos 0.7.0

`releases/dokimos/0.7.0.release.json` is the release's own `dokimos.release.json` (`dokimos-v0.7.0`, commit
`04b5f856`), as the 0.6.0 record is. Every artifact digest was recomputed from the downloaded release assets and
matches `dokimos-checksums.txt`. Registration is not approval. A set selects 0.7.0 only through a new profile version.

## REG-PAIR-001-R3: no record names a missing manifest

The readiness record said Praxis 3.17.0-3.19.0 were recorded, but no manifest existed. The releases are real, stable
and published (`v3.17.0` `aea29b4a`, `v3.18.0` `c66bb740`, `v3.19.0` `986211c1`). So the manifests are restored
from the published releases, not removed from the record. A release record is the Registry's immutable statement of
what was published, and EP-A6a-R3 needs every known release recorded before it can be assessed. Each manifest is the
release's own `echelon-release.json`, converted to `echelon.release/v2` in the same way as `3.20.0.release.json`.
The same conversion reproduces `3.20.0.release.json` byte for byte. Each archive was downloaded and checked against
`native-checksums.txt`. The checksum file's own digest is recorded. The three releases are in no snapshot, so no
profile can select them.

`conformance/validate-manifest-references.fsx` fails when:

- any checked-in JSON, Markdown, F# script or workflow names a `releases/<system>/<version>.release.json` that does
  not exist;
- any catalog snapshot row names a missing manifest, or one with a different digest, system or version;
- any manifest under `releases/` records a system or version other than its path.

It first proves that it fails on a synthetic tree with dangling references.
