# Prototype Scope

Source: Handoff Revision 7.1, § 20, plus the Run Layer Handoff (consolidated), §§ 11, 13, and the
**Improved Encounters Handoff**, §§ 7, 9, 12, 24.

**This is a boundary, not a suggestion.** Anything in the Cut list is out of scope unless explicitly
requested.

> **The run layer does not change this boundary except in one place.** Its own § 11 says so directly:
> *"Nothing in this run-layer handoff should delay the Revision 7.1 vertical-slice question. **Rank stacking
> is the only encounter mechanic added now, and it remains behind a flag.**"* The continuous-siege systems
> wait until the encounter prototype proves that card identity, placement, and hit/stand react to
> battlefield state.

---

## The question the prototype exists to answer

> Does the player face a real, recurring, non-obvious choice about **what a card becomes and where it
> goes** — and does the hit/stand decision in the **14–19 band** change with the battlefield?

The **first clause is the primary claim.** The second is the secondary one.

Revision 6 asked whether drawing at total 8 could be made tense. **That question is withdrawn.**

---

## In scope

- Two lanes, three sockets each, one shared junction
- Neutral 26-card shoe; every card may become **Club or Spade** at full effect
- Family locked at placement; **single-move adjustment window**
- **March Clock as a config preset — flat, soft escalation, and hard escalation all shipped in the first
  build** — plus the exactly-21 pullback
- **Visible Threat and Final Forecast as separate, separately labelled outputs**
- Run links only
- Formation Strength ×0.80–1.60
- Persistence with ×1.00 reversion
- Forced replacement at capacity
- Dealer as pure wave generator, standing on all 17s, resolving on bust
- Bust with capped Overload
- Lane stakes: **Bastion and Vault**
- Four enemy types
- Standing orders
- Deterministic, skippable combat
- **Hit and stand only**
- **Rank stacking — behind a flag, default off** (`../design/05-battlefield.md` § Rank stacking)

### The improved-encounter additions

The Improved Encounters Handoff adds work **inside** the encounter rather than beside it. Its own § 24
constrains this list as hard as anything in § Cut below.

| Added | Where |
|---|---|
| **The encounter timeline** — the primary visual language for tactical consequence | `../design/14-encounter-timeline.md` |
| **Exact committed-state resolver statistics** per lane | `../design/14-encounter-timeline.md` |
| **Counterfactual deltas** after a card is committed | `../design/14-encounter-timeline.md` |
| **Four tower forms** — Barrage / Siege Club, Snare / Ambush Spade | `../design/04-cards-as-defenses.md` |
| **Snare → bunch → Barrage** and deterministic bunching | `../design/06-dealer-and-enemies.md` |
| **Spatial breakpoint enemies** — Standard Bearer, Saboteur, Siege Engine, Lane-Switching Raider | `../design/06-dealer-and-enemies.md` |
| **The hidden card's visible destination lane** | `../design/06-dealer-and-enemies.md` |
| **Wave 2 authored to counter-rotate Wave 1** | `../design/05-battlefield.md` |
| **One optional opportunity unit** per encounter | `../design/06-dealer-and-enemies.md` |
| Standing orders **editable throughout, visible on the timeline** | `../design/05-battlefield.md` |

**The four tower forms are not a scope increase on top of four families — they are a partial refund of the
breadth Hearts and Diamonds took with them.** That framing is load-bearing: if they are read as "more
complexity," the first instinct under pressure will be to cut them back to two, which restores the hole
they were added to fill.

**Opportunity-unit payouts are encounter-local, and Favor stays out.** The Paymaster pays Favor, so it is
**deferred to the run layer**; the Supply Courier and Standard Wagon are rewritten to pay inside the
encounter — a cancelled reinforcement group, a buff that never activates. **Do not build Favor to support
an opportunity unit, and do not invent a substitute currency**, which would mean building an economy to
test a placement question (Known Discrepancy 13).

### Explicitly not added

Improved Encounters § 24 names nine things the encounter pass does **not** introduce, and the reasoning
behind the list matters more than the list: **the diagnosis is insufficient causal consequence, not
insufficient feature count.**

Baseline next-card preview · additional blackjack actions · more formation multipliers · arbitrary socket
stat bonuses · generalized enemy tower destruction · live combat clicking · more than four prototype tower
forms · player-facing optimal-play recommendations · a combined tactical utility score.

> ⚠ **"Arbitrary socket stat bonuses" collides with the shipped range-by-socket remedy.** Resolved for
> now in favour of the measurement: **range-by-socket stays authoritative**, breakpoints are a separate
> hypothesis, and the question is settled later by a four-step isolated experiment —
> `../reference/tuning-constants.md` § Known Discrepancies, entry 12.

