# Roadmap

**Status: Milestone 6 complete.** The encounter information pass is in: the fully known base wave with
spawn timing, the encounter timeline as its own region under the board, exact per-lane committed-state
statistics, counterfactual memory of the last card, the hidden card's destination lane, standing orders
editable throughout, candidate deltas with no sortable scalar, and hover instrumentation. Everything
Milestone 5 shipped still holds — three selectable arms, 17 scripted cases, JSONL logging, and the four
regression procedures as one filtered suite. **Open Decision 2 remains closed** (below).

**Next: Milestone 7, tactical depth** — breakpoint enemies, deterministic bunching, and the four tower
forms. **Build them at the current 4.0 / 3.0 / 2.0 range and change nothing about geometry**; the
geometry question is settled afterwards, in isolation.

> ⚠ **The milestones after 5 were renumbered by the Improved Encounters Handoff.** Rank stacking was
> Milestone 6; **it is now Milestone 9**, because the handoff moves it to the end of a seven-step sequence
> on the explicit grounds that *"stacking should deepen a functioning placement game, not rescue a shallow
> one."* Any note referring to "Milestone 6, the stacking pass" means what is now Milestone 9. The siege
> menu probe moved from 7 to 10.

This roadmap sequences the prototype defined in `prototype/SCOPE.md`. It is derived from the design, not
stated by it; the handoffs specify *what* to build, not *in what order* — **with two exceptions that are
stated**: the run layer's production sequence (§ Run-layer sequencing) and the Improved Encounters
Handoff's build order (§ Improved-encounter build order). Milestones 6–9 follow the second; 10 onward
follow the first.

---

## Open decisions

### 1. GDScript or C#? — ✅ **resolved: C#**

Settled at Milestone 0. The validation architecture requires enumerating all legal 2–5 card hands and
simulating 10,000 hands across three shoe configurations, plus forecast-equivalence tests — a real test
suite over a real headless simulation core. `dotnet test` with the core as a plain class library gives
that directly; GDScript would have meant building harness infrastructure that C# provides for free.

Enforced structurally: `core/Bastion.Core.csproj` fails the build if it ever references GodotSharp.

### 2. Socket geometry needed work before anything else — ✅ **resolved: range varies by socket**

Revision 7.1 flagged that **deep placement is weakly dominant whenever entry exceeds 0**: advancement eats
forward socket windows and leaves rear ones untouched. The pushback — run-link adjacency, the junction
socket, traps needing early application, leak thresholds — **lives in the resolver, not the engagement
arithmetic**, so it could not be settled on paper.

**It was measured, it held, and it has now been fixed.**

#### The measurement

`tests/Measurement/DeepPlacementSweep.cs` sweeps every socket permutation for boards of 2–4 towers across
all three arms, with identical cards throughout so that neither card power nor a run link can explain a
difference. Comparisons are made **within a fixed board shape** — same number of towers in each lane and
the junction. That control matters: a naive deepest-versus-shallowest split over the raw sweep reports the
same conclusion for the wrong reason, because the junction sits at mid-depth while also covering whichever
lane the player neglected.

Deep placement won in every arm, and the margin scaled with the clock — which triggered the pre-committed
reading in `prototype/VALIDATION.md`: **the socket geometry needs work before the march curve does.**

#### The remedy

`Sweep_candidate_geometries` swept nine candidates against a selection rule **committed before the numbers
were read**: smallest worst-arm depth effect, tie-break on the smaller spread between arms, rejecting any
candidate that merely inverts the bias into strong shallow dominance. Output: `telemetry/geometry-candidates.csv`.

**Winner: range differs by socket — 4.0, 3.0, 2.0, forward to rear.** Socket positions are unchanged.

| Arm | Curve | Before | After | With run links, after |
|---|---|---:|---:|---:|
| **A** | flat | −1.40 | **+0.73** | +1.27 |
| **B** | soft | −1.47 | **+0.40** | +0.60 |
| **C** | hard | −1.87 | **+0.40** | +0.53 |

Negative means deep placement leaked less, i.e. deep won. **Deep dominance is gone in every arm.**

Forward sockets now open with a wider window and therefore have more to lose; rear sockets open with less
and lose none. A short hand is better off forward, a hand that paid for a fifth card is better off deep,
and the crossover is the decision.

Runner-up was `range-mid` (`[4.0, 3.0, 2.5]`), tied on worst-arm effect at 0.733 and beaten on the
tie-break — spread between arms 0.533 against 0.333.

#### Three findings worth keeping

1. **Uneven socket spacing does not work, and it was the design's first-named remedy.** `[3,5,9]` and
   `[3,7,9]` left the margin unchanged or slightly worse than the control. Moving the middle socket does
   not change which end advancement arrives from. **Do not retry it without a new argument.**
2. **The remedy overshoots slightly.** The residual is a mild *shallow* lean, largest in Arm A (+1.27 with
   run links modelled) and smallest in Arm C — the curve the design specifies. Placement-depth logging
   stays in the Milestone 5 instrumentation set to watch it in playtest.
