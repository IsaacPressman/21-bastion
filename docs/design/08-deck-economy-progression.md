# Deck, Economy, and Progression

Source: Handoff Revision 7.1, §§ 14, 15, 16.

Most of this is **full-game intent, not prototype scope.** The Shoe and the Thinning Dilemma are live in
the prototype; the rest is context for why prototype systems are shaped as they are.

---

## The shoe

**26 cards: two copies of every rank, Ace through King.**

- The full game assigns printed native suits by commander and archetype.
- The draw pile **persists across the waves of an encounter**.
- **Reshuffle before a wave if fewer than eight cards remain.**

Prototype shoe is **neutral** — every card may become Club or Spade at full effect.

Shoe persistence is what makes the marked rank display (`09-information-and-ui.md`) a real reading skill
rather than decoration.

---

## The thinning dilemma

The central deckbuilding tension. Neither column may become default-correct.

| | Face-heavy | Many-card |
|---|---|---|
| Raw output | Lower | Higher |
| Board width | 2–3 towers | 4–5 towers |
| Run links | Rare (Queen only) | Frequent |
| Engagement | Best | Worst |
| Range and keywords | Best | Weakest |
| Bust rate | Higher | Lower |
| Failure mode | Thin board, few links | Fifth card missed |

**Many-card decks** are a **21-chasing precision build**, with the 3.0-unit pullback as the rescue
condition.

**Face-heavy decks** are a **position-and-range build** that finishes fast and fights on good ground.

> ⚠ **Stale claim.** Revision 7.1 § 14 still reads "the fifth card is worth taking only if it lands
> exactly," but § 4 of the same document **demotes that from design identity to unproven hypothesis** — and
> notes that if it were true it would probably be unhealthy. **§ 4 governs.** The intended shape is
> spectacular on 21, *sometimes defensible* on a safe miss, clearly worse on a bust. See
> `03-march-clock.md` § The fifth card is a hypothesis, and `../reference/tuning-constants.md`
> § Known Discrepancies.

> **Never let "cut your low cards" or "cut your high cards" become default correct play.** If one column
> wins in testing, **adjust the march curve first** — it is the cleanest lever between them.

This is the single most important balance instruction in the design. Note that it names the march curve
specifically; do not reach for a new bonus system.

---

## Acquisition

Standard cards, modified cards, face cards, Aces, Jokers, cursed cards.

The deck screen shows **expected cards per hand, native-suit distribution, board width, and recent link
frequency.** **It does not display a deck score.** (A score is a verdict — see the Reveal Consequences,
Not Conclusions pillar.)

---

## Economy

| Currency | Use |
|---|---|
| **Chips** | Buy cards, remove cards, upgrade defenses, repair the Bastion. |
| **Favor** | Rare. Rerolls, rule manipulation, commander abilities. |
| **Bastion Health** | The run ends at zero. Hard to restore. |

### How Favor is earned

**By risk taken and lanes held — not by hand quality.** Specifically: holding a lane the forecast called
Open, and standing on a hand you could have improved.

> Rewarding good hands would pay the player twice for the same thing and make strong hands snowball.

**Reward size scales with lanes held, not output produced.** Output is already its own reward; paying for
it again is the snowball failure mode that persistence multipliers were removed to avoid.

---

## Progression content

> **Deliberately under-specified. Every item below is intent, not specification.**

### Card modifiers

Roughly **20 at launch**, weighted toward effects that change **battlefield behavior without touching
blackjack arithmetic**: piercing, explosive, chaining, reinforced, echoing, anchoring, longer slow,
alternate targeting.

A smaller set operates on the hand through a **bounded interface**: value ±1, an extra Ace state, one
discard-and-redraw, one rank preview.

> Anything that changes bust thresholds, Formation Strength, or Dealer resolution is a **rule package**,
> not a modifier — rare, possibly mutually exclusive, closer to a game mode.

### Relics

*All values unpriced.*

| Relic | Effect |
|---|---|
| **True Colors** | One off-suit card counts as native per wave. |
| **Card Counter** | Reveals a band for the Dealer's hidden card. |
| **Steady Table** | The first bust of a region does not destroy the card. |
| **Surveyor** | Adds one socket to each lane. |
| **Bridge Builder** | One card counts as wild in runs. |
| **Soft Landing** | One Ace-state intervention per encounter. |
| **Long Road** | Reduces the march curve for one encounter. |
| **Field Promotion** | One family reassignment per encounter. |

*Field Promotion* is the sanctioned escape hatch for family locking, should testing show it is too
punishing.

### Commanders

Each has a starting shoe, a passive, and a **distinct decision texture**. **At most one launch commander
may alter the Formation Strength curve.** Others differentiate through native-suit distribution, socket
layout, march economy, information access, or tower behavior.

> A commander is a skin only if it produces the same decisions on the same battlefield.

### Metaprogression

**Unlocks expand possibility, not raw power.** No permanent damage bonuses that make early runs feel
intentionally underpowered.
