# Tuning Constants

**Every number in the design, in one place.** Implement these as data — a Godot resource or config file —
not as inlined literals. Several are expected to change; two must be swappable at runtime for the test
arms.

> Every number here is first-pass and expected to be wrong. No number carries a confidence interval,
> validity window, or tolerance. Those are outputs of playtesting.

---

## Geometry

| Constant | Value | Notes |
|---|---:|---|
| Path length | 12.0 | Diamonds extend this in the full game |
| Socket positions | 3.0, 6.0, 9.0 | Per lane |
| Sockets per lane | 3 | |
| Lanes | 2 | Prototype |
| Junction sockets | 1 | Shared; reduced contribution |
| Total sockets | 7 | |
| Default tower range | 3.0 | |
| Face card range (10/J/Q/K) | 4.0 | Also no junction contribution penalty |
| Default entry point | 0.0 | |
| Full engagement (3 towers, entry 0) | 18.0 | Derived, not independent |

**Engagement formula:**
```
engagement(socket s, range r, entry e, path L) = max(0, min(s + r, L) - max(s - r, e))
total = Σ over occupied sockets
```

> ⚠ **Total engagement is explanatory, not a balance number.** Do not multiply board power by an engagement
> fraction to estimate output — sockets are not interchangeable, and coverage lost from a 5.0-power King is
> not coverage lost from a 1.6-power two. **Balance through the resolver.** See
> `../design/03-march-clock.md`.

---

## March Clock presets — **all three ship in the first build**

Presets in **one config file, not three builds.** These are also the three test arms.

### Arm C — hard escalation (the curve the design documents specify)

| Card | Step | Cumulative Entry | Engagement Remaining | Cost |
|---|---:|---:|---:|---:|
| 1st, 2nd | 0.0 | 0.0 | 18.0 | free |
| 3rd | +1.5 | 1.5 | 16.5 | −8% |
| 4th | +2.5 | 4.0 | 13.0 | −28% |
| 5th | +3.5 | 7.5 | **6.0** | **−67%** |

### Arm B — soft escalation

| Card | Step | Cumulative Entry |
|---|---:|---:|
| 3rd | +1.0 | 1.0 |
| 4th | +1.5 | 2.5 |
| 5th | +2.0 | 4.5 |

### Arm A — flat control (Revision 6's step)

| Card | Step | Cumulative Entry |
|---|---:|---:|
| 3rd | +1.0 | 1.0 |
| 4th | +1.0 | 2.0 |
| 5th | +1.0 | 3.0 |

> ⚠ **Arm letters were reassigned in Revision 7.1.** Pre-7.1 documents used **Arm A = as-specified, Arm B =
> flat control** — the reverse of A and C now. Always check the letter against the curve.

| Constant | Value |
|---|---:|
| Exactly-21 pullback | **−3.0 units** — **deliberately unchanged in 7.1** |
| `entryClampMin` | 0.0 |
| `entryClampMax` | **9.0** — the rear socket position; validated against `max(socketPositions)` |
| Steps beyond the 5th card | **repeat the final step**, then clamp |
| Engagement range | 18.0 down to 6.0 across 2–5 cards; **3.0 at the clamp** |

Step is paid **at the moment of the draw, before the card is revealed.** Not refunded on bust.

**The clamp applies before the pullback.** Order matters — a 6-card 21 lands at 6.0 (9.0 clamped, then
−3.0), recovering 9.0 engagement, rather than being stranded at the clamp.

| Hand | Unclamped | Entry | Engagement |
|---|---:|---:|---:|
| 6 cards | 11.0 | 9.0 | 3.0 |
| 7+ cards | 14.5+ | 9.0 | 3.0 |
| 6-card 21 | — | 6.0 | 9.0 |

Without the clamp a 7-card hand spawns enemies past the end of the path — zero engagement, guaranteed full
leak, an automatic loss for a legal and rare hand.

**Tuning direction, if the fifth card proves binary: reduce the fifth step before raising the pullback.**
Revision 7 advised the opposite and was wrong — raising the pullback makes the mechanic *more* binary. See
`../prototype/RISKS-AND-ADDBACKS.md`.

---

## Formation Strength

| Final Total | Multiplier |
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
| ≤11 | ×0.90 |
| Bust | ×0.80 |
| Persisted tower (start of next wave) | ×1.00 |

Curve span: 2.0×.

