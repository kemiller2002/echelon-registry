# Repository lifecycle contract proof fixtures

Synthetic, clearly non-product fixtures for `spec/repository-lifecycle-contract.md`.
They prove the Registry abstraction without depending on any real system:

- `lifecycle-fixture` declares `echelon.repository-lifecycle` v1, so its
  resolved component carries `repositoryLifecycle`.
- `legacy-fixture` declares nothing, so its resolved component has no
  `repositoryLifecycle` field (non-declaring releases are unchanged).
- `invalid-lifecycle-fixture` declares the capability on a `web-package`
  release; the resolver must refuse it.

Artifact digests are SHA-256 of the literal artifact name; these are not
published bytes and must never be installed.