3. **The march curve was not touched and the clock did not soften.** The fifth card cost −67% before the
   remedy and −71% after. That was a hard constraint: the three arms are pre-committed test arms, and a
   geometry change that quietly flattened the curve would have answered the fifth-card question before the
   playtest could ask it.

Two caveats on the original measurement, neither of which changed the verdict:

1. **Run links are modelled** (Milestone 2) and the sweep was re-run with them, output in
   `telemetry/deep-placement-runs.csv`. Ranks follow a depth-symmetric valley (6-5-6) so a contiguous pair
   forms a 2-run at identical total power whether it sits shallow or deep. Before the remedy the margin
   held and modestly widened (A −1.80, B −2.07, C −2.13); after it, all three are positive.
2. The spawn schedule and tower cooldown are inventions of Milestone 1
   (`reference/tuning-constants.md` § Invented for the resolver). Entry-0 timing effects in particular are
   a direct consequence of a 1.0 s cooldown.

### 3. Run-link adjacency rules — ✅ **resolved**

Full rules and reasoning in `design/04-cards-as-defenses.md` § Adjacency. In short: linear within a lane,
no cross-lane adjacency, junction adjacent to neither (a run island). One run per tower — the longest
chain containing it. Ties resolve toward the smallest lowest socket index. The Queen takes one value
chosen at lock to maximize run length, and a run must contain at least one non-Queen card.

**Consequence: the 4-run is geometrically impossible in the prototype**, so the +35% tier is cut and runs
cap at 3. It returns with larger socket counts, which upgrades the Surveyor relic from a coverage bump to
a link-tier unlock. Encoded in `TuningLoader`, which fails the load if the tuned run lengths and the
geometry disagree.

### 4. Overload's target lane — ✅ **resolved, and the premise was wrong**

There is no provisional placement to carry. On a bust the card is destroyed and **never placed at all**,
so "the lane where it was provisionally placed" described something that does not happen.

**Overload strikes the lane with the highest current Visible Threat; ties break toward the Bastion lane.**
Deterministic, no new draw-phase UI, no provisional state, and unsteerable. Shown in the hand panel's bust
branch as "Bust → Overload: Lane 1" alongside the ×0.80. See `design/07-bust-and-overload.md`.

### 5. March steps beyond the fifth card — ✅ **resolved**

Repeat the final step indefinitely, **and clamp entry at 9.0** — the rear socket's position, so enemies
never spawn past the player's last defense. The clamp applies **before** the pullback, so a six-card 21
lands at 6.0 with 9.0 engagement recovered.

Two consequences **noted and accepted, not fixed**:

1. **Past the clamp, cards are free on the clock.** A seventh card costs nothing in march terms. Mild
   perverse incentive, accepted — bust probability at six-plus cards is enormous, and every further card
   forces replacing one of your own towers. Flag it; do not engineer around it.
2. **Board width caps at seven.** Card eight onward replaces something the player placed, so beyond seven
   cards there is no board benefit at all — pure cost. Falls out of the existing capacity rule.

---

## Milestones

### Milestone 0 — Foundation ✅