---

## Card power curve

Approximately `value^0.7`.

| Value | A(1) | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10/J/Q/K | A(11) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Base power | 1.0 | 1.6 | 2.2 | 2.6 | 3.1 | 3.5 | 3.9 | 4.3 | 4.7 | 5.0 | 5.4 |

| Constant | Value |
|---|---:|
| Ace Bastion power (natural blackjack) | 5.0 |
| Ace Bastion counts as a hand card | no |
| Ace Bastion shares hand multiplier | yes |

---

## Run links

| Run Length | Power Bonus |
|---|---:|
| 2 | +15% to both |
| 3 | +25% to all three |
| 4 | **absent — unreachable in prototype geometry** |

Consecutive values in **adjacent sockets**, direction-agnostic. Queen is wild (one value, chosen at lock
to maximize run length); Ace is 1 or 11 matching its current state. Computed at lock. A tower belongs to
**at most one run** — the longest chain containing it.

| Adjacency rule | Value |
|---|---|
| Within a lane | Linear: 3–6 and 6–9 adjacent, 3–9 not |
| `crossLaneAdjacency` | **false** |
| `junctionParticipatesInRuns` | **false** — the junction is a run island |
| Tie-break between equal-length runs | The run with the **smallest lowest socket index** wins |
| Guard | A run must contain **at least one non-Queen** card |

**The 4-run tier is absent on purpose.** Three sockets per lane, no cross-lane adjacency, junction
excluded → a run of four cannot be built. It returns when socket counts grow, which is what makes the
**Surveyor** relic a link tier rather than a coverage bump.

`TuningLoader` requires exactly the run lengths the geometry can reach, so adding a socket **fails the
load** until the 4-run tier is restored — rather than silently paying nothing.

---

## Enemies

| Enemy | Count | Health | Speed | Armor | Spacing | Leak Damage |
|---|---:|---:|---:|---:|---:|---:|
| Swarm unit | 8 | 4 | 1.00 | 0 | 0.45 s | 1 |
| Armored soldier | 3 | 12 | 0.65 | 1.5 flat | 1.50 s | 2 |
| Fast raider | 5 | 5 | 1.60 | 0 | 0.75 s | 1 |
| Siege engine | 1 | 30 | 0.40 | 2.0 flat | — | 5 |

| Constant | Value |
|---|---:|
| Armor damage floor | **0.25** — armor cannot reduce a hit below this |
| Spade / King armor bypass | Ignores **half** of flat armor |

### Dealer card → unit mapping

| Card | Unit |
|---|---|
| 2–4 | Swarm pack |
| 5–7 | Fast raiders |
| 8–10 | Armored soldiers |
| J | Skirmisher (lane-changes at junction) |
| Q | Standard bearer (buffs nearby enemies) |
| K | Siege engine |
| A | Herald — elite at 11, fragile scout at 1 |

---

## Rules and thresholds

| Constant | Value |
|---|---|
| Shoe size | **26** — two copies of each rank A–K |
| Reshuffle threshold | fewer than **8** cards remain before a wave |
| Dealer draw policy (prototype) | **stands on all 17s, including soft 17** |
| Dealer resolves on player bust | **yes, in full** |
| Overload damage | **equal to the busting card's base power** — does not scale with excess |
| `overloadTargetLane` | **`highest_visible_threat`** — the busting card is never placed, so it inherits no lane |
| `overloadTieBreakStake` | **`bastion`** |
| Adjustment window | **one move total** — relocate one tower one socket, *or* swap two adjacent towers |
| Standing-order changes | free; do not consume the move |
| Adjustment window on bust | **none** — placement locks immediately |
| Open/Held threshold | **Open** if predicted leakage ≥ **half** of empty-lane damage |
| Wave resolution time | **12–20 s** at normal speed |
| Tie-breaking | **spawn order** |

---

## Pacing targets

| Activity | Budget |
|---|---:|
| Hand decisions and placement | 14–19 min |
| Combat resolution | 6–9 min |
| Rewards and deck decisions | 6–9 min |
| Shops, events, routing | 4–6 min |
| Transitions and boss presentation | 2–4 min |
| **Total run** | **30–45 min** |

Run shape: 3 regions, 12 combat encounters, 27 waves.

---

## Resolved in Revision 7.1

### ✅ Fifth-card engagement — was −58%, is **−67%**

