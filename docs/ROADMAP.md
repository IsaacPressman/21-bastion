# Roadmap

**Status: Milestone 2 complete.** The hand, shoe, placement, family locking, run links, forced replacement,
and formation multiplier all run headless and write the board the resolver already consumed; the output
landmarks reproduce exactly. Milestone 3 (wave loop) is next. **Open Decision 2 was re-measured with run
links modelled (caveat 1 below) and the deep-placement margin held — the socket-geometry remedy is still
owed before march tuning.**

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

### 2. Socket geometry may need work before anything else — ⚠ **measured, and confirmed**

Revision 7.1 flagged that **deep placement is weakly dominant whenever entry exceeds 0**: advancement eats
forward socket windows and leaves rear ones untouched. The pushback — run-link adjacency, the junction
socket, traps needing early application, leak thresholds — **lives in the resolver, not the engagement
arithmetic**, so it could not be settled on paper.

**It has now been measured.** `tests/Measurement/DeepPlacementSweep.cs` sweeps every socket permutation for
boards of 2–4 towers across all three arms, with identical cards throughout so that neither card power nor a
run link can explain a difference. Output: `telemetry/deep-placement.csv`.

**Deep placement wins in every arm, and the margin scales with the clock:**

| Arm | Curve | Mean leak, deepest minus shallowest | Reading |
|---|---|---:|---|
| **A** | flat | **−1.40** | deep wins |
| **B** | soft | **−1.47** | deep wins |
| **C** | hard | **−1.87** | deep wins |

Compared **within a fixed board shape** — same number of towers in each lane and the junction. That control
matters: a naive deepest-versus-shallowest split over the raw sweep reports the same conclusion for the wrong
reason, because the junction sits at path position 6.0 (reading as mid-depth) while also covering whichever
lane the player neglected. Holding the shape fixed removes that confound.

The measurement reproduces the design's prediction exactly:

- **At entry 0.00 the depth effect is absent, and what little moves is pure resolver-side timing.** All
  sockets give identical engagement, so any difference here is fire-order and cooldown, not geometry. Three
  shapes move, and they do not agree: two towers both in lane one favours **shallow** by 2, while the
  one-and-one and lane-one-plus-junction shapes each favour **deep** by 1. The net is small and mixed —
  which is the point. Forward towers opening fire sooner is a real resolver-side effect the engagement
  arithmetic cannot see, but at entry 0 it does not systematically favour either side.
- **Once entry exceeds 0, deep wins in every shape that varies**, and the margin grows from Arm A to Arm C.
  The depth effect only becomes one-directional once advancement starts eating forward windows.

> **Per the pre-committed reading in `prototype/VALIDATION.md`: the socket geometry needs work before the
> march curve does.** Do not renegotiate this now that the numbers are in.

Remedies, in the order the design proposes them: **uneven socket spacing**, range differences by position, or
lane-specific leak thresholds. **Not the march curve.**

Two caveats worth carrying into that work, neither of which changes the verdict:

1. **Run links are now modelled** (Milestone 2), and the sweep was re-run with them —
   `tests/Measurement/DeepPlacementSweep.cs` `Sweep_placement_depth_with_run_links_modelled`, output in
   `telemetry/deep-placement-runs.csv`. To keep the depth comparison clean, ranks follow a
   depth-symmetric valley (6-5-6), so a contiguous pair forms a 2-run at identical total power whether it
   sits shallow or deep — no power gradient confounds the result. **The margin did not shrink; it held and
   modestly widened** (A −1.80, B −2.07, C −2.13, versus −1.40 / −1.47 / −1.87 without runs). Deep placement
   still wins in every arm, exactly as predicted: with runs available equally to shallow and deep contiguous
   boards, they do not rescue shallow placement. The geometry remedy stands.
2. The spawn schedule and tower cooldown are inventions of this milestone
   (`reference/tuning-constants.md` § Invented for the resolver). The entry-0 timing effects in
   particular — including the one shape that favours shallow — are a direct consequence of a 1.0 s
   cooldown; a shorter one would shrink them. They do not touch the entry-above-0 verdict.

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

### Milestone 3 — Wave loop

- Full phase state machine from `design/01-core-loop.md`
- Dealer: upcard deployed as Vanguard pre-deal, hidden card, draws to 17, every card deploys
- Bust: card destroyed, ×0.80, Overload at base power, no adjustment window, Dealer resolves in full
- Adjustment window: **one move total** (relocate one socket or swap two adjacent), standing orders free,
  families locked
- Lane stakes: Bastion and Vault
- Persistence with ×1.00 reversion at wave boundary

**Done when:** `design/example-wave.md` replays end to end and every number in it matches.

### Milestone 4 — Presentation and information

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
Shown is absent.

### Milestone 5 — Validation build

- **Arms A, B, and C selectable by configuration** — flat, soft, and hard march presets in one build
- Ten scripted battery fixtures, deterministically seeded, each presentable twice with different
  presentation
- Full instrumentation logging per `prototype/VALIDATION.md`, including **placement depth** and **which
  adjustment move was wanted**
- The **fifth-card outcome measurement**: for each arm, how often a safe miss beat the stand-at-four
  counterfactual by resolver output
- The four regression procedures runnable as a suite

**Done when:** a playtest session can be run, logged, and analyzed without code changes between arms.

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
