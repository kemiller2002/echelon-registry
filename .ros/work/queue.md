# Work Queue

| ID | Work | Status | Tags | Priority |
|---|---|---|---|---|
| ROS-INSTALL-3-7-1 | ROS-INSTALL-3-7-1 | complete |  |  |
| WI-0001 | Govern echelon-registry with Praxis 3.7.1 and Ordo 1.4.0; refresh distribution readiness | complete | praxis, ordo, toolchain | medium |
| WI-0002 | Record Ordo 1.4.1 and select it in echelon-current | complete | ordo, registry | medium |
| WI-0003 | Move echelon-registry to Ordo 1.4.1 | complete | ordo, toolchain | medium |
| WI-0004 | WI-0004 | complete |  |  |
| WI-0005 | Record Praxis 3.7.2 and select it in echelon-current | complete |  | high |
| WI-0006 | Record Visual Engineering 1.0.1 and select it in echelon-current | complete |  | medium |
| WI-0007 | Move echelon-registry to Praxis 3.7.2, Ordo 1.4.2; adopt Conditor | complete | praxis, ordo, toolchain, conditor | medium |
| WI-0008 | Record Strata 0.1.0 and select it in echelon-current; let optional components skip platforms their release does not ship | complete |  | medium |
| WI-0009 | Record Ordo 1.5.0 and select it in echelon-current | complete |  | medium |
| WI-0010 | Move echelon-registry to Ordo 1.5.0 via echelon-current 1.2.0 (conditor upgrade --current) | complete |  | medium |
| WI-0011 | Record Strata 0.1.1 and select it in echelon-current | complete | release, strata | high |
| WI-0012 | Record Arca 0.1.0 (NuGet package family as attested GitHub release assets), register the arca system, select it as an optional project binding in echelon-current 1.4.0, and let the resolver select a NuGet package family (REG-REL-014) | complete | release, arca, channel, resolver | high |
| WI-0013 | Record Fides 0.1.0, register fides, and select it as an optional project binding in echelon-current 1.5.0 | complete | registry, fides | high |
| WI-0014 | Record Arca 0.2.0 and select it in echelon-current 1.6.0 | complete |  | medium |
| WI-0015 | Record Fides 0.2.0 and select it in echelon-current 1.7.0 | complete | registry, fides | high |
| WI-0016 | Record summa-contracts 0.1.0 (EchelonFoundry.Summa.Contracts, the Chrona-to-Summa contract), register summa-contracts, select it as an optional project binding in echelon-current 1.8.0 | complete | registry, summa, chrona | high |
| WI-0017 | Record Arca 0.2.1 (multi-tab offline-queue fix) and select it in echelon-current 1.9.0 | complete | arca, release | high |
| WI-0018 | Record Limen 0.8.0 and limen-fsharp 0.8.0 (the F# store packages), register limen-fsharp, select both in echelon-current 1.10.0 | complete |  | medium |
| WI-0019 | Record Limen 0.9.0 and limen-fsharp 0.9.0 (the routing module and EchelonFoundry.Limen.Routing) and select both in echelon-current 1.11.0 | complete |  | medium |
| WI-0020 | Record Arca 0.3.0 (Core, GitHub and the new Limen bridge package) and select it in echelon-current 1.11.0 | complete | arca, release, channel | high |
| WI-0021 | Record Forma 0.5.0 and select it in echelon-current 1.13.0 | complete | registry, forma | high |
| WI-0022 | Record Praxis 3.9.1 (URL-addressable state foundations, Limen routes schema vendored) and select it in echelon-current 1.14.0 | complete | praxis, echelon-current | high |
| WI-0023 | Record Conditor 0.8.0 (Limen.Routing web scaffold; deep-linking Indy preset) | complete | conditor | high |
| WI-0024 | Record Praxis 3.10.0 (web and hub URL state through Limen.Routing) and select it in echelon-current 1.15.0 | complete | praxis, echelon-current | high |
| WI-0025 | Record Arca 0.4.0 (namespace-scoped change tokens, explicit erasure, stable account ids at sign-out) and select it in echelon-current 1.16.0 | complete |  | high |
| WI-0026 | Record Praxis 3.11.0 and select it in echelon-current 1.17.0 | complete |  | high |
| WI-0027 | Record Conditor 0.8.2 (Praxis 3.10.0 and 3.11.0 qualified for upgrade --current) | complete |  | high |
| WI-0028 | Record Conditor 0.8.1 so the Conditor release history has no gap | complete |  | medium |
| WI-0029 | Record Arca 0.4.1 (revise keeps the entry's account id at sign-out) and select it in echelon-current 1.18.0 | complete |  | high |
| WI-0030 | Record Arca 0.5.0 (batched read-cache save, pinned GitHub reads; arca#38, arca#39) and select it in echelon-current 1.19.0 | complete | arca, release | high |
| WI-0031 | Register Forma 0.6.1 and Folio 0.4.0 and select them in echelon-current 1.20.0 | complete |  | medium |
| WI-0033 | REG-RU-007: never select in echelon-current a version the latest released Conditor refuses for a consumer-declared component; enforce in distribution conformance | complete |  | high |
| WI-0034 | When apps declare forma/folio in conditor.json (Conditor 0.9.0 opt-in), add them to conformance/conditor-declared-components.json in the same cycle (REG-RU-007 pending item) | complete |  | medium |
| WI-0035 | Record Conditor 0.9.0 and correct the 0.8.3 mentions in REG-RU-007 | complete |  | high |
| WI-0036 | Register Ordo 1.6.0 and select it in echelon-current 1.21.0 on current main (supersedes the conflicted WI-0032 branch, PR #66) | complete |  | high |
| WI-0037 | Register Ordo 1.7.0 and select it in echelon-current 1.22.0 | complete |  | high |
| WI-0038 | Record Conditor 0.10.0 | complete |  | high |
| WI-0039 | REG-SIG-001..008: requirements for signed release attestations (DF-CON-2026-A004 decision 2 precondition) | blocked |  | medium |
| WI-0040 | Capture REG-RU-001..006 from Conditor's rolling-upgrade notes (b61c3d9e) as Registry requirements (spec only) | blocked |  | medium |
| WI-0041 | Add a requirements work type to workProtocol.completionEvidence and capture the REG-SIG/REG-RU implementation items | complete |  | medium |
| WI-0042 | PROPOSAL-REQUIRED: implement REG-SIG-001..008 and REG-SIG-CT-001..008 (signing key and protected signing workflow, releaseAttestation in resolved sets, conformance checks). Key custody and workflow design go to the program lead as a proposal before any build. | ready | proposal-required, reg-sig | high |
| WI-0043 | Implement REG-RU-001: library release workflows open the Registry registration PR automatically (needs coordination with each library repository) | ready |  | medium |
| WI-0044 | Implement REG-RU-002: releasedAt and migrationNotes in the release manifest schema (echelon.release/v2 or successor) | ready |  | medium |
| WI-0045 | Implement REG-RU-004: stalenessPolicy in echelon-current resolved sets, in the shape Conditor reads | ready |  | medium |
| WI-0046 | Implement REG-RU-006: consumerContracts claims checked at registration and backfilled on active web-package and NuGet-library releases; depends on the REG-SIG implementation (WI-0042) | ready | depends-on-WI-0042 | medium |
| WI-0047 | Record Conditor 0.10.1 | complete |  | medium |
| WI-0048 | Record Conditor 0.10.2 (workstation host tools from the Registry; downgrade refusal) | complete |  | medium |
| WI-0049 | Record Praxis 3.12.0 | complete |  | medium |
| WI-0050 | Record Conditor 0.10.3 | complete |  | medium |
| WI-0052 | Record Conditor 0.10.4 | complete |  | medium |
| WI-0053 | Cut echelon-current 1.23.0: Praxis 3.11.0 -> 3.12.0 | complete |  | medium |
| WI-0054 | Record Ordo 1.8.2 | complete |  | medium |
| WI-0055 | Record Conditor 0.10.5 | complete |  | medium |
| WI-0056 | Record Forma 0.9.0 and select it in echelon-current | complete | registry, forma, dependency | high |
| WI-0057 | Record Praxis 3.14.0 and Dokimos 0.6.0 | complete |  | medium |
| WI-0058 | Record Conditor 0.10.7 and Praxis 3.15.0 | complete |  | medium |
| WI-0059 | Record Praxis 3.16.0 and Ordo 1.17.0 | complete | release | high |
| WI-0060 | Cut echelon-current 1.25.0: Praxis 3.16.0, Dokimos 0.6.0, Ordo 1.17.0 | complete | release, channel | high |
| WI-0061 | REG-PAIR-001: capability pairing constraint (verified-completion-attested: praxis >=3.17.0 requires dokimos >=0.7.0), register Dokimos 0.7.0, restore Praxis 3.17.0-3.19.0 manifests, record Conditor 0.10.10 | complete | release, registry | high |
| WI-0062 | Cut echelon-current 1.27.0: Dokimos 0.6.0 -> 0.7.0, add percepta-semantic 0.1.0, declare the verified-completion-attested pairing; record the release set and its owner approval (EP-A6a-R1/R2) after REG-RU-007 with released Conditor | complete | release, channel | high |
| WI-0063 | Register and independently verify Percepta semantic 0.2.1 | complete |  | medium |