Revision 7 reported 7.5 units remaining at entry 7.5; **the correct value is 6.0.** The error was summing
socket 9's full 6.0 window against a remaining path of only 4.5 units — i.e. omitting the `min(s + r, L)`
term.

| Socket | Window | At entry 7.5 |
|---|---|---:|
| 3 | 0–6 | 0.0 |
| 6 | 3–9 | 1.5 |
| 9 | 6–12 | 4.5 |
| | | **6.0** |

All other rows were checked and hold: entry 1.5 → 16.5, entry 4.0 → 13.0, 4-card 21 at entry 1.0 → 17.0,
5-card 21 at entry 4.5 → 12.0.

**Downstream:** the engagement-fraction output estimates that used this figure are **withdrawn entirely**,
not corrected — see the warning under the engagement formula above.

### ✅ 3+3+5+5 engagement comparison — was 28%, is **38%**

18.0 against 13.0.

### ✅ March placement bias — was stated backwards

Revision 7 called the flat step a tax on **rear** placement. Entry advances from the spawn side, so it
consumes the **forward** socket's window first. It was a tax on **forward** placement.

This correction produced a **new** open risk: deep placement is weakly dominant whenever entry exceeds 0.
See `../design/03-march-clock.md` and `../prototype/RISKS-AND-ADDBACKS.md`.

---

## Invented for the resolver

> **Nothing in this section comes from the design.** The handoff specifies the *enemy* side of combat
> completely and the *tower* side not at all. There is no fire rate, no meaning for "power" in combat
> units, no tick rate, no splash or slow magnitude, no junction penalty, no spawn schedule, no lane
> assignment for Dealer units, and no base-wave composition outside one paragraph of prose in
> `../design/example-wave.md`. **The resolver cannot run without them.**

These were decided in one deliberate pass at Milestone 1 rather than invented at call sites, and they live
in `data/tuning.json` behind a comment block that says the same thing. They are first-pass and expected to
be wrong, exactly like every number above — but unlike those, **there is no design statement behind them to
check against.** Treat a disagreement here as a decision to revisit, not a bug.

### Simulation

| Constant | Value | Reasoning |
|---|---|---|
| `sim.tickSeconds` | **0.05** (20 Hz) | 240–400 ticks over the 12–20 s wave. Every tuned duration below and every enemy spacing lands on an exact tick boundary, so no spawn time needs rounding. **Nothing in the resolver rounds** — positions, health, and damage stay `double`. The loader rejects any duration that is not a whole multiple of the tick. |

### Towers

| Constant | Value | Reasoning |
|---|---|---|
| Meaning of card power | **damage per shot** | `shot = basePower × formationMultiplier × (1 + runBonus)`. |
| `towers.cooldownSeconds` | **1.0** | One shared cooldown, not one per family. The resolver contract names "cooldown" as a shared input so it has to exist; a single value keeps a first pass honest about being invented. |
| Default targeting | **nearest to the tower** | Ties by spawn order. Chosen so that *both* Focus modes stay meaningful — a leading-target default would make one of the design's own standing orders a no-op. |
| `towers.junctionPathPosition` | **6.0** | The middle socket. Derived from geometry the same way the entry clamp is; the loader fails the load if they disagree. |
| `towers.junctionContributionFraction` | **0.50** | The design says "reduced contribution" and never quantifies it. At 0.5 the junction's total throughput matches a lane socket's while being split across two lanes — it buys breadth and forfeits focus, on top of forfeiting runs. |
| `towers.junctionFaceCardExempt` | **true** | Stated by `../design/04-cards-as-defenses.md`; only the flag is new. |

### Ace Bastion placement

> **Introduced at Milestone 2.** The design specifies the Ace Bastion's *power* (5.0), that it does not
> count as a hand card, and that it shares the hand multiplier — but **not which socket it occupies or
> which family it wears.** `WaveDraft.AceBastion` decides both as first-pass choices; treat a disagreement
> here as a decision to revisit, not a bug.

| Constant | Value | Reasoning |
|---|---|---|
| Ace Bastion socket | **junction if free, else the deepest empty lane socket** | A King-class anchor has face-card range and the junction exemption, so at the junction it covers both lanes at full power — the natural home for a free anchor. A natural is two cards, so a socket is always free. |
| Ace Bastion family | **Club** | A neutral placeholder. The anchor has no design-stated suit keyword; Club is a first pass. Revisit in Milestone 3 with bust, stakes, and Overload, where the anchor's combat behaviour first matters. |

