# Key Risks and the Add-Back Sequence

Source: Handoff Revision 7.1, §§ 21, 22.

---

# Part 1 — The Add-Back Sequence

Four systems were cut or suspended **for diagnostic reasons rather than because they were bad**.

> **The order and trigger for each are fixed now, while the reasoning is fresh. Cuts made for a test become
> the design by default if nobody writes down when they come back.**

## 1. Dealer Total Comparison

**Trigger:** the primary arm shows players changing hit/stand with the battlefield, and the scripted
battery shows decisions diverging from basic strategy.

> ⚠ The handoff says "Arm A" here, but that wording predates the 7.1 relettering — **Arm A is now the flat
> control**, and the as-specified curve is Arm C. Read this as *the primary arm*, whichever the arms report
> as the design. See Known Discrepancies in `../reference/tuning-constants.md`.

**Form on return:** **comparison pays the Vault, not the battlefield.** Beating the Dealer improves the
encounter reward; losing reduces it. The blackjack incentive then *competes* with the battlefield incentive
instead of reinforcing it, which is the tension this design exists to create.

> **Battlefield-side comparison effects — Vanguard withdrawal, army advancement — do not return in any
> form.**

**Risk if skipped:** the game ships without an opponent, the blackjack framing becomes vestigial, and it is
21-solitaire.

## 2. Persistence Multipliers

**Trigger:** *only* if playtests show that reverting persisted towers to ×1.00 makes the second wave of an
encounter feel weightless.

**Form on return:** a **partial retention** — persisted towers keep half the difference between their
locked multiplier and 1.00. **Full locking is not returning**; it produced snowball and UI load.

## 3. A Second Link Rule

**Trigger:** runs alone prove too rare to shape placement, measured by **run frequency per hand** in
instrumentation.

**Form on return:** **pairs, at a lower value than runs.** Keywords and auras remain out until a link rule
has survived a full test cycle.

## 4. Many-Card Support

**Trigger:** the archetype is unviable in **all three arms**.

> ⚠ The handoff's § 21 says "Arm A and Arm B both," carried over from the two-arm structure; its § 20 says
> "unviable in all three." **§ 20 governs** — with three arms, all three is the meaningful condition.

**Form on return:** designed against the **measured deficit**. **Not Wide Formation.**

> A flat attack-speed bonus at the card counts the march taxes is the exact refund loop this revision
> removed; **whatever returns must not be a function of card count alone.**

---

# Part 2 — Key Risks

## The march curve is the wrong shape

The step sizes are **the most important numbers in the game** and **must be config-tunable on the first
build**. The escalation is what makes hand length a real decision; if it is wrong, nothing downstream will
read correctly.

## The fifth card is binary

A **67%** engagement loss is severe enough that **a safe miss may be board collapse**, leaving only
exact-21-or-nothing.

> **Reduce the fifth step before raising the pullback.**

**⚠ Revision 7 advised the opposite; that was wrong.** Raising the pullback makes success more attractive
but makes the mechanic ***more* binary**, widening the gap between landing and missing. **Softening the
step is what creates the defensible middle outcome the design wants.**

This is the single most likely place for stale guidance to cause damage — the reversed instruction reads
plausibly in both directions. The target shape is in `../design/03-march-clock.md` § The fifth card is a
hypothesis; the measurement is the primary output of the test arms.

## Deep placement dominates

Because entry advances from the spawn side, **every unit of advancement degrades forward sockets while
leaving rear ones untouched** — so deep placement is weakly dominant whenever entry exceeds 0.

**A mechanic added to enrich placement may be flattening it.**

Measured through **placement-depth logging**. If it holds, **fix the socket geometry — uneven spacing,
range differences by position, or lane-specific leak thresholds — before touching the march curve.**

Flagged in `../design/03-march-clock.md`. This is the first thing to measure once the resolver runs.

## Locking family is too punishing

If players report feeling trapped by a family committed three cards before the wave was known, the fix is a
**limited escape** (a relic, one reassignment per encounter) rather than reopening the window.

> **Free reassignment empties the draw phase.**

## The single adjustment move is either useless or decisive

Instrumented. If it is **decisive**, restrict it to **empty sockets with no swapping**.

> **Do not widen it back toward per-tower movement; grant extra moves through relics instead.**

## Suspending comparison strands the blackjack layer

**Mitigated only by Add-Back 1.**

> **This is the risk most likely to be quietly forgotten.**

## Removing live combat makes the game feel passive

Mitigated by standing orders and skippable combat. If it still reads as a spreadsheet, **add pre-lock
expression, not live clicking.**

## Runs make placement fiddly rather than interesting

If players solve an adjacency puzzle instead of reading the battlefield, the handoff's remedy was to
**cut the four-run and reduce the percentages.**

⚠ **Half of that remedy is already spent.** The four-run was cut for geometric reasons — three sockets per
lane with no cross-lane adjacency makes it impossible to build — so only **reducing the percentages**
remains available. If runs still read as fiddly after that, the next lever is the tie-break or the
adjacency rules, not another tier cut. See `../design/04-cards-as-defenses.md`.

## Forced replacement feels bad rather than tense

It is **meant to be a real loss**. If it reads as punishment, **the answer is more sockets, not a softer
rule.**

## The game is still basic strategy with cosmetics

> **The core risk, retained across three revisions.** The battery is designed to detect it. If the answer
> is yes, **the honest response is a structural change, not another tuning pass.**

---

## Pattern

Every risk above names its own remedy, and in most cases names remedies that are *forbidden*. That is the
useful part. When a playtest surfaces one of these, the response is already written down — the job is to
apply it, not to redesign around it.
