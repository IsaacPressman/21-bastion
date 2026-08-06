# 21 Bastion

## Gameplay Design Handoff — Revision 7.1

*Revision 7.1 is a correction pass, not a structural revision. An arithmetic error in the March Clock understated the fifth card's cost, and the resulting output estimates were used to assert a design identity that is now demoted to a hypothesis. The direction of the march's placement bias was stated backwards. The adjustment window was far more permissive than intended. And the forecast was described as one contract when it is two. Sections 4, 6, 8, 10, 17, 18, 20 and 22 are affected. See Section 24.*

*Revision 6 proposed a set of systems that individually solved real problems and collectively cancelled each other out. Wide Formation refunded the March Clock. Bust dodged the Dealer. Vanguard withdrawal reintroduced basic strategy through the door the ±0.15 swing had left by. Revision 7 removes every one of those loops, narrows the claim the design is making, and states plainly what the prototype is and is not testing. It also fixes the march's geometry, which was taxing the wrong thing.*

---

## 0. What Changed in How We Are Thinking

Revision 6 claimed that a draw at total 8 could be made tense. That claim is withdrawn.

The March Clock was sized against path length rather than socket spacing, so a single step shaved one tower's engagement window and left the other two untouched — a 5.6% cost against a third tower worth roughly 50% of board power. Correcting the step size to bite at low totals makes five-card hands unplayable. That window is very narrow, and building inside it was a mistake.

**The narrowed claim:**

> Decision density comes from what a card becomes, where it goes, and what it displaces. Hit/stand is a live decision in the 14–19 band — roughly where blackjack has always put it — and the design's job is to make that band's stakes battlefield-specific, not to manufacture tension at 8.

This is a smaller claim than Revision 6 made. It is also one the prototype can actually test.

The March Clock is retained, reshaped, and given a different job: it prices *hand length* on an escalating curve so that the fifth card is a deliberate, dangerous, precision play rather than a default. It is no longer asked to make low totals tense.

Every number in this document is first-pass and expected to be wrong. No number carries a confidence interval, validity window, or tolerance. Those are outputs of playtesting.

---

## 1. High Concept

**21 Bastion** is a roguelite tower-defense game in which the player builds each wave's defenses by playing blackjack.

Every drawn value becomes a physical defense. The player chooses what each card becomes and where it stands, and lives with that choice. The hand's total sets formation-wide power. The Dealer's hand is the army walking toward you.

**Core promise:**
Build the perfect defense without going over 21.

**Central tension:**
Every card is another defense — but the army is already marching, your sockets are already full, and what you commit during the draw is what you fight with.

---

## 2. Design Pillars

### Blackjack Builds the Battlefield

Every blackjack decision must directly change tower placement, defensive power, or enemy position. Blackjack is never a separate minigame.

### Commitments Are Made Under Uncertainty

What a card becomes is decided when it is drawn, before the wave is fully known. Information arriving later permits adjustment, not re-solving. A player who defers every decision until everything is revealed is not playing the game.

### Reveal Consequences, Not Conclusions

Hand consequences and battlefield consequences are shown separately. The game never displays a combined recommendation, an optimal-play percentage, or a green/red verdict. Combining them is the player's job.

### Placement Must Rival the Hand

If the hand multiplier swings output more than every placement decision combined, the tower-defense layer is decoration. Socket scarcity, run links, forced replacement, and lane triage exist to keep the two layers comparable.

### One System Per Job

Revision 6 had three systems keeping many-card decks viable and two making drawing costly, and they fought. Each pressure in this design has exactly one mechanism behind it. When a mechanism is added, the one it duplicates is removed.

### Randomness Creates Adaptation

Bad draws create difficult decisions, not automatic losses. A poor card always has a use, even an inefficient one.

---

## 3. Core Gameplay Loop

### Before a Wave

1. Reveal lane stakes, the base wave, and the Dealer's Vanguard — the upcard, already deployed as a unit on the field.
2. Deal the opening two cards. The march has not begun.
3. **Place each card: choose its family and socket. Family is now locked.**
4. Choose to hit or stand.
5. **Each hit advances the army by an escalating march step**, paid before the card is revealed.
6. Place the new card, replacing an existing tower if sockets are full.
7. On stand, the Dealer resolves: hidden card revealed, draws to 17, every card deployed as a unit.
8. **Adjustment window:** one tower relocates one socket, or two adjacent towers swap. One move total. Standing orders may be set or changed freely. Families are fixed. No further draws.
9. Lock and resolve.

If the player busts, the busting card is destroyed, Overload fires, placement locks, **the Dealer still resolves in full**, and combat begins.

### Combat

A deterministic resolution of a fully previewed state, not an input phase. No critical hits, misses, or random targeting. Watchable, fast-forwardable, skippable. Standing orders execute automatically. A regular wave resolves in roughly 12–20 seconds at normal speed.

The forecast is exact because nothing happens live that could invalidate it.

### After a Wave

1. Review which lanes leaked, by how much, and why.
2. Between waves of an encounter, towers and shoe state persist. **Persisted towers revert to ×1.00 Formation Strength.**
3. After an encounter, take a reward and choose a route.

---

## 4. The March Clock

### The Job It Now Does

The March Clock prices hand length. It does not make low totals tense — nothing can, at a step size that leaves long hands playable.

### The Geometry Problem It Had

With sockets at path positions 3, 6, and 9 and a range of 3.0 units, each tower's engagement window is:

| Socket | Window | Length |
|---|---|---:|
| 3 | 0–6 | 6.0 |
| 6 | 3–9 | 6.0 |
| 9 | 6–12 | 6.0 |
| **Total** | | **18.0** |

Advancing the army's entry point by 1.0 unit eats one unit from the socket-3 tower and *nothing* from the other two, because their windows begin at or after the new entry point. Total engagement falls from 18.0 to 17.0 — a 5.6% cost.

Worse, that cost is not a tax on drawing. Because entry advances from the spawn side, it eats the **forward** socket's window first and the rear socket's last — so it is a tax on forward placement, which a player avoids by building deep. The intended pressure was close to inverted.