### Suit keywords

| Constant | Value | Reasoning |
|---|---|---|
| `suits.clubs.splashRadius` | **1.0** | A third of socket spacing: rewards hitting a clustered swarm without reaching from one socket's kill zone into another's. Falls out as counterplay — a column looser than 1.0 unit outruns the blast, which is why fast raiders (1.2 units apart) are splash-proof and armored soldiers (0.975 apart) are not. |
| `suits.clubs.splashFraction` | **0.50** | Armor and the damage floor apply **per hit**, so a swarm and an armored soldier in the same blast take different amounts. |
| `suits.spades.slowMultiplier` | **0.60** | |
| `suits.spades.slowSeconds` | **1.5** | |
| `suits.spades.slowStacks` | **false** | Refresh, never compound. Two Spades stacking to ×0.36 is a hard stop wearing the word "slow". The loader rejects `true`. |

### Standing orders

Hold's socket threshold and Focus's mode are **per-tower board state, not tuning**. Only trigger-on-group
needs numbers.

| Constant | Value |
|---|---|
| `standingOrders.triggerGroupMinEnemies` | **3** |
| `standingOrders.triggerGroupRadius` | **1.0** — the splash radius |

Hold takes a path position rather than a socket index, because the junction has no lane socket index and can
still hold. Focus is an enum (`None` / `PreferArmored` / `PreferLeading`) because the design offers two
*alternatives*, not two toggles — and it is a **preference, not a restriction**: a Focus-armored tower with
no armored target in range still fires.

### The wave

| Constant | Value | Reasoning |
|---|---|---|
| `waves.dealerCardDeploysFullPack` | **true** | A Dealer card deploys the whole pack from its enemy row: a 3 is eight swarm units, a King is one siege engine. This is what the design's own table describes — "Swarm pack — many, fragile" and "Fast raiders" against a singular "Siege engine" — and it is what makes the upcard readable as a threat shape. |
| `waves.dealerLaneAssignment` | **`alternate_from_vanguard`** | The Vanguard takes the encounter's vanguard lane; every later card alternates. Deterministic, unsteerable, and it keeps both lanes live so lane triage stays a real decision rather than resolving itself from the upcard. |
| `waves.groupGapSeconds` | **1.0** | Groups run sequentially within a lane — base wave, then Vanguard, then Dealer draws in draw order — each at its own spacing, separated by this gap. |
| Vanguard start | **at the entry point, at t=0** | "Already standing on the field" is *presentation* — the upcard is visible on the board during the draw — not a separate movement rule. A head start would be a second march system. |

### Encounters

Base wave composition now ships as data (`encounters` in `data/tuning.json`), because
`../design/example-wave.md` is the Milestone 3 acceptance test and needs to be replayable.

`example_wave`: lane one **Bastion**, three armored soldiers; lane two **Vault**, **six** fast raiders.
Six, not the roster's five — the document states lane two forecasts **6 damage** undefended and fast raiders
leak 1 each. **Encounter groups carry explicit counts that override the roster; the roster `count` is the
Dealer pack size.** Lane one's three armored soldiers leak 2 each, also 6.

### Resolver rules that are choices, not data

Two more decisions have no tuning key because they are structural:

- **Tick phase order** is pinned in `core/Resolve/Resolver.cs`: spawn → towers fire → remove dead → move →
  leak check. Firing before moving gives a tower its shot at an enemy in its final tick. Two towers that
  would each kill the same enemy produce different timelines depending which fires first, so this order is
  not allowed to be incidental.
- **Lanes resolve independently.** Enemies never leave their lane and a junction tower fires into both at a
  reduced contribution each, so a lane's outcome depends on nothing outside it. **The Skirmisher's
  junction lane-change is the one specified behaviour that breaks this** — it is not a rule added inside a
  phase, it is a change to the shape of the lane loop. See `core/Resolve/UnmodelledBehaviour.cs`.

---

## Known Discrepancies

Live in Revision 7.1. **Resolve deliberately; do not silently pick a side.**

### 1. Arm letters were reassigned — ⚠ material

Revision 7 had two arms: **A = as-specified, B = flat control.** Revision 7.1 has three: **A = flat,
B = soft, C = hard (as-specified).** A and C are effectively swapped.

