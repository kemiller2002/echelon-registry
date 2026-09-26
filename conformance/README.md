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

## Provenance scenarios

These apply to components that send or accept execution envelope v2, or that declare a provenance capability. See `spec/echelon-integration-standard.md` §5.1–§5.9 and `spec/followup-protocol.md`.

| Scenario | Core result | Integration result / required observation |
|---|---|---|
| component alone, caller requires provenance | PASS | unavailable |
| v2 provider with provenance capability, caller requires provenance | PASS | available; v2 envelope sent |
| v1-only provider, caller requires provenance | PASS | misconfigured, with a diagnostic; no stripped v1 envelope is sent |
| v1-only provider, caller does not require provenance | PASS | available; v1 envelope sent |
| v2 provider that does not preserve or propagate provenance, caller requires provenance | PASS | misconfigured |
| v2 round trip | PASS | actor, execution, operationId, and `provenance` stored and returned verbatim, including unknown fields and unsupported majors |
| unknown actor | PASS | recorded as `"unknown"`, never filled from Git, prose, or guesses |
| derived record | PASS | provider's own `created` contribution; source in `derivedFrom` and verbatim in `sources`; discovering actor not overwritten |
| malformed provenance | PASS | invocation rejected with a validation result, never accepted without provenance |
| replay of the same operationId | PASS | same result; no second `created` contribution |

## Running the executable checks

This repository is a specification. Its executable checks cover what a specification can prove: every schema, example, registry file, and fixture, plus reference models of the v1↔v2 mapping (`lib/envelope.mjs`) and of provenance-aware resolution (`lib/resolution.mjs`).

```bash
npm ci      # installs ajv and ajv-formats (devDependencies only)
npm test    # node conformance/run.mjs; TAP output, non-zero exit on failure
```

Requires Node.js 22 or later. CI runs the same command (`.github/workflows/conformance.yml`).

The runner checks:

1. Every schema in `schemas/` and `contracts/` compiles (JSON Schema 2020-12, Ajv strict mode except `strictRequired`), and every schema's own `examples` validate.
2. The vendored Praxis files in `schemas/vendor/praxis/` and `conformance/praxis-provenance-record/` match the SHA-256 digests in their `SOURCE.json`. The file sets must be identical.
3. `examples/*.json` validate against the manifest version they declare, and `registry/systems.json` validates against `schemas/registry.schema.json`.
4. `fixtures/execution-envelope/{v1,v2}/{valid,invalid}` and `fixtures/agent-identity/{valid,invalid}` are accepted or rejected as their directory says. The cases include:
   - a v2 envelope with a Praxis actor passes;
   - a v2 envelope with a v1-style actor fails;
   - an agent without `model` fails;
   - a `system` kind in v2 fails;
   - malformed carried provenance fails;
   - unsupported majors and legacy blocks are carried.
5. The vendored Praxis provenance-record cases are checked (`praxis-provenance-record/manifest.json`):
   - every `valid` case, and every end-to-end step, validates against the record schema and can be carried in an envelope v2;
   - `valid-unversioned` validates as artifact provenance, which is read as version 1;
   - `unsupported-version` is not interpreted as major 1, but is carried verbatim;
   - every structurally invalid case is rejected.
6. `fixtures/envelope-mapping/*`: v1 → v2 yields exactly the documented v2, and round-trips where marked. v2 → v1 reports every field it loses.
7. `fixtures/resolution/scenarios.json`: the provenance scenarios above resolve as stated, and core is always PASS.

### Semantic-only Praxis cases

JSON Schema cannot express these invalid cases. Each needs a provenance codec (`RQ-ROS-2026-A013`). The runner asserts that the schema *accepts* each one, so this list cannot drift silently. A system that implements a codec MUST reject them.

| Case | Rule a codec enforces |
|---|---|
| `invalid/agent-without-execution.json` | an agent contribution is keyed `EXE-` (kind-dependent key) |
| `invalid/two-created.json` | at most one `created` |
| `invalid/created-after-modified.json` | nothing precedes `created` |
| `invalid/credential-in-actor.json`, `invalid/credential-in-reason.json` | no credential-shaped values |
| `invalid/source-not-in-lineage.json` | every `sources` key is in `derivedFrom` |
| `invalid/self-lineage.json` | a subject does not derive from itself |

The successor pairs (`successor/*`) also need a codec's successor check (`RQ-ROS-2026-A015`). Here the runner checks only that each side can be carried verbatim in an envelope.

### Refreshing vendored Praxis files

Copy the files from kemiller2002/praxis at the new commit, then regenerate `SOURCE.json` (commit and SHA-256 per file). Never edit a vendored file by hand.