**A consequence worth flagging before any build.** At entry 0 all three sockets give identical engagement. Every unit of advancement degrades forward sockets while leaving rear ones untouched, so **deep placement is weakly dominant whenever entry exceeds 0**, and more so as the clock bites harder. A mechanic added to enrich placement may be flattening it.

Run-link adjacency, the junction socket, traps that need early application, and enemies that must be stopped before a leak threshold all push back — but none of that pushback lives in the engagement arithmetic. It lives in the resolver. This is the first thing to measure once the resolver runs: if deep placement wins everywhere, the socket geometry needs work before the march curve does.

**Correction: the step must be comparable to the 3.0-unit socket spacing to consume whole windows rather than shaving one.**

### The Rule

* Path length 12 units. Enemies normally enter at position 0.
* The opening two cards are free.
* Each subsequent card advances the entry point by an **escalating** step, paid at the moment of the draw, before the card is revealed.

| Card | Step | Cumulative Entry | Engagement Remaining | Cost |
|---|---:|---:|---:|---:|
| 3rd | +1.5 | 1.5 | 16.5 | −8% |
| 4th | +2.5 | 4.0 | 13.0 | −28% |
| 5th | +3.5 | 7.5 | 6.0 | −67% |

The third card is cheap, which is correct — a third tower is worth far more than 8% of engagement. The fourth is a real decision. The fifth is close to lethal.

### Exactly 21 Pulls the Army Back

**Reaching exactly 21, at any card count, pulls the entry point back 3.0 units**, clamped at 0.

| Hand | Entry | Engagement | Cost |
|---|---:|---:|---:|
| 3-card 21 | 0.0 | 18.0 | none |
| 4-card 21 | 1.0 | 17.0 | −6% |
| 5-card 21 | 4.5 | 12.0 | −33% |

This is the design's most dramatic moment and it does a great deal of structural work. A fifth card taken and missed is punishing; a fifth card taken and *landed* converts a near-lethal position into a wide, heavily linked, ×1.60 board. Long hands become a precision play with a visible rescue condition, rather than an archetype that gets a flat bonus for existing.

### Total Engagement Is Explanatory, Not a Balance Number

Revision 7 multiplied board power by a total-engagement fraction to estimate output, and used the result to assert that the fifth card is worth taking only if it reaches 21. **That estimate is withdrawn.**

Summed engagement is a scalar over a board whose sockets are not interchangeable. Advancement removes different amounts of coverage from different sockets, and three units taken from a 5.0-power King is not three units taken from a 1.6-power two. A single fraction cannot express that, and it should never have been multiplied into an output figure.

**Use total engagement to explain the clock to the player and to the team. Balance through the resolver.** The march curve's real cost is whatever the resolver reports as changed lane leakage, measured per configuration, not what a fraction predicts.

### The Fifth Card Is a Hypothesis, Not an Identity

Revision 7 stated as design that the fifth card is worth taking only if it reaches 21. That claim was produced by the withdrawn scalar, so it is unproven — and if it turned out to be true, it would probably be unhealthy. A strictly binary fifth card reduces the decision to a rank count:

* Exact 21 — rescued
* Safe miss — functionally dead
* Bust — worse

In that shape the battlefield only sets how desperate the player is, and the choice collapses into counting the one rank that saves them.

**The target shape instead:**

* Exact 21 is spectacular.
* A safe miss is usually bad, but sometimes defensible — because of a run it completes, a replacement it avoids, a family the lane needs, or a stake worth less than health.
* A bust is clearly worse than both.

Whether the −67% curve permits that middle outcome is an open question and probably the most important thing the prototype measures. See Section 20.

### Diamonds

In the full game, Diamond structures extend path length, which reduces the proportional cost of every march step. This is Diamonds' primary strategic identity and the main counterplay to hand length. Not in prototype.

---

## 5. Blackjack System

Baseline rules remain recognizable blackjack. Number cards use their printed value, face cards count as 10, Aces are 1 or 11. Choosing a defense family never changes blackjack value or shoe composition.

**Hit** — advance the march, then draw. Place the card, replacing an existing tower if sockets are full.

**Stand** — end drawing. The Dealer resolves and deploys in full, then the adjustment window opens.

**Split, Double Down** — post-prototype. Intent only; no numbers, no implementation contract.

**Surrender, Insurance** — cut permanently.

---

## 6. Formation Strength

| Final Total | Formation Strength |
|---|---:|
| 21 (any card count) | ×1.60 |
| 20 | ×1.50 |
| 19 | ×1.40 |
| 18 | ×1.30 |
| 17 | ×1.20 |
| 16 | ×1.15 |
| 15 | ×1.10 |
| 14 | ×1.05 |
| 13 | ×1.00 |
| 12 | ×0.95 |
| 11 or below | ×0.90 |
| Bust | ×0.80 |

The curve spans 2.0×, against run links that can add roughly 35% to a well-placed board and an engagement range spanning 18.0 down to 6.0. The hand matters most on any single card; the board and the clock matter more across a full formation.

### Perfect Formation

Exactly 21 pulls the army back 3.0 units. That is the entire bonus. There is no separate multiplier bump, attack-speed bonus, or card-count table.

### Wide Formation — deleted

Revision 6 granted +10% attack speed per card beyond the third. Against a march that cost 17–28% of engagement at those same card counts, it was very nearly an exact refund — the two systems were fighting over identical hands, at precisely the length where the march was supposed to bite.

Many-card decks now earn their keep through board width, run links, and the 21 pullback. If that proves insufficient, the fix is the march curve, not a new bonus.

### Standing Low

Standing below 17 is legal and costs real output. It is correct when the formation already answers the wave, when sockets are full and the forced replacement is bad, when the march step would cost more than the marginal tower gains, or when a relic rewards low totals.

---

## 7. Cards as Defenses

### Family Is Locked at Placement

**When a card is drawn, the player chooses its family, and that choice is permanent for the wave.**

This is the design's primary commitment. Family is chosen under uncertainty — before the hidden card deploys, before the hand is complete — and cannot be undone once the wave is known. A player who could reassign families after full reveal would place carelessly during the draw and solve the puzzle at the end, which empties the entire draw phase of consequence.

