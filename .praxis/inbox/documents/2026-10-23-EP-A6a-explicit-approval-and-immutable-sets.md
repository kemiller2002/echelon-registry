# EP-A6a: Record release-set approval explicitly and keep every approved set immutable

Priority: P2
Owner system: Echelon Registry (compatibility authority)
Target repositories: echelon-registry
Intake: Praxis document ingestion (`.praxis/inbox/documents/`). Source material, not a canonical requirement.
Program: kemiller2002/cohort-based-engineering `program/IMPLEMENTATION-PLAN.md` increment A6a; contract
`program/CONTRACTS.md` C6; evidence `program/CURRENT-STATE.md` §2 A6 and §5.

## Finding

- **Approval is implied, not recorded.** "Approved" today means four things together: the release is listed in
  `snapshots/echelon-current.catalog.json`, inside the semver ranges of `profiles/echelon-current.profile.json`, written
  into `channels/echelon-current/*.json`, and qualified by REG-RU-007. No release manifest (`echelon.release/v2`, 65
  files) has an approval field, and no record says who approved a set or on what evidence.
- **Old sets are lost.** Earlier echelon-current profile versions are not kept. Five repositories (registry, percepta,
  forma, limen, aegis) pin `.conditor/authority/resolved-release-set.json` sha256 `ea1b39a0…` (echelon-current 1.2.0),
  whose profile and snapshot exist only as those copies.
- **Unapproved releases are silent, not assessed.** Praxis 3.17.0–3.19.0 are recorded but left out of the snapshot.
  Dokimos 0.7.0 is not selected. Forma 0.9.1, Limen 0.9.1 and Aegis 2.0.0 are not registered at all, because
  REG-RU-001..006 automatic registration is not implemented. A consumer cannot tell "rejected" from "not looked at".
- **Why it matters.** Rehearsal 3's dependency lock (Indy-init `docs/REHEARSAL-DEPENDENCY-LOCK.md` DEP-CURRENT-002)
  requires "the newest approved stable release", and the program's evidence-reuse rule (A6) must never silently
  select a newer, unapproved release. Both need an explicit approval record.

## Requirements (proposed)

- **EP-A6a-R1.** Every echelon-current profile version is kept as an immutable set, for example
  `sets/echelon-current/<version>.json`: components, versions, descriptor digests, platform, and the resolved
  set's digest. A published set is never edited; a change is a new version.
- **EP-A6a-R2.** Each set carries `approval: { by, at, evidence[] }`. `by` names a human with release authority, and
  `evidence[]` points at the REG-RU-007 qualification and compatibility runs for exactly that set. A set without
  approval is `proposed` and is never selected as current.
- **EP-A6a-R3.** A release known to the Registry but not in the current approved set is recorded as
  `assessed: pending | rejected` with a reason. Do this now for Praxis 3.17.0–3.19.0, Dokimos 0.7.0, Forma 0.9.1,
  Limen 0.9.1 and Aegis 2.0.0.
- **EP-A6a-R4.** Restore echelon-current 1.2.0 as an immutable historical set from the pinned copies, verifying sha256
  `ea1b39a0…`, so the five pinning repositories resolve against a record the Registry still holds.
- **EP-A6a-R5.** Resolution (`tools/resolve-profile.fsx`) never selects a release outside an approved set. Add a test
  that a newer `assessed: pending` release is not selected.

## Acceptance evidence

- The schema for sets and approvals, with negative tests: an unapproved set selected; an edited published set; an
  approval without evidence.
- `distribution-conformance.yml` still resolves deterministically twice and fails on any byte difference.
- The R4 restoration matches `ea1b39a0…`.

## Compatibility

Channel files keep their current shape; approval is additive. Conditor's `RegistryAuthorityBinding` keeps working
unchanged; it may later verify the approval record (Conditor CON-INDY-050..054 track that side). No consumer is
moved to a new release by this change.

## Related records (not duplicates)

REG-RU-001..007 (`spec/rolling-upgrade-requirements.md`) cover registration and qualification; this adds approval and
immutability. Conditor CON-INDY-050..054 cover environment receipt reuse.
