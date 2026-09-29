# Installation Inventory Protocol v1

Capabilities: `installation.register`, `installation.query`, `installation.remove`
Provider: `project-administration` (executable `administration`)

## Responsibilities

- **Echelon Registry** describes what systems exist and what they provide.
  It never records an installation instance.
- **Project Administration** records what is actually installed where. It
  owns event identity, the append-only history, the current-state
  projection, and its persistence layout.
- **Spokes** (Praxis's `praxis installation register`, Conditor, release
  workflows) supply the semantic payload plus the Echelon execution
  envelope. They never read or write the provider's files.

## Requirements

`operationId` is the idempotency key. Replaying an operation returns the
existing result and records nothing new. Registering an installation that is
identical to the recorded current state (same system, version and artifact
on the same target) records nothing. The same version with a different
artifact digest is refused: released versions are immutable.

History is canonical. An upgrade adds an event and keeps the earlier one; a
removal adds a `removed` event and never deletes history.

Targets are `repository` (stable `owner/repository`) or `environment` (an
explicitly configured logical ID). A caller MUST NOT infer or send a
hostname, username, home directory, full local path, hardware serial or MAC
address. The provider refuses values shaped like them.

An installer MUST NOT register a component whose install receipt did not
match, and MUST NOT report registration for an indeterminate install. It
records removal only after the uninstall receipt matched.

## Optionality

Installation registration is an optional integration. A caller that cannot
resolve `installation.register`, or whose provider is unreachable, MUST
continue normal core behavior and report the diagnosable outcome
`unavailable` or `misconfigured`. A repository or profile MAY make
successful registration required by policy; only then does failure stop the
caller.

## Wire mapping

The provider's request document `echelon.installation.request/v1` carries
the contract payload plus `operationId`, `correlationId` and `actor` from
the execution envelope, and a `capability` discriminator
(`installation.register`, `installation.remove`, and the provider-specific
`installation.verify` / `installation.reconcile`). Results are
`echelon.installation.result/v1` with `status` `recorded`, `replayed`,
`unchanged`, `refused`, `invalid` or `failed`.

## Release metadata

Systems publish `echelon.release/v1` (`schemas/release-manifest.schema.json`)
with each release so an installer can resolve the system id, canonical
repository, version, executable, capabilities and artifact digests without
duplicating them.
