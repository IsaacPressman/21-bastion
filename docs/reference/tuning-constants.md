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
| Tower range by socket | 4.0, 3.0, 2.0 | **Forward to rear.** The socket-geometry remedy — see § Resolved at Milestone 5 |
| Junction tower range | 3.0 | The middle socket's, derived not tuned; the junction shares its ground |
| Face card range bonus (10/J/Q/K) | +1.0 | **Added to the socket's range, not substituted.** Also no junction contribution penalty |
| Default entry point | 0.0 | |
| Full engagement (3 towers, entry 0) | 17.0 | Derived, not independent: 7.0 + 6.0 + 4.0 |

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

## Rank stacking — **flag-gated, default off**

`../design/05-battlefield.md` § Rank stacking. **Not implemented — Milestone 6.** No `stacking` section
exists in `data/tuning.json` yet; the key names below are the intended shape, not current data.

Every one of these is a *rule* rather than a magnitude, which is deliberate: **stacking has no numeric
bonus to tune.**

| Constant | Value |
|---|---|
| `stacking.enabled` | **false** by default — the arms are measured without it first |
| Match rule | **Same rank**, not blackjack value. J+J stacks; J+Q does not |
| `stacking.maxDepth` | **2** in the prototype |
| Aces | **Cannot stack** |
| Power bonus | **none** |
| Run eligibility of a stacked socket | **none** — it cannot participate in a run |
| Families within a stack | May differ; both behaviors originate from the shared socket |
| Formation Strength | **Each layer keeps its own multiplier.** Stack power is the sum of the layers' individually modified power |
| Position, range origin, March exposure | **Shared** by both layers |

⚠ **No damage penalty, no shared-cooldown change, no rarity, no stack-specific upgrade.** If stacking
proves automatic, the *first* remedy is one spatial or cadence cost tested in isolation — and never
simultaneously with a March change (`../prototype/VALIDATION.md` § Rank-stacking sequence).

---

## Pacing targets

⚠ **Revised against the run layer and not re-tuned.** "Rewards and deck decisions" and "shops, events,
routing" are both absorbed by the single strategic order.

| Activity | Budget |
|---|---:|
| Hand decisions and placement | 14–19 min |
| Combat resolution | 6–9 min |
| **Strategic orders** | **6–10 min** |
| Charters, phase transitions, boss presentation | 2–4 min |
| **Total run** | **30–45 min** |

Run shape: **3 siege phases** (Encirclement, Breach, Last Stand) — not 3 regions.

| Constant | Value |
|---|---|
| Encounter play vs campaign decisions | **~70 / 30**, a pacing target settled by playtest, **not enforced as a timer** |
| Command-phase cadence | **30–60 s**; Charters and rare major events may run longer |
| Encounter budget | ⚠ 7.1's **12 combat encounters, 27 waves** is **not restated** by the run layer — see Known Discrepancy 10 |

---

## Run layer — first-pass constants

> **None of this is implemented, and none of it is prototype scope** (`../prototype/SCOPE.md`). Recorded
> here so that every number in the design stays in one place.

### Campaign time and resources

| Constant | Value | Source |
|---|---|---|
| Phase clock | **~8 campaign hours per phase**, config-tunable, **reset per phase** | `../design/12-campaign-time-and-orders.md` |
| Scout / Reconnoiter | **1h** | |
| Repair / Fortify | **2h** | |
| Train / Temper | **2h** | |
| Raid Dealer Supply | **3h** | |
| Muster | **1–2h** | |
| Hold / Redeploy | **0–1h** | |
| Concede | Varies; often **saves** time | |
| Favor cap | **3**, first pass | |
| Chips | **cut** — no general-purpose money resource exists | |
| Time expiring | Triggers the scheduled Dealer action or assault. **Never causes defeat** | |

### Geography and fronts

| Constant | Value |
|---|---|
| Outer fronts | **3** (North Gate, River Works, East Ward) **plus the Bastion** |
| Front states | **4** — Held, Compromised, Lost, Conceded |
| Neglect outcome table | **6–8** authored outcomes per front, **shown before the player commits time elsewhere** |
| Terrain generation | **Authored. No procedural baseline** |

### Dealer recruitment

