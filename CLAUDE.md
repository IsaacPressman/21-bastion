# 21 Bastion — Claude Code Context

A roguelite tower-defense game in which the player builds each wave's defenses by playing blackjack.
Every drawn card becomes a physical defense; the hand's total sets formation-wide power; the Dealer's
hand is the army walking toward you.

**Status: Milestone 3 complete.** The wave loop runs headless: the phase state machine
(`core/Wave/WaveSession.cs`), the Dealer's draw-to-17, bust with the Overload strike, the one-move
adjustment window, lane stakes, and persistence with ×1.00 reversion — all driving the Milestone 1 resolver
and Milestone 2 producer unchanged in shape. `docs/design/example-wave.md` replays end to end
(`tests/Wave/`). Milestone 4 (presentation) is next. **Open Decision 2 stands: deep placement is still
weakly dominant (the margin held with run links modelled), so the socket geometry needs work before the
march curve does — Milestone 3 deliberately did not touch it.** See `docs/ROADMAP.md`.

**Current design revision: 7.1** — a correction pass over Revision 7, not a structural revision. It fixed
an arithmetic error in the March Clock, reversed the stated direction of the march's placement bias,
withdrew the engagement-fraction output estimates, narrowed the adjustment window to one move, split the
forecast into two named contracts, and reassigned the test-arm letters. **Pre-7.1 material is stale in
specific, load-bearing ways** — see `docs/reference/tuning-constants.md` § Resolved and § Known
Discrepancies before trusting any remembered number.

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
```

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
| `05-battlefield.md` | Sockets, persistence, adjustment window, lane stakes, standing orders, resolver |
| `06-dealer-and-enemies.md` | Dealer as wave generator, Dealer cards as units, enemy stats |
| `07-bust-and-overload.md` | Bust handling, capped Overload |
| `08-deck-economy-progression.md` | Shoe, thinning dilemma, economy, relics, commanders |
| `09-information-and-ui.md` | What is shown and what is never shown |
| `10-run-structure.md` | Regions, encounter budget, escalation, modes |
| `example-wave.md` | A fully worked wave — use as an implementation acceptance test |

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

- `handoff-revision-7-1.md` — **current.** The unsplit handoff these docs are derived from. Its § 24 lists
  every correction made over Revision 7.
- `handoff-revision-7.md` — **superseded.** Kept for history. Several of its numbers and two of its
  instructions are now known to be wrong; do not cite it.

The split docs are authoritative for implementation. If they disagree with the 7.1 archive, the
disagreement is a bug — flag it.

---

## Conventions

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
  build Hearts, Diamonds, Split, Double Down, relics, commanders, or metaprogression unless asked.
- **Cite the doc when implementing a rule.** A comment naming the section beats restating the rule.
- **When a design question is genuinely unanswered**, say so and point at the risk register rather than
  inventing a rule. Several gaps are deliberate.
