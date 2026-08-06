# 21 Bastion — Documentation

Split from `archive/handoff-revision-7-1.md` (Gameplay Design Handoff, **Revision 7.1**).

Start at `../CLAUDE.md` for orientation and the hard invariants.

> **Revision 7.1 is a correction pass.** It fixed a March Clock arithmetic error, reversed the stated
> direction of the march's placement bias, withdrew the engagement-fraction output estimates, cut the
> adjustment window to one move, split the forecast into two named contracts, and **reassigned the test-arm
> letters**. See `reference/tuning-constants.md` § Resolved and § Known Discrepancies before trusting a
> remembered number.

---

## Design specification — `design/`

Read the specific document for the system you are touching.

| File | Covers |
|---|---|
| [`00-pillars-and-identity.md`](design/00-pillars-and-identity.md) | High concept, the narrowed claim, six design pillars, final identity |
| [`01-core-loop.md`](design/01-core-loop.md) | Wave phase order, combat framing, post-wave |
| [`02-blackjack-and-formation.md`](design/02-blackjack-and-formation.md) | Blackjack rules, Formation Strength, power curve, output landmarks |
| [`03-march-clock.md`](design/03-march-clock.md) | Escalating march, engagement geometry, exactly-21 pullback |
| [`04-cards-as-defenses.md`](design/04-cards-as-defenses.md) | Family locking, suits, face cards, Aces, run links |
| [`05-battlefield.md`](design/05-battlefield.md) | Sockets, persistence, adjustment window, stakes, standing orders, resolver |
| [`06-dealer-and-enemies.md`](design/06-dealer-and-enemies.md) | Dealer as wave generator, card→unit mapping, enemy stats |
| [`07-bust-and-overload.md`](design/07-bust-and-overload.md) | Bust handling, capped Overload |
| [`08-deck-economy-progression.md`](design/08-deck-economy-progression.md) | Shoe, thinning dilemma, economy, relics, commanders |
| [`09-information-and-ui.md`](design/09-information-and-ui.md) | Shown / not shown — the fairness constraints |
| [`10-run-structure.md`](design/10-run-structure.md) | Regions, time budget, escalation, modes |
| [`example-wave.md`](design/example-wave.md) | A fully worked wave — the end-to-end acceptance test |

## Steering

| File | Covers |
|---|---|
| [`ROADMAP.md`](ROADMAP.md) | Build order, milestones, open decisions |
| [`ARCHITECTURE.md`](ARCHITECTURE.md) | Proposed structure, determinism, testing strategy |
| [`GLOSSARY.md`](GLOSSARY.md) | Terms of art |
| [`reference/tuning-constants.md`](reference/tuning-constants.md) | Every number in one place, plus known discrepancies |

## Prototype

| File | Covers |
|---|---|
| [`prototype/SCOPE.md`](prototype/SCOPE.md) | In, out, and scope-drift warnings |
| [`prototype/VALIDATION.md`](prototype/VALIDATION.md) | Test arms, scripted battery, success criteria, instrumentation, regression |
| [`prototype/RISKS-AND-ADDBACKS.md`](prototype/RISKS-AND-ADDBACKS.md) | Key risks and the fixed add-back sequence |

## Archive

| File | Status |
|---|---|
| [`archive/handoff-revision-7-1.md`](archive/handoff-revision-7-1.md) | **Current.** The unsplit handoff these docs derive from. Its § 24 lists every correction over Revision 7. |
| [`archive/handoff-revision-7.md`](archive/handoff-revision-7.md) | **Superseded.** Kept for history. Several numbers and two instructions are now known wrong — do not cite it. |

The 7.1 handoff is the source of truth for **intent**. The split documents are authoritative for
**implementation**; if they disagree with 7.1, that is a bug worth flagging.

---

## Reading orders

**New to the project:** `design/00-pillars-and-identity.md` → `design/01-core-loop.md` →
`design/example-wave.md` → `prototype/SCOPE.md`

**About to write code:** `../CLAUDE.md` → `ROADMAP.md` → `ARCHITECTURE.md` →
`reference/tuning-constants.md`

**About to change a number:** `reference/tuning-constants.md` → `prototype/VALIDATION.md` § Regression →
`prototype/RISKS-AND-ADDBACKS.md`

**Considering adding a system:** `design/00-pillars-and-identity.md` § One System Per Job →
`prototype/SCOPE.md` § Scope drift warnings → `prototype/RISKS-AND-ADDBACKS.md` § Add-Back Sequence
