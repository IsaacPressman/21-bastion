# Key Risks and the Add-Back Sequence

Source: Handoff Revision 7.1, §§ 21, 22. **Run-layer risks** are from the Run Layer Handoff
(consolidated), §§ 1, 2, 3.

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

⚠ **What "pays the Vault" means changed with the run layer.** Chips are cut, and Favor is never a
reward-floor currency. Comparison therefore pays the encounter's **ordinary campaign reward** — the
captured supplies, the exposed service, the Muster or Rerank the Vault would have funded. **It must not pay
Time**, which would let a blackjack outcome buy campaign actions and re-open the door between the two
clocks. See `../design/12-campaign-time-and-orders.md`.

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

## Deep placement dominates — ✅ measured, confirmed, remedied

Because entry advances from the spawn side, **every unit of advancement degrades forward sockets while
leaving rear ones untouched** — so deep placement was weakly dominant whenever entry exceeded 0.

**A mechanic added to enrich placement was flattening it.**

Measured through the resolver sweep rather than left to playtest, confirmed in all three arms, and
remedied at Milestone 5 by **range differences by position** (4.0 / 3.0 / 2.0, forward to rear). The march
curve was not touched. Full result in `../ROADMAP.md` § Open Decision 2.

**Note for anyone revisiting this:** of the three remedies named above, **uneven spacing was measured and
does not work.** Lane-specific leak thresholds remain untried, but they change what a lane is worth rather
than what a socket is worth, so they are not a depth remedy.

The residual is a mild *shallow* lean, largest in the flat control arm. **Placement-depth logging stays
in the instrumentation set** to watch it.

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

# Part 3 — Rank-Stacking Risks

Stacking is the one encounter mechanic the run layer adds, and it ships **flag-gated, default off**
(`../design/05-battlefield.md` § Rank stacking). Its three accepted risks each name their own remedy.

## Stacking softens forced replacement

Forced replacement is **one of the three pillars of decision density** — not a safety valve. A mechanic
that lets a player at capacity absorb a card without tearing anything down is aimed squarely at it.

**Instrumented:** stack-at-capacity rate, replacement rate, and **whether players stack reflexively
whenever a match exists.**

> If forced-replacement frequency drops sharply with the flag on, that is the ship/cut question answering
> itself. It is Open Question 7 in `../ROADMAP.md`.

## Stacking may worsen deep-placement dominance

Concentrated power naturally prefers **safe rear sockets.**

> **Diagnose socket geometry before taxing stacks.**

The geometry remedy is already in (range 4.0 / 3.0 / 2.0, forward to rear — `../ROADMAP.md` § Open
Decision 2), and the residual is a mild *shallow* lean. So a rear cluster appearing in the stacking pass is
a **stacking** result, not a geometry one — which is exactly why the pass runs after the remedy rather than
before it.

## Stacking becomes automatic

**Form of the fix:** test **one** cost in isolation, and prefer a **spatial or cadence** cost — a longer
shared cooldown — over a damage tax.

> **Do not add a flat damage penalty by default.** Stacking already pays three costs: forfeited run
> eligibility, forfeited coverage breadth, and shared March exposure. A damage penalty is a fourth, and it
> is the one that makes the trade illegible.

**And do not change March and stacking simultaneously.** The arms are pre-committed test arms; two moving
variables destroy the reading.

---

# Part 4 — Run-Layer Risks

Not yet live, and none of them may delay the encounter slice. Recorded now for the same reason as the
add-back triggers: **the remedy is worth writing down while the reasoning is fresh.**

## The campaign becomes a second, larger game

**The named failure of the whole layer.** Target is roughly 70% encounter, 30% campaign, and the guard is
the first standing constraint: **every campaign mechanic must create a more interesting next encounter, or
be cut.**

**Detected by** the cadence log — time on the command screen, backtracking, and **number of distinct menus
opened.** One decision surface is the target; menu count is how that target fails quietly.

## Pressure becomes a punishment spiral

Neglect and loss must **reshape** the siege, never recursively make every later decision worse.

**Three structural guards, all already in the design:** the phase clock **resets per phase** so an early
mistake stays priceable; neglect draws from a **bounded authored table** whose outcomes are shown in
advance; and **every encounter has a reward floor.**

> **Losing every outer district is not defeat**, and total concession must leave a winnable Last Stand. A
> concession system whose logical conclusion is an unwinnable run is not a choice, and players learn that
> after one run.

## Dealer adaptation reads as rubber-banding

If the game appears to counter the player for playing well, adaptation is a punishment.

**The remedy is structural rather than tuned:** recruitment is **public** (a visible three-card row with
marked intent), **raidable** (three hours removes a candidate), **lagged** by a full phase, and keyed to
**build composition only** — never win rate, health, or loss streaks.