One tower's position remains adjustable in the adjustment window (Section 10). Family does not.

### Off-Suit Deployment

In the full game, a card may be deployed into any unlocked family. **Native deployment gives the full family behavior; off-suit deployment gives the generic form — full power, but no family keyword, no native synergy, no family-exclusive upgrades.**

> You can always cover a lane. You can only *solve* it natively.

An off-suit Spade damages but does not slow. An off-suit Club fires but does not splash. One cost, expressed in behavior rather than arithmetic. Not in prototype; the prototype shoe is neutral and every card may become Club or Spade at full effect.

### Suit Identities

**Hearts — Troops.** Guards, archers, medics, patrols. *Keyword: mobile.*

**Diamonds — Construction.** Walls, barricades, extractors. Extend path length. *Keyword: extend.*

**Clubs — Artillery.** Cannons, mortars, ballistae. *Keyword: splash.*

**Spades — Traps and Control.** Spikes, tar, poison, route switches. *Keyword: slow.*

### Face Cards

Face cards are all value 10, so they cannot form runs with each other. They buy their advantage through properties low cards cannot stack into:

**All 10/J/Q/K** — Range 4.0 instead of 3.0, and may occupy the shared junction socket without the usual contribution penalty.

**Jack** — Mobile. Relocates to an adjacent socket once mid-wave, automatically, when nothing is in range.

**Queen** — **Wild in runs.** A Queen counts as any value for the purpose of forming a run link. She is the only way a face card joins a run, and the only bridge across a gap in a sequence.

**King** — Anchor. Ignores half of flat armor; cannot be displaced.

### Aces

Aces count as 1 or 11 and mirror that on the field: 1.0 power compact utility, or 5.4 power formation-defining. A hit that forces an Ace from 11 to 1 transforms the battlefield object immediately, and the forecast updates before commitment. Aces count as 1 or 11 for runs, matching their current state.

---

## 8. Card Power Curve

Tower power is sublinear in card value, approximately value^0.7.

| Value | A(1) | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10/J/Q/K | A(11) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Base power | 1.0 | 1.6 | 2.2 | 2.6 | 3.1 | 3.5 | 3.9 | 4.3 | 4.7 | 5.0 | 5.4 |

A ten is five times the blackjack value of a two but only three times the tower power.

### Output Landmarks

Raw output before links and engagement, first-pass:

| Hand | Calculation | Output | Entry |
|---|---|---:|---:|
| Natural A + K, plus Ace Bastion | (5.4 + 5.0 + 5.0) × 1.60 | 24.64 | 0.0 |
| 2 + 3 + 4 + 5 + 7 = 21 | 13.4 × 1.60 | 21.44 | 4.5 |
| 6 + 7 + 8 = 21 | 11.7 × 1.60 | 18.72 | 0.0 |
| 2 + 4 + 6 + 8 = 20 | 12.0 × 1.50 | 18.00 | 4.0 |
| K + Q = 20 | 10.0 × 1.50 | 15.00 | 0.0 |
| 6 + 8 + 4 = 18 | 10.4 × 1.30 | 13.52 | 1.5 |
| 10 + 6 = 16 | 8.5 × 1.15 | 9.78 | 0.0 |
| 3 + 3 + 5 + 5 = 16 | 10.6 × 1.15 | 12.19 | 4.0 |

The last pair remains the design's signature comparison: two hands totaling sixteen, one with 25% more raw output and the other with 38% more engagement. They should never play the same, and now they fail differently too.

**These are raw-output landmarks only.** Do not multiply them by an engagement fraction to estimate board effectiveness — see Section 4. Entry position is listed beside each hand as context for reading resolver output, not as a factor to apply.

Natural blackjack grants the **Ace Bastion** — a free 5.0-power King-class anchor that does not count as a hand card and shares the hand's multiplier.

---

## 9. Run Links

**One link rule. Runs only.**

Revision 6 had pairs at +20%, two-runs at +15%, shared keywords, and a Queen command aura. Pairs and two-runs were nearly the same effect wearing different trigger conditions. Keywords and auras were a second subsystem inside a mechanic that had not been tested once.

A pair is a rank coincidence you notice. A run is something you can draw toward and build across a hand — it connects placement back to the sequence of cards, which is the reason links exist at all.

### The Rule

Consecutive card values in adjacent sockets form a run. Direction does not matter. Aces count as 1 or 11 matching their current state; a Queen is wild.

| Run Length | Effect |
|---|---|
| 2 | +15% power to both towers |
| 3 | +25% power to all three |
| 4 | +35% power to all four |

Runs are computed at lock, shown in the forecast, and fully deterministic.

### Why This Matters

A 5 placed next to your 6 is worth substantially more than a 5 placed anywhere else, and the player can see that before deciding whether the march step is worth paying. Links are the main reason a card's *identity* matters spatially rather than only as a number added to a sum, and they are what keeps a three-tower board from being a trivial arrangement.

They are also the primary support for low-value cards, which protects the thinning dilemma without inflating base power.

---

## 10. Battlefield

### Sockets and Scarcity

* Two lanes
* **Three sockets per lane**, at path positions 3, 6, 9
* **One shared junction socket**, firing into either lane at reduced contribution
* Seven sockets total

**At capacity, a non-busting drawn card must replace an existing tower before the player may stand.** The removed tower's power, links, and locked multiplier go with it. The player may never bank an improved total while leaving a card unplaced.

Forced replacement is one of the three things the game's decision density actually rests on. It is not a safety valve.

### Persistence

Towers persist across the waves of an encounter and reset at the encounter boundary. **Persisted towers revert to ×1.00 Formation Strength at the start of the next wave.** They keep their base power, family, and socket; they lose the multiplier their hand earned.

Revision 6 locked each hand's multiplier onto its towers permanently, which produced snowball, a screen full of tower groups at different multipliers, and a bust that could drag six towers down instead of three — pushing late waves toward automatic stands.

