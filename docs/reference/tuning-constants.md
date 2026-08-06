# Tuning Constants

**Every number in the design, in one place.** Implement these as data — a Godot resource or config file —
not as inlined literals. Several are expected to change; two must be swappable at runtime for the test
arms.

> Every number here is first-pass and expected to be wrong. No number carries a confidence interval,
> validity window, or tolerance. Those are outputs of playtesting.

---

## Geometry

| Constant | Value | Notes |
|---|---:|---|
| Path length | 12.0 | Diamonds extend this in the full game |
| Socket positions | 3.0, 6.0, 9.0 | Per lane |
| Sockets per lane | 3 | |
| Lanes | 2 | Prototype |
| Junction sockets | 1 | Shared; reduced contribution |
| Total sockets | 7 | |
| Default tower range | 3.0 | |
| Face card range (10/J/Q/K) | 4.0 | Also no junction contribution penalty |
| Default entry point | 0.0 | |
| Full engagement (3 towers, entry 0) | 18.0 | Derived, not independent |

**Engagement formula:**
```
engagement(socket s, range r, entry e, path L) = max(0, min(s + r, L) - max(s - r, e))
total = Σ over occupied sockets
```

> ⚠ **Total engagement is explanatory, not a balance number.** Do not multiply board power by an engagement
> fraction to estimate output — sockets are not interchangeable, and coverage lost from a 5.0-power King is
> not coverage lost from a 1.6-power two. **Balance through the resolver.** See
> `../design/03-march-clock.md`.

---

## March Clock presets — **all three ship in the first build**

Presets in **one config file, not three builds.** These are also the three test arms.

### Arm C — hard escalation (the curve the design documents specify)

| Card | Step | Cumulative Entry | Engagement Remaining | Cost |
|---|---:|---:|---:|---:|
| 1st, 2nd | 0.0 | 0.0 | 18.0 | free |
| 3rd | +1.5 | 1.5 | 16.5 | −8% |
| 4th | +2.5 | 4.0 | 13.0 | −28% |
| 5th | +3.5 | 7.5 | **6.0** | **−67%** |

### Arm B — soft escalation

| Card | Step | Cumulative Entry |
|---|---:|---:|
| 3rd | +1.0 | 1.0 |
| 4th | +1.5 | 2.5 |
| 5th | +2.0 | 4.5 |

### Arm A — flat control (Revision 6's step)

| Card | Step | Cumulative Entry |
|---|---:|---:|
| 3rd | +1.0 | 1.0 |
| 4th | +1.0 | 2.0 |
| 5th | +1.0 | 3.0 |

> ⚠ **Arm letters were reassigned in Revision 7.1.** Pre-7.1 documents used **Arm A = as-specified, Arm B =
> flat control** — the reverse of A and C now. Always check the letter against the curve.

| Constant | Value |
|---|---:|
| Exactly-21 pullback | **−3.0 units** — **deliberately unchanged in 7.1** |
| `entryClampMin` | 0.0 |
| `entryClampMax` | **9.0** — the rear socket position; validated against `max(socketPositions)` |
| Steps beyond the 5th card | **repeat the final step**, then clamp |
| Engagement range | 18.0 down to 6.0 across 2–5 cards; **3.0 at the clamp** |

Step is paid **at the moment of the draw, before the card is revealed.** Not refunded on bust.

**The clamp applies before the pullback.** Order matters — a 6-card 21 lands at 6.0 (9.0 clamped, then
−3.0), recovering 9.0 engagement, rather than being stranded at the clamp.

| Hand | Unclamped | Entry | Engagement |
|---|---:|---:|---:|
| 6 cards | 11.0 | 9.0 | 3.0 |
| 7+ cards | 14.5+ | 9.0 | 3.0 |
| 6-card 21 | — | 6.0 | 9.0 |

Without the clamp a 7-card hand spawns enemies past the end of the path — zero engagement, guaranteed full
leak, an automatic loss for a legal and rare hand.