### The one addition from the run layer: rank stacking

Two same-rank towers may share a socket, depth 2, no Aces, no power bonus, no run eligibility.

**It ships off.** The arms are run **with stacking disabled first**, then the same fixtures are repeated
with it enabled — the sequence is in `VALIDATION.md` § Rank-stacking sequence. A flag that defaults on
would fold a new variable into the March Clock measurement, which is the one measurement the prototype
exists to take.

> Rank stacking is in scope because it creates a **second placement archetype**, not because it relieves
> socket pressure. If it reads as a forced-replacement escape valve, that is the failure, not the feature.

---

## Cut from prototype

| Cut | Reason |
|---|---|
| Printed native suits | Not needed to test placement decisions |
| **Hearts, Diamonds** | Two families suffice; Diamonds' path extension is a full-game system |
| Off-suit keyword loss | Requires printed native suits |
| **Dealer total comparison** | **Suspended as a diagnostic** — see `RISKS-AND-ADDBACKS.md` |
| Split, Double Down | Post-prototype; intent only |
| Relics, commanders, card modifiers, metaprogression | Progression layer, not the core claim |
| Freeform pathing | Fixed sockets are what create scarcity |
| The Works stake | Third stake type, not needed for triage testing |
| **Wide Formation** | **Deleted, not suspended.** It refunded the march. |
| **Pair links, keyword links, Queen command aura** | **Deleted.** One link rule — runs only. |
| **The entire run layer** | Siege map, fronts, front states, concession, campaign time, Favor, strategic orders, Dealer recruitment, doctrine, Charters, card histories, exhaustion/Reserve. **Deferred, not cut** — see below |
| **Chips** | **Cut from the design, not merely the prototype.** No general-purpose money resource exists |

Note the distinction: **Dealer comparison is suspended** (scheduled to return). **Wide Formation, pair
links, keyword links, and the Queen aura are deleted** (they return only against a measured deficit, and
not in their original form). **Chips are cut outright** — Time replaced them.

### The run layer is deferred, in a stated order

It is not "later, sometime." The production sequence is fixed
(`../ROADMAP.md` § Run-layer sequencing): the encounter vertical slice, then the flag-gated stacking pass,
then a **menu-level siege probe** with no persistent geography simulation, then a four-encounter mini-run,
then the full run slice. **Skipping to persistent geography is the drift to watch for**, because it is the
expensive half and the probe is the half that answers whether the pressure lands at all.

---

## Scope drift warnings

Ten things will feel like small additions and are not:

1. **A second link rule.** Runs-only is a deliberate reduction. Adding pairs back is Add-Back 3 and has a
   trigger condition.
2. **Any bonus keyed on card count.** That is Wide Formation wearing a new hat — the exact refund loop
   Revision 7 removed.
3. **Any payout on beating the Dealer.** That is what basic strategy optimizes, and it is the failure this
   design exists to avoid.
4. **A wider adjustment window.** One move is the baseline by construction — it settles five specification
   questions that per-tower movement leaves open. Extra moves arrive **through relics and commanders**, not
   a raised baseline (`../design/05-battlefield.md`).
5. **A single combined "forecast" number.** There are two contracts answering different questions
   (`../design/05-battlefield.md` § Two Forecasts, Not One). Merging them for convenience is how the
   forecast stops being trusted.
6. **Any cost or bonus attached to a stack.** Depth 2, same rank, no Aces, no power bonus — and **no damage
   penalty.** If stacking proves automatic, the first remedy is a **spatial or cadence** cost tested in
   isolation, never a flat damage tax, and never at the same time as a March change
   (`../design/05-battlefield.md` § Accepted risks).
7. **Any campaign effect that reaches into encounter arithmetic.** Campaign time never modifies March
   entry; no persistent multiplier crosses an encounter boundary. A front may change path length, socket
   layout, route structure, and lane stakes — all resolver *inputs* — and nothing else
   (`../design/11-siege-geography.md`).
8. **A single sortable number on a candidate placement.** `Projected value: 5.1 → 3.4` is a combined
   verdict computed per hover, and it converts placement into brute-force search. Candidate previews carry
   **causal deltas** (`../design/14-encounter-timeline.md` § Candidate placements show causal deltas).
9. **A fifth tower form, or a Family → Mode submenu.** Four direct forms is the tested shape. A fifth is
   the "more than four prototype tower forms" § 24 rules out, and a two-step menu converts one decision
   into two.
10. **Generalized tower destruction.** The Saboteur **disables temporarily**. An enemy roster that can
    permanently eat the board turns placement risk into placement fear, and the fairness contract is what
    makes the deterministic forecast worth trusting.