Reverting to ×1.00 keeps exactly one live multiplier on screen at a time, removes the snowball, keeps bust scoped to the current hand, and lets old towers decay gently in relevance while the current hand stays the point. It also preserves the thing persistence exists for: sockets fill during the second wave, and every card after that forces a replacement.

### The Adjustment Window

After the Dealer resolves and the full army is visible:

* **One move total:** relocate a single tower to an adjacent empty socket, *or* swap two adjacent towers. Not both, and not per tower.
* **Standing orders may be set or changed freely.**
* **Families are locked. No reassignment.**
* No further draws.

Revision 7 allowed every tower to shift one socket. On a full seven-socket board that is close to full-board revision after complete information, which would have made the adjustment window the real placement puzzle and the draw phase provisional. It also left five specification questions unanswered — whether a swap consumes both towers' moves, whether sequential swaps can carry a card further than one socket, whether shifts resolve in order or simultaneously, whether a lane can be rotated, and whether persisted towers move on equal terms. Every answer produces a different game.

One global move answers all five by construction. It is enough to absorb a bad hidden reveal and to create a tactical beat — a single move can still make or break a run link — and it cannot rebuild a board.

If one move proves too tight, the expansion path is **adjustment points granted by relics and commanders**, not a higher baseline. Test one-move against Revision 7's every-tower version only after the baseline has been played.

### Lane Stakes

Lanes are not interchangeable. Each encounter assigns stakes, shown before the opening deal:

| Stake | Effect of a Leak |
|---|---|
| **Bastion** | Direct Bastion health damage. The lethal lane. |
| **Vault** | Chips and Favor lost from this encounter's reward. |
| **Works** | A placed tower is destroyed and does not persist. *Full game.* |

A player who is healthy but poor triages differently from one who is rich and nearly dead. Boss encounters use Bastion stakes in every lane.

### Standing Orders

Because combat has no live input, the adjustment window offers pre-committed conditionals:

* **Hold** — fire only at enemies past a chosen socket.
* **Focus** — prefer armored targets, or prefer the leading target.
* **Trigger on group** — a trap waits for a minimum number of enemies in radius.

Modeled exactly by the resolver and shown in the forecast.

### Resolver and Forecast

One deterministic resolver drives both forecast and wave: same spawn schedule, health, armor, speed, paths, range, cooldown, targeting, and rounding. Ties resolve by spawn order.

Per lane it outputs empty-lane damage, predicted damage under the current plan, damage prevented, per-tower activity, and the cause of remaining leakage.

### Two Forecasts, Not One

During the draw the game cannot forecast the final wave, because the Dealer's hidden card and subsequent draws have not resolved. Revision 7 described a single contract and then showed the number changing mid-example, which is exactly the behaviour that destroys trust in it.

There are two distinct outputs and they must be named differently in the interface and typed differently in the code.

| | **Visible Threat** | **Final Forecast** |
|---|---|---|
| When | During the draw | After Dealer resolution |
| Modelled against | Base wave plus Vanguard — the revealed army only | The complete army |
| Guarantee | Exact against what is currently on the field | Exact against the wave that will run |
| Is it a prediction of the wave? | **No** | **Yes** |

**Only the Final Forecast is the combat contract.** If it says a lane leaks two, the wave leaks two.

Visible Threat is exact about a smaller question, and the interface must say so plainly — it is what the currently revealed force would do, not what the wave will do. Players who read it as a promise will feel the game break it when reinforcements land.

*Implementation:* these are separate return types from the resolver, not the same type with a flag. A Visible Threat must not be renderable in a slot expecting a Final Forecast. Trust in the forecast is a foundational claim of this design; a number that silently changes meaning mid-hand is the cheapest possible way to lose it.

### Coverage Display

Show the predicted leakage number per lane and color it. Two words on a plain threshold:

* **Open** — predicted leakage is at least half of empty-lane damage.
* **Held** — below that.

The number is primary; the label is a glance-read.

---

## 11. The Dealer

**The Dealer is a wave generator. Their hand is their army. There is no comparison between totals.**

### Why Comparison Is Suspended

Revision 6 deleted the Dealer's ±0.15 Formation Strength swing because busts were immune to it, which made gambling more attractive against dangerous Dealers. It then reintroduced the same incentive in a new location: beating the Dealer withdrew their Vanguard, losing advanced their army.

"Maximize the probability of beating the Dealer" is precisely what blackjack basic strategy optimizes. Any mechanic that pays out on the comparison pulls play back toward basic strategy no matter what the battlefield says — which is the exact failure this whole design exists to avoid.

**Comparison is suspended for the prototype so the battlefield can be tested as the sole driver of hit/stand.** This is a diagnostic, not a permanent deletion. It removes a confound; it does not create pressure away from basic strategy. Section 21 schedules its return.

### Dealer Cards Are Enemies

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

The **upcard is visible from the opening deal and is already standing on the field** as the Vanguard. The hidden card is a reinforcement the player knows is coming but cannot identify. Every card the Dealer draws while resolving adds another unit.

"Dealer shows a King" is no longer an abstract pressure category the interface must translate. It is a siege engine at the head of lane two.

### Resolution

1. Upcard revealed and deployed before the opening deal.
2. One hidden card dealt and removed from the shoe.
3. The player plays under that uncertainty.
4. On stand *or bust*, the Dealer reveals and draws to 17.
5. Every Dealer card deploys.
6. Adjustment window, then lock.

**Prototype Dealer rule:** stands on all 17s, including soft 17.

Because resolution is now purely "deploy," there is no outcome for a bust to dodge. The hidden card was always marching.

### Dealer Personalities

Post-prototype, one per region at launch. Personalities change draw policy, information, or which units their ranks produce — never the resolution rule.

* **The Countess** — court cards deploy in pairs.
* **The Warlord** — hits soft 18.
* **The Magician** — hidden card dealt face-up, but may be swapped once after the player stands.
* **The Collector** — towers destroyed in a wave return as enemy units in the next.

---

## 12. Enemies