- ✅ Language settled (C#); three-project solution scaffolded, `net8.0` to match GodotSharp
- ✅ Git initialized on `main` (files staged, not committed)
- ✅ Tuning-data layer: `data/tuning.json` holds every value from `reference/tuning-constants.md`,
  none inlined — **including all three march-curve presets**, with a validating loader
- ✅ Test harness runnable headless — no scene tree, no window
- ✅ Oracle gate (`DebugGate`) for the instrumentation-only values in `design/09-information-and-ui.md`
- ✅ Build-time guard: `Bastion.Core` fails the build if it references GodotSharp

- ✅ Scene wiring: `scenes/root.tscn` carries `Bootstrap` and is set as `run/main_scene`; verified running
  in the editor

**Done when:** a test can load tuning data and assert a value, with no Godot window opened. **Met**, and
the Godot layer is confirmed to reach the core and read `data/tuning.json` through `res://` — the one link
the headless tests cannot check.

### Milestone 1 — The resolver ✅

The single deterministic simulation that drives both forecast and wave. **Built first**; everything else is
a client of it.

- ✅ Path, sockets, entry point, **per-socket** engagement calculation — `core/March/`, with the entry
  clamp and the exactly-21 pullback, checked against every published table
- ✅ Enemy spawning by schedule and spacing, movement, leak damage — `core/Resolve/ArmyBuilder.cs`
- ✅ Tower targeting, range, cooldown, armor with the 0.25 floor and half-armor bypass for **Spades and
  Kings**; face-card range 4.0; Club splash and Spade slow
- ✅ Deterministic tie-breaking by spawn order, everywhere, through one stated rule
- ✅ Standing orders — Hold, Focus, Trigger on group — modeled exactly, no approximate tier
- ✅ Per-lane outputs: empty-lane damage, predicted damage, damage prevented, per-tower activity, cause of
  leakage
- ✅ **Two forecast return types** — `VisibleThreat` and `FinalForecast` — sharing no base class, no
  interface, and no conversion. ⚠ **Amended at Milestone 6:** a Visible Threat now carries a
  `RevealedTimeline`, because the encounter timeline has to be readable *during the draw*. The
  asymmetry moved rather than disappeared — it is a **different type** from the Final Forecast's
  `WaveTimeline`, and `TimelinePlayer` takes a `WaveTimeline` and nothing else, so a Visible Threat
  is drawable and still unplayable
- ✅ Deep-placement dominance measured (Open Decision 2 above)

**Done when:** the resolver runs headless, produces identical output for identical input across runs, and
reproduces the engagement tables in `design/03-march-clock.md`. **Met.**

**Two invariants established at this milestone**, both hard to retrofit:

1. There is **one simulation path**. The visual wave is a *presentation* of a resolver run, never a
   re-simulation.
2. There are **two forecast types**, and one cannot be rendered where the other is expected. Since
   Milestone 6 that is enforced at the *playback* boundary rather than by the absence of a timeline:
   both carry a schedule, only one carries a `WaveTimeline`, and only a `WaveTimeline` can be
   animated.

**A third fell out of the build and is worth keeping.** The board and the army each carry the entry point,
and the resolver **rejects a pair that disagrees**. Both are downstream of the same March Clock reading, so a
mismatch means one was built against a different moment in the hand — which would resolve cleanly and answer
the wrong question. That is the one failure a forecast contract cannot survive.

**What Milestone 1 does not model**, deliberately and by name in `core/Resolve/UnmodelledBehaviour.cs`: Jack
mobility, Queen wildness (Milestone 2), the Standard bearer's buff, the Skirmisher's junction lane-change,
the Herald's 1-vs-11 split, and Overload's application (Milestone 3). **The Skirmisher is the one that
matters structurally** — lanes currently resolve independently, and a unit crossing between them changes the
shape of the lane loop rather than adding a rule inside a phase.

### Milestone 2 — Hand and formation ✅

The producer middle: Milestone 1 built every *consumer* (`TowerState`, `BoardState`, the resolver) and
every tuning input; Milestone 2 builds the logic that *writes* those fields. **No resolver changes were
needed.**

- ✅ Blackjack: hit, hard/soft totals, Ace 1/11 with immediate battlefield transformation — `core/Hand/HandState.cs`
  (ace state derived from the hand multiset, so the 11→1 flip is one recomputation, not a mutation)
- ✅ Shoe: 26 cards, persistence across waves, reshuffle under 8, deterministic from a seed — `core/Cards/Shoe.cs`
- ✅ Formation Strength curve, consumed via `HandState.FormationMultiplier`
- ✅ Card power curve; **Ace Bastion on natural** — `core/Hand/WaveDraft.cs`
- ✅ Family selection locked at placement (Club/Spade), enforced in `core` by construction
- ✅ Run links — full algorithm in `core/Board/RunLinks.cs` (Queen wildness, Ace state, junction island,
  no cross-lane, one run per tower, the equal-length tie-break). `QueenWildness` removed from `UnmodelledBehaviour`
- ✅ Socket occupancy and forced replacement at capacity — placing onto an occupied socket replaces its tower
- ✅ March Clock escalating step and exactly-21 pullback, now driven from a real hand via `WaveDraft`

**Done when:** the output landmarks table in `design/02-blackjack-and-formation.md` reproduces exactly, and
`3+3+5+5` and `10+6` produce visibly different boards. **Met** — `tests/Hand/OutputLandmarkTests.cs`,
`tests/Hand/EnumerationTests.cs`, and the run/hand/shoe suites. Open Decision 2 was re-measured with runs
modelled (above); the margin held.

**Scope was held at the final board.** Deferred to Milestone 3, by name: the Dealer's draw policy, the
adjustment window, bust's Overload *strike*, lane-stakes wiring into a wave loop, the phase state machine,
and Jack mobility (a resolver-time behaviour, still stubbed in `Resolve/UnmodelledBehaviour.cs`). The
Ace Bastion's socket and family are first-pass choices flagged in `reference/tuning-constants.md`
§ Ace Bastion placement, to revisit in Milestone 3.

### Milestone 3 — Wave loop ✅

The orchestration layer. Milestones 1 and 2 built every consumer and producer; Milestone 3 drives them
through the phases of a wave and enforces the phase boundaries. **The resolver's shape was unchanged** —
the two additions (an optional Overload burst, the Standard-bearer aura) are rules inside phases that
already existed, not a new lane loop.

- ✅ Full phase state machine from `design/01-core-loop.md` — immutable `core/Wave/WaveSession.cs`
  (`WavePhase`: AwaitingPlacement → DrawDecision → AdjustmentWindow → Locked, plus a terminal BustLocked)
- ✅ Dealer: upcard deployed as Vanguard pre-deal, hidden card, **draws to 17** (`core/Dealer/DealerHand.cs`,
  stands on all 17s), every card deploys
- ✅ Bust: card destroyed, ×0.80, **Overload at base power** struck at the highest current Visible Threat
  lane (read pre-hit, unsteerable), no adjustment window, Dealer resolves in full
- ✅ Adjustment window: **one move total** (relocate one socket or swap two adjacent), standing orders free,
  families locked — producers on `core/Hand/WaveDraft.cs`, enforced by `WaveSession`
- ✅ Lane stakes: Bastion and Vault, surfaced per lane and used in the Overload tie-break
- ✅ Persistence with ×1.00 reversion at the wave boundary (`WaveSession.Settle`)
- ✅ **Standard-bearer buff and Herald 1/11 split** modelled (the two localized Dealer face-card behaviours);
  **Jack mobility and the Skirmisher lane-change remain deferred** — both need mutable runtime position /
  lane coupling, still stubbed in `Resolve/UnmodelledBehaviour.cs`

**Done when:** `design/example-wave.md` replays end to end and every number in it matches. **Met** —
`tests/Wave/ExampleWaveReplayTests.cs`, honoring the doc's own discrepancy carve-outs (integer-governed
leakage, pile count 21, the 14 s timing not asserted). The invented pieces (Overload application shape, the
Standard-bearer aura, the `herald_scout` row) are flagged in `reference/tuning-constants.md` § Invented.

