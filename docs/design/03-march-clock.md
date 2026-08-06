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

With sockets at path positions 3, 6, and 9 and a range of 3.0 units, each tower's engagement window is:

| Socket | Window | Length |
|---|---|---:|
| 3 | 0–6 | 6.0 |
| 6 | 3–9 | 6.0 |
| 9 | 6–12 | 6.0 |
| **Total** | | **18.0** |

Advancing the army's entry point by 1.0 unit eats one unit from the socket-3 tower and *nothing* from the
other two, because their windows begin at or after the new entry point. Total engagement falls from 18.0
to 17.0 — a **5.6%** cost.

Worse, that cost is not a tax on drawing. **Because entry advances from the spawn side, it eats the
forward socket's window first and the rear socket's last — so it is a tax on forward placement, which a
player avoids by building deep.** The intended pressure was close to inverted.

> **Correction: the step must be comparable to the 3.0-unit socket spacing to consume whole windows
> rather than shaving one.**

Keep this reasoning attached to the numbers. If socket spacing or range is ever retuned, the march step
sizes must be re-derived from them — they are not independent.

---

## ⚠ Deep placement may be weakly dominant

**Flagged before any build.** At entry 0 all three sockets give identical engagement. Every unit of
advancement degrades forward sockets while leaving rear ones untouched, so **deep placement is weakly
dominant whenever entry exceeds 0**, and more so as the clock bites harder.

**A mechanic added to enrich placement may be flattening it.**

Run-link adjacency, the junction socket, traps that need early application, and enemies that must be
stopped before a leak threshold all push back — **but none of that pushback lives in the engagement
arithmetic. It lives in the resolver.**

> **This is the first thing to measure once the resolver runs. If deep placement wins everywhere, the
> socket geometry needs work before the march curve does.**

Instrumented via placement-depth logging (`../prototype/VALIDATION.md`). The remedy, if confirmed, is
socket geometry — uneven spacing, range differences by position, or lane-specific leak thresholds — **not
the march curve.**

---

## The rule

- Path length **12 units**. Enemies normally enter at position **0**.
- The **opening two cards are free.**
- Each subsequent card advances the entry point by an **escalating** step, paid **at the moment of the
  draw, before the card is revealed.**

Shipping curve (**Arm C — hard escalation**, the curve specified by the design):

| Card | Step | Cumulative Entry | Engagement Remaining | Cost |
|---|---:|---:|---:|---:|
| 3rd | +1.5 | 1.5 | 16.5 | −8% |
| 4th | +2.5 | 4.0 | 13.0 | −28% |
| 5th | +3.5 | 7.5 | 6.0 | −67% |

The third card is cheap, which is correct — a third tower is worth far more than 8% of engagement. The
fourth is a real decision. The fifth is close to lethal.

The other two presets (flat, and soft escalation) ship in the same build. See
`../reference/tuning-constants.md` § March Clock Presets.

### Beyond the fifth card

Hands longer than five cards are legal — four Aces plus 2,2,3,3 reaches eight. **The final step repeats
indefinitely, and entry clamps at 9.0.**

| Hand | Unclamped | Actual entry | Engagement |
|---|---:|---:|---:|
| 6 cards | 11.0 | **9.0** | 3.0 |
| 7 cards | 14.5 | **9.0** | 3.0 |
| 6-card 21 | — | **6.0** | 9.0 |

**Why clamp.** Uncapped repetition puts a seven-card hand *past the end of the path* — enemies spawning at
the Bastion, zero engagement, a guaranteed full leak. That is an automatic loss for a legal, rare,
genuinely impressive hand, and it feels worse than any amount of severity.

**Why 9.0.** It is the rear socket's own position, so **enemies never spawn past your last defense.** It
leaves 3.0 engagement of 18.0 — brutal, survivable. The clamp is derived from geometry, not chosen
independently of it; `TuningLoader` fails the load if the two disagree.

**The clamp applies before the pullback.** A six-card 21 lands at entry 6.0 and recovers 9.0 units of
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

For entry point `e`, a socket at position `s` with range `r` on a path of length `L`:

```
engagement(s) = max(0, min(s + r, L) - max(s - r, e))
total_engagement = Σ engagement(s) over occupied sockets
```

Every row in every table above reproduces exactly. Note the `min(s + r, L)` term — omitting it was the
source of the Revision 7 arithmetic error (socket 9's full 6.0 window summed against a remaining path of
only 4.5 units).

Engagement is a property of **occupied** sockets. The 18.0 figure assumes all three sockets in a lane are
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

**Whether the −67% curve permits that middle outcome is an open question and probably the most important
thing the prototype measures.** It is the primary measurement of the test arms — see
`../prototype/VALIDATION.md`.

---

## Diamonds — the counterplay (full game only)

In the full game, Diamond structures **extend path length**, which reduces the proportional cost of every
march step. This is Diamonds' primary strategic identity and the main counterplay to hand length.

**Not in prototype.** Note the consequence: the prototype tests the march curve with its intended
counterplay absent.