| Enemy | Count | Health | Speed | Armor | Spacing | Leak Damage |
|---|---:|---:|---:|---:|---:|---:|
| Swarm unit | 8 | 4 | 1.00 | 0 | 0.45 s | 1 |
| Armored soldier | 3 | 12 | 0.65 | 1.5 flat | 1.50 s | 2 |
| Fast raider | 5 | 5 | 1.60 | 0 | 0.75 s | 1 |
| Siege engine | 1 | 30 | 0.40 | 2.0 flat | — | 5 |

Armor cannot reduce a hit below 0.25. Spade traps and Kings ignore half of flat armor.

The base wave is the encounter's own composition; the Dealer's hand is added to it. A wave is never fully known until the player stands, but its shape is known from the upcard.

---

## 13. Bust and Overload

* The busting card is destroyed and never placed.
* Formation Strength for this hand's towers drops to ×0.80.
* Persisted towers from earlier waves are unaffected — they are already at ×1.00.
* **The Dealer resolves in full.** Hidden card deploys, draws to 17, everything arrives.
* Overload fires. Placement locks. The wave resolves.

### Overload

**The busting card deals immediate damage equal to its base power to the lane where it was provisionally placed.** It does not scale with the amount by which the hand exceeded 21.

Revision 6 scaled Overload with excess, which made busting at 28 strictly better than busting at 22 and rewarded blowing out harder. Capping at base power keeps bust productive without making it a strategy.

Bust now has exactly one axis: your own penalty. It costs a card, a multiplier, and the march step already paid, and it returns a single burst. It is clearly bad, occasionally the least bad option, and never a play you angle for.

---

## 14. Deck Construction

### Shoe

26 cards: two copies of every rank, Ace through King. The full game assigns printed native suits by commander and archetype. The draw pile persists across the waves of an encounter; reshuffle before a wave if fewer than eight cards remain.

### The Thinning Dilemma

| | Face-heavy | Many-card |
|---|---|---|
| Raw output | Lower | Higher |
| Board width | 2–3 towers | 4–5 towers |
| Run links | Rare (Queen only) | Frequent |
| Engagement | Best | Worst |
| Range and keywords | Best | Weakest |
| Bust rate | Higher | Lower |
| Failure mode | Thin board, few links | Fifth card missed |

Many-card decks are now a 21-chasing precision build: the fifth card is worth taking only if it lands exactly, and the 3.0-unit pullback is what makes it land. Face-heavy decks are a position-and-range build that finishes fast and fights on good ground.

**Never let "cut your low cards" or "cut your high cards" become default correct play.** If one column wins in testing, adjust the march curve first — it is the cleanest lever between them.

### Acquisition

Standard cards, modified cards, face cards, Aces, Jokers, cursed cards. The deck screen shows expected cards per hand, native-suit distribution, board width, and recent link frequency. It does not display a deck score.

---

## 15. Economy and Rewards

**Chips** — buy cards, remove cards, upgrade defenses, repair the Bastion.

**Favor** — rare. Rerolls, rule manipulation, commander abilities. Earned by **risk taken and lanes held**, not by hand quality: holding a lane the forecast called Open, and standing on a hand you could have improved. Rewarding good hands would pay the player twice for the same thing and make strong hands snowball.

**Bastion Health** — the run ends at zero. Hard to restore.

Reward size scales with lanes held, not output produced.

---

## 16. Progression Content

Deliberately under-specified. Every item below is intent, not specification.

### Card Modifiers

Roughly 20 at launch, weighted toward effects that change battlefield behavior without touching blackjack arithmetic: piercing, explosive, chaining, reinforced, echoing, anchoring, longer slow, alternate targeting. A smaller set operates on the hand through a bounded interface: value ±1, an extra Ace state, one discard-and-redraw, one rank preview.

Anything that changes bust thresholds, Formation Strength, or Dealer resolution is a rule package, not a modifier — rare, possibly mutually exclusive, closer to a game mode.

### Relics

*All values unpriced.*

**True Colors** — one off-suit card counts as native per wave.
**Card Counter** — reveals a band for the Dealer's hidden card.
**Steady Table** — the first bust of a region does not destroy the card.
**Surveyor** — adds one socket to each lane.
**Bridge Builder** — one card counts as wild in runs.
**Soft Landing** — one Ace-state intervention per encounter.
**Long Road** — reduces the march curve for one encounter.
**Field Promotion** — one family reassignment per encounter.

### Commanders

Each has a starting shoe, a passive, and a distinct decision texture. At most one launch commander may alter the Formation Strength curve. Others differentiate through native-suit distribution, socket layout, march economy, information access, or tower behavior. A commander is a skin only if it produces the same decisions on the same battlefield.

### Metaprogression

Unlocks expand possibility, not raw power. No permanent damage bonuses that make early runs feel intentionally underpowered.

---

## 17. Information and Fairness

### Shown

* Lane stakes, base wave, and empty-lane damage before the opening deal
* The Dealer's Vanguard, on the field, from the start
* **Visible Threat** per lane during the draw, labelled as revealed-force only, updating live on every draw and placement
* **Final Forecast** per lane after Dealer resolution, labelled as the combat contract
* Current total, hard/soft state, Formation Strength, summed power, active runs
* **Remaining rank composition, with busting ranks visibly marked**
* Ace transformations and their power consequence, before commitment
* Current entry position, and **which socket windows the next march step would cut into** — shown on the lane, not as a single engagement number
* Full army after the Dealer resolves, before lock
* A post-wave explanation of what leaked and why

### Not Shown

* Combined utility, hit edge, stand edge, or recommended action
* Green/red indicators or optimal placement highlights
* An exact bust percentage

Marking the busting ranks makes risk a reading skill rather than a lookup. The player sees six safe cards left in a pile of twenty-two and feels it. A percentage is one arithmetic step from the oracle the pillars prohibit, and it makes the rank display decorative.

### Recovery

At least one redraw, reserve, or discard tool is always available. A poor card always has a use.

---

## 18. Example Wave

Lane one carries **Bastion** stakes: three armored soldiers. Lane two carries **Vault** stakes and a fast reinforcement package; undefended it forecasts six damage. The Dealer's Vanguard is a **10** — an armored soldier already standing at the head of lane one.

