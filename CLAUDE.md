# 21 Bastion — Claude Code Context

A roguelite tower-defense game in which the player builds each wave's defenses by playing blackjack.
Every drawn card becomes a physical defense; the hand's total sets formation-wide power; the Dealer's
hand is the army walking toward you.

**Status: Milestone 5 complete — the prototype is playable, instrumented, and ready to playtest. Next is
Milestone 6, the flag-gated rank-stacking pass.** The
wave loop, the presentation layer, and the validation build are all in: three march arms and 17 scripted
battery cases selectable at launch, per-state JSONL logging, the fifth-card measurement, and the four
regression procedures as one filtered suite. `docs/design/example-wave.md` replays end to end
(`tests/Wave/`). See `docs/ROADMAP.md`.

**Two results from Milestone 5 that change what you should assume:**

1. **Open Decision 2 is closed.** Deep placement was measured, confirmed dominant in every arm, and
   remedied — **range now varies by socket (4.0 / 3.0 / 2.0, forward to rear)** rather than being one
   flat 3.0. Socket positions are unchanged. **Uneven spacing, the design's first-named remedy, was
   measured and does not work.** Engagement totals moved with it: full occupancy at entry 0 is **17.0,
   not 18.0**, and the fifth card costs −71% rather than −67%. Any remembered engagement number from
   before this is wrong.
2. **The fifth card is not "functionally dead" in Arm C** by resolver output — it is still the better
   play about half the time, with a clean crossover at 18. That does **not** settle the arm question:
   the pre-committed reading needs the player half too, and the measurement conditions on the card being
   safe, so it excludes the bust risk that is the real counterweight. See `docs/ROADMAP.md` § Milestone 5.

**Current design revision: 7.1** — a correction pass over Revision 7, not a structural revision. It fixed
an arithmetic error in the March Clock, reversed the stated direction of the march's placement bias,
withdrew the engagement-fraction output estimates, narrowed the adjustment window to one move, split the
forecast into two named contracts, and reassigned the test-arm letters. **Pre-7.1 material is stale in
specific, load-bearing ways** — see `docs/reference/tuning-constants.md` § Resolved and § Known
Discrepancies before trusting any remembered number.

**Plus the Run Layer Handoff** (`docs/archive/handoff-run-layer.md`), now incorporated across the docs. It
governs the *run*; 7.1 governs the *encounter*. **Where they disagree, the run layer wins — and it changes
no encounter arithmetic**: March Clock presets, Formation Strength, and the resolver are untouched by it.
What it does change:

- **The product fork is resolved.** A blackjack tower defense with a **siege-shaped run** — roughly 70%
  encounter, 30% campaign. The campaign must not become a second strategy game.
- **Three regions become three siege phases** (Encirclement, Breach, Last Stand), one continuous siege over
  authored persistent fronts. Geography persists across encounters; **towers still do not.**
- **Chips are cut.** Time pays for campaign actions; **Favor** (cap 3) buys rare rule-bending; **Bastion
  Health reaching zero is the only ordinary defeat condition.** Losing every district is not defeat.
- **The relic layer becomes Doctrine** — 4–7 behavior-changing globals, not twenty passive percentages.
- **The Dealer gets a fixed 26-card opposing shoe** built by public, raidable, one-for-one replacements.
- **One encounter mechanic is added: rank stacking**, flag-gated and default off — the only part inside
  prototype scope, and the subject of **Milestone 6**.

Four new collisions with 7.1 are logged as **Known Discrepancies 8–11** (the Vault stake's payload, the two
26-card shoes, the un-restated encounter budget, and the Long Road relic). Discrepancy 9 is unresolved and
must not be settled in passing.

---

## Tech stack

| | |
|---|---|
| Engine | Godot 4.7, GL Compatibility renderer |
| Language | **C#**, `Godot.NET.Sdk/4.7.1` |
| Target framework | **net8.0** — Godot 4.7 ships GodotSharp for net8.0; every project must match |
| SDK installed | .NET 10.0.302. **No .NET 8 runtime is installed** — projects set `RollForward=Major` so net8.0 assemblies run on .NET 10 |
| Tests | xUnit, headless, no scene tree |
| Platform | Windows (dev); D3D12 rendering device configured |
| Repo | git, branch `main`. Files are staged but **not committed** |

