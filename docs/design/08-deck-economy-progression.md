# Deck, Economy, and Progression

Source: Handoff Revision 7.1, §§ 14, 15, 16. **Economy, reward verbs, card identity, and exhaustion** are
from the Run Layer Handoff (consolidated), §§ 4, 7, 10 — which **supersedes 7.1's economy outright**.

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

> ⚠ **The Dealer gets its own 26-card campaign shoe in the full game** — a *different object* that happens
> to share a size, built by visible one-for-one replacements (`06-dealer-and-enemies.md` § The opposing
> shoe). In the prototype the Dealer draws from **this** pile. How the two coexist is unresolved; see
> `../reference/tuning-constants.md` § Known Discrepancies.

**Rank count is sacred.** The campaign may change a card's character, history, family, modifier, or
availability — but **nothing the enemy does silently alters blackjack rank distribution.** That constraint
is what forces the Reserve rule in § Exhaustion below, and it is why **Cut is a player verb only.**

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

## Acquisition and the reward verbs

Standard cards, modified cards, face cards, Aces, Jokers, cursed cards.

The deck screen shows **expected cards per hand, native-suit distribution, board width, and recent link
frequency.** **It does not display a deck score.** (A score is a verdict — see the Reveal Consequences,
Not Conclusions pillar.)

The baseline progression verbs are **Acquire, Cut, Temper, Repaint, Promote, and Rerank ±1.** **Bind
remains cut.** These are delivered through **campaign orders, consequences, captured supplies, and named
services** — not through a generic post-combat card reward every time
(`12-campaign-time-and-orders.md`).

| Verb | Meaning | Why it is interesting |
|---|---|---|
| **Acquire** | Add a rank/card to the shoe | Improves one tactical option **while changing future blackjack distribution** |
| **Cut** | Remove a chosen card permanently | Chosen probability surgery. **Never inflicted casually by enemies** |
| **Temper** | Add or change the card's **one** allowed modifier | Changes battlefield behavior without stacking endless upgrades |
| **Repaint** | Change native family | Changes deck-family structure **without changing blackjack rank** |
| **Promote** | Grant a named battlefield behavior unlocked by the card's history | Turns memorable play into future identity |
| **Rerank ±1** | Change rank by one | Weakens or strengthens **tower power, run structure, and blackjack distribution at once** |

Note that Acquire, Cut, and Rerank are the three that move rank distribution, and all three are **player
choices**. That is the sacred-rank-count rule expressed as a verb list.

---

## Card identity: histories, promotion, and exhaustion

### Histories

Cards may accumulate **named history tags from resolver events** — *Held North Gate During the First
Breach*, *Broke the Dealer's Siege Engine*.

> **Histories do not automatically grant power or experience levels.** They create **eligibility** for
> future Promote choices.

**Each card may carry at most one gameplay modifier**; history can remain as flavor beyond that cap. A
history that paid out automatically would be an XP system, and an XP system makes early cards
mechanically obsolete rather than differently useful.

### Exhaustion without rank loss

> **Superseding rule.** An exhausted veteran is **replaced for the next encounter by a Reserve copy of the
> same rank.** The shoe keeps the same rank counts, bust probabilities, and run distribution; only the
> card's **special identity** is temporarily absent.

- **One exhaustion state only: Fresh or Exhausted.** It does not stack toward injury or death.
- A **Reserve copy** has the same blackjack rank and base tower power, but **no modifier, no native-family
  bonus, no veterancy effect, and no history-triggered promotion behavior.**
- The original returns **after one encounter** unless a special effect says otherwise.
- **Enemy-inflicted permanent capture is rare, telegraphed, and recoverable.** A captured card is
  represented by a Reserve of the same rank until the original is recovered — **again preserving rank
  count.**

This is the cleanest illustration of *rank count is sacred*: the enemy can take your best 7's identity, and
cannot take a 7 out of your shoe. Losing a card would change bust probability, which would let enemy
pressure edit blackjack — the one thing the campaign layer may never do.

---

## Economy

> **Chips are cut.** There is no general-purpose money resource in the baseline run. Full treatment in
> `12-campaign-time-and-orders.md` § Campaign resources.

| Resource | Job | Never used for |
|---|---|---|
| **Time** | Ordinary campaign actions — Fortify, Muster, Train, Raid, Reconnoiter, preparation | Tactical rule-breaking or emergency encounter manipulation |
| **Favor** | Rare command authority: bend one encounter rule in a bounded way. **First-pass cap: 3** | Routine repairs, card acquisition, reranking, or ordinary services |
| **Bastion Health** | Measures how close the run is to defeat. Hard to restore | Buying upgrades or paying for strategic orders |

Chips and Time were the same job with two mechanisms. Time won because **spending it advances the siege**,
so a purchase has a battlefield consequence rather than only an opportunity cost.

### How Favor is earned

**By voluntarily accepting meaningful pre-resolution risk and successfully protecting important stakes —
not by hand quality.**

> Rewarding good hands would pay the player twice for the same thing and make strong hands snowball.

**Not awarded** for reaching 20/21, for high Formation Strength, or for outperforming a Final Forecast —
which, being exact, cannot be outperformed at all.

Prototype-eligible triggers: standing while the **Visible Threat** still shows a Bastion lane **Open** and
then holding it after the reveal; a flagged high-risk hand decision that finishes with **no Bastion
leakage**; accepting a costly **forced replacement** and preserving the threatened stake.

⚠ **Favor is not the reward floor.** Every encounter has a reward floor so that poor combat and siege-state
variance cannot compound into a spiral — but it is paid in ordinary campaign terms, never in Favor.

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

### Relics — superseded by Doctrine

⚠ **The relic *layer* is cut. The named effects are not.**

The run layer makes **Doctrine** the persistent placement-layer progression: four to seven
**behavior-changing globals** per run, built over one or two encounters, with **"twenty passive percentage
relics" as the explicitly named failure mode.** Full treatment and the forward mapping of every 7.1 relic —
including which one is now suspect and which one is load-bearing — is in `13-doctrine-and-charters.md`.

Two carried forward here because they are cited elsewhere:

- **Surveyor** (one extra socket per lane) is load-bearing wherever it now lives, because extra sockets
  **unlock the 4-run tier** that prototype geometry cannot reach (`04-cards-as-defenses.md`).
- **Field Promotion** becomes the **Field Reassignment** doctrine — still the sanctioned escape hatch for
  family locking, and bounded further: first card per lane, after the Dealer reveal only.

### Commanders

Each has a starting shoe, a passive, and a **distinct decision texture**. **At most one launch commander
may alter the Formation Strength curve.** Others differentiate through native-suit distribution, socket
layout, march economy, information access, or tower behavior.

> A commander is a skin only if it produces the same decisions on the same battlefield.

### Metaprogression

**Unlocks expand possibility, not raw power.** No permanent damage bonuses that make early runs feel
intentionally underpowered.
