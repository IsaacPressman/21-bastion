# The March Clock

Source: Handoff Revision 7.1, § 4.

**This is the most important system in the game to get tunable, and the most likely to be wrong.** It
ships as **three config presets**, not one curve — see `../prototype/VALIDATION.md`.

---

## The job it now does

The March Clock **prices hand length**. It does not make low totals tense — nothing can, at a step size
that leaves long hands playable.

---

## The geometry problem it had (and why the fix is shaped this way)

Sockets sit at path positions 3, 6, and 9. **Range differs by socket** — 4.0, 3.0, 2.0 — for the reason
in the next section. Each tower's engagement window is:

| Socket | Range | Window | Length |
|---|---:|---|---:|
| 3 | 4.0 | 0–7 | 7.0 |
| 6 | 3.0 | 3–9 | 6.0 |
| 9 | 2.0 | 7–11 | 4.0 |
| **Total** | | | **17.0** |

Both outer windows are clipped by the path: socket 3 would reach back to −1 and socket 9 forward to 11
against a path of 12.

Advancing the army's entry point eats the **forward** socket's window first and the rear socket's last,
because entry advances from the spawn side. Under the original flat 3.0 range that made the cost a **tax
on forward placement, which a player avoids by building deep** — the intended pressure close to inverted.
Range-by-position is the answer to that, not a change to the step.

> **Correction: the step must be comparable to the 3.0-unit socket spacing to consume whole windows
> rather than shaving one.**

Keep this reasoning attached to the numbers. If socket **spacing** is ever retuned, the march step sizes
must be re-derived from it — they are not independent. Range is a separate lever: it redistributes
engagement between sockets without changing the spacing the steps are sized against, which is exactly why
it could be used as the remedy while the arms stayed fixed.

---

## ✅ Deep placement was weakly dominant, and the geometry was fixed

**Flagged before any build, measured at Milestone 1, remedied at Milestone 5.**

The original geometry gave all three sockets an identical 6.0 window at entry 0, so every unit of
advancement degraded forward sockets while leaving rear ones untouched — **deep placement was weakly
dominant whenever entry exceeded 0**, and more so as the clock bit harder. Run-link adjacency, the
junction socket, and leak thresholds all push back, but **none of that pushback lives in the engagement
arithmetic — it lives in the resolver**, so it could only be settled by running it.

It was. `tests/Measurement/DeepPlacementSweep.cs` confirmed deep dominance in every arm, and the
pre-committed reading in `../prototype/VALIDATION.md` took effect: **fix the socket geometry before the
march curve.** Nine candidates were swept against a selection rule committed before the numbers were read
(`telemetry/geometry-candidates.csv`).

**Result: range differences by position.** Forward sockets open with a wider window and therefore have
more to lose; rear sockets open with less and lose none. A short hand is better off forward, a hand that
paid for a fifth card is better off deep, and the crossover is the decision.

| Arm | Depth effect before | After |
|---|---:|---:|
| A (flat) | −1.40 | +0.73 |
| B (soft) | −1.47 | +0.40 |
| C (hard) | −1.87 | +0.40 |

Negative means deep placement leaked less, i.e. deep won.

**Two honest caveats.** Uneven socket spacing — the remedy this document named *first* — was measured and
**does not work**: it left the margin unchanged or slightly worse, because moving the middle socket does
not change which end advancement arrives from. And the remedy slightly overshoots: the residual is now a
mild *shallow* lean, largest in Arm A (+1.27 with run links modelled) and smallest in Arm C. Placement-depth
logging stays in the instrumentation set to watch it.

### 🔬 Breakpoints are a second hypothesis, not a replacement

Improved Encounters § 9 declares **spatial breakpoint enemies** the *"baseline solution to deep-placement
dominance"* and adds *"do not give sockets arbitrary statistical bonuses to create identity"* — which,
read literally, argues against the remedy that is already in the build.

**That language is softened, and this is the wording that governs:**

> **Spatial breakpoints give forward and middle positions distinct tactical jobs and may reduce or
> eliminate the need for socket-specific range. The currently validated range-by-socket values remain
> authoritative until breakpoints are implemented and re-measured in isolation.**

**Range-by-socket remains the baseline remedy.** Breakpoints are added as a separate tactical-depth
hypothesis. They may eventually replace range asymmetry; **they have not earned that yet**, and a measured
remedy is not reverted on the strength of an unbuilt one.

