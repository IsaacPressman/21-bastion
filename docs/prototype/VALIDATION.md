# Validation Architecture

Source: Handoff Revision 7.1, § 20 (Test Arms through Regression).

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

### The secondary measurement

The same three arms disambiguate the **many-card archetype**, since three separate Revision 7 changes — no
Wide Formation, escalating march, links reduced to runs — all landed on it, and a single build cannot say
which one killed it.

- Unviable in **C**, viable in **A** → the curve is the cause.
- Unviable in **all three** → links and board width are insufficient alone, and the archetype needs a
  mechanism — designed then **against a measured deficit rather than guessed at** (Add-Back 4).

### Implementation consequence

The march curve must be **swappable by configuration, not recompilation**, and all three presets ship in
the first build. This is the most concrete argument for the data-driven tuning approach in
`../ARCHITECTURE.md`.

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
