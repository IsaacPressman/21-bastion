# Validation Architecture

Source: Handoff Revision 7.1, § 20 (Test Arms through Regression). **The rank-stacking sequence, the siege
probe, and run-layer instrumentation** are from the Run Layer Handoff (consolidated), §§ 11, 12.
**Improved-encounter instrumentation, success criteria, and failure signals** are from the Improved
Encounters Handoff, §§ 18, 20, 21, 22.

> That is the whole validation architecture.

---

## Test arms

**Three arms, shipped as presets in one config file — not three builds.**

| Arm | Curve | Steps (3rd / 4th / 5th) | Cumulative Entry |
|---|---|---|---|
| **A** | Flat control | 1.0 / 1.0 / 1.0 | 1.0 / 2.0 / 3.0 |
| **B** | Soft escalation | +1.0 / +1.5 / +2.0 | 1.0 / 2.5 / 4.5 |
| **C** | Hard escalation | +1.5 / +2.5 / +3.5 | 1.5 / 4.0 / 7.5 |

**Arm C is the curve specified in the design documents.** Arm A is Revision 6's flat step.

> ⚠ **The arm letters changed in Revision 7.1.** In Revision 7 there were two arms, and **Arm A was the
> as-specified build with Arm B as the flat control** — the exact reverse of A and C now. Any pre-7.1
> reference to "Arm A (primary)" means what is now **Arm C**. Check the letter against the curve, never
> against memory.

### The primary measurement

> **The primary measurement is the shape of the fifth-card outcome, not aggregate output.**

For each arm, report:

1. **How often a safe fifth-card miss was nonetheless the better play** — measured by resolver output
   against the **stand-at-four counterfactual**;
2. separately, **whether players say they would take it again.**

**Arm C is expected to produce the binary outcome** described in `../design/03-march-clock.md` § The fifth
card is a hypothesis — rescued on exact 21, functionally dead on a safe miss, worse on a bust.

> **If it does, Arm B is the design.**

That sentence is the point of the whole exercise. The design is not defending Arm C; it is testing whether
Arm C is too sharp.

> ⚠ **Optional opportunity units change what this measurement measures.** The Milestone 5 sweep compared
> safe-miss against stand-at-four on **predicted leak alone**, which was correct while leak was the only
> thing a fifth card could buy. Once a wave carries an opportunity unit
> (`../design/06-dealer-and-enemies.md`), a card that leaks *more* may still be right — it cancelled a
> later reinforcement group, stripped a buff off the enemies still to come, completed a run, or avoided a
> costly replacement — and a leak-only comparison cannot see it.
>
> **The measured arm numbers remain valid for the encounter they were taken in.** They stop being the
> whole question. Re-run the sweep after opportunity units exist, and report both, rather than replacing
> the first with the second.

### The secondary measurement

The same three arms disambiguate the **many-card archetype**, since three separate Revision 7 changes — no
Wide Formation, escalating march, links reduced to runs — all landed on it, and a single build cannot say
which one killed it.

> ⚠ **Rank stacking is a fourth thing that lands on the many-card archetype**, which is why the stacking
> pass runs *after* the arms are measured without it. Reading the archetype from a stacking-enabled build
> would confound the Add-Back 4 trigger.

- Unviable in **C**, viable in **A** → the curve is the cause.
- Unviable in **all three** → links and board width are insufficient alone, and the archetype needs a
  mechanism — designed then **against a measured deficit rather than guessed at** (Add-Back 4).

### Implementation consequence

The march curve must be **swappable by configuration, not recompilation**, and all three presets ship in
the first build. This is the most concrete argument for the data-driven tuning approach in
`../ARCHITECTURE.md`.

---

## Rank-stacking sequence

Rank stacking (`../design/05-battlefield.md`) ships **flag-gated and default off.** The procedure is
ordered, and the order is the point.

1. Run **Flat, Soft, and Hard** March presets with stacking **disabled**. *(This is the existing arm
   measurement — done at Milestone 5.)*
