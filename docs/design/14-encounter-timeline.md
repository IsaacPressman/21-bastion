# The Encounter Timeline

Source: Improved Encounters Handoff, §§ 3, 4, 5, 6, 14, 15, 16.

This document covers **how tactical consequence is displayed**. `09-information-and-ui.md` remains the
authority on *what* may be shown and what is forbidden; this is the surface that shows it.

---

## The diagnosis this answers

> **The problem is not insufficient decision count. The problem is that the player often cannot form a
> concrete intention before drawing another card.**

If the battlefield communicates only that a lane is vaguely weak, then Hit means *"maybe another tower
would help."* The target is:

> "Lane one still leaks an armored soldier before socket 6. I need a specific kind of answer, and I know
> what another March step will cost me. I do not know whether the next card will solve it."

### The encounter thesis

> **The player should never wonder why they might want another card. They should know exactly what
> battlefield problem remains, but not whether the next draw will solve it.**

That sentence is the information boundary for the whole encounter layer. Every rule below serves it, and
the failure signal in `../prototype/VALIDATION.md` — *the player still cannot say why they want another
card* — is the test of whether it landed.

**Note what the thesis is not.** It is not a licence to widen what is shown. The uncertainty it protects
is the *draw*; the certainty it demands is the *battlefield*. A change that makes the next rank more
predictable fails it just as badly as one that leaves the lane illegible.

---

## The timeline is the primary visual language

> **Status: DECIDED — required for the improved encounter prototype.**

Each lane displays a deterministic time-and-path strip. Enemy groups appear as scheduled markers, tower
firing windows as overlays, and **March advancement visibly shifts the enemy schedule deeper into the
tower windows.**

It must communicate, in one place:

| Shown on the strip | Why it belongs there and not in a stat block |
|---|---|
| Enemy spawn timing and progression | Timing *is* the problem being solved; a spawn table makes the player simulate it mentally |
| Tower engagement windows | The per-socket window replaces the withdrawn engagement scalar (`03-march-clock.md`) |
| March advancement | The step's cost is legible as lost attacks rather than as a change in a number |
| Slow and bunching | Compression is a spatial event; a multiplier cannot show a column closing up |
| Hold orders | An order that shifts a firing window should visibly shift a firing window |
| Positional enemy breakpoints | The breakpoint is a place on the strip, which is what makes forward placement mean something |
| Dealer reinforcements | The one located uncertainty, drawn where it will arrive |
| **Attacks lost to a Hit** | The entire March decision, expressed as a consequence rather than a price |

The intended read is **not** "entry moves from 1.5 to 4.0." It is:

> "If you draw again, this cannon loses two shots before the Siege Engine crosses socket 9."

### Standing orders live on the timeline

Standing orders must alter this same display rather than opening a separate abstract menu — **Hold**
shortens or shifts a firing window, **Focus** highlights which enemy segment receives priority, and
**Trigger on Group** marks the clump currently satisfying the condition. Setting an order should feel like
editing the timeline. Full rules in `05-battlefield.md` § Standing orders.

### Why this is the compression mechanism, not another system

A drawn card can ask the player to weigh rank, four deployment forms, socket, run structure, enemy
breakpoints, standing orders, March cost, lane stakes, and Dealer uncertainty at once. **The answer to
that load is not another mechanic.** It is refusing to make the player mentally multiply stats where the
resolver can show a physical consequence:

> Prefer *"this tower gets two shots before the Banner crosses"* over *"range 3.0 × enemy speed 0.65 ×
> cooldown 1.4."*

Detailed numbers stay inspectable. **The scheduling picture is primary.** This is the same subtractive
discipline as the rest of the information design — see `../prototype/RISKS-AND-ADDBACKS.md` § Cognitive
load for the failure mode and its trigger.

---

## Exact consequences for the committed state

> **Status: DECIDED.** The resolver already knows what the current formation will do. Use it.

Per lane, the player may see predicted leak count and damage, **which** enemy leaks, first expected leak
time, which breakpoint ability fires, effective damage still required before a relevant breakpoint,
effective damage currently delivered, attacks or triggers each tower receives, which socket windows the
next March step removes, active runs, and current standing-order effects.

