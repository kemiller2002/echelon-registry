# REG-PAIR-001: Record cross-component pairing constraints; register Dokimos 0.7.0 and missing Praxis manifests

Priority: P1
Owner system: Echelon Registry
Target repositories: echelon-registry
Intake: Praxis document ingestion. Source material, not a canonical requirement.
Related: `2026-10-23-EP-A6a-explicit-approval-and-immutable-sets.md` covers approval and immutable sets. This input
covers **pairing**, which EP-A6a does not.

## Evidence

- Profile echelon-current **1.26.0** (`3cb34b9`) selects Praxis 3.20.0. Its catalog
  (`snapshots/echelon-current.catalog.json`) lists only Dokimos 0.2.0 and 0.6.0.
- Rehearsal 3 qualification (kemiller2002/indy-init-rehearsal-003
  `research/rehearsal-003-engineering-hardening.md` §7, OBSERVED):
  - attested verified completion needs Praxis ≥ 3.17 **and** Dokimos ≥ 0.7.0;
  - Praxis 3.20.0 with Dokimos 0.6.0 refuses with `VERIFIER_ATTESTATION_UNSUPPORTED`;
  - "Praxis 3.20.0 and Dokimos 0.7.0 must move as a pair";
  - "the 3.17.0–3.19.0 manifests are missing, though a record says they exist".
- Consequence: the approved set cannot run the owner-approved OD-9 verified-completion pilot
  (cohort-based-engineering `program/EXECUTION-STATE.md`).

## Requirements (proposed)

- **REG-PAIR-001-R1.** A profile can declare a pairing constraint between components for a named capability, for
  example `verified-completion-attested: praxis >=3.17.0 requires dokimos >=0.7.0`. Registry validation fails when a
  snapshot selects versions that enable the capability's host side without its paired side, unless the snapshot
  records the capability as disabled.
- **REG-PAIR-001-R2.** Register Dokimos 0.7.0 with its release manifest and digests, so that qualification can assess
  it. Registration is not approval.
- **REG-PAIR-001-R3.** Restore or correct the Praxis 3.17.0–3.19.0 manifest records, so that no record names a
  manifest that does not exist.

## Acceptance criteria

- Validation fails on a fixture snapshot with Praxis 3.20.0, Dokimos 0.6.0 and verified completion enabled. It passes
  with the capability recorded as disabled.
- Dokimos 0.7.0 appears in the catalog with matching digests.
- A consistency check fails when any record references a missing manifest.

## Not decided here

Whether 1.26.0 is amended or a 1.27.0 is approved is an owner decision (A6a).