**Two places in 7.1 still carry the old letters**, both in § 21:

| Location | Says | Should read |
|---|---|---|
| Add-Back 1 trigger | "Arm A shows players changing hit/stand" | *the primary arm* — A is now the flat control |
| Add-Back 4 trigger | "Arm A and Arm B both show the archetype unviable" | **all three arms** — § 20 states this explicitly and governs |

Any pre-7.1 reference to "Arm A (primary)" means what is now **Arm C**. Check letters against curves.

### 2. Thinning dilemma still asserts the withdrawn fifth-card claim — ⚠ material

§ 14 reads *"the fifth card is worth taking only if it lands exactly"* — but § 4 of the same document
**demotes that from design identity to unproven hypothesis**, and notes it would probably be unhealthy if
true.

**§ 4 governs.** Flagged in `../design/08-deck-economy-progression.md`.

### 3. Example wave pile count — minor, uncorrected

`../design/example-wave.md` says "six safe cards in a pile of twenty-two." With 3 player cards, 1 upcard,
and 1 hidden card removed from a 26-card shoe, **21** remain. The six safe cards (two each of A, 2, 3) are
correct. Cosmetic; affects the prose, not the system.

### 4. Example wave reports fractional leakage — ⚠ material, resolved in favour of integers

`../design/example-wave.md` gives lane leakage as **6.0 → 3.8 → 5.1 → 3.4**, but every enemy's
`leakDamage` is an integer (1, 2, 5) and no rule anywhere produces a fraction of one.

**Integers govern.** Leakage is the integer sum of `leakDamage` over units that reach the end. The
alternative — scaling a leak by the unit's surviving health — is a new rule the design never states, and
inventing one to match prose would put a mechanic in the game because of a typo.

> **Reproduce the decision, not the decimals.** The example's *shape* is the acceptance test: an undefended
> lane at 6, a Visible Threat below that, a Final Forecast that rises when reinforcements land, and an
> adjustment that brings it back down. Every one of those relations holds with integers.

### 5. Example wave quotes lane-ideal engagement, not the occupied-socket sum — minor

The worked example reports "full **18.0** engagement" with only **two** towers placed, then 16.5 and 13.0.
Those are the three-socket lane-ideal figures from `../design/03-march-clock.md`, not the board's actual
occupied-socket sum — and engagement is explicitly a property of **occupied** sockets.

**No engagement test may assert against the example's prose numbers.** The published tables in
`03-march-clock.md` are the ones with full occupancy behind them, and those are what
`tests/March/EngagementTests.cs` checks.

### 6. Wave resolution target and armored soldier speed are incompatible — ⚠ material, unresolved

`combat.waveResolutionSeconds` targets **12–20 s**. An armored soldier moves at **0.65** along a **12.0**
path, so it crosses in **18.46 s on its own**, and a full pack of three at 1.50 s spacing cannot finish
before **21.46 s** — as the only group in its lane, starting at t=0, with nothing else scheduled.

**No spawn schedule reconciles this.** The undefended worked example runs **25.4 s**; defended it runs
**17.0 s**, inside the window. So the pacing target implicitly assumes a board that kills things early.

All three inputs are design numbers, so this is logged rather than fixed. Three candidate resolutions, in
rough order of how little they disturb:

1. **Accept it** — the target describes a defended wave, which is every wave the player actually plays.
2. **Raise the armored soldier's speed** toward 0.8, which also softens its role as the slow anchor.
3. **Widen the target**, which is the least informative option because the target exists to keep combat
   watchable.

Pinned by `tests/Resolve/PacingTests.cs`, which fails if somebody resolves it — remove this entry then.

### 7. Vanguard pack size — table says plural, worked example says singular — ⚠ material

The unit table in `../design/06-dealer-and-enemies.md` reads "8–10 **Armored soldiers**" (plural, and
singular for "Siege engine"), which is what `waves.dealerCardDeploysFullPack` implements: a 10 deploys the
roster's **three** armored soldiers. But `../design/example-wave.md` calls the Vanguard 10 "**an** armored
soldier already standing at the head of lane one" — one unit.

**The table governs**, because pack size is what makes the upcard a readable threat *shape* rather than a
number, which is the whole stated point of the Dealer redesign. The consequence is that the worked example's
lane one is heavier than its prose suggests. Revisit if waves land too hard — this is the single largest
lever on wave size in the prototype.