The two are not obviously exclusive. Range redistributes *engagement*; breakpoints redistribute *what must
be solved where*. A lane where the Standard Bearer must die before socket 6 gives forward sockets a job no
range figure confers — and it is equally possible that stacking both overshoots, since the residual
shallow lean above is already the measured side effect of the remedy that shipped.

**The experiment, in isolation and in this order:** build breakpoints keeping 4.0 / 3.0 / 2.0 → measure →
run the identical sweep at flat 3.0 → compare → only then decide whether range asymmetry is still
necessary. **Do not tune breakpoints and range together.** Four outcomes are pre-committed, including
*keep 4/3/2 unchanged* and *reduce asymmetry because the combination overshot*.

Full protocol and the outcome table: **Known Discrepancy 12** in `../reference/tuning-constants.md`.

---

## The rule

- Path length **12 units**. Enemies normally enter at position **0**.
- The **opening two cards are free.**
- Each subsequent card advances the entry point by an **escalating** step, paid **at the moment of the
  draw, before the card is revealed.**

Shipping curve (**Arm C — hard escalation**, the curve specified by the design):

| Card | Step | Cumulative Entry | Engagement Remaining | Cost |
|---|---:|---:|---:|---:|
| 3rd | +1.5 | 1.5 | 15.5 | −9% |
| 4th | +2.5 | 4.0 | 12.0 | −29% |
| 5th | +3.5 | 7.5 | 5.0 | −71% |

The third card is cheap, which is correct — a third tower is worth far more than 9% of engagement. The
fourth is a real decision. The fifth is close to lethal.

> **The geometry remedy did not soften the clock.** These costs were −8% / −28% / −67% under the flat
> range. That they barely moved is deliberate: the three march arms are pre-committed test arms, and a
> geometry change that quietly flattened the curve would have answered the fifth-card question before the
> playtest could ask it.

The other two presets (flat, and soft escalation) ship in the same build. See
`../reference/tuning-constants.md` § March Clock Presets.

### Beyond the fifth card

Hands longer than five cards are legal — four Aces plus 2,2,3,3 reaches eight. **The final step repeats
indefinitely, and entry clamps at 9.0.**

| Hand | Unclamped | Actual entry | Engagement |
|---|---:|---:|---:|
| 6 cards | 11.0 | **9.0** | 2.0 |
| 7 cards | 14.5 | **9.0** | 2.0 |
| 6-card 21 | — | **6.0** | 8.0 |

**Why clamp.** Uncapped repetition puts a seven-card hand *past the end of the path* — enemies spawning at
the Bastion, zero engagement, a guaranteed full leak. That is an automatic loss for a legal, rare,
genuinely impressive hand, and it feels worse than any amount of severity.

**Why 9.0.** It is the rear socket's own position, so **enemies never spawn past your last defense.** It
leaves 2.0 engagement of 17.0 — brutal, survivable. The clamp is derived from geometry, not chosen
independently of it; `TuningLoader` fails the load if the two disagree.

**The clamp applies before the pullback.** A six-card 21 lands at entry 6.0 and recovers 8.0 units of
engagement — still a real rescue at the point where you most need one.

### Two accepted consequences

Noted rather than fixed:

1. **Past the clamp, further cards cost nothing on the clock.** A seventh card is free in march terms.
   This is a mild perverse incentive and it is **accepted**: at six-plus cards the bust probability is
   enormous, and every card past the seventh forces a replacement of one of your own towers. The remaining
   costs are severe enough without the clock. **Flag it; do not engineer around it.**
2. **Board width caps at seven regardless.** Seven sockets, so card eight onward replaces something the
   player placed. Beyond seven cards there is no board benefit at all — pure cost. This falls out of the
   existing capacity rule and needs no new handling.

### Engagement formula

For entry point `e`, a socket at position `s` with **its own** range `r` on a path of length `L`:

```
engagement(s) = max(0, min(s + r, L) - max(s - r, e))
total_engagement = Σ engagement(s) over occupied sockets
```

Every row in every table above reproduces exactly. Note the `min(s + r, L)` term — omitting it was the
source of the Revision 7 arithmetic error (socket 9's full window summed against a shorter remaining
path). Note also that `r` is read **per socket** — see § The geometry problem it had.

Engagement is a property of **occupied** sockets. The 17.0 figure assumes all three sockets in a lane are
filled.

---

## Exactly 21 pulls the army back

**Reaching exactly 21, at any card count, pulls the entry point back 3.0 units**, clamped at 0.

| Hand | Entry | Engagement | Cost |
|---|---:|---:|---:|
| 3-card 21 | 0.0 | 18.0 | none |
| 4-card 21 | 1.0 | 17.0 | −6% |
| 5-card 21 | 4.5 | 12.0 | −33% |

