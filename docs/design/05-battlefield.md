# Battlefield

Source: Handoff Revision 7.1, § 10. **Rank stacking** is from the Run Layer Handoff (consolidated), § 2.

---

## Sockets and scarcity

- **Two lanes**
- **Three sockets per lane**, at path positions 3, 6, 9
- **Range differs by socket** — 4.0, 3.0, 2.0, forward to rear
- **One shared junction socket**, firing into either lane at reduced contribution, at the middle socket's
  position and therefore its range
- **Seven sockets total**

Range is not uniform, and the asymmetry is load-bearing. A flat range gave every socket an identical
engagement window, which made deep placement weakly dominant the moment the march began; forward sockets
now open wider and have more to lose. See `03-march-clock.md` § Deep placement was weakly dominant, and
the geometry was fixed.

**Socket adjacency is linear within a lane** — 3–6 and 6–9 are adjacent, 3–9 is not — with **no cross-lane
adjacency**, and **the junction adjacent to neither lane**. The junction is a *run island*: it buys breadth
and forfeits synergy. See `04-cards-as-defenses.md` § Adjacency for the reasoning and the consequence
(a 4-run is geometrically impossible in the prototype).

### Forced replacement

> **At capacity, a non-busting drawn card must replace an existing tower before the player may stand.**

The removed tower's power, links, and locked multiplier go with it. **The player may never bank an
improved total while leaving a card unplaced.**

> Forced replacement is one of the three things the game's decision density actually rests on. It is not a
> safety valve.

The other two are family locking and the march step. If forced replacement reads as punishment rather than
tension, **the answer is more sockets, not a softer rule.**

---

## Rank stacking

> **Status: DECIDED — flag-gated for the prototype.**
>
> Rank stacking is included because it creates a **second placement archetype — density versus spread** —
> **not** because it is a free socket-pressure valve. Run the March Clock test arms with stacking **off**
> first, then repeat with the flag enabled (`../prototype/VALIDATION.md` § Rank-stacking sequence).

**Two towers of the same rank may occupy one socket as a single stack.** Matching is by **rank, not
blackjack value**: J+J stacks, **J+Q does not.**

| Rule | Committed behavior |
|---|---|
| **Match** | Same rank only |
| **Depth** | **2** in the prototype |
| **Aces** | Cannot stack |
| **Power bonus** | **None** |
| **Run eligibility** | A stacked socket **cannot participate in a run** |
| **Family** | The two cards **may have different families**; both behaviors originate from the shared socket |
| **Formation Strength** | **Each layer retains its own multiplier.** Stack power is the sum of each layer's individually modified power |
| **Position** | Both layers share socket, range origin, March exposure, and any positional penalties |

**The trade is spatial: save capacity, lose coverage breadth and run eligibility.** Nothing is added on top
of that — no power bonus, no cooldown change, no keyword interaction.

### Why the multiplier rule changed

An earlier proposal (Addendum A) had a cross-wave stack **inherit the lower multiplier.** That is
superseded. The lower-multiplier rule adds a **hidden third cost** to stacking and makes a fresh card lose
power merely for sharing a socket.

Each card therefore **keeps its own Formation Strength contribution.** This also **prevents multiplier
laundering**, because neither layer changes multiplier when stacked — there is no direction in which power
can be moved between hands.

Note the interaction with persistence: a persisted tower has already reverted to ×1.00, so stacking a fresh
card onto it produces one socket carrying one ×1.00 layer and one live-multiplier layer. **That is
intended.** It is also the only place in the design where two multipliers coexist on one socket, which is
worth watching in the UI — § Persistence exists partly to keep one live multiplier on screen.

### The two placement archetypes

| | **Spread** | **Density** |
|---|---|---|
| **Wants** | Distinct consecutive ranks | Duplicate ranks |
| **Board shape** | Wide, adjacent, linked | Concentrated strongpoints |
| **Primary value** | Run adjacency and coverage | Socket economy and multifunction positions |
| **Acquisition question** | Does this complete a chain? | Do I want another copy of this rank? |

