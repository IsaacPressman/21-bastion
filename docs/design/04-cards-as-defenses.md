# Cards as Defenses

Source: Handoff Revision 7.1, §§ 7, 9.

---

## Family is locked at placement

> **When a card is drawn, the player chooses its family, and that choice is permanent for the wave.**

This is **the design's primary commitment**. Family is chosen under uncertainty — before the hidden card
deploys, before the hand is complete — and cannot be undone once the wave is known.

The reasoning, which must survive any refactor: a player who could reassign families after full reveal
would place carelessly during the draw and solve the puzzle at the end, which empties the entire draw
phase of consequence.

**One tower's** position remains adjustable in the adjustment window (`05-battlefield.md`) — one move for
the whole board, not one per tower. **Family is never adjustable.**

If this proves too punishing in testing, the fix is a *limited escape* — a relic, one reassignment per
encounter — **not** reopening the window. See `../prototype/RISKS-AND-ADDBACKS.md`.

---

## Suit identities

| Suit | Role | Examples | Keyword |
|---|---|---|---|
| **Hearts** | Troops | Guards, archers, medics, patrols | *mobile* |
| **Diamonds** | Construction | Walls, barricades, extractors — extend path length | *extend* |
| **Clubs** | Artillery | Cannons, mortars, ballistae | *splash* |
| **Spades** | Traps and Control | Spikes, tar, poison, route switches | *slow* |

**Prototype:** only Clubs and Spades. Hearts and Diamonds are cut — see `../prototype/SCOPE.md`.

---

## Off-suit deployment (full game only)

In the full game, a card may be deployed into any unlocked family.

> **Native deployment gives the full family behavior; off-suit deployment gives the generic form — full
> power, but no family keyword, no native synergy, no family-exclusive upgrades.**

> You can always cover a lane. You can only *solve* it natively.

An off-suit Spade damages but does not slow. An off-suit Club fires but does not splash. One cost,
expressed in **behavior rather than arithmetic**.

**Not in prototype.** The prototype shoe is neutral and every card may become Club or Spade at full effect.

---

## Face cards

Face cards are all value 10, so **they cannot form runs with each other**. They buy their advantage
through properties low cards cannot stack into.

| Card | Property |
|---|---|
| **All 10/J/Q/K** | Range **4.0** instead of 3.0, and may occupy the shared junction socket without the usual contribution penalty. |
| **Jack** | Mobile. Relocates to an adjacent socket once mid-wave, automatically, when nothing is in range. |
| **Queen** | **Wild in runs.** Counts as any value for the purpose of forming a run link. She is the only way a face card joins a run, and the only bridge across a gap in a sequence. |
| **King** | Anchor. Ignores half of flat armor; cannot be displaced. |

Note the geometry consequence: range 4.0 changes a face card's engagement window to 8.0 rather than 6.0,
which interacts directly with the March Clock formula in `03-march-clock.md`.

---

## Aces

Aces count as 1 or 11 and **mirror that on the field**: 1.0 power compact utility, or 5.4 power
formation-defining.

A hit that forces an Ace from 11 to 1 **transforms the battlefield object immediately, and the forecast
updates before commitment.**

Aces count as 1 or 11 for runs, matching their current state.

This is one of the few places where a blackjack event has an instant, visible, physical consequence — it
is worth implementing carefully rather than as a number change.

---

## Run links

> **One link rule. Runs only.**

Revision 6 had pairs at +20%, two-runs at +15%, shared keywords, and a Queen command aura. Pairs and
two-runs were nearly the same effect wearing different trigger conditions. Keywords and auras were a
second subsystem inside a mechanic that had not been tested once.

> A pair is a rank coincidence you notice. A run is something you can draw toward and build across a
> hand — it connects placement back to the sequence of cards, which is the reason links exist at all.

### The rule

**Consecutive card values in adjacent sockets form a run.** Direction does not matter. Aces count as 1 or
11 matching their current state; a Queen is wild.

| Run Length | Effect |
|---|---|
| 2 | +15% power to both towers |
| 3 | +25% power to all three |
| ~~4~~ | ~~+35%~~ — **cut from the prototype; geometrically impossible.** See below. |

Runs are **computed at lock, shown in the forecast, and fully deterministic.**

### Adjacency

| Question | Answer |
|---|---|
| Within a lane | **Linear.** 3–6 and 6–9 are adjacent; 3–9 is not. |
| Across lanes, matching depth | **Not adjacent.** |
| The junction socket | **Adjacent to neither lane.** A run island — no tower placed there can ever be in a run. |

**Why the junction is an island.** Adjacent to both lanes makes it a run *hub* that can join chains in two
lanes at once, which makes it the auto-best socket and collapses the placement decision. Adjacent to one
lane is arbitrary and asymmetric. Neither gives the socket a clean identity:

> **The junction buys breadth and forfeits synergy.**

That is a real trade every time a good run card is drawn, and it costs zero explanation.

**Why no cross-lane runs.** Runs spanning lanes would reward splitting coverage, which fights lane triage.

### The 4-run is unreachable in the prototype

With three sockets per lane, no cross-lane adjacency, and the junction excluded, **a run of four cannot be
built.** The +35% tier is cut from the prototype table; **runs cap at 3.**

It returns in the full game when socket counts grow — which makes the **Surveyor** relic (one extra socket
per lane) meaningfully more interesting than a coverage bump, since it **unlocks a link tier**.

Enforced in data: `TuningLoader` requires exactly the run lengths the geometry can reach, so adding a
socket fails the load until the 4-run tier is restored rather than silently paying nothing.

### One run per tower

**A tower belongs to at most one run: the longest chain containing it.** No stacking a 2-run bonus on top
of a 3-run.

### Resolution algorithm, evaluated at lock

1. Within each lane, scan the socket chain and find **maximal consecutive-value sequences of length ≥ 2.**
2. **Tie-break:** if two maximal runs of equal length contend for the same tower, **the run whose lowest
   socket index is smallest wins**, and the leftover tower is unlinked.
3. Apply the bonus for the resolved run length.

Worked example of step 2 — **5-6-5** across sockets 3/6/9 yields `5-6` and `6-5`, both 2-runs sharing the
middle tower. The run at sockets 3–6 wins; the 5 at socket 9 is unlinked.

The forward-socket tie-break is deterministic, needs no UI, and **leans very slightly against the
deep-placement clustering** flagged in `03-march-clock.md`.

### The Queen

**The Queen takes exactly one value, chosen at lock to maximize run length.** She is not two values at
once — she is one value picked well.

`4-Q-6` across a lane resolves as **4-5-6, a full 3-run.** That bridging case is the entire reason she is
wild.

**One guard: a run must contain at least one non-Queen card.** Otherwise two adjacent Queens form a run
out of nothing.

### Why this matters

A 5 placed next to your 6 is worth substantially more than a 5 placed anywhere else, and the player can
see that before deciding whether the march step is worth paying.

Links are the main reason a card's *identity* matters spatially rather than only as a number added to a
sum, and they are what keeps a three-tower board from being a trivial arrangement.

They are also **the primary support for low-value cards**, which protects the thinning dilemma without
inflating base power.

*(Adjacency, run-per-tower, and Queen bridging were open questions after Revision 7.1; all three are
resolved above.)*