> Without a way to interfere, adaptation is rubber-banding. With it, it is an arms race — and the option to
> **deliberately allow** a visible King is what makes it a decision.

## Concession is never correct

Concession must **sometimes be strategically correct, not merely less bad.** The mechanism is that both
columns are **certain**: a known cost for a known structural benefit. If a concession's benefit is a
probability, it is a gamble, and gambling already has a home in this game.

**Detected by** the geography log's **Lost vs Conceded cause** field. If Conceded almost never appears, the
benefits are too small or too uncertain.

## Favor becomes purple money

**Cap 3, first pass.** Favor is *stored command authority*, and the failure is it degrading into a second
currency for ordinary purchases.

**Detected by** the Favor log's most pointed field: **whether the spend changed the encounter decision or
merely erased a mistake.** Favor that only undoes errors is a mulligan wearing a uniform.

## The two clocks fuse

Campaign time and the March Clock are **the same shape and never the same number.** Campaign time must
never modify hand-scale March entry, and no campaign effect may reach into Formation Strength or the march
curve.

> The 7.1 relic **Long Road** — "reduces the march curve for one encounter" — violates this directly and is
> flagged as suspect in `../design/13-doctrine-and-charters.md`. It is the shape to watch for, not just
> the one instance.

---

# Improved-encounter risks

Source: Improved Encounters Handoff, §§ 15, 16, 22. These are risks of the **information** layer, and they
share a pattern worth naming: every one of them is made *worse* by adding a mechanic.

## The encounter becomes a solvable puzzle

The game is deterministic and increasingly transparent, and that is **intentional**. The danger is that
full information plus exhaustive candidate preview turns placement into brute-force optimization.

> The problem is not that an optimal solution mathematically exists. The problem is if the interface makes
> it **trivial to discover without understanding why.**

**Detected by** hover instrumentation — candidate forms hovered, candidate sockets hovered, and states
where the player inspects nearly every combination before committing.

**Remedy: reduce sortable candidate outputs and emphasize causal tradeoffs.** Explicitly **not** the
remedy: hiding battlefield information. That would attack the encounter thesis — the battlefield is
supposed to be certain, and only the draw uncertain — to fix a problem caused by the *shape* of what is
shown rather than the amount. Guardrails in `../design/14-encounter-timeline.md`.

## Cognitive load makes placement into homework

A drawn card can ask the player to weigh rank, four forms, socket, run structure, breakpoints, standing
orders, March cost, lane stakes, and Dealer uncertainty at once.

**Detected by** placement time: median, 90th percentile, and time by card number in hand. **Placement
times exploding is the signal.**

**Remedy, in order:** simplify presentation, reduce candidate forms, make the timeline more legible.
**The answer is not another mechanic**, and it is specifically not a helper that summarizes the decision —
that is the combined verdict invariant 2 forbids, arriving as an accessibility feature.

> The timeline **is** the compression mechanism. If load is too high, the timeline is not doing its job
> yet; adding a second aid on top of it concedes that and doubles the surface.

## The four forms are not actually different

**Detected by** form choice per rank. If everyone uses the same form for a given rank, the forms are not
tactically differentiated — and four undifferentiated options cost the same attention as four real ones
while returning nothing.

**Remedy:** differentiate behavior, not numbers. A coefficient change makes one form better; a behavior
change makes each form *correct somewhere*. Note the related signal — **Snare and Barrage independently
useful but never intentionally combined** — which points at bunching legibility rather than at the forms.

## Optional opportunities stop being optional

Two-sided by construction, and both sides are failures:

| Signal | Meaning | Remedy |
|---|---|---|
| **Ignored** | Payoff too small or too detached from the run | Raise or reconnect the payoff |
| **Always pursued** | Mandatory objectives in disguise | **Lower** the payoff, or raise situationality |

The second is the more likely one, and the instinct it will provoke — make the reward better so it feels
worth it — is exactly backwards.

## Wave 2 does not produce adaptation

If Wave 2 feels like Wave 1 with more enemies, **persistence is not doing the job it exists for.**

**Remedy: rewrite encounter pairs before adding progression systems.** Persistence exists to create
scarcity and forced adaptation (`../design/05-battlefield.md` § Wave 2), and a progression system layered
on top of a Wave 2 that does not bite would be paying for a fix in the wrong place.

---

## Pattern

Every risk above names its own remedy, and in most cases names remedies that are *forbidden*. That is the
useful part. When a playtest surfaces one of these, the response is already written down — the job is to
apply it, not to redesign around it.

The improved-encounter risks sharpen that into a rule: **when the information layer fails, the response is
never a new mechanic.** Four of the five above would be made worse by one.
