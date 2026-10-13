# Release Attestation Requirements

Status: accepted for implementation (the program lead routed these on
2026-10-13)
Owner: Echelon Registry
Consumers: Conditor (`upgrade --current`, `verify`, `doctor`, and
`conditor drift`)
Source: Conditor `docs/requirements/rolling-upgrade-notes.md` REG-SIG-001..008
(as revised in kemiller2002/conditor#88), `DF-CON-2026-A004` decision 2

## Why

Today a Conditor build must name every exact version of a project binding
(Forma, Folio, Limen) before any consumer can move to it. Every library
release therefore needs a Conditor release, and the portfolio drifts. On
2026-10-13 the user accepted *contract-and-line qualification* (option D)
on one condition: Registry release records must be signed first.

Under option D, Conditor qualifies a binding contract and a release line
(for example Forma `>=0.2.0 <1.0.0`). The Registry asserts, per release,
that the release honours that contract.

Conditor's side is implemented and released in Conditor 0.10.0. It is inert:
its pinned key set (`trust/registry-release-keys.json`) is empty, so every
project binding stays under exact qualification. These requirements are what
the Registry must implement before a Conditor release pins a Registry key and
option D takes effect.

Two keys must agree. A release outside a Conditor build's exact
`qualifiedVersions` is upgradable only when:

1. the Conditor build qualifies its line; and
2. the Registry has signed an attestation that it honours the contract.

Unsigned claims never qualify anything, whether for a target or for a
source version.

## Key

**REG-SIG-001** The Registry SHALL hold one dedicated release-attestation
signing key. It SHALL be used only to sign release attestations and the
trust document (REG-SIG-006).

- **Algorithm:** ECDSA over NIST P-256 with SHA-256 (`ecdsa-p256-sha256`).
  Conditor verifies with the .NET base class library and adds no
  cryptography dependency.
- **Private key:** it SHALL NOT exist in this repository, in any pull
  request, in workflow logs, or in an agent session. It lives in either:
  - a GitHub Actions environment secret of a protected environment, for
    example `release-attestation`, with required reviewers; or
  - a KMS or HSM that the signing workflow reaches through OIDC.
- **Public key:** it SHALL be published as base64 (standard alphabet,
  padded) DER SubjectPublicKeyInfo, with a stable `keyId` of the form
  `echelon-registry-YYYY-NN`, in `trust/release-signing-keys.json` (REG-SIG-006).

## What is signed

**REG-SIG-002** The signed payload SHALL be one release attestation
*statement*: the UTF-8 bytes (no BOM, no trailing newline) of exactly this
line:

```text
echelon.release-attestation/v1|<systemId>|<version>|<releaseManifestSha256>|<contracts>
```

| Field | Exact form |
|---|---|
| `systemId` | the Registry system id of the release, as in its release manifest `systemId` |
| `version` | the release manifest `version`, byte for byte |
| `releaseManifestSha256` | lowercase hex SHA-256 of the exact release manifest bytes that the catalog snapshot records (64 characters) |
| `contracts` | the claimed consumer contract ids, sorted by ordinal (byte) order and joined with `,` with no spaces; at least one; an id SHALL NOT contain `\|`, `,` or whitespace |

The only contract defined today is `echelon.web-package-binding/v1`. It is
the contract of Conditor's `docs/web-package-upgrade-contract.md`: an npm
`package.json` pin of an exact version or a GitHub release-asset tarball,
locked by npm, whose package artifact the Registry digests.

The signature SHALL be the ECDSA P-256/SHA-256 signature of those bytes, in
IEEE P1363 fixed-field form (`r || s`, 64 bytes), encoded as base64
(standard alphabet, padded). A DER-encoded signature SHALL NOT be used.

Known-answer example. For Forma 0.6.1 with release manifest digest `M`, the
statement is:

```text
echelon.release-attestation/v1|forma|0.6.1|M|echelon.web-package-binding/v1
```

The statement binds the release manifest digest. Signing therefore binds the
manifest's bytes without changing them, so existing release manifests,
catalog snapshots, and resolved-set digests stay valid.

**REG-SIG-002a** Each attestation SHALL be stored detached at
`releases/<systemId>/<version>.attestation.json`
(`echelon.release-attestation/v1`), with these fields:

- `systemId`;
- `version`;
- `releaseManifestSha256`;
- `consumerContracts`, sorted;
- `keyId`;
- `algorithm`;
- `signature`;
- `signedAt`;
- `workflowRun`, the URL of the run that signed it.

The catalog snapshot row of that release SHALL bind the file with
`attestation: { "path": ..., "sha256": ... }`, so the channel digest chain
covers it.

## Who signs

**REG-SIG-003** Only the Registry's protected attestation workflow SHALL
sign.

- It SHALL run on the default branch, after the release registration pull
  request has been reviewed and merged.
- It SHALL use the protected environment of REG-SIG-001.
- It SHALL commit the attestation through its own pull request, which passes
  the conformance checks of this document before merge.
- Agents, pull-request workflows, and forks SHALL NOT have access to the
  private key.
- The workflow SHALL support a backfill mode that signs every active
  project-binding release in `echelon-current` that has no attestation yet.
- The workflow SHALL refuse to sign a release whose manifest bytes do not
  match the snapshot digest.
- The workflow SHALL refuse to sign a contract claim that has not been
  reviewed. The claim comes from the release's registration pull request,
  for example a `consumerContracts` field reviewed together with the
  manifest.

## Where Conditor reads it

**REG-SIG-004** The resolver (`tools/resolve-profile.fsx`) SHALL copy each
selected release's attestation into its resolved-set component entry, next
to `releaseManifest`:

```json
"releaseAttestation": {
  "schema": "echelon.release-attestation/v1",
  "consumerContracts": [ "echelon.web-package-binding/v1" ],
  "keyId": "echelon-registry-2026-01",
  "algorithm": "ecdsa-p256-sha256",
  "signature": "<base64 IEEE P1363 r||s>"
}
```

The copy SHALL be byte-faithful in value. Conditor rebuilds the statement
from the entry's own `systemId`, `version` and `releaseManifest.sha256`,
together with `consumerContracts`. A release without an attestation SHALL
have no `releaseAttestation` member. The member SHALL NOT be `null` or
empty.

## How Conditor verifies (normative for Registry output)

**REG-SIG-005** Registry output SHALL be such that Conditor 0.10.0's
verifier accepts it. A contract claim qualifies only when **all** of the
following hold:

1. `schema` is `echelon.release-attestation/v1`, and `algorithm` is
   `ecdsa-p256-sha256`.
2. `keyId` is in the key set pinned in that Conditor build, and the key has
   no `revokedAt`.
3. The signature verifies over the statement rebuilt from the entry.
4. The claimed contracts include the contract Conditor qualifies.
5. The version lies in the descriptor's `qualifiedRange`, and is not in
   `deniedVersions`.

Every other outcome leaves the release under exact qualification, and the
plan states the reason. A source version qualifies by attestation only
through the repository's digest-verified recorded authority, which is the
set the repository was last applied from, and only when that set selects
exactly that version.

## Rotation

**REG-SIG-006** `trust/release-signing-keys.json`
(`echelon.registry-keys/v1`) SHALL list every key the Registry has used. Each
entry carries:

- `keyId`;
- `algorithm`;
- `publicKey`;
- `notBefore`;
- `introducedBy`, the key id that signed its introduction, or `null` for the
  first key;
- an optional `revokedAt` with `revocationReason`.

The document SHALL be signed (a detached `trust/release-signing-keys.sig.json`)
by the current key. A new key SHALL be:

1. added to the document, which the current key signs;
2. pinned by a Conditor release (Conditor `trust/registry-release-keys.json`,
   `conditor.registry-release-keys/v1`);
3. used to sign attestations only after that Conditor release exists.

Until step 2, attestations under the new key qualify nothing. Releases
signed by the previous key remain valid until that key is revoked.
Overlapping validity SHALL be recorded through `notBefore` and the
introduction chain.

## Revocation

**REG-SIG-007** A revoked key SHALL keep its entry, with `revokedAt` and
`revocationReason` set. It is never deleted.

On suspected compromise, the following SHALL happen:

1. The Registry publishes the revocation.
2. The Registry asks Conditor for a release that marks the key revoked in
   its pinned set. Until that release exists, consumers keep trusting the
   key, so it SHALL be treated as urgent.
3. The Registry re-signs, under a valid key, every active release that only
   the revoked key attests.

Releases attested only by a revoked key fall back to exact qualification.

## Audit

**REG-SIG-008** Every attestation SHALL be traceable to the workflow run that
produced it, through the `workflowRun` field and a GitHub artifact
attestation of the attestation file. No file in the repository SHALL contain
private key material.

## Conformance tests the Registry SHALL add

These checks run in `.github/workflows/distribution-conformance.yml`, as
`conformance/validate-release-attestations.fsx` with fixtures under
`examples/release-attestation/`. They SHALL be dependency-free F# and use
the BCL, like the other conformance scripts.

**REG-SIG-CT-001 (known answer).** A committed **test-only** key pair
(`examples/release-attestation/test-key.*`) is permitted only under
`examples/` and is never trusted by any channel. The test signs fixed
statements, and its rebuilt statement and signature SHALL verify byte for
byte. One vector SHALL match the Forma example above with a fixed `M`.
Conditor's test suite can then reuse the same vectors.

**REG-SIG-CT-002 (channel coverage).** For every platform set of
`echelon-current`, every `project-binding` entry of class `web-package` that
has an attestation SHALL carry a `releaseAttestation` that verifies under a
key in `trust/release-signing-keys.json` that is not revoked. The rebuilt
statement SHALL use the entry's own `systemId`, `version` and
`releaseManifest.sha256`. Once backfill is complete, the check SHALL also
require that every active web-package release in the channel has an
attestation.

**REG-SIG-CT-003 (digest chain).** Every `releases/*/*.attestation.json` file
SHALL:

- be bound by its catalog snapshot row (path and SHA-256);
- name the same `systemId` and `version` as its release manifest;
- carry `releaseManifestSha256` equal to the snapshot's manifest digest.

**REG-SIG-CT-004 (resolver fidelity).** Re-resolving every channel set SHALL
reproduce each `releaseAttestation` value-for-value from the attestation
files. The existing byte-for-byte regeneration check covers the set files.

**REG-SIG-CT-005 (negative fixtures).** Each of these fixtures SHALL fail
verification:

- the version changed;
- the manifest digest changed;
- the contract list unsorted or changed;
- the key id unknown;
- the key revoked;
- a DER-encoded signature;
- `algorithm` other than `ecdsa-p256-sha256`;
- a `null` or empty `releaseAttestation`.

**REG-SIG-CT-006 (trust document).** The following SHALL hold:

- `trust/release-signing-keys.json` parses;
- key ids are unique;
- every `introducedBy` resolves to an earlier key;
- its detached signature verifies under its current key;
- revoked keys carry `revokedAt` and `revocationReason`.

**REG-SIG-CT-007 (no private key).** The check SHALL fail if any tracked file
outside `examples/release-attestation/` contains a PEM `PRIVATE KEY` block,
or a JWK `"d"` member alongside `"kty": "EC"`.

**REG-SIG-CT-008 (activation order, like REG-RU-007).** An attestation key
SHALL NOT be relied on for a channel selection until it is pinned. The
check SHALL download the latest released Conditor, verified against its
`checksums.txt` and build-provenance attestation, as REG-RU-007 already
does, and read its pinned key set from `conditor components --json` or an
equivalent read-only output. A channel selection of a consumer-declared
component (`conformance/conditor-declared-components.json`) SHALL then be
either:

- exactly qualified by that Conditor; or
- attested under a key that Conditor pins and has not revoked.

Until Conditor exposes its pinned key set in a read-only command, this check
SHALL treat the pinned set as empty, so REG-RU-007's exact rule applies
unchanged.

## Relationship to other series

- **REG-RU-006** (Conditor notes): `consumerContracts` in release manifests
  is realised by the reviewed claim of REG-SIG-003 and the signed statement
  of REG-SIG-002. The claim takes effect only inside a verified attestation.
- **REG-RU-007** stays in force. Before a Conditor release pins a Registry
  key, exact qualification by the latest released Conditor remains the only
  path, for project bindings as well.