### Projects

| Project | Role |
|---|---|
| `21 Bastion.csproj` (root) | Godot layer — `game/`, `telemetry/`. Assembly `21 Bastion`, namespace `Bastion.Game` |
| `core/Bastion.Core.csproj` | **Engine-free** game logic. A build target fails the build if it ever references GodotSharp |
| `tests/Bastion.Core.Tests.csproj` | xUnit suite over the core |

The root project globs `**/*.cs`, so `core/` and `tests/` are explicitly `Compile Remove`d from it.

## Commands

```bash
dotnet build "21 Bastion.sln"        # all three projects
dotnet test tests/Bastion.Core.Tests.csproj

# Oracle-tier instrumentation (bust probability, expected output, combined utility).
# Compiled out by default; must be a command-line global property to reach Bastion.Core.
dotnet build "21 Bastion.sln" -p:BastionInstrumentation=true
dotnet test tests/Bastion.Core.Tests.csproj -p:BastionInstrumentation=true

# The four regression procedures as one suite (prototype/VALIDATION.md § Regression).
# Run before touching the march curve, Formation Strength, run percentages, tower power,
# Overload, or the resolver.
dotnet test tests/Bastion.Core.Tests.csproj --filter Category=Regression

# Golden baselines are regenerated DELIBERATELY and never on failure. This rewrites them and
# then fails the run, so a regeneration cannot be mistaken for a pass.
BASTION_REGEN_BASELINES=1 dotnet test tests/Bastion.Core.Tests.csproj --filter Category=Regression
```

The instrumented measurement sweeps are slow on purpose and write to `telemetry/`:
`fifth-card.csv` (~80 s, the primary measurement), `deep-placement*.csv`, `geometry-candidates.csv`,
`shoe-simulation.csv`.

### Running Godot

Not on PATH, but present at:

```
C:\Users\iwpre\Downloads\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe
```

Use the `_console` variant from a terminal — the plain one detaches and writes no stdout. It must be the
**mono** build; the standard build has no C# support.

```bash
# Smoke test: loads tuning, builds the whole UI, runs the opening StateChanged, quits. No window.
godot --headless --path . --quit-after 120
```

### Playtest launch flags

Milestone 5's done-when clause is that a session runs, logs, and analyzes **without code changes between
arms**. Flags go after `--`, following the convention `CaptureRun` set, so they cannot collide with
Godot's own. `--arm` rebuilds tuning immutably; it never writes the file.

```bash
godot --path . -- --arm B --fixture 2-split      # a scripted case on a chosen arm
godot --path . -- --arm A --fixture 7-onlyrank-b # -b is the mirrored presentation
godot --path . -- --arm C                        # no case named: the facilitator picker opens
godot --path . -- --arm C --seed 4242            # free play on a chosen seed
godot --path . -- --no-log                       # suppress the session log
```

Cases live in `data/battery.json`; an unknown id prints the full list. Sessions log one JSON line per
offered state to `telemetry/sessions/` (gitignored). **The `oracle` key is absent unless the build was
made with `-p:BastionInstrumentation=true`** — checking that round trip is how the gate is verified.

### Capture run — screenshots of every phase

**The UI cannot be reviewed by reading it.** Anchored regions, wrapped flow containers, and hand-drawn
geometry only resolve at a real viewport size. `game/devtools/CaptureRun.cs` walks a scripted wave and
writes a PNG per phase, and first checks that a synthesised click on the board actually places a tower —
which is the part that has already caught a hit test silently reading the OS cursor instead of the event.

```bash
dotnet build "21 Bastion.sln" -p:BastionDevTools=true
godot --path . -- --capture                      # writes .captures/ (gitignored)
godot --path . -- --capture --capture-out <dir>
```

Opens a window for a few seconds — screenshots need a real renderer, so `--headless` cannot produce them.
Exits non-zero if the click check or any write fails. Flags go after `--` so they cannot collide with
Godot's own.

