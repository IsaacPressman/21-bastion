# 21 Bastion — Documentation

Split from two handoffs:

- `archive/handoff-revision-7-1.md` — Gameplay Design Handoff, **Revision 7.1**. The **encounter**.
- `archive/handoff-run-layer.md` — Rank Stacking & Continuous Siege Run Layer, consolidated. The **run**.

Start at `../CLAUDE.md` for orientation and the hard invariants.

> **Revision 7.1 is a correction pass.** It fixed a March Clock arithmetic error, reversed the stated
> direction of the march's placement bias, withdrew the engagement-fraction output estimates, cut the
> adjustment window to one move, split the forecast into two named contracts, and **reassigned the test-arm
> letters**. See `reference/tuning-constants.md` § Resolved and § Known Discrepancies before trusting a
> remembered number.

> **The run layer supersedes 7.1 where they disagree — and it does not touch encounter arithmetic.** The
> March Clock presets, the Formation Strength curve, and the deterministic resolver are unchanged by it. It
> resolves the product fork (a blackjack tower defense with a **siege-shaped run**), replaces three regions
> with three siege phases, **cuts Chips**, makes Bastion Health the only ordinary defeat condition, gives
> the Dealer a fixed 26-card opposing shoe, replaces the relic layer with **Doctrine** — and adds exactly
> **one** encounter mechanic, **rank stacking**, behind a flag. Four new discrepancies (8–11) are logged in
> `reference/tuning-constants.md`.

---

## Design specification — `design/`

Read the specific document for the system you are touching.

**The encounter — 00 through 09.** This is the prototype.

| File | Covers |
|---|---|
| [`00-pillars-and-identity.md`](design/00-pillars-and-identity.md) | Product fork, high concept, the narrowed claim, six design pillars, final identity |
| [`01-core-loop.md`](design/01-core-loop.md) | Wave phase order, combat framing, post-wave |
| [`02-blackjack-and-formation.md`](design/02-blackjack-and-formation.md) | Blackjack rules, Formation Strength, power curve, output landmarks |
| [`03-march-clock.md`](design/03-march-clock.md) | Escalating march, engagement geometry, exactly-21 pullback |
| [`04-cards-as-defenses.md`](design/04-cards-as-defenses.md) | Family locking, suits, face cards, Aces, run links |
| [`05-battlefield.md`](design/05-battlefield.md) | Sockets, **rank stacking**, persistence, adjustment window, stakes, standing orders, resolver |
| [`06-dealer-and-enemies.md`](design/06-dealer-and-enemies.md) | Dealer as wave generator, card→unit mapping, enemy stats, **the opposing shoe and public recruitment** |
| [`07-bust-and-overload.md`](design/07-bust-and-overload.md) | Bust handling, capped Overload |
| [`08-deck-economy-progression.md`](design/08-deck-economy-progression.md) | Shoe, thinning dilemma, **reward verbs, card identity, exhaustion, economy** |
| [`09-information-and-ui.md`](design/09-information-and-ui.md) | Shown / not shown — the fairness constraints, at both scales |
| [`example-wave.md`](design/example-wave.md) | A fully worked wave — the end-to-end acceptance test |

**The run — 10 through 13.** Deferred, not cut. Nothing here is prototype scope except the rank-stacking
flag, which lives in `05`.

| File | Covers |
|---|---|
| [`10-run-structure.md`](design/10-run-structure.md) | The continuous siege: run pillars, standing constraints, three phases, victory/defeat/Last Stand, cadence, time budget, run memory, modes |
| [`11-siege-geography.md`](design/11-siege-geography.md) | Persistent authored fronts, the four front states, neglect, concession |
| [`12-campaign-time-and-orders.md`](design/12-campaign-time-and-orders.md) | The phase clock, Time/Favor/Bastion Health, the seven strategic orders, shops and rewards, the menu probe |
| [`13-doctrine-and-charters.md`](design/13-doctrine-and-charters.md) | Doctrine as the placement-layer progression, Charters, **what happened to relics**, commanders |

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
| [`archive/handoff-run-layer.md`](archive/handoff-run-layer.md) | **Current, for the run.** Rank stacking, the continuous siege, campaign economy, opposing shoe, victory/defeat. **Supersedes 7.1 where they disagree**, except on encounter arithmetic. |
| [`archive/handoff-revision-7-1.md`](archive/handoff-revision-7-1.md) | **Current, for the encounter.** Its § 24 lists every correction over Revision 7. |
| [`archive/handoff-revision-7.md`](archive/handoff-revision-7.md) | **Superseded.** Kept for history. Several numbers and two instructions are now known wrong — do not cite it. |

The two current handoffs are the source of truth for **intent**. The split documents are authoritative for
**implementation**; if they disagree with a current handoff, that is a bug worth flagging.

Where the two current handoffs disagree with *each other*, the run layer wins — with the standing carve-out
that it changes **no** encounter-level arithmetic. The four known collisions are logged as discrepancies
8–11 in `reference/tuning-constants.md`.

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

**Building rank stacking:** `design/05-battlefield.md` § Rank stacking → `prototype/VALIDATION.md`
§ Rank-stacking sequence → `prototype/RISKS-AND-ADDBACKS.md` Part 3 → `ROADMAP.md` § Milestone 6

**Thinking about the run layer:** `design/10-run-structure.md` → `ROADMAP.md` § Run-layer sequencing →
`prototype/SCOPE.md` § The run layer is deferred, in a stated order. **Read the sequencing before the
content** — the do-not-build column is the load-bearing half.