**The socket-geometry remedy owed here was delivered at Milestone 5** — see Open Decision 2. Milestones 3
and 4 left socket geometry untouched by design.

### Milestone 4 — Presentation and information ✅

Built in `game/presentation/` and `game/input/`, wired by the composition root in `game/Bootstrap.cs`:
`PhaseHeader`, `BattlefieldView`, `BattlefieldPanel`, `HandPanel`, `CombatPlaybackView`, `PostWaveView`,
`BoardInteraction`, `PhaseControls`. The UI is built in code rather than authored in the scene file, so
every view is version-controlled C# and the scene is just an entry node.

**The UI cannot be reviewed by reading it** — anchored regions and hand-drawn geometry only resolve at a
real viewport size — so `game/devtools/CaptureRun.cs` walks a scripted wave and screenshots every phase.

- Two separate panels: hand consequences, battlefield consequences. **No combined widget.**
- **Visible Threat** during the draw, labelled as revealed-force only, updating on every draw and placement
- **Final Forecast** after Dealer resolution, labelled as the combat contract
- Remaining rank composition with busting ranks marked — **no percentage**
- Entry position, and **which socket windows the next march step cuts into, drawn on the lane** — not a
  single engagement number
- Open/Held labels on the plain threshold, raw number primary
- Watchable, fast-forwardable, skippable combat playback
- Post-wave leak explanation

**Done when:** every item in `design/09-information-and-ui.md` § Shown is present and every item in § Not
Shown is absent. **Met.**

### Milestone 5 — Validation build ✅

- ✅ **Arms A, B, and C selectable by configuration** — `--arm A|B|C` after `--`, applied by rebuilding
  tuning immutably (`game/Startup/LaunchOptions.cs`, `Bootstrap.SelectArm`). Never writes the file.
- ✅ Scripted battery — `data/battery.json` + `core/Validation/`. All ten items of
  `prototype/VALIDATION.md` § Scripted battery are covered; several name a *contrast* rather than one
  state, so they expand to **17 cases**, each presented **twice** as a lane-mirrored variant, for 34
  presentable states. `--fixture 2-split`, or a facilitator picker when no case is named.
- ✅ Full instrumentation — `core/Validation/StateRecord.cs` and `SessionSnapshot` for everything
  derivable from a session, `game/telemetry/PlaytestLog.cs` for the four things only the interface
  knows. JSONL, one line per offered state, in `telemetry/sessions/` (gitignored).
- ✅ **The fifth-card outcome measurement** — `tests/Measurement/FifthCardOutcomeSweep.cs`, output in
  `telemetry/fifth-card.csv`. Result below.
- ✅ The four regression procedures as one suite — `dotnet test --filter Category=Regression`.

**Done when:** a playtest session can be run, logged, and analyzed without code changes between arms.
**Met.**

#### The mirror is a checked claim, not an assertion

"Each state presented at least twice with different presentation" is only worth anything if the two
presentations are *the same decision*. Variant B is **generated**, not hand-authored — lanes swap
wholesale and the opening deal order reverses — and `tests/Validation/BatteryTests.cs` asserts that a
mirrored case's Final Forecast is the original's with the lanes exchanged. Hand-authoring both halves
is precisely how two presentations quietly stop being the same decision.

#### The fifth-card measurement did not come out as predicted