**Gated by `BastionDevTools`, deliberately separate from `BastionInstrumentation`** — the point is to
photograph the build a player would get, and folding it into the oracle flag would mean every screenshot
showed an instrumented build. Compiled out by default, and inert without `--capture` even when compiled in.

**Scene wiring is done.** `scenes/root.tscn` carries `game/Bootstrap.cs` and is set as `run/main_scene`.
Running prints the design revision, the active march arm, and its entry positions — a smoke test that the
Godot layer reaches the engine-free core and reads `data/tuning.json` through `res://`.

`Bootstrap` is the **composition root**: it loads tuning once and passes it down. Do not make tuning
globally reachable via an Autoload singleton — that is a service locator, and it is what makes systems
hard to run headlessly, which is the constraint the core/game split exists to protect.

---

## The narrowed claim

Revision 7 exists because Revision 6 stacked systems that cancelled each other out. The design now makes
one claim, and the prototype exists to test it:

> Decision density comes from what a card becomes, where it goes, and what it displaces. Hit/stand is a
> live decision in the 14–19 band — roughly where blackjack has always put it — and the design's job is
> to make that band's stakes battlefield-specific, not to manufacture tension at 8.

Anything that widens this claim back out is a regression, not a feature.

---

## Hard invariants

These are load-bearing. Violating one silently breaks the design's central argument. If a task seems to
require breaking one, stop and say so rather than working around it.

1. **One system per job.** Each pressure in this design has exactly one mechanism behind it. When adding
   a mechanism, delete the one it duplicates. Revision 6 died of overlapping systems that refunded each
   other; see `docs/prototype/RISKS-AND-ADDBACKS.md`.
2. **Never show a combined verdict.** No recommended action, no hit/stand edge, no optimal-placement
   highlight, no green/red indicator, no exact bust percentage. Hand consequences and battlefield
   consequences are displayed *separately*. Combining them is the player's job.
3. **Family is locked at placement.** Chosen when the card is drawn, permanent for the wave. Position is
   adjustable by one socket in the adjustment window; family never is.
4. **One deterministic resolver, two named forecasts.** The same code path drives forecast and live wave —
   same spawn schedule, health, armor, speed, paths, range, cooldown, targeting, rounding, and
   tie-breaking. But there are **two distinct outputs, and they are separate return types, never one type
   with a flag**:
   - **Visible Threat** — during the draw. Exact against *the revealed force only*. **Not a prediction of
     the wave.**
   - **Final Forecast** — after Dealer resolution. Exact against the complete army. **This alone is the
     combat contract:** if it says a lane leaks two, the wave leaks two.

   A Visible Threat must not be renderable where a Final Forecast is expected.
5. **Total engagement is explanatory, not a balance number.** Never multiply board power by an engagement
   fraction to estimate output — sockets are not interchangeable. **Balance through the resolver.**
6. **The adjustment window is one move total** — relocate one tower one socket, *or* swap two adjacent
   towers. Not per tower. Extra moves come from relics, never a wider baseline.
7. **Combat is deterministic.** No critical hits, no misses, no random targeting. Ties resolve by spawn
   order.
8. **The Dealer resolves in full on bust.** Bust never dodges the wave. Resolution is purely "deploy" —
   there is no outcome for a bust to escape.
9. **No Dealer total comparison in the prototype.** Suspended as a diagnostic, not deleted. Scheduled to
   return paying the Vault, never the battlefield. See `docs/prototype/RISKS-AND-ADDBACKS.md`.
10. **March step sizes must be config-tunable from the first build**, and **all three presets ship in the
    first build** (they are the test arms). Same for Formation Strength, run percentages, tower power, and
    Overload. Never hardcode a tuning value at a call site.
11. **Every number is first-pass and expected to be wrong.** No number in the design carries a confidence
    interval, validity window, or tolerance. Those are outputs of playtesting, not inputs.