**Tuning direction, if the fifth card proves binary: reduce the fifth step before raising the pullback.**
Revision 7 advised the opposite and was wrong — raising the pullback makes the mechanic *more* binary. See
`../prototype/RISKS-AND-ADDBACKS.md`.

---

## Formation Strength

| Final Total | Multiplier |
|---|---:|
| 21 (any card count) | ×1.60 |
| 20 | ×1.50 |
| 19 | ×1.40 |
| 18 | ×1.30 |
| 17 | ×1.20 |
| 16 | ×1.15 |
| 15 | ×1.10 |
| 14 | ×1.05 |
| 13 | ×1.00 |
| 12 | ×0.95 |
| ≤11 | ×0.90 |
| Bust | ×0.80 |
| Persisted tower (start of next wave) | ×1.00 |

Curve span: 2.0×.

---

## Card power curve

Approximately `value^0.7`.

| Value | A(1) | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10/J/Q/K | A(11) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Base power | 1.0 | 1.6 | 2.2 | 2.6 | 3.1 | 3.5 | 3.9 | 4.3 | 4.7 | 5.0 | 5.4 |

| Constant | Value |
|---|---:|
| Ace Bastion power (natural blackjack) | 5.0 |
| Ace Bastion counts as a hand card | no |
| Ace Bastion shares hand multiplier | yes |

---

## Run links

| Run Length | Power Bonus |
|---|---:|
| 2 | +15% to both |
| 3 | +25% to all three |
| 4 | **absent — unreachable in prototype geometry** |

Consecutive values in **adjacent sockets**, direction-agnostic. Queen is wild (one value, chosen at lock
to maximize run length); Ace is 1 or 11 matching its current state. Computed at lock. A tower belongs to
**at most one run** — the longest chain containing it.

| Adjacency rule | Value |
|---|---|
| Within a lane | Linear: 3–6 and 6–9 adjacent, 3–9 not |
| `crossLaneAdjacency` | **false** |
| `junctionParticipatesInRuns` | **false** — the junction is a run island |
| Tie-break between equal-length runs | The run with the **smallest lowest socket index** wins |
| Guard | A run must contain **at least one non-Queen** card |

**The 4-run tier is absent on purpose.** Three sockets per lane, no cross-lane adjacency, junction
excluded → a run of four cannot be built. It returns when socket counts grow, which is what makes the
**Surveyor** relic a link tier rather than a coverage bump.

`TuningLoader` requires exactly the run lengths the geometry can reach, so adding a socket **fails the
load** until the 4-run tier is restored — rather than silently paying nothing.

---

## Enemies

| Enemy | Count | Health | Speed | Armor | Spacing | Leak Damage |
|---|---:|---:|---:|---:|---:|---:|
| Swarm unit | 8 | 4 | 1.00 | 0 | 0.45 s | 1 |
| Armored soldier | 3 | 12 | 0.65 | 1.5 flat | 1.50 s | 2 |
| Fast raider | 5 | 5 | 1.60 | 0 | 0.75 s | 1 |
| Siege engine | 1 | 30 | 0.40 | 2.0 flat | — | 5 |

| Constant | Value |
|---|---:|
| Armor damage floor | **0.25** — armor cannot reduce a hit below this |
| Spade / King armor bypass | Ignores **half** of flat armor |

### Dealer card → unit mapping

| Card | Unit |
|---|---|
| 2–4 | Swarm pack |
| 5–7 | Fast raiders |
| 8–10 | Armored soldiers |
| J | Skirmisher (lane-changes at junction) |
| Q | Standard bearer (buffs nearby enemies) |
| K | Siege engine |
| A | Herald — elite at 11, fragile scout at 1 |

---

## Rules and thresholds