The design expected Arm C to make a safe fifth card **functionally dead**, with the pre-committed
consequence that *if it does, Arm B is the design*. By resolver output, it does not.

| Arm | Safe fifth card was the better play | Mean leak delta |
|---|---:|---:|
| A (flat) | 71.6% | −4.29 |
| B (soft) | 68.0% | −3.80 |
| C (hard) | **48.4%** | **−0.74** |

Negative delta means hitting leaked less. The arms separate strongly and in the expected direction —
Arm C very nearly neutralises the fifth card *on average* — but case by case it is still worth taking
about half the time, which is not "dead".

**The shape is the more interesting result, and it reproduces the design's narrowed claim.** In every
arm there is a clean crossover at 18:

| Four-card total | 14 | 15 | 16 | 17 | **18** | 19 | 20 |
|---|---:|---:|---:|---:|---:|---:|---:|
| Arm C, hit better | 71% | 65% | 57% | 59% | **16%** | 1% | 0% |

**Three caveats, none of which are grounds for renegotiating the reading:**

1. **This is only half the measurement.** VALIDATION.md asks for the resolver comparison *and*,
   separately, whether players say they would take it again. The arm verdict needs both halves; only
   the first exists yet.
2. **It conditions on the card being safe.** The *decision* to hit also carries bust risk, and at 16+
   most cards bust. "A safe fifth card is usually good" is entirely compatible with "hitting is usually
   bad" — bust risk is the counterweight, and that is the design working rather than failing.
3. **Placement is optimised on both sides.** An extra tower is worth more to an exhaustive search than
   to a person. The comparison is fair, but it is an upper bound on what the fifth card buys.

#### Findings worth carrying forward

- **Run frequency is 38–43% of hands** across all three shoes (`telemetry/shoe-simulation.csv`). The
  pre-committed reading was that runs *too rare to shape placement* trigger Add-Back 3 (pairs). They are
  not rare. **Add-Back 3 is not triggered.**
- **Face-heavy busts less than baseline** (25.5% against 29.2%), which reads backwards until you notice
  the simulation's stand-on-17 policy: face-heavy hands reach 17 in two cards and never hit again. Worth
  remembering before treating that column as a difficulty signal.

### Milestone 6 — Encounter information and the timeline ✅

**The first and largest stage of the Improved Encounters Handoff**, and the one its diagnosis says matters
most: *the problem is not insufficient decision count, it is that the player cannot form a concrete
intention before drawing.* Everything here is an **information** change. No new mechanics.

- ✅ **The base wave is fully known** before the opening hand — types, counts, spawn order, timing, lane
  assignment, and empty-lane damage, stated in arrival order (`game/presentation/BattlefieldPanel.cs`).
  Breakpoint abilities wait on Milestone 7, which is where breakpoints exist
- ✅ **The encounter timeline** — its own region under the board, `x = time`, one row per lane:
  `core/Resolve/TimelineStrip.cs` and `game/presentation/TimelineView.cs`. Enemy groups as scheduled
  markers, tower firing bands with a tick per shot, slow spans, Hold markers, the located hidden card,
  and a ghost row for the next march step
- ✅ **Exact committed-state statistics** per lane — `core/Resolve/LaneConsequence.cs`: which enemy gets
  through, first leak time, armor-effective damage required and delivered, the shortfall, attacks per
  tower. The shortfall is anchored on the leaking unit; it moves onto a breakpoint at Milestone 7
- ✅ **Counterfactual deltas** after a card is committed — `game/presentation/CounterfactualPanel.cs`,
  built from the same `CandidateDelta` the hover shows, so the memory of what a card did cannot
  disagree with what it did
- ✅ **The hidden card's destination lane is visible** from the start; its rank is not
- ✅ **Standing orders editable throughout**, locking only at combat, and named on the timeline row they
  change
- ✅ Candidate previews carry **causal deltas and no sortable scalar** — `core/Resolve/CandidateDelta.cs`,
  whose property list is pinned by a test; **hover counts instrumented**, with the exhaustive-search
  flag reduced in `SessionAnalysis`

**Done when:** a player can state the battlefield problem they are trying to solve before each Hit
decision, and the March step reads as *"this cannon loses two shots"* rather than *"entry moves to 4.0."*
**Built; the player half is a playtest question, not a build one.**

> **This milestone is where the encounter thesis is won or lost.** Its failure signal —
> *the player still cannot say why they want another card* — carries the instruction **do not add more
> mechanics.** If Milestone 6 does not land, Milestones 7 and 8 will not rescue it.

#### Three results worth carrying forward

1. **A march step redistributes attacks; it does not only subtract them.** Measured on the worked
   example: the forward tower drops 12 shots to 10 while the **rear tower rises 2 to 4**, because the
   forward tower now kills less and leaves the rear one more to shoot at. Engagement *window* is
   monotonic in entry — that is closed-form geometry — but **per-tower attack count is not.** The
   timeline therefore states the step as a change, never as a loss; a ghost band labelled "attacks
   lost" over a tower that gained two would be false on the surface the design leans on hardest.
   `tests/Wave/NextStepThreatTests.cs` pins it.
