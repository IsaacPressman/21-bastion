# Architecture

**Status: partly built.** The handoff specifies game design, not code structure; everything here is
inferred from design constraints rather than stated by them. Revise freely — but the four hard
requirements below come directly from the design and are not negotiable without changing the design.

Milestone 0 landed requirements **3** and **4** (engine-free headless core, tuning as data) and the
`core/Config` and `core/Diagnostics` layers. **Milestone 1 landed requirements 1 and 2** with the resolver:
one simulation path in `core/Resolve/Resolver.cs`, and two forecast types that share no ancestor.

---

## Four hard requirements

### 1. One simulation path

> **The Final Forecast is a contract. If it says a lane leaks two, the wave leaks two.**

There must be **exactly one resolver**. Forecasts are resolver runs. The visible wave is a *presentation*
of a resolver run — replaying a recorded timeline, not re-simulating.

The failure mode this prevents is well known and hard to reverse: a fast "estimate" path and an accurate
"real" path that drift apart under maintenance until the forecast quietly becomes a lie. Since the design
sells the forecast as exact, that drift is not a bug to fix later; it invalidates the game.

**Practical shape:** the resolver consumes an immutable board state and returns a timeline of events plus
per-lane outcomes. The forecast reads the outcomes. Combat playback animates the timeline.

### 2. Two forecast types, distinguished by the type system

The resolver answers **two different questions** and they must not share a type:

| | **Visible Threat** | **Final Forecast** |
|---|---|---|
| When | During the draw | After Dealer resolution |
| Input | Base wave + Vanguard — revealed force only | The complete army |
| Claim | Exact about what is on the field now | Exact about the wave that will run |
| Combat contract? | **No** | **Yes** |

> **Separate return types, not the same type with a flag. A Visible Threat must not be renderable in a slot
> expecting a Final Forecast.**

This is a type-system requirement rather than a convention because the failure is silent and expensive: a
number that keeps its name while changing its meaning mid-hand is the cheapest possible way to lose trust
in the forecast, which is a foundational claim of the design. Revision 7 described one contract and then
demonstrated it changing mid-example.

The resolver core is shared; only the input army and the wrapper type differ. Both are independently
verified in regression (`prototype/VALIDATION.md` step 4).

### 3. Headless, deterministic core

The regression procedures in `prototype/VALIDATION.md` require enumerating every legal 2–5 card hand and
simulating 10,000 hands across three shoe configurations. That must run **without a Godot window, without
the scene tree, and fast.**

So the game logic — cards, hands, formation, march, sockets, links, resolver, enemies — is a **plain
library with no engine dependencies.** Godot supplies rendering, input, and scene management, and calls
into it.

Determinism requirements:

- No wall-clock time, no frame-rate coupling in the simulation. Fixed simulation ticks.
- No unseeded randomness. The shoe takes an explicit seed; the scripted battery depends on it.
- Ties resolve by spawn order, explicitly, everywhere.
- Same input → byte-identical output, every run.

### 4. Tuning values as data

Every number in `reference/tuning-constants.md` is loaded from data. **No inlined literals at call sites.**

Two specific drivers, both immediate rather than hypothetical:

- **Arms A, B, and C** differ only in the march curve. All three **ship as presets in one config file in
  the first build** — they are not three builds, and switching between them must not require
  recompilation.
- The march step sizes are the design's most-likely-wrong numbers and are explicitly required to be
  config-tunable on the first build.

The House Rules mode (`design/10-run-structure.md`) is a third driver arriving later: every entry in that
menu toggles a rule the prototype hardcodes. Expressing those as rule flags from the start makes both House
Rules and the test arms nearly free.

**Rank stacking is the fourth driver, and it is immediate.** It ships **flag-gated and default off**, and
the validation procedure requires running the same fixtures and the same arms twice — once with the flag
off, once on (`prototype/VALIDATION.md` § Rank-stacking sequence). That is the arm pattern exactly: a rule
toggle selected at launch, applied by rebuilding tuning immutably, never by rewriting the file and never by
recompiling.

---

## Room left for the run layer

The run layer (`design/10-run-structure.md` through `13-doctrine-and-charters.md`) is deferred, and the
prototype's job is to avoid foreclosing it. **Three properties are what it needs, and all three are already
required for other reasons** — which is the useful part: nothing needs building now.

1. **Encounter geometry is data, not constants.** A front state is a geometry override — path length,
   socket positions, socket count, lane count, lane stakes. All five already live in `data/tuning.json`
   behind one derivation each, because hard invariant 10 requires it.
2. **The campaign layer belongs in `core`, not `game`.** A siege menu is a state machine over fronts,
   clocks, and orders — exactly the kind of thing the regression suites will want to simulate headless.
   Requirement 3 already forbids the alternative.
3. **Nothing may cross the encounter boundary as a multiplier.** Geography and card identity persist;
   Formation Strength does not. `WaveSession.Settle` already reverts persisted towers to ×1.00 at the wave
   boundary, and the encounter boundary clears the board entirely.

**The one thing that would foreclose the run layer** is a call site reading geometry, stakes, or lane
count from a literal. That is already a build-review error under hard invariant 10; it is now also a
structural one.

⚠ **One structural question is open and should not be answered in passing:** the run layer gives the Dealer
its own fixed 26-card campaign shoe, while `core/Dealer/DealerHand.cs` draws from the *player's* shoe today
— and that shared pile is what makes the marked-rank display a reading skill. See
`reference/tuning-constants.md` § Known Discrepancies 9.

---

## Proposed layout

Built (✅) and planned:

```
21 Bastion.sln
Directory.Build.props    ✅ shared TFM, roll-forward, the BastionInstrumentation gate
data/tuning.json         ✅ every tuning value, all three march presets
21 Bastion.csproj        ✅ Godot layer, assembly "21 Bastion", namespace Bastion.Game
  scenes/                ✅ root.tscn (main scene) - board, panels, combat playback
  game/                  ✅ Bootstrap.cs - the composition root
    presentation/           timeline playback, animation
    input/                  placement, family selection, adjustment window
  telemetry/             ✅ (empty) instrumentation logging
core/Bastion.Core.csproj ✅ engine-free game logic; guarded against GodotSharp
  Config/                ✅ TuningData, TuningLoader, TuningValidationException
  Diagnostics/           ✅ DebugGate
  Cards/                 ◐ Rank, Card, ace state — shoe and power lookup pending (M2)
  Hand/                     blackjack state, totals, formation strength
  Board/                 ✅ SocketRef, Family, StandingOrder, TowerState, BoardState
                            — placement rules, forced replacement, run links pending (M2)
  March/                 ✅ entry point, escalating step, engagement
  Dealer/                ◐ DealerDeployment (card→unit, lane) — draw policy pending (M3)
  Resolve/               ✅ ArmyBuilder, Resolver, Targeting, timeline, outcomes,
                            VisibleThreat, FinalForecast, UnmodelledBehaviour
tests/                   ✅ Config/, Diagnostics/, March/, Resolve/, Measurement/
docs/
```

`core/Board/TowerState` carries `FormationMultiplier` and `RunBonus` as **separate** fields rather than one
pre-multiplied number. That is the seam Milestone 2 attaches to: it *writes* those fields, and the resolver
keeps reading them without change. It also lets per-tower activity reporting explain *why* a tower hit as
hard as it did, which is the point of reporting it at all.

The `core` ↔ `game` boundary is the load-bearing one. If a `core` type imports a Godot type, requirement 3
is broken — so `Bastion.Core.csproj` carries a `GuardEngineFreeCore` target that fails the build on a
GodotSharp reference. Verified to fire.

Note the root project globs `**/*.cs` from the repository root, so `core/` and `tests/` are explicitly
`Compile Remove`d from it or they would compile twice.

---

## Notes on specific systems

**Composition root.** `Bootstrap` loads tuning once and passes it down. Resist making it an Autoload
singleton: global reachability is a service locator, and depending on one from inside game systems is
what would quietly break requirement 3 by making those systems unrunnable without the scene tree.

**Phase state machine.** `design/01-core-loop.md` is a small explicit state machine with a hard boundary
between the draw phase and the adjustment window. Model it as such — the boundary is a design invariant,
not an implementation detail, and blurring it is exactly what the Commitments Are Made Under Uncertainty
pillar guards against.

**Family locking.** Enforce it in `core`, not in the UI. A rule this central should be impossible to
violate through a code path, not merely un-clickable.

**Socket occupancy and rank stacking.** A socket currently holds zero or one tower; stacking makes it zero,
one, or two, matched by **rank** rather than value. Two consequences are worth designing for rather than
patching in: **run-link detection must exclude a stacked socket** (it becomes a run island, like the
junction), and **each layer keeps its own `FormationMultiplier`** — which is already possible only because
`TowerState` stores multiplier and run bonus as separate fields rather than one pre-multiplied number. The
resolver sees one firing position with two shots sharing socket, range origin, and March exposure.

**Engagement.** A pure function of entry, socket positions, ranges, and path length. It has closed-form
tables in the design to test against — write those tests first; they caught a real error in Revision 7 (see
`reference/tuning-constants.md` § Resolved).

**Do not build an "effective output" helper** that multiplies power by an engagement fraction. That
estimate is withdrawn: sockets are not interchangeable, and a convenience function computing it will get
used for balance decisions no matter what the comment says. Engagement is reported **per socket**, for
explanation and instrumentation. Balance comes from resolver output.

**Adjustment window.** One move for the whole board, enforced in `core`. The single-move rule exists partly
because per-tower movement left five specification questions unanswered (`design/05-battlefield.md`) — a
model where "moves remaining" is a board-level counter answers all five by construction. Standing-order
changes do not consume the move.

**Standing orders.** Modeled exactly by the resolver, or they do not ship. There is no "approximately
forecast" tier.

**Debug-only values.** Bust probability, expected outputs, and combined utility exist for instrumentation
and must never reach a player build. Gated by `core/Diagnostics/DebugGate.cs`: `OracleOnly` skips the
computation entirely rather than discarding its result, and `RequireInstrumented` guards a whole routine.

Two design points worth not undoing:

- The gate is **compile-time**, so the values are absent from a player binary rather than merely
  unreachable.
- It is **not** tied to the Debug configuration, because a Godot *debug export* is still a player build.
  Enabling it requires `-p:BastionInstrumentation=true` as a command-line global property — a project
  cannot set it on another project's behalf, since the gate compiles into `Bastion.Core`.

---

## Testing strategy

| Layer | What it covers |
|---|---|
| **Unit** | Power curve, formation strength, engagement, run detection, ace states, armor floor |
| **Benchmark** | The output landmarks table in `design/02-blackjack-and-formation.md`, reproduced exactly |
| **Equivalence** | Final Forecast outcomes == playback outcomes, **and** Visible Threat == a resolver run against the revealed force alone — on every scripted fixture |
| **Enumeration** | All legal 2–5 card hands: **raw** output and entry position — never a derived engagement-adjusted output |
| **Statistical** | 10,000 hands × 3 shoe configs: output, bust rate, board width, run frequency, entry |
| **Acceptance** | `design/example-wave.md`, replayed end to end |

The enumeration and statistical suites are the regression gate: per `prototype/VALIDATION.md`, they run
**before** any change to the march curve, Formation Strength, run percentages, tower power, Overload, or
the resolver.
