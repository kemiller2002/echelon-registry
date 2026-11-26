---
id: PF-GOV-ADV
title: Adversarial review of the transparency and portfolio governance operating model
classification: public
status: complete for this iteration; findings applied where marked
updated: 2026-11-25
work_item: GOV-TRN-001
method: Each perspective answers seven questions: values · distrusts · could exploit · needs · challenges · unnecessary cost · could invalidate. Findings marked **[applied]** changed a deliverable in this iteration; **[open]** are in the open decisions register or roadmap.
---

# Adversarial review

## 0. The central assumption

*"Transparency creates trust."* The evidence review (`portfolio/research/transparency-competitive-advantage.md`)
does not support it as a general law: benefits with strong evidence are narrow (coordinated
disclosure speeds patching; open designs allow third-party verification; shared research data is
reused). Commercial benefits such as shorter sales cycles are documented only by trust-center vendors.
Transparency creates value only when the published information is **accurate, current, useful and
evidenced**. On 2026-11-25 Echelon's public record contains unfilled templates presented as charters,
version facts that disagree, zero `SECURITY.md` files and one exposed credential. Publishing more of
that would *reduce* trust. Consequence **[applied]**: the policy pairs every publication duty with an
accuracy and freshness duty (POL-ORG-TRN-001 §1, §6, §12); metrics measure accuracy and incidents,
not volume; the trust center has preconditions before it may be built.

## 1. Perspectives

### Prospective enterprise customer
- **Values:** honest status, known limitations, roadmap, a named security contact.
- **Distrusts:** a one-person organization; "AI-built" software; statements with no independent assurance.
- **Could exploit:** public roadmap to negotiate; public gaps to demand concessions.
- **Needs:** current approved answers, incident history, subprocessors, continuity plan.
- **Challenges:** "Who answers at 3 a.m.?"; "What happens if the founder is unavailable?"
- **Unnecessary cost:** answering bespoke questionnaires that a public record could satisfy.
- **Could invalidate:** if public claims are found stale once, every other public claim is discounted. **[applied]** claim freshness, TC-R-21.

### Financial institution CISO
- **Values:** evidence of control operation, third-party risk inputs, GitHub as subservice organization clarity.
- **Distrusts:** first-party evidence; agents with write access to production paths; self-reported provenance.
- **Could exploit:** n/a (counterparty).
- **Needs:** SOC 2 Type II or equivalent, penetration test letter, BCP/DR evidence — **none exist**.
- **Challenges:** separation of duties with one owner (Q-GOV-03); agent authority boundaries.
- **Unnecessary cost:** building trust-center tooling before any independent report exists.
- **Could invalidate:** the regulatory reality that bank TPRM requires independent assurance regardless of transparency. **[applied]** claim status distinguishes `assessed-independent`; trust center precondition 4.

### Vendor risk manager
- **Values:** standard questionnaire mappings (SIG, CAIQ), consistent answers, versioned evidence.
- **Distrusts:** answers that differ across channels.
- **Could exploit:** inconsistencies between public repositories and submitted answers.
- **Needs:** one authoritative answer per concern with as-of dates.
- **Challenges:** licensed questionnaire text cannot be published (DD-GOV-001 rule 5; PUB-SG-09).
- **Unnecessary cost:** maintaining answers in several places. **[applied]** single answer units mapped, no parallel vocabulary (DDT-02).
- **Could invalidate:** public answers contradicting signed contractual answers create liability.

### Security researcher
- **Values:** a VDP with safe harbour, private reporting channel, credit, timely fixes.
- **Distrusts:** policies with no staffed response.
- **Could exploit:** public architecture to target the narrow places where invariants are not enforced; public CI configuration.
- **Needs:** scope, contact, supported versions.
- **Challenges:** a single owner cannot meet 3-day acknowledgement reliably. **[applied]** VDP publishes targets, not promises; staffing noted.
- **Unnecessary cost:** none significant.
- **Could invalidate:** a report ignored after the VDP is published is worse than having no VDP.

### Malicious external actor
- **Values:** public secrets, internal hostnames, unremediated issues, detection logic, test deploy keys.
- **Distrusts:** n/a.
- **Could exploit:** the exposed credential (OD-GOV-012); Causa's documented public test key (OD-GOV-014); e-mail addresses in provenance for phishing (OD-GOV-017); agent prompt injection through public issues read by autonomous agents.
- **Needs:** any restricted information in history, logs or artifacts.
- **Challenges:** "secure by construction" claims (find the bypass).
- **Unnecessary cost:** for Echelon, retrospective scanning of every surface. **[applied]** preventive controls first (PUB-SG-01, -10, -12, -15).
- **Could invalidate:** a successful exploit of a published "eliminated" vulnerability class. **[applied]** claim discipline; Secure Construction Standard already forbids such claims without scoped evidence.
- **Additional finding [open]:** autonomous agents reading public issues and pull requests are a prompt-injection surface; Praxis requirements should treat public issue text as untrusted input (roadmap R-15).

### Competitor
- **Values:** roadmap, rejected-opportunity analysis, pricing, method details.
- **Distrusts:** n/a.
- **Could exploit:** imitation of methods and positioning; using published negative results against Echelon.
- **Needs:** commercial strategy specifics (kept internal with `competitive-specific` justification).
- **Challenges:** is openness a moat? Evidence: no measured harm from imitation, no measured benefit either.
- **Unnecessary cost:** none.
- **Could invalidate:** publishing before an IP decision makes patents impossible and ends trade-secret status. **[applied]** intellectual-property justification category (POL-ORG-TRN-001 §4/§5.4; OD-GOV-026).