| Constant | Value |
|---|---|
| Opposing shoe size | **26**, fixed under normal recruitment |
| Recruitment row | **3** visible candidates, each with a visible replacement target and marked Dealer intent |
| Recruitment cadence | **1** one-for-one replacement per strategic beat, in phases where recruitment is active |
| Raid effect | Removes, steals, or blocks **1** visible candidate |
| Adaptation lag | **One phase.** Phase II responds to Phase I signals; Phase III to Phase II |
| Permitted target signals | **Build composition and repeated tactical commitments only** |

### Doctrine, Charters, and card identity

| Constant | Value |
|---|---|
| Doctrine pieces per run | **4–7**, with no expectation that every run reaches the maximum |
| Doctrine build time | **1–2 encounters** per piece |
| Charters per run | **2**, normally after the first two siege phases |
| Modifiers per card | **1** — history beyond that cap is flavor |
| Exhaustion states | **2** — Fresh or Exhausted. Does not stack toward injury or death |
| Exhaustion duration | **1 encounter**, substituted by a same-rank **Reserve copy** |

---

## Resolved at Milestone 5

### ✅ Socket geometry — deep placement was weakly dominant, and range now varies by socket

**Open Decision 2, closed.** Range was one flat 3.0 for every socket, which gave all three an identical
6.0 window at entry 0. Advancement enters from the spawn side, so it could only ever eat the forward
socket's window — a tax on forward placement that a player avoids by building deep.

Measured, not asserted. `tests/Measurement/DeepPlacementSweep.cs` confirmed deep dominance in every arm,
which triggered the pre-committed reading in `../prototype/VALIDATION.md`: **fix the socket geometry before
the march curve.** `Sweep_candidate_geometries` then swept nine candidates against a selection rule
committed before the numbers were read — smallest worst-arm depth effect, tie-break on the smaller spread
between arms, rejecting strong shallow inversion. Output: `telemetry/geometry-candidates.csv`.

| Arm | Depth effect before | After | With run links, after |
|---|---:|---:|---:|
| A (flat) | −1.40 | +0.73 | +1.27 |
| B (soft) | −1.47 | +0.40 | +0.60 |
| C (hard) | −1.87 | +0.40 | +0.53 |

Negative means deep placement leaked less. **Deep dominance is gone in every arm.**

Three things worth carrying forward:

1. **Uneven socket spacing does not work.** It was the design's *first-named* remedy and it was measured:
   `[3,5,9]` and `[3,7,9]` left the margin unchanged or slightly worse. Moving the middle socket does not
   change which end advancement arrives from. **Do not retry it without a new argument.**
2. **The remedy overshoots slightly.** The residual is a mild shallow lean, largest in Arm A. Placement-depth
   logging stays in the instrumentation set to watch it in playtest.
3. **The march curve was untouched, and the clock did not soften.** The fifth card cost −67% before and
   −71% after. That was a constraint, not a coincidence: the arms are pre-committed test arms.

**This is a deliberate divergence from the Revision 7.1 archive**, which specifies a flat 3.0 range. It is
not a transcription bug — do not "correct" it back. Every number is first-pass and expected to be wrong
(CLAUDE.md hard invariant 11); this one was measured wrong and replaced.

---

## Resolved in Revision 7.1

### ✅ Fifth-card engagement — was −58%, is **−67%**

> ⚠ **Superseded by the geometry remedy above.** The reasoning below is correct and still worth reading —
> the dropped `min(s + r, L)` term is a live trap — but the arithmetic describes the flat-range geometry.
> Under range-by-socket the fifth card leaves **5.0 of 17.0**, a −71% cost.

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

18.0 against 13.0 under the flat-range geometry. Under range-by-socket it is 17.0 against 12.0, i.e.
**29%** — the comparison the correction was making still holds, and its magnitude barely moved.

### ✅ March placement bias — was stated backwards

Revision 7 called the flat step a tax on **rear** placement. Entry advances from the spawn side, so it
consumes the **forward** socket's window first. It was a tax on **forward** placement.