The player is dealt **6** and **8**. Total 14, ×1.05. Entry 0.0, full 18.0 engagement. They commit both as Clubs — permanently — at lane one's sockets 6 and 9. No run: 6 and 8 are not consecutive. Lane two's **Visible Threat** reads **Open, 6.0** — what the revealed force would do, not a promise about the wave.

They hit. **Entry advances to 1.5; engagement drops to 16.5.** The card is a **4**, reaching hard 18, ×1.30. They commit it as a Spade at lane two's socket 3.

Two panels now say different things.

**Hand:** Ace, 2, and 3 survive. A 3 reaches 21 and pulls the whole army back 3.0 units, back to entry 0 and full engagement. A 2 reaches 20. The rank display shows six safe cards in a pile of twenty-two. Everything else busts to ×0.80.

**Battlefield:** lane two's Visible Threat is **Open, 3.8**, against the revealed force only. A 5 would form a 4–5 run at lane two's next socket, but a 5 busts. The next march step costs 2.5 units — entry 4.0, engagement 13.0, a 21% drop from here — and it lands before the card is seen.

They weigh it: six safe cards out of twenty-two, a Vault lane worth reward rather than health, an armored Vanguard in the lethal lane that the two Clubs are already handling, and a fourth card that costs a fifth of their remaining engagement whether it helps or not.

They stand.

The Dealer reveals a **6** — fast raiders — and draws a **7**, adding more. Total 23; the Dealer busts, but that no longer matters. **Their entire hand deploys anyway.** Lane two's **Final Forecast** — now the combat contract — reads **Open, 5.1**.

Adjustment window, and one move. Families are locked, so the lone Spade in lane two stays a Spade. The player wants two things — the Spade forward to catch the raiders earlier, and the socket-9 Club moved to the junction to cover both lanes — and can have exactly one. They take the Spade, because a trap that fires late is worth nothing, and set a *Hold* order so it waits for the group rather than triggering on the lead scout.

Combat resolves in fourteen seconds. Lane one holds. Lane two leaks 3.4 as the Final Forecast said — a chunk of the Vault, no Bastion damage.

Had they hit and busted on a King: entry would have advanced to 4.0, Overload would have dealt 5.0 to lane two, the formation would have run at ×0.80, and the Dealer would have deployed exactly the same army. Worse in every direction, with one burst of consolation.

---

## 19. Run Structure

Three regions, **30–45 minutes**.

Each region: two regular encounters (two waves), one elite (two waves), one Dealer boss (three waves), two or three noncombat nodes. Twelve combat encounters, twenty-seven waves.

| Activity | Budget |
|---|---:|
| Hand decisions and placement | 14–19 min |
| Combat resolution | 6–9 min |
| Rewards and deck decisions | 6–9 min |
| Shops, events, routing | 4–6 min |
| Transitions and boss presentation | 2–4 min |
| **Total** | **30–45 min** |

### Escalation

**Region 1 — Foundation.** Two lanes, three sockets, standard march curve, mixed lane stakes.

**Region 2 — Pressure.** A third lane in some encounters. Enemies that destroy or displace towers. Shifting sockets. Split. Native-suit synergies strong enough that off-suit genuinely costs.

**Region 3 — Distortion.** Linked hands and simultaneous fronts. Dealers who alter card access. Destructible terrain that invalidates prior placement. Optional altered-threshold packages.

Escalation must change how the player thinks, not how long they watch.

### Modes

**Standard Run.** **Daily Deal.** **Endless Siege.** **House Rules** — a menu selected before a run: Dealer hits soft 17, towers do not persist, native deployment only, minimum four cards per hand, doubled march curve, families reassignable. **Challenge Contracts** — handcrafted scenarios.

---

## 20. Prototype Scope and Validation

### The Question

> Does the player face a real, recurring, non-obvious choice about **what a card becomes and where it goes** — and does the hit/stand decision in the 14–19 band change with the battlefield?

The first clause is the primary claim. The second is the secondary one. Revision 6 asked whether drawing at total 8 could be made tense; that question is withdrawn.

### Content

* Two lanes, three sockets each, one shared junction
* Neutral 26-card shoe; every card may become Club or Spade at full effect
* Family locked at placement; single-move adjustment window
* March Clock as a config preset — flat, soft escalation, and hard escalation all shipped in the first build — plus the exactly-21 pullback
* Visible Threat and Final Forecast as separate, separately labelled outputs
* Run links only
* Formation Strength ×0.80–1.60
* Persistence with ×1.00 reversion
* Forced replacement at capacity
* Dealer as pure wave generator, standing on all 17s, resolving on bust
* Bust with capped Overload
* Lane stakes: Bastion and Vault
* Four enemy types
* Standing orders
* Deterministic, skippable combat
* Hit and stand only

### Cut From Prototype

Printed native suits, Hearts, Diamonds, off-suit keyword loss, Dealer total comparison, Split, Double Down, relics, commanders, card modifiers, metaprogression, freeform pathing, the Works stake, Wide Formation, pair links, keyword links, Queen command aura.

### Test Arms

Because three separate changes — no Wide Formation, escalating march, links reduced to runs — all land on many-card decks, a single build cannot say which one killed the archetype if it dies.

* **Arm A — flat control.** 1.0 per card, as Revision 6.
* **Arm B — soft escalation.** +1.0 / +1.5 / +2.0. Cumulative 1.0 / 2.5 / 4.5.
* **Arm C — hard escalation.** +1.5 / +2.5 / +3.5. Cumulative 1.5 / 4.0 / 7.5. As specified above.

These are presets in one config file, not three builds.

**The primary measurement is the shape of the fifth-card outcome**, not aggregate output. For each arm, report how often a safe fifth-card miss was nonetheless the better play — measured by resolver output against the stand-at-four counterfactual, and separately by whether players say they would take it again. Arm C is expected to produce the binary outcome described in Section 4. If it does, Arm B is the design.

Secondarily, the same three arms disambiguate the many-card archetype, since three separate Revision 7 changes all landed on it. If many-card is unviable in C and viable in A, the curve is the cause. If it is unviable in all three, links and board width are insufficient alone and the archetype needs a mechanism — designed then against a measured deficit rather than guessed at.