### SOC 2 auditor
- **Values:** policies adopted with dates, operated controls, population completeness, independent evidence, change management.
- **Distrusts:** policies marked "proposed"; evidence generated and stored by the same agent; self-reported provenance.
- **Could exploit:** n/a.
- **Needs:** system description boundaries (GitHub as subservice organization), management assertions.
- **Challenges:** stale context files contradicting code (assurance ECH2 §3.6) would be sampled as management inconsistency.
- **Unnecessary cost:** collecting evidence for controls that are not in scope.
- **Could invalidate:** "continuous assurance" without completeness proof (selective omission). Requirement in the assurance evidence architecture §6; CA-PROV-03.

### Privacy officer
- **Values:** data minimization, no personal data in public repositories, retention schedules.
- **Distrusts:** "public by default" applied to records that contain people.
- **Could exploit:** n/a.
- **Needs:** inventory of personal data in public repositories. **Found:** owner e-mail in Praxis event logs; an e-mail address in agile-assessment; HelixNote handles health data (private).
- **Challenges:** append-only evidence vs deletion rights (assurance evidence architecture §8).
- **Unnecessary cost:** none.
- **Could invalidate:** a personal data exposure in a public repository; Git history makes erasure impractical. **[applied]** personal data is never default-public (§4); PUB-SG-04.

### Intellectual property attorney
- **Values:** clear licences, IP assignment to the entity, protection decisions before publication.
- **Distrusts:** mixed licences (PolyForm Noncommercial, MIT, none) and unlicensed public research.
- **Could exploit:** n/a.
- **Needs:** licence per repository class; contributor terms for agent-generated code.
- **Challenges:** who owns code generated by agents and by contributors; entity-specific IP assignment (organization administration README already requires explicit assignment).
- **Unnecessary cost:** none.
- **Could invalidate:** publication defeating patentability. **[applied]** IP category; **[open]** OD-GOV-018 licences.

### Regulatory examiner
- **Values:** accurate public statements, complaint and incident handling, vendor management.
- **Distrusts:** public security statements that exceed operated controls (FTC v. Zoom; SolarWinds 2024 ruling).
- **Could exploit:** public statements as admissions.
- **Needs:** consistency between public statements and practice.
- **Challenges:** whether DAF administration (Unbound) brings examination; unresolved in Unbound's legal hypotheses.
- **Unnecessary cost:** none.
- **Could invalidate:** a misleading public claim. **[applied]** POL-ORG-TRN-001 §12.

### Software engineer
- **Values:** fewer places to update, fast validation, clear ownership.
- **Distrusts:** governance overhead; "registry updates" for trivial changes.
- **Could exploit:** marking every evaluation `not-required`.
- **Needs:** automation (manifests generate central records).
- **Challenges:** another manifest to maintain.
- **Unnecessary cost:** duplicate facts. **[applied]** single source per fact (RM-06); manifests generate central entries; advisory rollout.
- **Could invalidate:** if RM-03 becomes ceremony, evaluations become noise. Mitigation: `registry-impact` aid (RM-17) and sampling review.

### Autonomous engineering agent
- **Values:** bounded, task-specific context; machine-readable boundaries; clear authority.
- **Distrusts:** template text that looks authoritative (DRF-05).
- **Could exploit:** n/a, but agents propagate stale facts at scale.
- **Needs:** "who owns capability X" answerable from records; non-responsibilities.
- **Challenges:** conflicting instructions between repositories (e.g. Praxis branch rule vs hosted session branch names, OD-GOV-003).
- **Unnecessary cost:** reading whole portfolios. **[applied]** KSI-06 task packages.
- **Could invalidate:** agents fabricating registry entries to satisfy RM-03. Mitigation: validator rejects unknown systems, unverified maturity claims (PF005, PF009).

### Investor
- **Values:** focus, evidence of demand, defensibility, key-person risk management.
- **Distrusts:** 29 initiatives and ~64 repositories for one accountable owner.
- **Could exploit:** n/a.
- **Needs:** priorities and what has been stopped.
- **Challenges:** is the portfolio too broad? The register makes the breadth visible; that is useful information, not a flaw of the register.
- **Unnecessary cost:** governance effort on dormant work. **[applied]** ABN-* dispositions proposed.
- **Could invalidate:** transparency revealing lack of focus without a prioritization decision. **[open]** roadmap R-01: owner prioritization of initiatives.

### Organizational operations manager
- **Values:** low maintenance cost, clear owners, deadlines.
- **Distrusts:** policies whose upkeep exceeds capacity (review cycles for 29 initiatives, 64 repositories, 4 policies).
- **Could exploit:** n/a.
- **Needs:** a review calendar that fits one person.
- **Challenges:** semi-annual and quarterly reviews may be unrealistic. **[applied]** staleness threshold 180 days in the validator; reviews driven by triggers (RM-03) rather than calendars where possible.
- **Unnecessary cost:** a trust center application before evidence exists. **[applied]** precondition gate.
- **Could invalidate:** governance that is not maintained becomes the stale public record this work set out to remove.

## 2. Cross-cutting conclusions

1. **Accuracy before volume.** The first deliverable of transparency is correcting what is already
   public (credential, templates, drift), not publishing more.
2. **Independence is the binding constraint** for financial-institution customers; transparency does
   not substitute for it. Budget and sequence independent assurance explicitly (INIT-006).
3. **Key-person risk** (one accountable owner) undermines separation of duties, VDP response and review
   cadence. It is the largest unaddressed organizational risk visible from public records.
4. **Agents are both the main producers of drift and the main beneficiaries of good records.** The
   controls with the highest expected return are those that make agents update records as part of
   the change (RM-03/06) and that give agents bounded context (KSI-06).