Spread and density pull against each other cleanly, because **run eligibility is exactly what a stack gives
up.** A hand that stacks is a hand that stopped building chains.

### Accepted risks

1. **Stacking softens forced replacement**, which is one of the three pillars of decision density.
   Instrument stack-at-capacity rate, replacement rate, and whether players stack **reflexively** whenever
   a match exists.
2. **Stacking may worsen deep-placement dominance**, because concentrated power naturally prefers safe rear
   sockets. **Diagnose socket geometry before taxing stacks.** (The prototype's geometry remedy is already
   in — `../ROADMAP.md` § Open Decision 2 — so the stacking pass runs against range-by-socket, and a
   returning depth bias is a stacking result rather than a geometry one.)
3. **If stacking becomes automatic**, first test a **spatial or cadence cost** such as a longer shared
   cooldown. **Do not add a flat damage penalty by default** — a damage penalty is a fourth hidden cost on
   top of forfeited runs, forfeited breadth, and shared March exposure.

---

## Persistence

Towers persist across the waves of an encounter and reset at the encounter boundary.

> **Persisted towers revert to ×1.00 Formation Strength at the start of the next wave.**

They keep their base power, family, and socket; **they lose the multiplier their hand earned.**

### Why

Revision 6 locked each hand's multiplier onto its towers permanently, which produced snowball, a screen
full of tower groups at different multipliers, and a bust that could drag six towers down instead of
three — pushing late waves toward automatic stands.

Reverting to ×1.00:

- keeps exactly **one live multiplier on screen at a time**,
- removes the snowball,
- keeps **bust scoped to the current hand**,
- lets old towers decay gently in relevance while the current hand stays the point,
- and preserves the thing persistence exists for: **sockets fill during the second wave, and every card
  after that forces a replacement.**

That last point is the real purpose. Persistence exists to create scarcity, not to bank power.

### The encounter boundary is where persistence stops

The run layer states the same rule from the campaign side: **the shape of the ground persists; towers do
not persist across encounters** (`11-siege-geography.md`). Geography and card identity may carry forward
**precisely because they do not carry Formation Strength forward** — no compounding multipliers across
encounters.

So a front's state can change the path length, socket layout, route structure, and lane stakes a battle is
fought on, and none of that is a multiplier. **A campaign effect that reached into Formation Strength or
the march curve would be the snowball this reset exists to prevent.**

---

## The adjustment window

Opens after the Dealer resolves and the full army is visible.

- **One move total:** relocate a single tower to an adjacent empty socket, *or* swap two adjacent towers.
  **Not both, and not per tower.**
- **Standing orders may be set or changed freely.**
- **Families are locked. No reassignment.**
- **No further draws.**

> This is a **response** window, not a **solving** window.

### Why one move, not one per tower

Revision 7 allowed every tower to shift one socket. On a full seven-socket board **that is close to full-
board revision after complete information**, which would have made the adjustment window the real placement
puzzle and the draw phase provisional.

It also left five specification questions unanswered:

1. Does a swap consume both towers' moves?
2. Can sequential swaps carry a card further than one socket?
3. Do shifts resolve in order or simultaneously?
4. Can a lane be rotated?
5. Do persisted towers move on equal terms?

**Every answer produces a different game. One global move answers all five by construction.**

It is enough to absorb a bad hidden reveal and to create a tactical beat — a single move can still make or
break a run link — **and it cannot rebuild a board.**

### If one move proves too tight

**The expansion path is adjustment points granted as a reward, not a higher baseline.** Under the run layer
that means **doctrine and Charters** (`13-doctrine-and-charters.md`) rather than the relic layer 7.1
assumed; the rule it protects is unchanged. Test one-move against Revision 7's every-tower version only
after the baseline has been played.

**Instrumented.** If the move is never used, it is a candidate for deletion. If it is decisive, restrict it
to empty sockets with no swapping — **do not widen it back toward per-tower movement.**

