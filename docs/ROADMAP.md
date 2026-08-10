# Roadmap

**Status: Milestone 4 complete; Milestone 5 in progress.** The wave loop runs headless and the game is
playable end to end: the phase state machine, the Dealer's draw-to-17, bust with the Overload strike, the
one-move adjustment window, lane stakes, persistence with x1.00 reversion, and the full presentation layer
over them. `design/example-wave.md` replays end to end (`tests/Wave/`).
**Open Decision 2 is closed** — deep placement was measured, confirmed dominant, and remedied by making
range vary with socket depth (below). Milestone 5, the validation build, is under way.

This roadmap sequences the prototype defined in `prototype/SCOPE.md`. It is derived from the design, not
stated by it; the handoff specifies *what* to build, not *in what order*.

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
  interface, and no conversion. Only the Final Forecast carries a timeline, so there is nothing on a
  Visible Threat for playback to animate
- ✅ Deep-placement dominance measured (Open Decision 2 above)

**Done when:** the resolver runs headless, produces identical output for identical input across runs, and
reproduces the engagement tables in `design/03-march-clock.md`. **Met.**

**Two invariants established at this milestone**, both hard to retrofit:

1. There is **one simulation path**. The visual wave is a *presentation* of a resolver run, never a
   re-simulation.
2. There are **two forecast types**, and one cannot be rendered where the other is expected.

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

See `prototype/SCOPE.md` § Cut from prototype. Note especially the three scope-drift warnings: a second
link rule, any bonus keyed on card count, and any payout on beating the Dealer. Each has a defined trigger
and return form in `prototype/RISKS-AND-ADDBACKS.md` — none should arrive ad hoc.