This correction produced a **new** open risk: deep placement is weakly dominant whenever entry exceeds 0.
**That risk was measured and remedied at Milestone 5 — see § Resolved at Milestone 5.** The direction
stated here is unchanged and still correct; what changed is that forward sockets now open wide enough to
be worth the tax.

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
| Ace Bastion socket | **junction if free, else the deepest empty lane socket** | A King-class anchor has face-card range and the junction exemption, so at the junction it covers both lanes at full power — the natural home for a free anchor. |
| Ace Bastion family | **Club** | A neutral placeholder. The anchor has no design-stated suit keyword; Club is a first pass. Revisit in Milestone 3 with bust, stakes, and Overload, where the anchor's combat behaviour first matters. |
| Ace Bastion on a full board | **no anchor** | *Corrected at Milestone 4.* The original reasoning here was "a natural is two cards, so a socket is always free" — false once towers persist: two fresh towers plus five carried ones fill all seven sockets from the third wave on. A natural on a full board now simply goes unanchored, because the anchor is a bonus and the alternative is destroying a tower the player never chose to replace. |

### Persisted towers as board objects

> **Resolved at Milestone 4.** `../design/05-battlefield.md` § The adjustment window lists *"Do persisted
> towers move on equal terms?"* among the five questions the one-global-move rule was meant to settle by
> construction. It settles the other four; this one it does not touch, so it is answered here.

| Question | Answer | Reasoning |
|---|---|---|
| Does a placement onto a persisted tower's socket replace it? | **yes** | § Persistence states the purpose outright: "sockets fill during the second wave, and every card after that forces a replacement." A carried tower that could not be replaced would make persistence a lockout rather than a source of scarcity. |
| Can the single adjustment move relocate or swap a persisted tower? | **yes, on equal terms** | The rule is "relocate one tower one socket," unqualified as to whose. By the third wave most of the board is carried, so a window that could not touch them would be a window over almost nothing. |
| Can a persisted tower take a standing order? | **yes, still free** | An order is a pre-committed conditional about how a tower fights *this* wave, not a property of the hand that placed it — and combat has no live input, so the alternative is a board of towers with no tactics. |

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

### Milestone 5: battery encounters and shoe presets

Both are named by `../prototype/VALIDATION.md` and specified by nothing.

**Battery encounters** (`battery_severe`, `battery_mild`, `battery_held`, `battery_even`,
`battery_vault_first`, `battery_bastion_first`). The scripted battery calls for "a severe Open lane,
versus a mild one, versus one already Held" and never says how severe. Severity is set by undefended
leak damage; the counts are chosen to separate the three clearly and are otherwise arbitrary.

Note the deliberate asymmetry between two of them: `battery_vault_first` and `battery_bastion_first` are
the same field with the **stakes exchanged and the waves left alone**, which changes the decision — that
is the triage question item 6 asks. The lane *mirror* used for variant B swaps stakes and waves
**together**, which preserves it. Confusing the two would turn a contrast into a duplicate.

**Shoe presets** (`baseline`, `faceHeavy`, `manyCard`). Step 3 of the regression procedure simulates
10,000 hands each for "baseline, face-heavy, and many-card shoes" without saying what those are. Every
preset holds the shoe at `rules.shoeSize`, so bust rate and board width stay comparable — a preset of a
different size would change the reshuffle cadence, which is a second variable nobody asked for.

| Preset | Composition | Measured effect |
|---|---|---|
| `baseline` | two of every rank | 2.91 cards/hand, 29.2% bust |
| `faceHeavy` | four of each ten-valued rank | 2.44 cards/hand, 25.5% bust |
| `manyCard` | loaded with A–5 | 3.58 cards/hand, 18.2% bust, entry 3.02 |

⚠ **`faceHeavy` busts *less* than baseline**, which reads backwards until you notice the simulation's
stand-on-17 policy: face-heavy hands reach 17 in two cards and never hit again. Do not read that column
as a difficulty signal.

### Milestone 3: bust, Overload, and Dealer face-card units

The handoff describes each of these qualitatively and gives no numbers or application shape. All are
first-pass; a disagreement is a decision to revisit, not a bug.

- **Overload application shape.** The design fixes the *magnitude* (the busting card's base power, from
  the card-power curve — no new tuning key) and the *target* (the highest current Visible Threat lane, ties
  to Bastion), but leaves "one enemy, all enemies, splash?" open. First pass: an **instantaneous burst at
  the opening tick, before any tower fires**, spending the base power on the units present in the struck lane
  in spawn order, spilling a kill's remainder onto the next. It **ignores armor** — it is raw card power, not
  a tower shot — and its victims are removed as an `OverloadEvent` rather than through the normal death phase,
  so they carry no killing socket. Because only the units present at the opening tick are hit, its reach
  depends on the spawn schedule. Modelled in `core/Resolve/Resolver.cs`; passed only on a bust.