2. **`CombinedBoard` was silently ignoring its entry argument** whenever nothing was persisted, and
   returning the board at the draft's own entry. Harmless until Milestone 6, because every caller
   asked for the entry it already had; the first caller that asked for a *different* one tripped the
   resolver's board-versus-army guard immediately. **That guard is the reason this was a one-line fix
   rather than a wrong forecast.**
3. **Hard Invariant 4 was refined, not broken.** "Only the Final Forecast carries a timeline" was the
   *implementation* of "a Visible Threat must not be renderable where a Final Forecast is expected".
   The rule survives; the mechanism moved to the playback boundary. See § Known Discrepancies 17.

### Milestone 7 — Tactical depth ⬜

The mechanical half, deliberately **after** the information half.

- **Spatial breakpoint enemies** — Standard Bearer, Saboteur (temporary disable), Siege Engine, and the
  Lane-Switching Raider (`design/06-dealer-and-enemies.md` § Spatial breakpoints)
- **Deterministic bunching** — minimum spacing, no passing, followers capped behind a slowed leader
- **Four tower forms** — Barrage / Siege Club, Snare / Ambush Spade, chosen as four direct options with no
  Family → Mode submenu (`design/04-cards-as-defenses.md` § Prototype tower forms)
- **Snare → bunch → Barrage** legible on the timeline, plus the two other named interactions
- **The junction as uncertainty hedge** — intercepting lane-switchers, covering the located unknown

**Done when:** the three named interactions are reachable and readable, and form choice varies by lane
state rather than by rank.

🔬 **Build breakpoints at the current 4.0 / 3.0 / 2.0 range, and change nothing about geometry.** The
handoff calls breakpoints the baseline solution to deep-placement dominance; that claim is softened —
**range-by-socket stays authoritative**, and breakpoints are a separate tactical-depth hypothesis until
measured. The geometry question is settled *after* this milestone, in isolation:

1. Build breakpoints keeping **4.0 / 3.0 / 2.0**
2. Measure — `DeepPlacementSweep`, all three arms
3. Run the identical sweep at flat **3.0 / 3.0 / 3.0**
4. Compare, then decide whether range asymmetry is still necessary

**Do not tune breakpoints and range together.** Four outcomes are pre-committed, including *keep 4/3/2*
and *reduce asymmetry because the combination overshot into shallow dominance*. Known Discrepancy 12.

### Milestone 8 — Encounter authoring ⬜

- **Per-wave authored composition** — a **prerequisite**, not a nice-to-have. `baseWave` becomes a list
  renamed out of the singular, and the loader enforces *authored wave count == encounter wave count*.
  Until this lands, Wave 2 cannot differ from Wave 1 at all (Known Discrepancy 14)
- **Wave 2 creates a materially different tactical demand** and makes prior commitments relevant — by role
  reversal, a new breakpoint, or relocated uncertainty. **Not necessarily a literal counter-rotation**
  (`design/05-battlefield.md` § Wave 2)
- **One optional opportunity unit** per encounter, embedded physically as a unit rather than as a checklist
  objective, paying **encounter-local** consequences — a cancelled reinforcement group, a buff that never
  activates. **No Favor, and no substitute currency** (Known Discrepancy 13)
- Encounter pairs authored against the **2–3 plausible placements** target

**Done when:** Wave 2 produces materially different placement reasoning from Wave 1, measured rather than
asserted.

### Milestone 9 — Validate the base encounter, then rank stacking ⬜

Two steps, in this order, and **the order is the whole point.**

**First, validate the base encounter** against § The improved encounter is working if
(`prototype/VALIDATION.md`). Only then:

Two same-rank towers share a socket: depth 2, no Aces, no power bonus, no run eligibility, each layer
keeping its own multiplier (`design/05-battlefield.md` § Rank stacking).

- Socket occupancy grows a second layer; **`stacking.enabled` defaults to false** and is a launch flag like
  `--arm`, not a rebuild
- Run-link detection excludes stacked sockets — a stacked socket is a run island for the same practical
  reason the junction is
- Resolver treats a stack as one firing position with two shots, sharing socket, range origin, and March
  exposure
- Stacking instrumentation added to `SessionSnapshot`: match opportunity, stack chosen, replacement
  alternative, capacity state, socket depth, families in stack
- **The same fixtures and the same organic encounter re-run with the flag on**, compared on
  forced-replacement frequency, stack-at-capacity rate, run frequency, placement depth, and many-card
  viability

**Done when:** the battery and the arms run identically with the flag off, and the five comparison metrics
are reportable with it on, without code changes between the two passes.
`tests/Measurement/SessionBaselineReport.cs` is that report, and it is deliberately the same reducer for
both passes.

