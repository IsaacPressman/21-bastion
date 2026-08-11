# The Dealer and Enemies

Source: Handoff Revision 7.1, §§ 11, 12. **The opposing shoe and public recruitment** are from the Run
Layer Handoff (consolidated), § 6. **Spatial breakpoints, deterministic bunching, the hidden card's
visible lane, and optional opportunity units** are from the Improved Encounters Handoff, §§ 2.2, 8, 9, 12.

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

### The hidden card's lane is visible from the start

> **Status: DECIDED.** The hidden card's **rank stays unknown; its destination lane does not.**

The player knows *"something unknown is coming to lane two."* They do not know its rank or the enemy it
becomes. Any **further** Dealer draws remain unknown in every respect until resolution.

This is uncertainty that **does not prevent intention** — the distinction the encounter thesis rests on
(`14-encounter-timeline.md`). A player who knows only that something is coming somewhere cannot form a
plan, and their Hit decision degrades into "maybe another tower would help." A player who knows the lane
can hedge it deliberately, which is also what gives the junction its job (`05-battlefield.md` § The
junction is the uncertainty hedge).

> **Consequence for doctrine.** Because destination lane is now baseline information, **Watchtower and
> similar effects no longer reveal it.** The upgraded effect reveals the hidden card's **rank class** —
> Low, Mid, High, or Court. Exact implementation is future content work
> (`13-doctrine-and-charters.md`).

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

Four enemy types was the full prototype roster under Revision 7.1. The Improved Encounters Handoff adds
the breakpoint enemies below.

---

## The base wave is fully known before the deal

> **Status: DECIDED.** The base wave is **not a source of uncertainty.**

Before the opening hand the player sees enemy types, spawn order, spawn timing, lane assignment, lane
stakes, **spatial breakpoint abilities**, empty-lane damage, and the Vanguard's rank, unit, and lane.

**The player must be able to form a battlefield plan before drawing.** All remaining uncertainty is
located in exactly two places: the hidden card's rank, and whatever the Dealer draws after it. See
`09-information-and-ui.md` § Shown.

---

## Spatial breakpoints

> **Status: DECIDED as a mechanic. Its role as a geometry remedy is an open, measured question.**

Socket identity should emerge from **what enemies do at different points on the lane**. The handoff
originally called breakpoints the *baseline solution* to deep-placement dominance; **that claim is
softened**, because the prototype already ships a measured remedy — range varying by socket
(4.0 / 3.0 / 2.0). The governing wording:

> **Spatial breakpoints give forward and middle positions distinct tactical jobs and may reduce or
> eliminate the need for socket-specific range. The currently validated range-by-socket values remain
> authoritative until breakpoints are implemented and re-measured in isolation.**

So: **build breakpoints for the tactical jobs they create**, keeping the current range values, and settle
the geometry question afterwards with the four-step experiment in `../reference/tuning-constants.md`
§ Known Discrepancies, entry 12. **Do not tune breakpoints and range together.**

The design intent, independent of that question:

| Depth | Why it should be worth taking |
|---|---|
| **Forward** | Some threats must be solved **early**, before a breakpoint fires |
| **Rear** | Retains more engagement after March advancement |
| **Middle** | Junction access, run topology, and breakpoint timing |

### Prototype breakpoint enemies

| Enemy | Breakpoint behavior |
|---|---|
| **Standard Bearer** | If alive when crossing a specified breakpoint, buffs nearby and following enemies. The player may need to kill it **before socket 6** |
| **Saboteur** | At its breakpoint, **disables the nearest eligible tower for a temporary duration** |
| **Siege Engine** | If alive when crossing **socket 9**, fires a Bastion shot. Killed before socket 9, it does not fire |
| **Lane-Switching Raider** | At the junction, changes lane by a **deterministic, previewed** rule |

Two constraints on this roster, both load-bearing:

- **The Saboteur disables; it does not destroy.** Do not begin with permanent destruction — disabling one
  of three towers is already a large effect, and the severity is an open question rather than a decided
  number (`../ROADMAP.md` § Improved-encounter open questions).