- **Overload target is read pre-bust.** The lane is computed from the Visible Threat **as shown before the
  hit** — the "Bust → Overload: Lane X" the hand panel carries — i.e. against the board as it stands at the
  decision (placed towers at the pre-hit multiplier and entry), not against the busted ×0.80 board. See
  `core/Wave/WaveSession.cs`.
- **Standard bearer aura** (`enemies.standard_bearer.aura`): `radius 2.0`, `speedMultiplier 1.5`. The handoff
  says only "buffs nearby enemies". Modelled as a **speed** buff applied to other live units within radius in
  the **same lane** (never across lanes, so it does not couple the lane simulations), strongest aura wins, no
  self-buff, composing on top of any Spade slow. Applied in the resolver's move phase.
- **Herald split** (`herald` / `herald_scout`): the Ace is "an elite at 11, a fragile scout at 1". The elite
  row is `herald` (the original); the scout is a second invented row `herald_scout` (fragile, fast). A Dealer
  Ace held low deploys the scout via the `dealerCardUnits.A_low` key, chosen on `Card.AceHigh` in
  `DealerDeployment`. Scout stats (health 4.0, speed 1.6, leak 1) are invented.
- **Still deferred past Milestone 3:** Jack mobility and the Skirmisher's junction lane-change — both need
  mutable runtime position / lane coupling and are left stubbed in `core/Resolve/UnmodelledBehaviour.cs`.

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

Discrepancies 1–7 are live in **Revision 7.1**. Discrepancies 8–11 are between **Revision 7.1 and the Run
Layer Handoff**. Discrepancies 12–16 are between the **Improved Encounters Handoff** and what is already
decided, measured, or built. **Resolve deliberately; do not silently pick a side.**

> **12 is open in a deliberate, structured way** — a hypothesis with a pre-committed experiment and four
> pre-committed outcomes, not an unsettled argument. **13 and 14 are now decided** (encounter-local
> opportunity payouts; per-wave composition as a required schema change).

> **Where the two handoffs disagree, the run layer supersedes** — it says so in its own § 0 — **except on
> encounter-level arithmetic**, which it explicitly does not touch: the March Clock presets, the Formation
> Strength curve, and the deterministic resolver are unchanged.

> **The Improved Encounters Handoff has no such precedence clause.** It describes itself as consolidating
> decisions made *after* 7.1 and the run-layer addendum, and as *"intended to be folded into the main
> gameplay handoff later"* — which is what this pass did. It supersedes nothing automatically. **Where it
> collides with a measured result, the measurement holds until a new measurement replaces it**, which is
> the whole substance of entry 12.

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

### 8. The Vault stake's payload — ⚠ material, resolved in favour of the run layer

Revision 7.1 § 10 says a Vault leak costs **"Chips and Favor"** from the encounter's reward. The run layer
**cuts Chips outright** and rules that **Favor is never a reward-floor currency** — it is earned only
through its risk-and-stake conditions. Both halves of 7.1's payload are gone.

**Resolution: a Vault leak reduces the encounter's ordinary campaign reward** — the captured supplies, the
service exposed, the Muster or Rerank the Vault would have funded. The stake's *job* is unchanged: a lane
worth reward rather than health, so triage stays a real decision, which is exactly what scripted battery
item 6 tests.

**Nothing in the prototype changes.** The prototype models the stake as a lane-outcome label and an Overload
tie-break; it has never paid out a currency. Updated in `../design/05-battlefield.md` and `../GLOSSARY.md`.

⚠ **Add-Back 1 is affected.** "Comparison pays the Vault" must pay the campaign reward and **must not pay
Time**, or a blackjack outcome would buy campaign actions and re-open the door between the two clocks.

### 9. Two 26-card shoes — ⚠ material, unresolved

The **player's** shoe is 26 cards (two of each rank). The run layer gives the **Dealer** a fixed **26-card
campaign shoe** built by one-for-one replacements. These are two different objects that happen to share a
size.

