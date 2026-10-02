# Echelon repository lifecycle contract v1

Status: normative
Capability id: `echelon.repository-lifecycle`
Contract version: `1`
Owner: Echelon Registry
Consumers: Conditor and any other Registry-driven installer

## Purpose

Registry says **what** system, release and artifact is selected. An installer
such as Conditor installs that exact executable and invokes a standard
lifecycle boundary. The component alone owns **how** it changes a repository.

This contract is the boundary between those responsibilities. A system that
conforms to it can be installed, initialized, verified, diagnosed and
upgraded by a Registry-driven installer without that installer containing any
system-specific URL, version, download, file-layout or lifecycle logic.

## Declaration

A release declares conformance in its immutable `echelon.release/v2`
`provides` list:

```json
{ "id": "echelon.repository-lifecycle", "contractVersion": 1 }
```

The declaration is a release fact. It is not inferred from a system name,
from `registry/systems-v2.json` distribution classes, or from the profile role.
Historical releases that do not declare it are not conforming and remain
handled (or refused) by their existing consumer semantics.

A declaring release MUST:

- use distribution class `self-contained-native-cli` (the only class v1 defines);
- name a non-null `executable`;
- publish exactly one `purpose: executable` artifact for every platform it supports.

The Registry resolver copies the declaration into each resolved component as:

```json
"repositoryLifecycle": { "contract": "echelon.repository-lifecycle", "contractVersion": 1 }
```

The field is absent when the selected release does not declare the
capability, so resolved sets for non-declaring releases are byte-identical to
those produced before this contract existed.

## Operations

`<executable>` is the Registry `executable` value of the selected release.
`<root>` is an absolute path to an existing repository directory.

| Operation | Invocation | Mutates | Success meaning |
| --- | --- | --- | --- |
| version | `<executable> version` | no | the executable reports its identity |
| status | `<executable> status --root <root>` | no | the component's repository state is healthy |
| init | `<executable> init --root <root>` | yes | the declared installed state is reached |
| verify | `<executable> verify --root <root>` | no | the installed state is valid |
| doctor | `<executable> doctor --root <root>` | no | no actionable problem was found |
| upgrade | `<executable> upgrade --root <root>` | yes | component-owned state matches the running release |

Consumers MUST NOT pass any other argument. Every option a component accepts
beyond `--root` is optional, and the component derives its defaults —
including the release version and immutable source reference it pins into
repository state — from its own stamped release identity.

## Behaviour

1. **Non-interactive.** Operations never prompt and never read standard input.
2. **Machine-readable.** Standard output carries exactly one JSON document per
   invocation. Diagnostics go to standard error.
3. **Stable exit semantics.**

   | Exit | Meaning |
   | --- | --- |
   | 0 | success / healthy / desired state reached |
   | 2 | invalid invocation (unknown operation, missing or invalid `--root`) |
   | 3 | desired state not satisfied: unhealthy, missing installation, or a refused ownership conflict |
   | any other non-zero | unexpected fault |

   A consumer treats every non-zero exit as failure. `2` and `3` exist so the
   consumer can report the class of failure precisely.
4. **Read-only inspection.** `status`, `verify` and `doctor` never mutate the
   repository.
5. **Idempotency.** Repeating `init` or `upgrade` with the same release on an
   already-satisfied repository changes no file.
6. **Ownership.** The component declares which repository resources it owns.
   - It never overwrites a resource whose ownership it cannot establish; it
     fails closed with exit `3`.
   - User-owned resources (for example policy the user edits after creation)
     are created only when missing and are preserved by `upgrade`.
   - The consumer never reads, writes, or interprets component-owned
     repository state. It knows only the operations above.

## Version identity

`<executable> version` emits a JSON object containing at least the following
properties. Property names are matched case-insensitively.

| Property | Meaning |
| --- | --- |
| `systemId` | canonical Registry system id |
| `repository` | canonical `owner/name` repository |
| `executable` | the executable name |
| `releaseVersion` | the semantic release version, without build metadata |
| `sourceCommit` | the full 40-character source commit the release was built from |

A released artifact MUST stamp `sourceCommit`. A consumer verifies every field
against the resolved release-set component (`systemId`, `repository`,
`executable`, `version`, `commit`) before invoking any repository operation.

## Consumer obligations

A conforming consumer:

- selects a lifecycle component only through a verified resolved release set
  whose component carries `repositoryLifecycle` with a supported version;
- installs exactly the selected platform artifact after verifying its SHA-256;
- verifies the installed executable's version identity before any repository
  operation;
- discloses every lifecycle invocation in an authorized plan whose digest is
  bound to the resolved-set identity and target repository;
- refuses an unsupported `contractVersion`, a missing executable, a missing or
  ambiguous platform artifact, and an inactive release; and
- never branches on a system id to decide lifecycle behaviour.