2. Repeat **the same scripted fixtures and organic encounter** with stacking **enabled.**
3. Compare **forced-replacement frequency, stack-at-capacity rate, run frequency, placement depth, and
   many-card viability.**
4. If stacking becomes automatic at capacity, **test one cost in isolation.** **Do not change March and
   stacking simultaneously.**

> ⚠ **The Improved Encounters Handoff moved step 2 much later.** Stacking is now the **last** item in a
> seven-step encounter sequence — improved information and timeline, breakpoint enemies, tactical tower
> forms, Wave 2 counter-rotation, an opportunity unit, **validate the base encounter**, *then* enable
> stacking and re-run.
>
> **"Stacking should deepen a functioning placement game, not rescue a shallow one."** The risk it names
> is specific: a shallow encounter plus stacking reads as an improvement, and the improvement is
> attributed to the wrong system. See `../ROADMAP.md` § Improved-encounter build order.
>
> Note the consequence for step 1 — **the baseline the stacking pass is read against is now the *improved*
> encounter, not the Milestone 5 one.** The Milestone 5 baseline still answers the March-arm question. It
> does not answer the stacking question once the encounter beneath it has changed.

Step 4's constraint is the same discipline as the geometry remedy: the arms are pre-committed test arms, so
a second variable moving at the same time destroys the reading rather than enriching it.

### Pre-committed readings for the stacking pass

| Log | Reading |
|---|---|
| **Stack chosen whenever a match existed** | Stacking is **reflexive**, not a choice. Test a spatial or cadence cost — a longer shared cooldown — **not** a flat damage penalty |
| **Forced-replacement frequency drops sharply** | Stacking is acting as a **safety valve** on one of the three pillars of decision density. That is the ship/cut question, and it is Open Question 7 |
| **Placement depth returns to a rear cluster** | Concentrated power prefers safe sockets. The geometry remedy is already in, so this is a **stacking** result, not a geometry one — **diagnose geometry before taxing stacks** |
| **Many-card viability improves only with stacking on** | Stacking is doing Add-Back 4's job. Decide deliberately whether that is the mechanism designed against the measured deficit, or a coincidence |

---

## Scripted battery

**Each state presented at least twice with different presentation so players cannot answer from memory.**

1. Hard 18 against a severe Open lane, versus a mild one, versus one already Held
2. Hard 16 as **10+6** versus **3+3+5+5**
3. Soft 17 versus hard 17
4. A fourth card that would complete a run versus one that would not
5. A hand at socket capacity where the best replacement is a **good** tower
6. A marginal hand with a **Vault** lane versus the same hand with a **Bastion** lane
7. A hand at 18 where the only 21 is a single surviving rank
8. A Dealer showing a **King** versus a Dealer showing a **3**
9. A placement where family choice must be committed before the lane's threat is fully known
10. A hand where **the single adjustment move** can save a run link **or** answer a lane, but not both

These are scripted fixtures, not random encounters — they need deterministic seeding and reproducible
setup.

---

## Success criteria

- Players **commit families deliberately** and can explain the commitment afterward.
- Players **place for runs**, not only for range.
- Players **change the hard-18 decision** between severe and mild lane states.
- Players make **different decisions for 10+6 and 3+3+5+5**.
- Players **triage differently** between Bastion and Vault lanes.
- Players read the Dealer's upcard **as a unit on the field, not a number**.
- Players **chase the fifth card sometimes, and regret it sometimes**.
- Forced replacement produces **visible hesitation**.
- Bust feels **bad, occasionally correct, and never desirable**.
- Combat is **skipped or watched by choice, not endured**.
- Players **want another encounter**.

### The improved encounter is working if

These are the Improved Encounters Handoff's own criteria. The first is the one everything else serves:

- **Before most Hit decisions, players can name the battlefield problem they are trying to solve.**
- Players use the **timeline** to reason about attacks, March loss, slow, and enemy breakpoints.
- Important cards routinely present **2–3 plausible deployments**.
- Players make **different placements for the same rank** under different enemy timing and stakes.
- **Snare changes the value of Barrage** in a way players notice and intentionally exploit.
- **Forward placement is sometimes correct** despite March exposure, because an early breakpoint matters.
- **Rear placement is sometimes correct**, because preserving engagement matters more.
- **Junction placement is used as a hedge**, not merely because no other socket was available.
- **Wave 2 feels like adaptation to an existing board**, not a fresh setup.
- Optional opportunities **sometimes** motivate an otherwise unnecessary draw.
- Safe fourth- and fifth-card misses are **occasionally defensible for battlefield reasons**.
- Players can explain **what the last committed card bought them**.
- Players do **not** routinely brute-force every hover combination.
- Placement stays **brisk** — the encounter does not become optimization homework.

Note that three of these are two-sided on purpose. Forward *sometimes*, rear *sometimes*, opportunities
*sometimes*: each names a failure in both directions, which is what stops the criterion from being
satisfied by a build that simply moved the dominance somewhere else.

---

## Failure signals

Each of these has its response attached, and **the response is almost never "add a mechanic."** That is
the diagnosis the whole encounter pass rests on.

| Signal | What it means and what to do |
|---|---|
| **The player still cannot say why they want another card** | The **information layer** has failed. Do not add more mechanics |
| **Players only compare leakage numbers** | The encounter collapsed into scalar minimization. Increase competing battlefield consequences — **not** hidden information |
| **Everyone builds deep** | Breakpoint enemies are too weak, too rare, or badly positioned. **Fix enemy timing before touching socket bonuses** |
| **Everyone uses the same form for a rank** | The four forms are not tactically differentiated enough |
| **Snare and Barrage are useful but never combined** | The bunching interaction is too weak or too hard to read |
| **Players hover every candidate before choosing** | The forecast has become a brute-force oracle. Reduce sortable outputs, emphasize causal tradeoffs |
| **Players ignore optional opportunities** | Payoff too small, or too detached from the run |
| **Players always pursue optional opportunities** | They are mandatory objectives in disguise. Lower the payoff or raise the situationality |
| **Wave 2 feels like Wave 1 with more enemies** | Persistence is not producing adaptation. **Rewrite encounter pairs before adding progression systems** |
| **Placement times explode** | Do not add decisions. Simplify presentation, reduce candidate forms, or make the timeline more legible |

The third row deserves a second look. *"Fix enemy timing before touching socket bonuses"* points the
opposite way from § Deep placement's pre-committed reading, and the resolution is now explicit: **the
shipped range-by-socket values stay authoritative**, breakpoints are built as a separate tactical-depth
hypothesis, and the geometry question is settled afterwards by an isolated four-step measurement — never
by tuning both at once. `../reference/tuning-constants.md` § Known Discrepancies, entry 12.

---

## Instrumentation

**Per offered state, log:** exact hand, Ace states, remaining rank counts, entry position and **per-socket
window remaining**, **socket occupancy and socket depth distribution**, active runs, **per-lane Visible
Threat** and stakes, Dealer upcard and deployed units, **the choice made and time to decide**, whether
placement changed before the choice, and **result versus Final Forecast**.

Note the two changes from Revision 7: engagement is logged **per socket**, not as a single number (the
summed scalar was withdrawn — `../design/03-march-clock.md`), and the forecast comparison is explicitly
against the **Final** Forecast.

**Debug only** (never player-facing — see `../design/09-information-and-ui.md`): exact bust probability,
stand and hit expected output, combined utility.

### Specific instrumentation with pre-committed readings

| Log | Reading |
|---|---|
| **Placement depth** | If towers cluster at socket 9 across every arm, the **deep-placement dominance** flagged in `../design/03-march-clock.md` is real, and **the socket geometry needs work before the march curve does.** |
| **Adjustment-window usage**, including which move was *wanted* where the interface can capture it | Never used → candidate for deletion. Players consistently want two → **the relic path opens, the baseline does not widen.** |
| **Combat watched / fast-forwarded / skipped** | *"If it is always skipped, that is information, not failure."* |
| **Run frequency per hand** | Too rare to shape placement → triggers Add-Back 3 (pairs). |

