# Echelon Integration Conformance

A component is conformant only if its core behavior survives optional integration loss.

Required scenarios:

| Scenario | Core result | Integration result |
|---|---|---|
| component alone | PASS | unavailable |
| component + empty registry | PASS | unavailable |
| component + compatible provider | PASS | available |
| provider not installed | PASS | unavailable |
| provider declared but inaccessible | PASS | misconfigured |
| provider contract incompatible | PASS | misconfigured |
| full supported ecosystem | PASS | available |

A test suite MUST NOT convert `unavailable` into a failing core-health result.

Financial/time integrations MUST additionally prove idempotency: replaying the same operation ID cannot create a second domain record.


## Distribution contract proof

`validate-distribution-fixtures.fsx` is a dependency-free F# conformance check for the first Registry distribution vertical slice.

It proves that:

- all distribution schema documents are parseable JSON;
- canonical system ids and aliases in `registry/systems-v2.json` do not collide;
- every `unownedCapabilities` entry in `registry/systems-v2.json` has no owner, names a decision and next action, and none of its capability ids is provided by any system;
- the proof profile selects the real Ordo `v1.4.0` release;
- profile, release and catalog snapshot SHA-256 identities match the exact checked-in bytes;
- the resolved release set matches the selected release version, repository, tag, commit, stage and distribution class;
- the selected release is active;
- the profile permits the release stage and target platform;
- every resolved artifact exists in the release manifest with the same digest; and
- the release contains an executable artifact for the resolved platform.

Run it from the repository root:

```bash
dotnet fsi conformance/validate-distribution-fixtures.fsx
```

The proof fixture intentionally uses one real published Ordo release. It is a contract/conformance seed, not the final Echelon engineering or Indy Init profile.
