# Prototype Scope

Source: Handoff Revision 7.1, § 20.

**This is a boundary, not a suggestion.** Anything in the Cut list is out of scope unless explicitly
requested.

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

Note the distinction: **Dealer comparison is suspended** (scheduled to return). **Wide Formation, pair
links, keyword links, and the Queen aura are deleted** (they return only against a measured deficit, and
not in their original form).

---

## Scope drift warnings

Five things will feel like small additions and are not:

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