**In the prototype they are the same pile.** `core/Dealer/DealerHand.cs` draws the upcard, the hole card,
and every draw-to-17 card from the player's remaining `Shoe`, and 7.1 § 11 says so explicitly ("one hidden
card dealt and **removed from the shoe**"). That shared pile is load-bearing for the marked-rank display —
the reading skill in `../design/09-information-and-ui.md` depends on Dealer cards leaving the same pile the
player draws from.

**Neither handoff reconciles them.** Three things are unresolved and none should be decided in passing:

1. Does the encounter still deal the Dealer's hand from the player's shoe, with the campaign shoe governing
   only *composition*? (Cheapest, and preserves the reading skill.)
2. Or does the Dealer draw from its own shoe at the table? (Matches the run layer's language, and **weakens
   the remaining-rank display**, because Dealer draws would no longer inform the player's bust risk.)
3. Either way, what does "26" mean when the player's shoe is being Acquired into and Cut from across a run?

**No code change is warranted yet.** Flagged so that whoever builds Dealer recruitment does not assume
option 2 by default.

### 10. The encounter budget is not restated — minor, un-reconciled

Revision 7.1 § 19 specifies **12 combat encounters and 27 waves** across three regions, with a per-region
composition (two regular, one elite, one boss, two or three noncombat nodes). The run layer replaces
regions with **three siege phases** and never restates either figure; its production sequence names a
**four-encounter mini-run** as an intermediate stage and nothing about the full count.

**Treat 12/27 as an un-reconciled first pass.** It is not contradicted, and it is not confirmed against a
structure where routing is a strategic order rather than a map. The pacing block it feeds (30–45 min) is
restated by the run layer and does survive.

### 11. Relic effects that violate run-layer constraints — ⚠ material, one instance

The relic layer is superseded by doctrine (`../design/13-doctrine-and-charters.md`), and most of 7.1's
eight named relics map forward cleanly. **One does not: Long Road**, "reduces the march curve for one
encounter."

Campaign time must never modify hand-scale March entry, and no campaign effect may reach into Formation
Strength or the march curve. A reward that softens the march curve is a campaign effect editing the
encounter's own pressure system.

**Do not carry Long Road forward without re-deciding it.** The shape to watch for is broader than the one
relic: any campaign-side effect whose payload is an encounter-arithmetic number.

### 12. Breakpoints versus range-by-socket as the deep-placement remedy — 🔬 **OPEN MEASURED COLLISION**

> **Status: deliberately open, and open in a specific way.** This is **not** an unresolved argument to be
> settled by whichever document is read last. It is a **hypothesis with a pre-committed experiment**, and
> until that experiment runs, **range-by-socket remains the baseline remedy and stays authoritative.**

| Source | Position |
|---|---|
| **Milestone 5** (measured) | Range varies by socket — **4.0 / 3.0 / 2.0**. Selected from nine candidates against a rule committed before the numbers were read. Deep dominance gone in all three arms |
| **Improved Encounters § 9** | **Spatial breakpoint enemies** are the *"baseline solution to deep-placement dominance"*, and *"do not give sockets arbitrary statistical bonuses to create identity"* |
| **Improved Encounters § 22** | *"Everyone builds deep → fix enemy timing **before touching socket bonuses**"* |
| **Improved Encounters § 24** | *"Arbitrary socket stat bonuses"* listed under **Explicitly Not Added** |

Range-by-socket **is** a positional statistical difference. Read literally, three sections of the new
handoff argue against a remedy that is already measured, shipped, and load-bearing — `TowerState.RangeFor`
is the single derivation and `TuningLoader` validates it.

#### The decision

**Range-by-socket remains the current baseline remedy. Breakpoints are added as a separate tactical-depth
hypothesis, not as its replacement.** They may eventually replace range asymmetry; **they have not earned
that yet.**

The Improved Encounters language is therefore **softened wherever it claims breakpoints are the baseline
solution.** The corrected wording, which governs and is restated in each affected document:

> **Spatial breakpoints give forward and middle positions distinct tactical jobs and may reduce or
> eliminate the need for socket-specific range. The currently validated range-by-socket values remain
> authoritative until breakpoints are implemented and re-measured in isolation.**

#### The experiment

Four steps, in this order, and **the isolation is the point**:

1. **Build breakpoint enemies while keeping the current 4.0 / 3.0 / 2.0 range.**
2. **Measure** — `tests/Measurement/DeepPlacementSweep.cs`, all three arms, as before.
3. **Run the exact same sweep at flat 3.0 / 3.0 / 3.0.**
4. **Compare.** Only then decide whether range asymmetry is still necessary.

> **Do not tune breakpoints and range together.** Two moving variables destroy the reading — the same
> discipline that governs the March arms, the geometry remedy, and the stacking pass.

#### The four outcomes, all valid, all pre-committed

| Result | Decision |
|---|---|
| Breakpoints alone solve depth bias | **Flatten range** to 3.0 / 3.0 / 3.0 |
| Breakpoints help, but not enough | **Retain some range asymmetry** — retune, do not revert |
| Combined system creates **shallow** dominance | **Reduce** range asymmetry |
| Breakpoints barely affect placement distribution | **Keep 4.0 / 3.0 / 2.0** unchanged |

Recording all four in advance is what stops the experiment from being read as a referendum on either
remedy. Note that the third is a live risk rather than a formality: the geometry remedy already overshoots
into a mild **shallow** lean (`../design/03-march-clock.md`), so breakpoints landing on top of it is the
outcome most likely to need a correction.

Flagged in `../design/03-march-clock.md`, `../design/06-dealer-and-enemies.md`, `../prototype/SCOPE.md`,
`../prototype/VALIDATION.md`, and `../ROADMAP.md` § Milestone 7.

### 13. The Paymaster pays Favor, which the prototype does not have — ✅ **RESOLVED**

Improved Encounters § 12 gives the Paymaster opportunity unit *"+1 Favor"*, and § 13 builds a whole
Favor-earning contract on battlefield risk. **Favor is a run-layer resource explicitly cut from prototype
scope** (`../design/12-campaign-time-and-orders.md`, `../prototype/SCOPE.md`).

#### The decision

> **No Favor in the prototype. Opportunity-unit payouts must be encounter-local. Favor and
> Dealer-recruitment rewards are full-run extensions of the same units.**

**And no substitute currency.** Inventing a prototype-only resource to carry these payouts would be
inventing an economy to test a placement question — the exact shape hard invariant 1 exists to prevent.

#### What an encounter-local payout looks like

The payoff lands **inside the encounter that offered it**, as a change to the wave the player is fighting:

| Unit | Killed before its breakpoint | Allowed through |
|---|---|---|
| **Supply Courier** | **Cancels a reinforcement group** scheduled later in this encounter | The reinforcement arrives normally |
| **Standard Wagon** | Upcoming enemies **lose a visible buff** | The buff activates |

Both are legible on the timeline, both are deterministic, and neither needs a resource to exist.

#### Why this is better for the prototype anyway

The question an opportunity unit exists to ask is:

> **Will a player risk another card for a non-survival tactical gain?**

**Favor is not needed to answer that** — and a currency payout would arguably answer a *different*
question, since a player chasing a campaign resource is reasoning about the run rather than the
battlefield. An encounter-local consequence keeps the fifth-card test where the prototype can read it, and
removes an economy dependency from the critical path.

The run-layer version of the same unit **adds** Favor or recruitment interference on top. That is an
extension, not a redesign.

### 14. Wave composition is not per-wave — ✅ **DECIDED: required schema change**

**This is not an open design question.** Improved Encounters § 11 requires Wave 2 to change the tactical
demand, but `EncounterTuning.BaseWave` holds **one** authored composition reused by every wave of the
encounter — so **the design cannot be expressed at all** in `data/tuning.json` today.

#### The change

`baseWave` becomes **per-wave authored data**, and is renamed, since a plural field called `baseWave` is a
trap:

```
encounter:
  waves:
    - <wave 1 composition>
    - <wave 2 composition>
```

`waves`, `waveDefinitions`, or `baseWaves` — any is clearer than the singular. Note that the existing
integer `waves` count then becomes **redundant**: the authored list carries it.

**Loader rule:** authored wave count **==** encounter wave count. `TuningLoader` fails the load otherwise,
in the same style as every other cross-field check it already performs.

#### Wave 2's goal is broader than literal counter-rotation

The handoff's worked example rotates lane roles (Swarm/Armor → Armor/Fast), and **that example should not
become the rule.** The authoring goal is:

> **Wave 2 creates a materially different tactical demand from Wave 1 and makes prior commitments
> relevant.**

Three shapes that satisfy it, only the first of which is a literal counter:

| Shape | Example |
|---|---|
| **Role reversal** | Swarm lane becomes the armor lane |
| **New breakpoint** | The same armor column, now led by a Standard Bearer that must die before socket 6 |
| **Relocated uncertainty** | The same lane roles, but the Dealer reinforcement now threatens the lane that was safe |

Requiring a literal reversal every time would make encounters predictable in exactly the way the
persistence test is supposed to prevent, and it would waste the two cheaper shapes — both of which reuse a
composition and change only what it demands.

**Sequencing:** this is a prerequisite for Milestone 8, not for Milestone 6 or 7. The improved encounter
is not implemented until it is done.

### 15. Standing orders are now editable earlier than 7.1 said — minor, resolved in favour of the new handoff

7.1 § 10 offers standing orders **in the adjustment window**. Improved Encounters § 17 makes them editable
**freely during planning and the adjustment window**, locking only when combat begins.

**The new handoff governs.** It is a widening rather than a contradiction, it does not touch the one-move
rule (orders never consumed the move), and it is what lets a Siege Club be told to hold at the moment it
is placed. Restated in `../design/05-battlefield.md` § Standing orders.

### 16. Four tower forms have no coefficients, and the enemy roster has no breakpoints — build gap, not a conflict

The handoff names Barrage, Siege, Snare, and Ambush behaviors qualitatively and says outright that their
coefficients are **open prototype questions** (§ 23.1). Likewise the four breakpoint enemies have no
health, speed, armor, breakpoint position, or effect magnitude.

`data/tuning.json` currently carries `suits.clubs` and `suits.spades` as single behaviors, and four enemy
types with no breakpoint field. **Every number these systems need would be an invention**, and the § Invented
for the resolver rule applies: invent them where the resolver cannot run without them, flag them in the
JSON, and record that a disagreement is a decision to revisit rather than a bug.

The Saboteur's disable duration is the one to watch — § 9 says *"do not begin with permanent
destruction"* and § 23.3 asks how severe breakpoints should be, so it is a first-pass number by
construction.

### 17. Hard Invariant 4's timeline asymmetry moved — ✅ **resolved at Milestone 6, and it is a refinement**

Milestone 1 implemented *"a Visible Threat must not be renderable where a Final Forecast is expected"* by
giving only the Final Forecast a timeline: with nothing on a Visible Threat to animate, playback could not
take one by accident.

Milestone 6 needs the encounter timeline **during the draw** — *"if you draw again, this cannon loses two
shots"* is the whole March decision, and it is unshowable once the Dealer has resolved. So a
`VisibleThreat` now carries a **`RevealedTimeline`**.

**The invariant is unchanged; its mechanism moved to the playback boundary.** `RevealedTimeline` and
`WaveTimeline` share no base class, no interface, and no conversion, and `TimelinePlayer`'s constructor
takes a `WaveTimeline` and nothing else — so a revealed force is **drawable and unplayable**. Both may
produce a `TimelineStrip`, which is a drawing model over raw events rather than a forecast, and that is
what lets the timeline stay one surface across the phase change.

Pinned by `tests/Resolve/RevealedTimelineTests.cs`, which asserts the type separation and the
constructor signature rather than trusting either to be remembered.

### 18. The Milestone 6 shortfall is anchored on the unit, not on a breakpoint — deliberate placeholder

`14-encounter-timeline.md` states the lane's requirement as *"armor-effective damage required before
socket 6"* — a **spatial breakpoint**, and breakpoints are Milestone 7.

Until one exists, `LaneConsequence` anchors the line on the leaking unit itself: `Required` is its
health, `Delivered` is what the formation lands on it, and `Shortfall` is the remainder. The sentence
keeps its shape — *"needs 2.1 more armor-effective damage"* — and only what it is measured against
changes when breakpoints arrive.

**Swap the anchor at Milestone 7, and do not treat the current values as a baseline for anything.** They
answer "what would it take to kill this unit at all", which is a strictly easier question than "before it
crosses the line that matters".