| Constant | Value |
|---|---|
| Shoe size | **26** — two copies of each rank A–K |
| Reshuffle threshold | fewer than **8** cards remain before a wave |
| Dealer draw policy (prototype) | **stands on all 17s, including soft 17** |
| Dealer resolves on player bust | **yes, in full** |
| Overload damage | **equal to the busting card's base power** — does not scale with excess |
| `overloadTargetLane` | **`highest_visible_threat`** — the busting card is never placed, so it inherits no lane |
| `overloadTieBreakStake` | **`bastion`** |
| Adjustment window | **one move total** — relocate one tower one socket, *or* swap two adjacent towers |
| Standing-order changes | free; do not consume the move |
| Adjustment window on bust | **none** — placement locks immediately |
| Open/Held threshold | **Open** if predicted leakage ≥ **half** of empty-lane damage |
| Wave resolution time | **12–20 s** at normal speed |
| Tie-breaking | **spawn order** |

---

## Pacing targets

| Activity | Budget |
|---|---:|
| Hand decisions and placement | 14–19 min |
| Combat resolution | 6–9 min |
| Rewards and deck decisions | 6–9 min |
| Shops, events, routing | 4–6 min |
| Transitions and boss presentation | 2–4 min |
| **Total run** | **30–45 min** |

Run shape: 3 regions, 12 combat encounters, 27 waves.

---

## Resolved in Revision 7.1

### ✅ Fifth-card engagement — was −58%, is **−67%**

Revision 7 reported 7.5 units remaining at entry 7.5; **the correct value is 6.0.** The error was summing
socket 9's full 6.0 window against a remaining path of only 4.5 units — i.e. omitting the `min(s + r, L)`
term.

| Socket | Window | At entry 7.5 |
|---|---|---:|
| 3 | 0–6 | 0.0 |
| 6 | 3–9 | 1.5 |
| 9 | 6–12 | 4.5 |
| | | **6.0** |

All other rows were checked and hold: entry 1.5 → 16.5, entry 4.0 → 13.0, 4-card 21 at entry 1.0 → 17.0,
5-card 21 at entry 4.5 → 12.0.

**Downstream:** the engagement-fraction output estimates that used this figure are **withdrawn entirely**,
not corrected — see the warning under the engagement formula above.

### ✅ 3+3+5+5 engagement comparison — was 28%, is **38%**

18.0 against 13.0.

### ✅ March placement bias — was stated backwards

Revision 7 called the flat step a tax on **rear** placement. Entry advances from the spawn side, so it
consumes the **forward** socket's window first. It was a tax on **forward** placement.

This correction produced a **new** open risk: deep placement is weakly dominant whenever entry exceeds 0.
See `../design/03-march-clock.md` and `../prototype/RISKS-AND-ADDBACKS.md`.

---

## Known Discrepancies

Live in Revision 7.1. **Resolve deliberately; do not silently pick a side.**

### 1. Arm letters were reassigned — ⚠ material

Revision 7 had two arms: **A = as-specified, B = flat control.** Revision 7.1 has three: **A = flat,
B = soft, C = hard (as-specified).** A and C are effectively swapped.

**Two places in 7.1 still carry the old letters**, both in § 21:

| Location | Says | Should read |
|---|---|---|
| Add-Back 1 trigger | "Arm A shows players changing hit/stand" | *the primary arm* — A is now the flat control |
| Add-Back 4 trigger | "Arm A and Arm B both show the archetype unviable" | **all three arms** — § 20 states this explicitly and governs |

Any pre-7.1 reference to "Arm A (primary)" means what is now **Arm C**. Check letters against curves.

### 2. Thinning dilemma still asserts the withdrawn fifth-card claim — ⚠ material

§ 14 reads *"the fifth card is worth taking only if it lands exactly"* — but § 4 of the same document
**demotes that from design identity to unproven hypothesis**, and notes it would probably be unhealthy if
true.

**§ 4 governs.** Flagged in `../design/08-deck-economy-progression.md`.

### 3. Example wave pile count — minor, uncorrected

`../design/example-wave.md` says "six safe cards in a pile of twenty-two." With 3 player cards, 1 upcard,
and 1 hidden card removed from a 26-card shoe, **21** remain. The six safe cards (two each of A, 2, 3) are
correct. Cosmetic; affects the prose, not the system.