### Scripted Battery

Each state presented at least twice with different presentation so players cannot answer from memory.

1. Hard 18 against a severe Open lane, versus a mild one, versus one already Held
2. Hard 16 as 10+6 versus 3+3+5+5
3. Soft 17 versus hard 17
4. A fourth card that would complete a run versus one that would not
5. A hand at socket capacity where the best replacement is a good tower
6. A marginal hand with a Vault lane versus the same hand with a Bastion lane
7. A hand at 18 where the only 21 is a single surviving rank
8. A Dealer showing a King versus a Dealer showing a 3
9. A placement where family choice must be committed before the lane's threat is fully known
10. A hand where the single adjustment move can save a run link or answer a lane, but not both

### Success Criteria

* Players commit families deliberately and can explain the commitment afterward.
* Players place for runs, not only for range.
* Players change the hard-18 decision between severe and mild lane states.
* Players make different decisions for 10+6 and 3+3+5+5.
* Players triage differently between Bastion and Vault lanes.
* Players read the Dealer's upcard as a unit on the field, not a number.
* Players chase the fifth card sometimes, and regret it sometimes.
* Forced replacement produces visible hesitation.
* Bust feels bad, occasionally correct, and never desirable.
* Combat is skipped or watched by choice, not endured.
* Players want another encounter.

### Instrumentation

Per offered state: exact hand, Ace states, remaining rank counts, entry position and per-socket window remaining, socket occupancy and socket depth distribution, active runs, per-lane Visible Threat and stakes, Dealer upcard and deployed units, the choice made and time to decide, whether placement changed before the choice, and result versus Final Forecast.

Log placement depth explicitly. If towers cluster at socket 9 across every arm, the deep-placement dominance flagged in Section 4 is real and the socket geometry needs work.

Debug only: exact bust probability, stand and hit expected output, combined utility.

Also log adjustment-window usage, including which move was *wanted* where the interface can capture it. If the single move is never used, it is a candidate for deletion; if players consistently want two, the relic path opens rather than the baseline widening.

Also log whether combat was watched, fast-forwarded, or skipped. If it is always skipped, that is information, not failure.

### Regression

Before changing the march curve, Formation Strength, run percentages, tower power, Overload, or the resolver:

1. Re-run the benchmark hand set and flag sign changes.
2. Enumerate all legal two-to-five-card hands; record raw output and entry position. Do not record a derived engagement-adjusted output — Section 4.
3. Simulate 10,000 hands each for baseline, face-heavy, and many-card shoes; report output, bust rate, board width, run frequency, and final entry position.
4. Verify Final-Forecast-versus-resolution equivalence on the scripted fixtures, and verify that Visible Threat matches a resolver run against the revealed force alone.

That is the whole validation architecture.

---

## 21. The Add-Back Sequence

Four systems were cut or suspended for diagnostic reasons rather than because they were bad. **The order and trigger for each are fixed now, while the reasoning is fresh.** Cuts made for a test become the design by default if nobody writes down when they come back.

### 1. Dealer Total Comparison

*Trigger:* Arm A shows players changing hit/stand with the battlefield, and the scripted battery shows decisions diverging from basic strategy.

*Form on return:* **comparison pays the Vault, not the battlefield.** Beating the Dealer improves the encounter reward; losing reduces it. The blackjack incentive then competes with the battlefield incentive instead of reinforcing it, which is the tension this design exists to create. Battlefield-side comparison effects — Vanguard withdrawal, army advancement — do not return in any form.

*Risk if skipped:* the game ships without an opponent, the blackjack framing becomes vestigial, and it is 21-solitaire.

### 2. Persistence Multipliers

*Trigger:* only if playtests show that reverting persisted towers to ×1.00 makes the second wave of an encounter feel weightless.

*Form on return:* a partial retention — persisted towers keep half the difference between their locked multiplier and 1.00 — rather than full locking. Full locking is not returning; it produced snowball and UI load.

### 3. A Second Link Rule

*Trigger:* runs alone prove too rare to shape placement, measured by run frequency per hand in instrumentation.

*Form on return:* pairs, at a lower value than runs. Keywords and auras remain out until a link rule has survived a full test cycle.

### 4. Many-Card Support

*Trigger:* Arm A and Arm B both show the archetype unviable.

*Form on return:* designed against the measured deficit. **Not Wide Formation.** A flat attack-speed bonus at the card counts the march taxes is the exact refund loop this revision removed; whatever returns must not be a function of card count alone.

---

## 22. Key Risks

**The march curve is the wrong shape.** The step sizes are the most important numbers in the game and must be config-tunable on the first build. The escalation is what makes hand length a real decision; if it is wrong, nothing downstream will read correctly.

**The fifth card is binary.** A 67% engagement loss is severe enough that a safe miss may be board collapse, leaving only exact-21-or-nothing. **Reduce the fifth step before raising the pullback.** Revision 7 advised the opposite; that was wrong. Raising the pullback makes success more attractive but makes the mechanic *more* binary, widening the gap between landing and missing. Softening the step is what creates the defensible middle outcome the design wants.

**Locking family is too punishing.** If players report feeling trapped by a family committed three cards before the wave was known, the fix is a limited escape (a relic, one reassignment per encounter) rather than reopening the window. Free reassignment empties the draw phase.

**The single adjustment move is either useless or decisive.** Instrumented. If decisive, restrict it to empty sockets with no swapping. Do not widen it back toward per-tower movement; grant extra moves through relics instead.

**Deep placement dominates.** Flagged in Section 4 and measured through placement-depth logging. If it holds, fix the socket geometry — uneven spacing, range differences by position, or lane-specific leak thresholds — before touching the march curve.

**Suspending comparison strands the blackjack layer.** Mitigated only by Section 21. This is the risk most likely to be quietly forgotten.

**Removing live combat makes the game feel passive.** Mitigated by standing orders and skippable combat. If it still reads as a spreadsheet, add pre-lock expression, not live clicking.

**Runs make placement fiddly rather than interesting.** If players solve an adjacency puzzle instead of reading the battlefield, cut the four-run and reduce the percentages.

