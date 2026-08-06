# The Dealer and Enemies

Source: Handoff Revision 7.1, §§ 11, 12.

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

## Dealer personalities (post-prototype)

One per region at launch. Personalities change **draw policy, information, or which units their ranks
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