**Order is load-bearing, and it now has two layers.** The arms were measured without stacking at Milestone
5, and **the encounter beneath them will have changed by Milestone 9** — so the stacking comparison is read
against a *re-taken* baseline on the improved encounter, not against the Milestone 5 numbers. The Milestone
5 baseline still answers the March-arm question and no longer answers this one. **Do not change the March
curve and stacking in the same pass.**

> **Watch for the failure, not just the effect.** Stacking is in scope because it creates a second
> placement archetype. If it reads as a forced-replacement escape valve, that is the ship/cut answer —
> and *"do not use stacking to compensate for an encounter that is not yet interesting."*

### Milestone 10 — Siege menu probe ⬜ *(after the encounter loop is playtested)*

**Menu level only. No persistent geography simulation.** Two visible fronts, one phase clock, four
preparation actions at fixed costs (`design/12-campaign-time-and-orders.md` § The siege menu probe).
**Time only — Favor enters later**, once encounter telemetry can identify the risk behaviors that earn it.

The probe tests one thing: whether the **self-similar pressure lands emotionally** — whether paying three
hours to repair a gate feels like the campaign-scale version of paying a March step to draw. Success
signals are in `prototype/VALIDATION.md` § The run layer.

**Nothing in it may delay the encounter vertical-slice question.**

### Milestones 11+ — the full run layer ⬜

Three named fronts, one phase, one doctrine project, public Dealer recruitment, one concession — then the
full three-phase run. See § Run-layer sequencing below for the stage table and its do-not-build column.

---

## Improved-encounter build order

**Stated by the design** (Improved Encounters Handoff § 19), not inferred. Milestones 6–9 are this list
grouped into deliverable units.

| # | Build | Milestone |
|---|---|---|
| 1 | Fully known base wave | 6 |
| 2 | **Timeline visualization** | 6 |
| 3 | Exact current-state resolver statistics | 6 |
| 4 | Counterfactual deltas after commitment | 6 |
| 5 | Spatial breakpoint enemies | 7 |
| 6 | Snare → bunch → Barrage interaction | 7 |
| 7 | Four prototype tower forms | 7 |
| 8 | Visible lane for the Dealer's hidden card | 6 |
| 9 | Standing orders integrated into the timeline | 6 |
| 10 | Wave 2 deliberately disturbs Wave 1 | 8 |
| 11 | Optional physical opportunity unit | 8 |
| 12 | Junction as uncertainty hedge | 7 |
| 13 | **Rank stacking flag** | 9 |

> **Do not add more blackjack actions before this sequence is tested.**

The handoff's separate test order (§ 18) collapses to the same shape and adds one step the build list
leaves implicit: **validate the base encounter** *before* enabling stacking. That validation is the first
half of Milestone 9.

**The drift to watch for is building 5–7 before 1–4.** Breakpoints and tower forms are the fun half;
information is the half the diagnosis actually blames. An encounter with breakpoints the player cannot see
coming is a *worse* encounter than one without them, because the deterministic forecast is the thing that
makes the whole design fair.

---

## Improved-encounter open questions

Recorded as stated in Improved Encounters § 23. **All twelve are deliberately left to playtesting rather
than paper design**, which is a different status from the run-layer questions below — those wait on a
build, these wait on a player.

| # | Question | Blocks |
|---|---|---|
| 1 | Exact Barrage, Siege, Snare, and Ambush coefficients? | Form tuning |
| 2 | How much slow makes bunching meaningful without making it mandatory? | Snare tuning |
| 3 | How severe should breakpoint abilities be? | Breakpoint tuning, Saboteur duration |
| 4 | Does the four-form prototype overload players? | Whether forms ship |
| 5 | Does exact current-state information make players thoughtful, or just encourage search? | The information contract itself |
| 6 | How much candidate-preview detail before hover becomes an oracle? | Candidate preview scope |
| 7 | Do opportunity units create meaningful marginal fourth/fifth-card draws? | The fifth-card middle outcome |
| 8 | Does Wave 2 counter-rotation justify keeping persistence? | Whether persistence ships |
| 9 | Does the junction earn its role as an uncertainty hedge? | Junction identity |
| 10 | Does stacking deepen the encounter or soften forced replacement too much? | Stack ship/cut — **also run-layer Q7** |
| 11 | Is the March Clock easier to understand through timeline consequences than numbers? | Timeline validation |
| 12 | Are 2–3 plausible placements per important card achievable through authoring? | The authoring metric itself |

Questions 5, 6, and 12 are the uncomfortable ones, because a bad answer to any of them indicts the pass
itself rather than a number in it. Q8 is worth reading twice: **it puts persistence on the table**, and
persistence is currently the reason the encounter boundary and forced replacement exist at all.

---

## Run-layer sequencing

Unlike the milestones above, **this sequence is stated by the design** (Run Layer Handoff § 13), not
inferred. The right-hand column is the load-bearing half.

