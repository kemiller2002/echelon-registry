---
id: PF-GOV-RM
title: Prioritized remediation and implementation roadmap
classification: public
status: proposed
updated: 2026-11-25
work_item: GOV-TRN-001
---

# Prioritized remediation and implementation roadmap

Priority: **P0** immediate (risk of harm now), **P1** before any public trust claim, **P2** next
quarter, **P3** when evidence supports it. Owner: **O** = accountable owner action (agents cannot do
it), **A** = agent work after authorization.

| ID | Pri | Item | Owner | Depends on | Done when |
|---|---|---|---|---|---|
| R-00 | P0 | Contain the exposed credential: revoke at the provider, then remove from the repository tip; history rewrite optional | O | — | provider confirms revocation; OD-GOV-012 closed |
| R-01 | P0 | Decide initiative priorities and dispositions (ABN-01..05) — the register shows 29 initiatives for one owner | O | — | priorities recorded in the internal register and projected |
| R-02 | P1 | Adopt (or amend) the four policies | O | — | OD-GOV-001 decided |
| R-03 | P1 | Enable secret scanning + push protection and private vulnerability reporting on all public repositories | O | — | M-22 scanning 100% |
| R-04 | P1 | Add `SECURITY.md` (template in `portfolio/governance/templates/`) to public repositories | A | R-02 | M-22 SECURITY.md 100% |
| R-05 | P1 | Full-history secret scan baseline of public repositories (PUB-SG-03); triage as incidents | A | R-00, OD-GOV-008 | baseline recorded |
| R-06 | P1 | Resolve OD-GOV-014 (Causa public test key) and OD-GOV-017 (e-mail in provenance) | O | — | decisions recorded |
| R-07 | P1 | Resolve OD-GOV-002 (assurance visibility) | O | — | decision recorded |
| R-08 | P2 | Register Vitium, Causa, Specula; classify EDF (identity only) | A | OD-GOV-023 | M-07 = 0 |
| R-09 | P2 | Pilot `.echelon/repository.json` in echelon-registry, praxis and one product; generate central entries from manifests | A | — | M-02 manifest-sourced > 0 |
| R-10 | P2 | Approve and implement Praxis RM-* (advisory mode first) | O then A | OD-GOV-003 | WI-0110 unblocked |
| R-11 | P2 | Correct documentation drift DRF-01..08 (legacy `rosVersion`, Summa README, templates, naming) | A | R-10 for RM-08/09 | findings closed |
| R-12 | P2 | Verify the dependency graph from package references of latest releases (resolve CYC-01/02 classification) | A | — | graph generated from manifests |
| R-13 | P2 | Decide security rule ownership (DUP-03) and amend the Secure Construction Standard | O | — | standard v0.2 |
| R-14 | P2 | Knowledge Store first increment (KSI §6: capability-owner lookup package) | A | — | HY-KSI-01 measured |
| R-15 | P2 | Praxis requirement: treat public issue/PR text as untrusted input for agents (prompt-injection surface) | A | — | requirement proposed in praxis |
| R-16 | P2 | Start measuring M-13/M-14/M-15 (questionnaire effort, turnaround, assurance spend) | O | — | first data points |
| R-17 | P2 | Reconcile trust center specifications (OD-GOV-020) | O + A | — | single information architecture |
| R-18 | P3 | Automate initiative projection via private CI pull requests (OD-GOV-019) | A | — | no manual projection runs |
| R-19 | P3 | Publish scanner validation methodology and first results (Secure Construction deliverable 1) | A | R-13 | report published |
| R-20 | P3 | Build the public trust center (static, generated) | A | R-02, R-03, R-05, R-17, at least one evidenced claim class | TC preconditions met |
| R-21 | P3 | Independent assurance sequencing for Unbound (penetration test, SOC scoping) | O | INIT-006 research | decision recorded |

## Not on the roadmap (deliberately)

- No new system is created for any responsibility identified here; first decide whether an existing
  system owns it (GAP-05/06/07 remain unowned pending decisions).
- No repository is archived, merged or made private/public by this roadmap; those are owner decisions.
- No trust center application before R-20's preconditions.