12. **The campaign never edits encounter arithmetic.** Campaign time does not modify March entry; no
    campaign effect touches Formation Strength or the march curve; no multiplier crosses an encounter
    boundary. A front may change path length, socket layout, route structure, and lane stakes — all
    resolver *inputs* — and nothing else. The two clocks are the same *shape*, never the same number.
13. **Rank stacking ships flag-gated and default off.** The arms were measured without it; the second pass
    is read against that baseline. **Never change the March curve and stacking in the same pass.** And no
    cost or bonus attaches to a stack — no power bonus, and specifically **no flat damage penalty**.
14. **Counter the build, never the player.** Dealer adaptation may read build composition and repeated
    tactical commitments. Never win rate, health, loss streaks, or a hidden skill estimate. This is why
    recruitment is public and raidable rather than merely fair.
15. **Rank count is sacred.** The game may change a card's character, history, family, modifier, or
    availability. **Enemy pressure never alters blackjack rank distribution** — which is why an exhausted
    or captured card is replaced by a same-rank Reserve copy rather than removed.

---

## Document map

Read the specific doc for the system you are touching. Do not work from memory of the handoff.

**Design specification** — `docs/design/`

| File | Covers |
|---|---|
| `00-pillars-and-identity.md` | Design pillars, high concept, final gameplay identity |
| `01-core-loop.md` | Wave phases, before/during/after, combat framing |
| `02-blackjack-and-formation.md` | Blackjack rules, Formation Strength curve, card power curve |
| `03-march-clock.md` | Escalating march, engagement geometry, exactly-21 pullback |
| `04-cards-as-defenses.md` | Family locking, suit identities, face cards, Aces, run links |
| `05-battlefield.md` | Sockets, **rank stacking**, persistence, adjustment window, lane stakes, standing orders, resolver |
| `06-dealer-and-enemies.md` | Dealer as wave generator, Dealer cards as units, enemy stats, **the opposing shoe and public recruitment** |
| `07-bust-and-overload.md` | Bust handling, capped Overload |
| `08-deck-economy-progression.md` | Shoe, thinning dilemma, **reward verbs, card identity, exhaustion, economy** |
| `09-information-and-ui.md` | What is shown and what is never shown, at both scales |
| `example-wave.md` | A fully worked wave — use as an implementation acceptance test |

**Run layer** — `docs/design/`, deferred and **not prototype scope** except the rank-stacking flag (in `05`)

| File | Covers |
|---|---|
| `10-run-structure.md` | The continuous siege: run pillars, standing constraints, three phases, victory/defeat/Last Stand, cadence, run memory, modes |
| `11-siege-geography.md` | Persistent authored fronts, the four front states, neglect, concession |
| `12-campaign-time-and-orders.md` | Phase clock, Time/Favor/Bastion Health, the seven strategic orders, shops and rewards, the menu probe |
| `13-doctrine-and-charters.md` | Doctrine as the placement-layer progression, Charters, what happened to relics, commanders |

**Validation build** — `core/Validation/`, `game/Startup/`, `game/telemetry/`

| File | Covers |
|---|---|
| `core/Validation/BatteryFixture.cs` | A scripted case and the script that reaches its offered state |
| `core/Validation/LaneMirror.cs` | Variant B: the same decision, lanes exchanged |
| `core/Validation/SessionSnapshot.cs` | Reads an offered state into a loggable record. **Per socket, never summed** |
| `core/Validation/Oracle.cs` | The three forbidden values, compiled out of a player build |
| `game/Startup/LaunchOptions.cs` | `--arm`, `--fixture`, `--seed`, `--log-out`, `--no-log` |
| `game/telemetry/PlaytestLog.cs` | JSONL per offered state, plus what only the interface knows |
| `tests/Regression/` | The four regression procedures, tagged `Category=Regression` |

**Steering** — `docs/`

| File | Covers |
|---|---|
| `ROADMAP.md` | Build order, milestones, open decisions |
| `ARCHITECTURE.md` | Proposed code structure, determinism requirements, testing approach |
| `GLOSSARY.md` | Terms of art used throughout |
| `reference/tuning-constants.md` | **Every number in one place**, plus known discrepancies |