| Stage | Build | Do **not** build yet | Status |
|---|---|---|---|
| **A. Encounter vertical slice** | Revision 7.1 encounter, deterministic resolver, telemetry, March arms | Run map, doctrine, Dealer recruitment | ✅ Milestones 0–5 |
| **A′. Improved encounter** | Timeline, exact consequences, breakpoints, tower forms, Wave 2, opportunity unit | Anything from stages C onward | ⬜ Milestones 6–8 |
| **B. Stacking pass** | Flag-gated rank stacking; second pass of the same fixtures | Stack-specific upgrades or rarity | ⬜ Milestone 9 |
| **C. Siege menu probe** | Two fronts, phase clock, four orders, visible consequences | Persistent geography simulation | ⬜ Milestone 10 |
| **D. Four-encounter mini-run** | Three named fronts, one phase, one doctrine project, public Dealer recruitment, one concession | Three-phase campaign, many Charters | ⬜ |
| **E. Full run vertical slice** | Three siege phases, geography history, Dealer adaptation lag, card histories, two Charters | Large modifier library, metaprogression | ⬜ |

**Stage A′ is an insertion, not part of the stated sequence.** The run layer's own § 13 goes straight from
the encounter vertical slice to the stacking pass; the Improved Encounters Handoff puts a whole encounter
pass between them. The two are compatible — nothing in A′ touches the run layer, and it postpones B rather
than reordering anything after it — but the sequence is no longer purely as-stated, and that is worth
knowing when reconciling against either handoff.

**The drift to watch for is skipping C.** Persistent geography is the expensive half; the menu probe is the
half that answers whether the pressure lands at all. Building D before C means discovering that the
campaign clock does not land, after paying for a geography simulation.

---

## Run-layer open questions

Recorded as stated in the Run Layer Handoff § 15, with current status. **None of these blocks Milestones
6–8 at all**, and #7 is now answered at **Milestone 9** rather than 6.

| # | Question | Blocks | Status |
|---|---|---|---|
| 1 | What exact socket geometry removes deep-placement dominance without creating a new obvious best depth? | Final March tuning, geography variants | ✅ **Answered for the prototype** — range by socket, 4.0/3.0/2.0. Residual is a mild *shallow* lean. Re-opens for front-specific geometry |
| 2 | Tuned phase-clock length and action-cost table after the menu probe? | Full siege pacing | ⬜ Needs Stage C |
| 3 | How strongly should Dealer recruitment react to build signals, and which signals are allowed? | Dealer adaptation model | ⬜ Constrained already: build composition and repeated tactical commitments **only** |
| 4 | Which encounter-risk events earn Favor, and does the cap of 3 create the intended scarcity? | Favor tuning | ⬜ **Downstream of encounter telemetry**, not of the campaign build |
| 5 | Final authored state transitions for each of the three outer fronts? | Persistent geography content | ⬜ Stage D/E content |
| 6 | How many doctrine pieces can coexist before the encounter UI becomes unreadable? | Doctrine launch budget | ⬜ 4–7 is a first pass cleared by nothing |
| 7 | Does the stacking flag materially reduce forced replacement, or only create a healthy third branch? | Stack ship/cut | ⬜ **Milestone 9 answers this** — moved from 6, and now asked of the *improved* encounter |
| 8 | Final Charter rules after the baseline run loop is proven? | Late-run variety | ⬜ Stage E |

Question 1 is worth reading twice: it is answered for **two lanes, three sockets, one junction.** Front
geography that changes socket layout re-opens it per front, and the measured lesson travels — **uneven
spacing does not work**, and range-by-depth does.

---

## Sequencing rationale

The resolver comes first because **the forecast contracts are the hardest thing to retrofit.** A game that
grows a forecast on top of an existing live simulation will have two code paths and will drift; and a
codebase that treats Visible Threat and Final Forecast as one type with a flag will leak the distinction
into the UI. Both constraints have to be structural from the first commit.

It also comes first because **the deep-placement question can only be answered by running it.** That
answer may change socket geometry, which invalidates march tuning done beforehand.

The tuning-data layer comes before the resolver because all three march presets and the march-curve risk
require runtime-swappable numbers, and retrofitting that means touching every call site.

Presentation comes late because the design's information rules are subtractive — the discipline is in what
is *not* built, which is easier to hold when the underlying systems already work.

---

## Out of scope

See `prototype/SCOPE.md` § Cut from prototype. Note especially the scope-drift warnings: a second link
rule, any bonus keyed on card count, any payout on beating the Dealer, **any cost or bonus attached to a
stack**, and **any campaign effect that reaches into encounter arithmetic**. Each has a defined trigger and
return form in `prototype/RISKS-AND-ADDBACKS.md` — none should arrive ad hoc.

**The entire run layer is deferred, not cut**, and its deferral has a stated order (§ Run-layer sequencing
above). The prototype's job is unchanged: prove that card identity, placement, and hit/stand react to
battlefield state. Until that is proven, the siege systems wait.
