# The Dealer and Enemies

Source: Handoff Revision 7.1, §§ 11, 12. **The opposing shoe and public recruitment** are from the Run
Layer Handoff (consolidated), § 6.

---

## The Dealer is a wave generator

> **The Dealer is a wave generator. Their hand is their army. There is no comparison between totals.**

### Why comparison is suspended

Revision 6 deleted the Dealer's ±0.15 Formation Strength swing because busts were immune to it, which made
gambling *more* attractive against dangerous Dealers. It then reintroduced the same incentive in a new
location: beating the Dealer withdrew their Vanguard, losing advanced their army.

> "Maximize the probability of beating the Dealer" is precisely what blackjack basic strategy optimizes.
> Any mechanic that pays out on the comparison pulls play back toward basic strategy no matter what the
> battlefield says — which is the exact failure this whole design exists to avoid.

**Comparison is suspended for the prototype so the battlefield can be tested as the sole driver of
hit/stand.** This is a **diagnostic, not a permanent deletion.** It removes a confound; it does not create
pressure away from basic strategy.

Return is scheduled and specified — see `../prototype/RISKS-AND-ADDBACKS.md` § Add-Back 1. **This is the
risk most likely to be quietly forgotten.**

---

## Dealer cards are enemies

Every card in the Dealer's hand deploys as a unit in the wave.

| Dealer Card | Unit |
|---|---|
| 2–4 | Swarm pack — many, fragile |
| 5–7 | Fast raiders |
| 8–10 | Armored soldiers |
| J | Skirmisher — changes lanes at the route junction |
| Q | Standard bearer — buffs nearby enemy units |
| K | Siege engine — high health, high leak damage, slow |
| A | Herald — an elite at 11, a fragile scout at 1 |

The **upcard is visible from the opening deal and is already standing on the field as the Vanguard.** The
hidden card is a reinforcement the player knows is coming but cannot identify. Every card the Dealer draws
while resolving adds another unit.

> "Dealer shows a King" is no longer an abstract pressure category the interface must translate. It is a
> siege engine at the head of lane two.

This is the whole point of the Dealer redesign. Resist any UI that reduces the Vanguard back to a number.

---

## Resolution order

1. Upcard revealed and **deployed before the opening deal**.
2. One hidden card dealt and removed from the shoe.
3. The player plays under that uncertainty.
4. On stand **or bust**, the Dealer reveals and draws to 17.
5. Every Dealer card deploys.
6. Adjustment window, then lock. *(Skipped on bust — placement locks immediately.)*

**Prototype Dealer rule: stands on all 17s, including soft 17.**

> Because resolution is now purely "deploy," there is no outcome for a bust to dodge. The hidden card was
> always marching.

---

## The opposing shoe and public recruitment (full game)

> **Status: DECIDED.** The Dealer adapts **compositionally** — not through hidden difficulty scaling, and
> not through total-comparison battlefield bonuses. **The Dealer should feel like another commander
> building an army in public.**

### The opposing-shoe contract

The Dealer has a **fixed-size 26-card campaign shoe.** Normal recruitment **never increases its size.**
Every recruitment is a **one-for-one replacement**: one visible candidate replaces one existing Dealer
card.

> A raided King matters because it prevented a specific **4 → King composition shift**, not because it
> removed one card from an ever-growing pile.

Normal recruitment may not permanently exceed the starting count of any elite category beyond explicit
roster caps. **Exact rank and family caps are content tuning; the fixed-size shoe is not.**

### Public recruitment

After relevant encounters, the Dealer receives a **visible recruitment row of three candidate cards.** Each
candidate has a known rank, a known enemy-unit identity, and a visible **replacement target** in the Dealer
shoe. **The Dealer's intended pairing is marked before the player acts.**

| Rule | First-pass contract |
|---|---|
| **Dealer shoe size** | Fixed at **26** under normal campaign recruitment |
| **Recruitment row** | **3** visible candidates, each paired with the Dealer card it would replace |
| **Intent** | The Dealer's preferred candidate/replacement pair is **marked before the player acts** |
| **Cadence** | Normally **1** one-for-one replacement per strategic beat, in phases where recruitment is active |
| **Raid** | Costs campaign time; lets the player destroy, steal, or block **one** visible candidate before recruitment resolves |
| **Adaptation lag** | Phase II responds mainly to **Phase I** build signals; Phase III to Phase II. **No immediate counter-picking after a single encounter** |
| **Target signal** | **Build composition and repeated tactical commitments only.** Never win rate, health, hidden skill estimates, or loss streaks |

### Why raiding is mandatory, not a feature

Without a way to interfere, **Dealer adaptation is experienced as rubber-banding** — the game sees the
player build something fun and manufactures its counter.

**Public recruitment plus raiding converts adaptation into an arms race.** The player may decide a visible
King is worth three hours to remove, or **intentionally allow it** because the current formation handles
siege engines well. That second option is the one that makes it a decision.

Adaptation lag serves the same end: a Dealer that counters last encounter is reacting to the player, and a
Dealer that counters the *phase* is reacting to the build.

### ⚠ This is a structural change from the prototype

**In the prototype the Dealer draws from the player's shoe.** The upcard and hidden card are removed from
the same 26-card pile the player draws from (`08-deck-economy-progression.md` § The shoe), and that shared
pile is what makes the marked-rank display a real reading skill.

The run layer gives the Dealer **its own** fixed 26-card campaign shoe. **These are two different objects
that happen to share a size**, and the second one does not exist yet — `core/Dealer/DealerHand.cs` resolves
against the player's remaining shoe today.

Whether the two shoes stay separate at the encounter layer, and what a separate Dealer shoe does to the
remaining-rank reading skill, is **not settled by either handoff.** Flagged in
`../reference/tuning-constants.md` § Known Discrepancies. Do not resolve it in passing.

---

## Dealer personalities (post-prototype)

One per **siege phase** at launch (Revision 7.1 said "one per region"; regions became phases —
`10-run-structure.md`). Personalities change **draw policy, information, or which units their ranks
produce — never the resolution rule.**

| Personality | Effect |
|---|---|
| **The Countess** | Court cards deploy in pairs. |
| **The Warlord** | Hits soft 18. |
| **The Magician** | Hidden card dealt face-up, but may be swapped once after the player stands. |
| **The Collector** | Towers destroyed in a wave return as enemy units in the next. |

---

## Enemies

| Enemy | Count | Health | Speed | Armor | Spacing | Leak Damage |
|---|---:|---:|---:|---:|---:|---:|
| Swarm unit | 8 | 4 | 1.00 | 0 | 0.45 s | 1 |
| Armored soldier | 3 | 12 | 0.65 | 1.5 flat | 1.50 s | 2 |
| Fast raider | 5 | 5 | 1.60 | 0 | 0.75 s | 1 |
| Siege engine | 1 | 30 | 0.40 | 2.0 flat | — | 5 |

**Armor cannot reduce a hit below 0.25.** Spade traps and Kings **ignore half of flat armor**.

The **base wave** is the encounter's own composition; **the Dealer's hand is added to it.**

> A wave is never fully known until the player stands, but its shape is known from the upcard.

Four enemy types is the full prototype roster.