**Forced replacement feels bad rather than tense.** It is meant to be a real loss. If it reads as punishment, the answer is more sockets, not a softer rule.

**The game is still basic strategy with cosmetics.** The core risk, retained across three revisions. The battery is designed to detect it. If the answer is yes, the honest response is a structural change, not another tuning pass.

---

## 23. Changelog from Revision 6

### Fixed loops

1. **Bust no longer dodges the Dealer.** Resolution is now purely deploy, and it happens on bust too. The hidden card was always marching.
2. **Overload no longer scales with excess.** Capped at the busting card's base power, so busting at 28 is not better than at 22.
3. **Wide Formation deleted.** It refunded 10–20% attack speed against a march costing 17–28% engagement at the same card counts — a near-exact cancellation at precisely the hands the march was meant to bite.
4. **Dealer comparison suspended.** Vanguard withdrawal and army advancement paid out on beating the Dealer, which is what basic strategy optimizes. Return scheduled in Section 21, paying the Vault instead.

### Reshaped

5. **The march is escalating and sized to socket spacing**, +1.5 / +2.5 / +3.5. The flat 1.0 step cost 5.6% of engagement and taxed forward placement rather than drawing.
6. **The exactly-21 pullback raised to 3.0 units**, making a landed fifth card a genuine rescue and long hands a precision play.
7. **Family is locked at placement.** The post-Dealer window allows one move and standing-order changes only.
8. **Links reduced to runs.** Pairs, shared keywords, and the Queen aura are cut; the Queen is now wild in runs instead.
9. **Persisted towers revert to ×1.00** rather than keeping locked multipliers — removing snowball and UI load while keeping the socket scarcity persistence exists for.

### Retained

The exact-composition decision model, the sublinear power curve, the Ace 11-to-1 transformation, the 10+6 / 3+3+5+5 comparison, socket scarcity and forced replacement, lane stakes, deterministic skippable combat, standing orders, the Dealer's cards as units, marked busting ranks in place of a percentage, and the prohibition on player-facing verdicts.

### Added

10. **A control arm** holding the march flat, so the three changes landing on many-card decks can be told apart.
11. **The add-back sequence**, with a trigger and a return form for every diagnostic cut.

---

## Final Gameplay Identity

21 Bastion is not a blackjack game with tower-defense animations, and not a tower-defense game with cards as a purchase menu.

Its identity is the moment a card lands face-up and the player has to decide, right then, what it becomes and what it displaces — before the army is fully known, and knowing they cannot take it back.

The hand shows what another card could be. The field shows what another card would cost: in ground, in a socket, in a tower you would have to tear down. The Dealer's army is standing there the whole time and grows every time they draw. Nothing on screen adds those together.

A version that hides the consequences is unfair. A version that shows the answer is solved. A version where every decision can be revised after the reveal has no decisions in it at all. Revision 7 is aimed at the space between those three.

---

## 24. Corrections in Revision 7.1

### Arithmetic

**The entry-7.5 engagement figure was wrong.** Revision 7 reported 7.5 units remaining; the correct value is 6.0.

| Socket | Window | At entry 7.5 |
|---|---|---:|
| 3 | 0–6 | 0.0 |
| 6 | 3–9 | 1.5 |
| 9 | 6–12 | 4.5 |
| | | **6.0** |

The error was summing socket 9's full 6.0 window against a remaining path of only 4.5 units. The fifth card therefore costs **−67%**, not −58%. All other rows were checked and hold: entry 1.5 → 16.5, entry 4.0 → 13.0, 4-card 21 at entry 1.0 → 17.0, 5-card 21 at entry 4.5 → 12.0.

**The march's placement bias was stated backwards.** Entry advances from the spawn side, so it consumes the forward socket's window first. The flat step taxed forward placement, not rear. Corrected in Section 4, along with the new observation that deep placement is weakly dominant whenever entry exceeds 0 — an unintended consequence that needs measuring before the march curve is tuned.

**The 3+3+5+5 engagement comparison** read 28%; it is 38% (18.0 against 13.0).

### Withdrawn claims

**The engagement-fraction output estimates are withdrawn.** Multiplying board power by a summed engagement fraction treats non-interchangeable sockets as fungible and ignores that coverage lost from a 5.0-power tower is not coverage lost from a 1.6-power tower. Total engagement stays as an explanatory device for the player and the team; balance moves to the resolver.

**"The fifth card is worth taking only if it reaches 21" is demoted from design identity to hypothesis.** It was derived from the withdrawn scalar, and under correction the five-card 20 estimate falls by roughly 20% from where Revision 7 put it. More importantly, if the claim proved true it would probably be unhealthy: a strictly binary fifth card reduces the decision to counting one rank, with the battlefield setting only how desperate the player is. Section 4 now states the target shape — spectacular on 21, usually bad but sometimes defensible on a safe miss, clearly worse on a bust — and Section 20 measures whether the curve permits that middle case.

### Systems changed

**The adjustment window is reduced to one move.** Revision 7's per-tower shift permitted near-total board revision after complete information, which would have made the adjustment phase the real placement puzzle and left five specification questions unanswered. One global move — relocate one tower one socket, or swap two adjacent towers — answers all five by construction. Extra moves become a relic and commander reward, not a wider baseline.

**The forecast is split into two named contracts.** Visible Threat during the draw, exact against the revealed force only and explicitly not a prediction of the wave; Final Forecast after Dealer resolution, exact against the complete army and the contract combat must reproduce. Separate return types, separate labels. Revision 7 described one contract and then demonstrated it changing mid-example.

**The march curve ships as three presets, not one.** Flat, soft escalation, and hard escalation are config in the first build and become the three test arms. Revision 7's guidance to raise the pullback before reducing the fifth step is reversed: raising the pullback makes the mechanic more binary, which is the failure being guarded against.

### Unchanged

The 3.0-unit pullback is held at its Revision 7 value deliberately. Correcting the arithmetic, splitting the forecast, and narrowing the adjustment window are enough for one pass; changing the pullback and the step curve together would mean tuning two coupled levers against a number that has just been shown to be untrustworthy. Let the three arms report first.
