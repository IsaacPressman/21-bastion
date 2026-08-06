# Roadmap

**Status: Milestone 0 complete.** Scaffold, tuning-data layer, headless test suite, and the oracle gate
are in place and green. No game logic yet — Milestone 1 (the resolver) is next.

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

### 2. Socket geometry may need work before anything else — **measure at Milestone 1**

Revision 7.1 flags that **deep placement is weakly dominant whenever entry exceeds 0**: advancement eats
forward socket windows and leaves rear ones untouched. A mechanic added to enrich placement may be
flattening it.

The pushback — run-link adjacency, the junction socket, traps needing early application, leak thresholds —
**lives in the resolver, not the engagement arithmetic.** So this cannot be settled on paper.

> **This is the first thing to measure once the resolver runs. If deep placement wins everywhere, fix the
> socket geometry before touching the march curve.**

Remedies, if confirmed: uneven socket spacing, range differences by position, or lane-specific leak
thresholds.

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

### Milestone 1 — The resolver

The single deterministic simulation that drives both forecast and wave. **Build this first**; everything
else is a client of it.

- ✅ Path, sockets, entry point, **per-socket** engagement calculation — `core/March/`, with the entry
  clamp and the exactly-21 pullback, checked against every published table
- Enemy spawning by schedule and spacing, movement, leak damage
- Tower targeting, range, cooldown, armor with the 0.25 floor and half-armor bypass
- Deterministic tie-breaking by spawn order
- Standing orders — Hold, Focus, Trigger on group — modeled exactly
- Per-lane outputs: empty-lane damage, predicted damage, damage prevented, per-tower activity, cause of
  leakage
- **Two forecast return types** — Visible Threat and Final Forecast — distinguished in the type system, not
  by a flag

**Done when:** the resolver runs headless, produces identical output for identical input across runs, and
reproduces the engagement tables in `design/03-march-clock.md`.

**Then immediately measure deep-placement dominance** (Open Decision 2). It is the first question the
resolver exists to answer, and the answer may change the socket geometry before any tuning happens.

**Two invariants established at this milestone**, both hard to retrofit:

1. There is **one simulation path**. The visual wave is a *presentation* of a resolver run, never a
   re-simulation.
2. There are **two forecast types**, and one cannot be rendered where the other is expected.

### Milestone 2 — Hand and formation

- Blackjack: hit, stand, hard/soft totals, Ace 1/11 with immediate battlefield transformation
- Shoe: 26 cards, persistence across waves, reshuffle under 8
- Formation Strength curve
- Card power curve; Ace Bastion on natural
- Family selection locked at placement (Club/Spade)
- Run links — after settling Open Decision 2
- Socket occupancy and forced replacement at capacity
- March Clock: escalating step paid pre-reveal, exactly-21 pullback

**Done when:** the output landmarks table in `design/02-blackjack-and-formation.md` reproduces exactly, and
`3+3+5+5` and `10+6` produce visibly different boards.

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