> **Lane 1 — Bastion**
> 1 Armored Soldier leaks for 2 Bastion damage.
> First leak: 11.4 s.
> Armor-effective damage required before socket 6: 9.0.
> Current formation delivers: 7.0.

**This is consequence, not recommendation**, and the distinction is the whole design. The lane states a
requirement and a shortfall; it does not name the card that closes them.

### Total engagement stays out of it

The timeline and the per-socket windows **replace** the summed engagement figure as the player-facing
readout. The scalar remains legal in debug tools and documentation and nowhere else — it treats
non-interchangeable sockets as fungible, which is why it was withdrawn as a balance tool
(`03-march-clock.md` § Total engagement is explanatory).

---

## Shortfall is stated in battlefield language, never card language

> **Status: DECIDED WITH CHANGE.**

Do **not** display *"a mid-rank Siege Club here will solve this."* That crosses the oracle line. Display:

> **Needs 2.1 more armor-effective damage before socket 6.**

The mental step the design is protecting is:

> battlefield requirement + drawn rank + available tower forms + geometry → **player judgment**

Naming the answer deletes the last arrow. Stating the requirement leaves every part of it intact.

---

## Candidate placements show causal deltas, not a score

> **Before drawing, show the requirement. After drawing, show the consequences of candidate actions. Do
> not show the answer.**

A candidate placement earns causal deltas:

- `Banner: survives → killed before socket 6`
- `Raider leak: 1 → 0`
- `Club 8 attacks: 3 → 2 after next March step`
- `Run: inactive → 3-card run`
- `Column: spread → compressed inside Barrage window`
- `Saboteur disable: fires → prevented`

What it must **not** earn is one sortable scalar — `projected value: 5.1 → 3.4` — because a single
comparable number lets the player brute-force every socket until the smallest one appears. That is the
same prohibition as § Not shown in `09-information-and-ui.md`, arriving through a new door: **a combined
verdict is no less a verdict for being computed per candidate.**

### Counterfactual memory

After a card is committed, the previous state is preserved long enough to show what the card changed:

> **Last placement: 4 Spade / Snare / Forward**
> Lane 1 leak: 2 → 0
> Banner: survives → killed
> Next March step: Club 8 loses 1 attack

**Players learn causality from deltas, not from absolute levels.** This is step 4 of the tactical loop
(`01-core-loop.md`) made visible, and it is what lets a player answer "what did that card buy you?" —
which is a success criterion, not a nicety.

---

## The placement quality target

> **The goal is not more legal placements. The goal is 2–3 competing plausible placements for important
> cards.**

| State | Reads as |
|---|---|
| One obviously correct socket | The puzzle is too obvious |
| Seven nearly interchangeable sockets | The state is too noisy |
| **2–3 competing plausible placements** | **The target** |

> "Forward-left kills the Standard Bearer early; middle-right completes my run; the junction hedges
> against the unknown reinforcement."

This is an **authoring metric**, not a mechanic. It is achieved by enemy timing, breakpoint placement, and
lane stakes — see `06-dealer-and-enemies.md` § Spatial breakpoints — and it is measured by asking players
which placements they seriously considered (`../prototype/VALIDATION.md` § Candidate-space health).

---

## The solvable-puzzle risk

The game is deterministic and increasingly transparent. **That is intentional**, and it carries a specific
danger: full information plus exhaustive candidate preview turns placement into brute-force optimization.

> The problem is not that an optimal solution mathematically exists. The problem is if the interface makes
> it trivial to discover **without understanding why.**

Guardrails, each of which is load-bearing:

| Guardrail | What it denies the brute-forcer |
|---|---|
| Candidate previews emphasize causal events | Nothing to sort on |
| **Multiple stakes** prevent collapse to one scalar | No single quantity to minimize |
| Optional opportunities add competing goals | The best defensive play is not always the best play |
| **Family and mode commitment** is irreversible | Search cannot be undone after the fact |
| Dealer hidden rank preserves located uncertainty | The board cannot be fully solved before standing |
| **Candidate hover count is instrumented** | The failure is detectable rather than assumed absent |

The last row is the one that turns this from a hope into a measurement. If players inspect nearly every
form-and-socket combination before committing, **the candidate preview is functioning as an oracle** and
the response is to reduce sortable outputs — not to hide information (`22 Failure Signals` in the
handoff, restated in `../prototype/VALIDATION.md`).