This is the design's most dramatic moment. A fifth card taken and missed is punishing; a fifth card taken
and *landed* converts a near-lethal position into a wide, heavily linked, ×1.60 board. Long hands become a
precision play with a visible rescue condition, rather than an archetype that gets a flat bonus for
existing.

**The pullback is deliberately held at its Revision 7 value.** Correcting the arithmetic, splitting the
forecast, and narrowing the adjustment window were enough for one pass; changing the pullback and the step
curve together would mean tuning two coupled levers against a number that had just been shown to be
untrustworthy. **Let the three arms report first.**

---

## Total engagement is explanatory, not a balance number

> **Revision 7 multiplied board power by a total-engagement fraction to estimate output. That estimate is
> withdrawn.**

Summed engagement is a scalar over a board whose **sockets are not interchangeable**. Advancement removes
different amounts of coverage from different sockets, and **three units taken from a 5.0-power King is not
three units taken from a 1.6-power two.** A single fraction cannot express that, and it should never have
been multiplied into an output figure.

> **Use total engagement to explain the clock to the player and to the team. Balance through the
> resolver.**

The march curve's real cost is **whatever the resolver reports as changed lane leakage**, measured per
configuration — not what a fraction predicts.

This has teeth in three places:

- Do not multiply the output landmarks in `02-blackjack-and-formation.md` by an engagement fraction.
- The regression enumeration must **not** record a derived engagement-adjusted output
  (`../prototype/VALIDATION.md`).
- The UI shows **which socket windows the next step cuts into, on the lane** — not a single engagement
  number (`09-information-and-ui.md`).

The Improved Encounters Handoff restates this and goes one step further: **the timeline and the per-socket
windows *replace* the scalar as the player-facing readout**, and the intended reading of a march step is
*"this cannon loses two shots before the Siege Engine crosses socket 9"* rather than *"entry moves from 1.5
to 4.0"* (`14-encounter-timeline.md`). The scalar survives in debug tools and in these documents. It has
no player-facing home at all.

---

## The fifth card is a hypothesis, not an identity

Revision 7 stated **as design** that the fifth card is worth taking only if it reaches 21. That claim was
produced by the withdrawn scalar, so it is **unproven — and if it turned out to be true, it would probably
be unhealthy.**

A strictly binary fifth card reduces the decision to a rank count:

| Outcome | Result |
|---|---|
| Exact 21 | Rescued |
| Safe miss | Functionally dead |
| Bust | Worse |

In that shape **the battlefield only sets how desperate the player is**, and the choice collapses into
counting the one rank that saves them.

### The target shape instead

| Outcome | Intended feel |
|---|---|
| **Exact 21** | Spectacular |
| **Safe miss** | Usually bad, but **sometimes defensible** — because of a run it completes, a replacement it avoids, a family the lane needs, or a stake worth less than health |
| **Bust** | Clearly worse than both |

**Whether the −71% curve permits that middle outcome is an open question and probably the most important
thing the prototype measures.** It is the primary measurement of the test arms — see
`../prototype/VALIDATION.md`.

### The Improved Encounters Handoff attacks this from the other side

Every remedy above tries to make the middle outcome survivable by **tuning the price**. Improved Encounters
§ 12 instead gives the card **something else to buy**: an optional opportunity unit means a board that
already survives can still justify another draw — to cancel a reinforcement group scheduled later in this
encounter, strip a buff off the enemies still to come, complete a run, avoid a costly replacement, or
enable a tactical interaction (`06-dealer-and-enemies.md` § Optional opportunity units). **The payoff is
encounter-local; Favor is not involved in the prototype.**

> **Exact 21 remains spectacular, but a safe miss does not have to be worthless.**

**This changes what the fifth-card measurement is measuring.** The Milestone 5 sweep compared safe-miss
against stand-at-four on **predicted leak alone**, because leak was the only thing a fifth card could buy.
With opportunity units in the wave, a card that leaks *more* may still be correct — and no leak-only
comparison can see that. The measured arm numbers stay valid for the encounter they were taken in, and
they stop being the whole question. Noted in `../prototype/VALIDATION.md` § The primary measurement.

---

## Diamonds — the counterplay (full game only)

In the full game, Diamond structures **extend path length**, which reduces the proportional cost of every
march step. This is Diamonds' primary strategic identity and the main counterplay to hand length.

**Not in prototype.** Note the consequence: the prototype tests the march curve with its intended
counterplay absent.