**Prototype** — `docs/prototype/`

| File | Covers |
|---|---|
| `SCOPE.md` | What is in the prototype and what is explicitly cut |
| `VALIDATION.md` | Test arms, scripted battery, success criteria, instrumentation, regression |
| `RISKS-AND-ADDBACKS.md` | Key risks and the fixed add-back sequence for diagnostic cuts |

**Archive** — `docs/archive/`

- `handoff-run-layer.md` — **current, for the run.** Rank Stacking & Continuous Siege, consolidated.
  Supersedes 7.1 where they disagree; changes no encounter arithmetic.
- `handoff-revision-7-1.md` — **current, for the encounter.** Its § 24 lists every correction made over
  Revision 7.
- `handoff-revision-7.md` — **superseded.** Kept for history. Several of its numbers and two of its
  instructions are now known to be wrong; do not cite it.

The split docs are authoritative for implementation. If they disagree with the 7.1 archive, the
disagreement is a bug — flag it.

---

## Conventions

- **Two data files, both validated at load.** `data/tuning.json` holds every number; `data/battery.json`
  holds the scripted battery's cases, loaded by `core/Validation/BatteryLoader.cs`, which is modelled on
  `TuningLoader` and fails just as loudly. **Only variant A of a case is authored** — variant B is
  mirrored from it at load, because hand-authoring both halves is how two presentations quietly stop
  being the same decision. The loader rejects an id ending in `-b`.
- **Range varies by socket** (`geometry.rangeBySocket`), and the face-card allowance is a **bonus added
  to it**, not an absolute. This is the Open Decision 2 remedy and it is load-bearing: a flat range is
  what made deep placement dominant. `TowerState.RangeFor` is the one derivation — do not read
  `rangeBySocket` at a call site.
- **Numbers live in one place: `data/tuning.json`**, loaded and validated by
  `core/Config/TuningLoader.cs`, documented in `docs/reference/tuning-constants.md`. Never inline a
  tuning value at a call site. The loader rejects internally inconsistent files at load, including
  data that would contradict a stated invariant (Overload scaling with excess, a Dealer that skips
  resolution on bust).
- **Some tuning values have no design behind them.** The `sim`, `towers`, `suits`, `standingOrders`,
  `waves`, and `encounters` sections were decided at Milestone 1 because the resolver could not run
  without them — the handoff specifies the enemy side of combat completely and the tower side not at
  all. They are listed with reasoning in `docs/reference/tuning-constants.md` § **Invented for the
  resolver**, and flagged in the JSON itself. **A disagreement there is a decision to revisit, not a
  bug**, because there is no design statement to check against.
- **Oracle-tier values go through `DebugGate`** (`core/Diagnostics/DebugGate.cs`), never around it.
  `OracleOnly` skips the computation entirely in a player build; `RequireInstrumented` guards a whole
  routine. The gate is compile-time and deliberately **not** tied to the Debug configuration, because a
  Godot *debug export* is still a player build.
- **Core stays engine-free.** `using Godot;` in `core/` is a build error by design, not a style
  preference — the regression suites must run headless.
- **Prototype scope is a boundary, not a suggestion.** `docs/prototype/SCOPE.md` lists what is cut. Do not
  build Hearts, Diamonds, Split, Double Down, commanders, metaprogression, or **any part of the run layer**
  — siege map, fronts, campaign time, Favor, strategic orders, Dealer recruitment, doctrine, Charters, card
  histories — unless asked. Relics are superseded by Doctrine and are not to be built either.
- **Geometry, stakes, and lane count are data, and that is what keeps the run layer possible.** A front
  state is a geometry override. A call site reading path length, socket positions, socket count, lane
  count, or lane stakes from a literal forecloses the campaign layer — which is already prohibited by
  invariant 10, and is now structural as well (`docs/ARCHITECTURE.md` § Room left for the run layer).
- **Cite the doc when implementing a rule.** A comment naming the section beats restating the rule.
- **When a design question is genuinely unanswered**, say so and point at the risk register rather than
  inventing a rule. Several gaps are deliberate.