**Not available on bust** — placement locks immediately (`07-bust-and-overload.md`).

---

## Lane stakes

Lanes are not interchangeable. Each encounter assigns stakes, **shown before the opening deal**.

| Stake | Effect of a Leak |
|---|---|
| **Bastion** | Direct Bastion health damage. The lethal lane. |
| **Vault** | This encounter's **campaign reward** is reduced. |
| **Works** | A placed tower is destroyed and does not persist. *Full game only.* |

> A player who is healthy but poor triages differently from one who is rich and nearly dead.

Boss encounters use **Bastion stakes in every lane**.

Prototype uses Bastion and Vault only.

⚠ **The Vault stake's payload changed with the run layer.** Revision 7.1 said a Vault leak costs "Chips and
Favor." **Chips are cut** (`12-campaign-time-and-orders.md`), and **Favor is never a reward-floor
currency** — it is earned only through its risk-and-stake conditions. A Vault leak therefore reduces the
encounter's ordinary campaign reward: the captured supplies, the service exposed, the Muster or Rerank the
Vault would have funded. The stake's *job* is unchanged — a lane worth reward rather than health, so that
triage is a real decision. See `../reference/tuning-constants.md` § Known Discrepancies.

---

## Standing orders

Because combat has no live input, the adjustment window offers **pre-committed conditionals**:

| Order | Behavior |
|---|---|
| **Hold** | Fire only at enemies past a chosen socket. |
| **Focus** | Prefer armored targets, or prefer the leading target. |
| **Trigger on group** | A trap waits for a minimum number of enemies in radius. |

**Modeled exactly by the resolver and shown in the forecast.** A standing order that the forecast cannot
model is not shippable.

---

## Resolver

**One deterministic resolver** drives both forecast and wave: same spawn schedule, health, armor, speed,
paths, range, cooldown, targeting, and rounding. **Ties resolve by spawn order.**

Per lane it outputs:

- empty-lane damage,
- predicted damage under the current plan,
- damage prevented,
- per-tower activity,
- the cause of remaining leakage.

There must be exactly one simulation code path — the visual wave is a *presentation* of a resolver run,
never an independent re-simulation. See `../ARCHITECTURE.md`.

---

## Two forecasts, not one

**During the draw the game cannot forecast the final wave**, because the Dealer's hidden card and
subsequent draws have not resolved. Revision 7 described a single contract and then showed the number
changing mid-example — **exactly the behaviour that destroys trust in it.**

There are two distinct outputs. **They must be named differently in the interface and typed differently in
the code.**

| | **Visible Threat** | **Final Forecast** |
|---|---|---|
| When | During the draw | After Dealer resolution |
| Modelled against | Base wave plus Vanguard — **the revealed army only** | **The complete army** |
| Guarantee | Exact against what is currently on the field | Exact against the wave that will run |
| Is it a prediction of the wave? | **No** | **Yes** |

> **Only the Final Forecast is the combat contract. If it says a lane leaks two, the wave leaks two.**

**Visible Threat is exact about a smaller question**, and the interface must say so plainly — it is what
the currently revealed force would do, **not what the wave will do.** Players who read it as a promise will
feel the game break it when reinforcements land.

### Implementation

> These are **separate return types from the resolver, not the same type with a flag.** A Visible Threat
> must not be renderable in a slot expecting a Final Forecast.

Trust in the forecast is a foundational claim of this design; **a number that silently changes meaning
mid-hand is the cheapest possible way to lose it.** Enforce the distinction in the type system, where it
cannot be forgotten. See `../ARCHITECTURE.md`.

---

## Coverage display

Show the **predicted leakage number per lane** and color it. Two words on a plain threshold:

| Label | Condition |
|---|---|
| **Open** | Predicted leakage is **at least half** of empty-lane damage. |
| **Held** | Below that. |

**The number is primary; the label is a glance-read.** This is the maximum amount of interpretation the
game is permitted to do for the player — see `09-information-and-ui.md`.
