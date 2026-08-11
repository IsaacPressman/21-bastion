# Cards as Defenses

Source: Handoff Revision 7.1, §§ 7, 9. **Prototype tower forms** and the **Snare → bunch → Barrage**
interaction are from the Improved Encounters Handoff, §§ 7, 8.

---

## Family and mode are locked at placement

> **When a card is drawn, the player chooses its family and its form, and that choice is permanent for the
> wave.**

This is **the design's primary commitment**. Family is chosen under uncertainty — before the hidden card
deploys, before the hand is complete — and cannot be undone once the wave is known.

The reasoning, which must survive any refactor: a player who could reassign families after full reveal
would place carelessly during the draw and solve the puzzle at the end, which empties the entire draw
phase of consequence.

**One tower's** position remains adjustable in the adjustment window (`05-battlefield.md`) — one move for
the whole board, not one per tower. **Neither family nor mode is ever adjustable.**

### What locks together

Placement commits four things at once, and the set grew by one under the Improved Encounters Handoff:

| Locked at placement | Adjustable afterwards |
|---|---|
| **Rank** | — |
| **Family** (Club / Spade) | Never |
| **Mode** (Barrage / Siege / Snare / Ambush) | Never |
| **Socket** | One move total, in the adjustment window |

Standing orders are the deliberate exception: they may be edited freely until combat begins and **do not
consume the positional move** (`05-battlefield.md` § Standing orders).

If this proves too punishing in testing, the fix is a *limited escape* — **not** reopening the window. The
run layer names the escape: the **Field Reassignment** doctrine, which lets the *first card placed in each
lane* be reassigned *after the Dealer reveal* (`13-doctrine-and-charters.md`). One Favor may also buy a
single reassignment where an effect permits it. See `../prototype/RISKS-AND-ADDBACKS.md`.

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

## Prototype tower forms

> **Status: DECIDED FOR PROTOTYPE.** The prototype keeps two families, and **each family receives two
> forms.**

These are **not extra complexity layered on top of four full-game families.** They are a replacement for
part of the tactical breadth that left with Hearts and Diamonds. Read them against `../prototype/SCOPE.md`
§ Cut from prototype: the cut stands, and this is what fills the hole it left.

### Club — Artillery

| Form | Role | Behavior |
|---|---|---|
| **Barrage Club** | Anti-group / splash | Faster firing, splash damage, **weak against heavy armor**, benefits strongly from compressed groups |
| **Siege Club** | Anti-armor / priority target | Slower firing, strong single-target damage, armor penetration or high armor-effective damage |

Siege is the answer to armored soldiers, Siege Engines, Standard Bearers, and anything else the lane
names as a priority. Barrage is the answer to a column that is — or can be made — tightly packed.

### Spade — Control

| Form | Role | Behavior |
|---|---|---|
| **Snare Spade** | Flow control | Lower direct damage, slows, **creates bunching**, sets up Barrage Clubs |
| **Ambush Spade** | Burst / precision trap | Higher one-time damage, **limited trigger count or long rearm**, strong against one dangerous target crossing its trigger point |

### The UI rule

The player chooses among **four direct deployment forms** — Barrage Club, Siege Club, Snare Spade, Ambush
Spade. **Do not build a two-step Family → Mode menu in the prototype.** Internally, family and mode may
remain separate data fields; that is a storage decision, not an interface one.

### Full-game mode structure — OPEN

**Do not assume four families × two modes = eight live choices per card.** The prototype tests whether
tactical forms add value at all. The full game might give modes to only some families, make alternate
forms upgrades, promote a prototype mode to a full family, or ship one form per family with doctrine
granting the alternative. **No commitment yet.**

---

## Tower-to-tower tactical interaction

> **Status: DECIDED.** The prototype needs at least one interaction where **one tower changes the
> battlefield so that another tower becomes more effective.**

Runs are positional synergy, but they are still a percentage bonus. The encounter also needs *behavioral*
synergy — a reason to build a sequence rather than accumulate damage.

### Snare → bunch → Barrage

The deterministic bunching rule lives with enemy movement (`06-dealer-and-enemies.md` § Deterministic
bunching), because it is a property of how enemies march rather than of the tower that causes it. Its
consequence here: a Snare Spade compresses the column upstream of the slowed unit, and **Barrage splash
becomes stronger against that compressed group.** The timeline must visibly show the compression, or the
interaction may as well not exist (`14-encounter-timeline.md`).

### The three interactions the prototype must support

1. **Control → Splash.** Snare compresses a group; Barrage exploits it.
2. **Standing Order → Priority Damage.** A Siege Club holds fire for the correct armored or high-priority
   target instead of spending its cooldown on the first thing in range.
3. **Early Kill → Enemy Formation Disruption.** Kill a breakpoint enemy before its trigger and prevent a
   downstream threat entirely (`06-dealer-and-enemies.md` § Spatial breakpoints).

> **The player should be building sequences, not only accumulating DPS.**

The failure signal is specific and worth pre-committing to: if Snare and Barrage are each independently
useful but **never intentionally combined**, the bunching interaction is too weak or too hard to read —
and the fix is legibility or magnitude, not a new mechanic.

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
| **All 10/J/Q/K** | **+1.0 range on top of whatever their socket grants**, and may occupy the shared junction socket without the usual contribution penalty. |
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

It returns in the full game when socket counts grow — which makes **Surveyor** (one extra socket per lane)
meaningfully more interesting than a coverage bump, since it **unlocks a link tier**. Under the run layer
that effect lives in doctrine, a Charter, or a front's geography rather than in a relic
(`13-doctrine-and-charters.md`); wherever it lands, the link-tier consequence travels with it.

> **Rank stacking interacts with runs by forfeiting them.** A stacked socket cannot participate in a run at
> all (`05-battlefield.md` § Rank stacking). That is the whole trade — density buys socket economy by
> giving up the thing spread is built on — so runs need no stacking-specific rule.

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
