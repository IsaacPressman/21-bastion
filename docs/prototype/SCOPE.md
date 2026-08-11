# Prototype Scope

Source: Handoff Revision 7.1, § 20, plus the Run Layer Handoff (consolidated), §§ 11, 13.

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

### The one addition: rank stacking

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

Seven things will feel like small additions and are not:

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