- **Generalized tower destruction is not baseline.** Ordinary enemies do not broadly attack towers in the
  prototype. **Specific, telegraphed positional threats** create placement risk without violating the
  fairness contract; a lane that can eat your board without warning does not.

The Lane-Switching Raider is the Skirmisher under a tactical name, and it remains the structurally
awkward one: lanes resolve independently today, so a unit crossing between them changes the shape of the
lane loop rather than adding a rule inside a phase. Still stubbed in `core/Resolve/UnmodelledBehaviour.cs`.

### Deterministic bunching

> Enemies have a **minimum legal spacing** and **do not pass one another** unless an explicit enemy ability
> says otherwise.

If a leading enemy is slowed and a follower would violate minimum spacing, **the follower's speed is
capped to maintain that spacing.** The result is column compression upstream of the slowed unit, and
Barrage splash becomes stronger against the compressed group (`04-cards-as-defenses.md` § Tower-to-tower
tactical interaction).

This is a movement rule, so it stays deterministic and stays in the resolver — invariant 7 is untouched.
**The timeline must show the compression**, because an interaction the player cannot see is an
interaction they cannot intend.

---

## Optional opportunity units

> **Status: DECIDED.** If every survival lane is already Held, the player needs a reason to consider
> further risk.

**Do not present these as checklist objectives.** Embed them physically into the wave as units.

### Prototype payouts are encounter-local — ✅ decided

> **No Favor in the prototype. Opportunity-unit payouts must land inside the encounter that offered them.
> Favor and Dealer-recruitment rewards are full-run extensions of the same units.**

**And no substitute currency.** Inventing a prototype-only resource to carry these payouts would be
inventing an economy to test a placement question — which is the shape hard invariant 1 exists to prevent.

| Unit | Killed before its breakpoint | Allowed through |
|---|---|---|
| **Supply Courier** | **Cancels a reinforcement group** scheduled later in this encounter | The reinforcement arrives normally |
| **Standard Wagon** | Upcoming enemies **lose a visible buff** | The buff activates |

Both are deterministic, both are legible on the timeline, and neither needs a resource to exist. The
**Paymaster** — *"kill before a breakpoint to gain +1 Favor"* — is **deferred to the run layer** with the
rest of Favor rather than rewritten.

**This is better for the prototype than a currency would have been.** The question an opportunity unit
exists to ask is:

> **Will a player risk another card for a non-survival tactical gain?**

Favor is not needed to answer it — and a currency payout would arguably answer a *different* question,
since a player chasing a campaign resource is reasoning about the run rather than about the battlefield in
front of them. An encounter-local consequence keeps the fifth-card test where the prototype can read it.

The run-layer version of the same unit **adds** Favor or recruitment interference on top. Extension, not
redesign. See `../reference/tuning-constants.md` § Known Discrepancies, entry 13.

### The requirements are the design

- **Optional means optional.** Failure must not make the encounter feel lost.
- They create **overcommitment temptation**, not mandatory chores.
- Rough target: **one meaningful opportunity per encounter**, not one per wave.

Both failure modes are pre-committed readings: players who **ignore** them mean the payoff is too small or
too detached from the run; players who **always** pursue them mean they are mandatory objectives in
disguise, and the answer is a lower payoff or more situationality — never a bigger reward
(`../prototype/VALIDATION.md`).

### Why this sits in the fifth-card argument

Optional opportunities are a **partial answer to the fifth-card binary problem** (`03-march-clock.md` § The
fifth card is a hypothesis). A board that already survives can still justify another card, because the
card cancels a later reinforcement group, strips a buff off the enemies still to come, completes a run,
avoids a costly replacement later, or enables a tactical interaction.

> **Exact 21 remains spectacular, but a safe miss does not have to be worthless.**

That is the "sometimes defensible" middle outcome the design has been unable to produce by tuning the
march curve alone — and note what it means for the arm question: it changes the *thing being measured*.
The Milestone 5 fifth-card measurement was taken without opportunity units, against pure leak output. See
`../prototype/VALIDATION.md` § The primary measurement.

