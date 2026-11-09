# EDS-ADR-002: echelon-current 1.38.0 (Forma 1.0.1 and the Echelon theme)

Source: owner decision EDS-ADR-002 (Echelon branding moves out of Forma into
`kemiller2002/echelon-design-system`) and the Forma namespace decision
(2026-11-09, "Do the major break"). Owner approval, 2026-11-09 ("Yes,
approved"): channel releases for Forma 1.0.0, the theme component and
echelon-current 1.38.0.

## Requirements

1. **Register Forma 1.0.1** (`kemiller2002/forma` tag `v1.0.1`) as a
   `web-package` GitHub-release record, every artifact digest taken from the
   published release and re-hashed from a download.
2. **Register the Echelon theme.** A new system `echelon-theme`
   (`kemiller2002/echelon-design-system`, class `web-package`) and its release
   1.0.0 (tag `v1.0.0`, package `@echelon-foundry/echelon-theme`, artifacts
   `echelon-foundry-echelon-theme-1.0.0.tgz`, `checksums.txt`,
   `asset-manifest.json`), digests verified against the release's
   `checksums.txt` and GitHub's API digest.
3. **Cut echelon-current 1.38.0.** The profile's Forma range moves to the 1.x
   line (`>=1.0.0 <2.0.0`), the profile gains `echelon-theme` as an optional
   (`required: false`) project binding (`>=1.0.0 <2.0.0`), and the channel,
   snapshot and release-set record are regenerated with the Registry's tools.
   Forma 1.0 is a major break (`ef-` -> `forma-`, Echelon branding removed);
   the set records it.
4. **REG-RU-007.** `echelon-theme` becomes a Conditor-declared component, so
   the channel cannot select a theme version the released Conditor refuses.
   The set is approved only after REG-RU-007 passes against a released
   Conditor that qualifies Forma 1.0.1 and the theme 1.0.0.
5. **Record the owner's approval** in `sets/echelon-current/1.38.0.json`,
   mirroring 1.37.2's approval block.
