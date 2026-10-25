# REG-PERCEPTA-SEMCLI: register the Percepta semantic CLI and cut the complete kickoff set

Priority: P0 for Indy-init Rehearsal 4 (precondition P1)
Owner system: Echelon Registry
Work item: WI-0063 (blocked on the upstream release)
Branch: `claude/percepta-semantic-registration` (based on the 1.27.0 cut, echelon-registry#94)
Intake: Praxis document ingestion. Source material, not a canonical requirement.

## Why

- Indy-init OD-INDY-2 (`docs/OWNER-DECISIONS-2026-10-24.md`, owner: "Release it"): UI completion needs evidence from
  the **released** Percepta semantic CLI, pinned by the kickoff release set.
- OD-INDY-8: the kickoff set is the newest stable, compatible, verified release of each system, approved in the
  Registry after Conditor qualification.
- echelon-current 1.27.0 (`sets/echelon-current/1.27.0.json`) records a VER-003 exception: `percepta-semantic-v0.1.0`
  (kemiller2002/percepta#39) was still unpublished on 2026-10-25T04:30Z.

## Already on the branch

- `registry/systems-v2.json`:
  - `percepta-semantic`: display name "Percepta Semantic CLI", `kemiller2002/percepta`, executable alias `percepta`,
    class `self-contained-native-cli`;
  - `percepta`: keeps only the executable alias `percepta-repo`.

  This follows Percepta `DF-PERCEPTA-2026-0003` decision 5.
- `tools/native-release-record.fsx` builds an `echelon.release/v2` record from the downloaded assets. Every digest is
  computed and must match the release's `checksums.txt`.

## Apply, once `GET /repos/kemiller2002/percepta/releases/tags/percepta-semantic-v0.1.0` exists

1. Download every asset into an empty directory. Confirm the tag commit with
   `GET /repos/kemiller2002/percepta/commits/percepta-semantic-v0.1.0`.
2. Check the asset names against `docs/PERCEPTA-SEMANTIC-CLI.md`: `percepta-semantic-0.1.0-<rid>.tar.gz` for
   linux-x64, linux-arm64, osx-x64 and osx-arm64, `…-win-x64.zip`, and `checksums.txt`. Then run:

   ```bash
   dotnet fsi tools/native-release-record.fsx -- --assets DIR --system percepta-semantic --version 0.1.0 \
     --repository kemiller2002/percepta --tag percepta-semantic-v0.1.0 --commit <tag commit> \
     --executable percepta --checksums checksums.txt \
     --artifact linux-x64=percepta-semantic-0.1.0-linux-x64.tar.gz \
     --artifact linux-arm64=percepta-semantic-0.1.0-linux-arm64.tar.gz \
     --artifact osx-x64=percepta-semantic-0.1.0-osx-x64.tar.gz \
     --artifact osx-arm64=percepta-semantic-0.1.0-osx-arm64.tar.gz \
     --artifact win-x64=percepta-semantic-0.1.0-win-x64.zip \
     --output "releases/percepta-semantic/$version.release.json"   # version=0.1.0
   ```

3. Check that each archive has a single top-level directory containing `percepta` (`percepta.exe` on Windows) and
   `.playwright/`. Conditor's workstation flattens a single top-level directory and expects the executable at its
   root (Conditor DF-CON-2026-A007).
4. Edit the profile and snapshot:
   - Profile 1.28.0 adds `{ "systemId": "percepta-semantic", "role": "host-tool", "required": true, "version":
     { "range": ">=0.1.0 <1.0.0" } }`. It is `required`, because the kickoff manifest requires Percepta and the release
     ships for all five platforms.
   - The snapshot gets a `percepta-semantic` system row and release row, and the 1.28.0 profile digest.
5. Regenerate the five resolved sets and `channel.json` (README "Moving the echelon-current channel"). Then run
   `tools/generate-release-set.fsx` for `sets/echelon-current/1.28.0.json`, and update
   `spec/ecosystem-distribution-readiness.md`.
6. Conditor: vendor the 1.28.0 linux-x64 set and extend the WI-0051 qualification (the sweep step, plus
   `percepta --version` in `echelon-current-1-27-0-kickoff-set-qualification`). Release the next Conditor. REG-RU-007
   needs no new qualification, because no consumer declares `percepta-semantic` in `conditor.json`.
7. Record the approval in `sets/echelon-current/1.28.0.json` only after REG-RU-007 and the Conditor qualification are
   green. Cite the owner's words of 2026-10-24 ("Release it", "Do what you recommend") and OD-INDY-8, and drop the
   1.27.0 VER-003 exception from the new set.