Deciding what a measurement means *before* taking it is the point. Do not renegotiate these readings after
seeing the data.

**When the stacking flag is on, also log:** match opportunity, whether the stack was chosen, what the
replacement alternative was, capacity state, socket depth, and the families in the stack. Readings for
those are in § Rank-stacking sequence above.

### Improved-encounter instrumentation

Most of this is **interface-side** — it measures how the player searched, not what the session contained —
so it lands in `game/telemetry/PlaytestLog.cs` rather than in `SessionSnapshot`.

**Placement behavior.** Time per card placement; median and 90th-percentile placement time; time by card
number in hand; **number of candidate forms hovered**; **number of candidate sockets hovered**; number of
times the player moves between two competing options before committing.

**Tactical understanding** — facilitator-observed, not derivable:

- Can the player explain the current battlefield shortfall **before** hitting?
- Can they explain **what the last card changed**?
- Do they reference timeline events, breakpoints, and runs, or **raw power**, when explaining a placement?

**Candidate-space health.** Occasionally ask: *"which placements were you seriously considering?"*

| Answer | Reading |
|---|---|
| **2–3**, usually | The target |
| **One**, repeatedly | The puzzle is too obvious |
| **Six or more**, repeatedly | The state is too noisy |

**Timeline usage.** Whether the player expands detailed stats or relies on the timeline; whether March-step
consequences are understood **before** drawing; whether standing-order changes are made from timeline
information.

**Hover-brute-force risk.** Flag states where the player inspects nearly every form-and-socket
combination before committing. **If that is common, the candidate preview is functioning as an oracle** —
the guardrail in `../design/14-encounter-timeline.md` § The solvable-puzzle risk, made measurable.

**Optional opportunities.** How often pursued; how often pursuing one causes an **additional Hit**; how
often that Hit is a fourth or fifth card; whether safe misses remain tactically defensible; whether
players describe them as optional or mandatory.

**Wave 2.** Persisted towers retained, replaced, and stacked when the flag is on; run links broken versus
preserved; **whether Wave 2 produces materially different placement reasoning from Wave 1.**

> The Wave 2 row is the one that decides whether encounter-scoped persistence earns its place at all
> (`../design/05-battlefield.md` § Wave 2 must disturb the Wave 1 solution).

---

## The run layer

Nothing here is built yet, and **nothing here may delay the encounter vertical slice.** It is recorded now
because the readings are worth pre-committing while the reasoning is fresh, and because two of the logs
below are prerequisites the encounter build has to satisfy first.

### The siege menu probe

**Build only a menu-level probe, and only after the encounter loop works.** Two visible fronts, one phase
clock, four preparation actions at fixed costs, **no persistent geography simulation.** Orders and costs
are in `../design/12-campaign-time-and-orders.md` § The siege menu probe.

**The first probe uses Time only.** Favor enters once encounter telemetry can identify the risk behaviors
that should earn it — which makes Favor **downstream of the encounter instrumentation**, not of the
campaign build.

Success signals: players can explain what they bought with time and what they let worsen elsewhere; players
**sometimes conserve time**; players describe the two clocks as related pressures **without being told the
analogy**; the campaign menu makes the next encounter **more anticipated, not delayed.**

### Run-layer instrumentation

| System | Log |
|---|---|
| **Stacking** | Match opportunity, stack chosen, replacement alternative, capacity state, socket depth, families in stack |
| **Time** | Hours remaining, order selected, visible alternatives, front transformations triggered, **unused time** |
| **Dealer recruitment** | Candidate row, replacement targets, intended pair, player raid choice, final one-for-one replacement, **build signals used for next-phase weighting** |
| **Geography** | Front state before/after, **Lost vs Conceded cause**, path-length changes, socket changes, Last Stand trigger, next-encounter modifier |
| **Favor** | Favor before/after, earning trigger, spend type, **whether the spend changed the encounter decision or merely erased a mistake** |
| **Run survival** | Bastion Health, phase time, scheduled assaults, outer fronts remaining, early-vs-scheduled Last Stand, final victory/defeat cause |
| **Card identity** | History tags earned, Promote choices, exhaustion, Reserve substitutions, modifier distribution |
| **Cadence** | Time spent on the command screen, backtracking, **number of distinct menus opened**, next-encounter start latency |

Three of these carry their reading in the log line itself, and are worth naming:

- **Unused time.** If players always spend to zero, time is not a resource, it is a checklist.
- **Whether a Favor spend changed a decision or erased a mistake.** Favor that only undoes errors is a
  mulligan wearing command authority.
- **Number of distinct menus opened.** The cadence target is one decision surface; menu count is how that
  target fails quietly.

---

## How to run it

Built at Milestone 5. Arms and cases are selected at launch; nothing here needs a rebuild between them.

```bash
# A scripted case on a chosen arm. Flags go after -- so they cannot collide with Godot's own.
godot --path . -- --arm B --fixture 2-split
godot --path . -- --arm A --fixture 7-onlyrank-b        # -b is the mirrored presentation
godot --path . -- --arm C                               # no case named: the facilitator picker opens
godot --path . -- --arm C --seed 4242                   # free play on a chosen seed
godot --path . -- --no-log                              # suppress the session log
```

**Cases.** The ten items above name contrasts as well as states, so they expand to 17 cases — ids like
`1-severe`, `2-split`, `8-king` — each with a generated `-b` mirror. `data/battery.json` is the source;
an unknown id prints the full list. Variant B swaps the two lanes wholesale and reverses the opening
deal, so the decision is identical and nothing on screen is.

**Logs.** One JSONL file per session at `telemetry/sessions/<utc>-arm<X>-<case>.jsonl`, one line per
offered state, carrying the state as offered and the choice that closed it. Gitignored — these are raw
sessions, not results.

**The oracle tier is absent unless asked for.** Bust probability, expected output, and combined utility
are compiled out; build with `-p:BastionInstrumentation=true` and they appear under an `oracle` key.
Confirming that round trip is how the gate is verified to be real rather than remembered.

**Measurements** (all `-p:BastionInstrumentation=true`, all writing to `telemetry/`):

| Sweep | Output |
|---|---|
| `FifthCardOutcomeSweep` | `fifth-card.csv` — the primary measurement. Slow, ~80 s. |
| `DeepPlacementSweep` | `deep-placement.csv`, `deep-placement-runs.csv`, `geometry-candidates.csv` |
| `ShoeSimulation` | `shoe-simulation.csv` |

---

## Regression

Runnable as one suite:

```bash
dotnet test tests/Bastion.Core.Tests.csproj --filter Category=Regression
```

Golden baselines live in `tests/Regression/baselines/` and are **regenerated deliberately, never on
failure** — `BASTION_REGEN_BASELINES=1`, which rewrites them and then fails the run so a regeneration
cannot be mistaken for a pass.

**Before changing the march curve, Formation Strength, run percentages, tower power, Overload, or the
resolver:**

1. **Re-run the benchmark hand set** and flag sign changes.
2. **Enumerate all legal two-to-five-card hands**; record **raw output and entry position**. ⚠ **Do not
   record a derived engagement-adjusted output** — `../design/03-march-clock.md`.
3. **Simulate 10,000 hands** each for baseline, face-heavy, and many-card shoes; report output, bust rate,
   board width, run frequency, and final entry position.
4. **Verify Final-Forecast-versus-resolution equivalence** on the scripted fixtures, **and verify that
   Visible Threat matches a resolver run against the revealed force alone.**

Step 4 is the two forecast contracts (`../design/05-battlefield.md`) made testable — both must be
independently verified, because they are different claims.

Steps 1–3 require the game logic to be runnable **headless, without the Godot scene tree** — the strongest
architectural constraint in the project. See `../ARCHITECTURE.md`.
