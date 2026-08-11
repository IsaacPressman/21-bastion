# Information and Fairness

Source: Handoff Revision 7.1, § 17.

This document is the operational form of the **Reveal Consequences, Not Conclusions** pillar. Treat the
"Not Shown" list as a hard constraint on every UI task.

---

## Shown

- Lane stakes, base wave, and **empty-lane damage** before the opening deal
- The Dealer's **Vanguard, on the field, from the start**
- **Visible Threat** per lane during the draw — **labelled as revealed-force only** — updating live on
  every draw and placement
- **Final Forecast** per lane after Dealer resolution — **labelled as the combat contract**
- Current total, hard/soft state, Formation Strength, summed power, active runs
- **Remaining rank composition, with busting ranks visibly marked**
- **Ace transformations and their power consequence, before commitment**
- Current entry position, and **which socket windows the next march step would cut into — shown on the
  lane, not as a single engagement number**
- Full army after the Dealer resolves, before lock
- A **post-wave explanation** of what leaked and why

---

## Not shown

- Combined utility, hit edge, stand edge, or **recommended action**
- Green/red indicators or **optimal placement highlights**
- **An exact bust percentage**

---

## Why the bust percentage is excluded

> Marking the busting ranks makes risk a **reading skill** rather than a lookup. The player sees six safe
> cards left in a pile of twenty-two and feels it.
>
> A percentage is one arithmetic step from the oracle the pillars prohibit, and it makes the rank display
> decorative.

This is the clearest statement of where the line sits. The game shows **inputs at full fidelity** and
performs **no synthesis across the two layers**.

The two panels — hand consequences and battlefield consequences — are deliberately separate surfaces. **Do
not build a combined summary widget**, even as a convenience, even behind a toggle.

The one permitted piece of interpretation is the Open/Held label in `05-battlefield.md`, and it is a
glance-read on a plain threshold with the raw number kept primary.

---

## Two forecasts must never share a surface

**Visible Threat and Final Forecast are different claims and must read as different claims.** Visible
Threat is exact about the revealed force; Final Forecast is exact about the wave that will run. Only the
second is a promise about combat.

- Label them distinctly. Never render one in a slot expecting the other — this is enforced by type, not by
  convention (`05-battlefield.md` § Implementation).
- Do not smooth the transition between them. The number *should* change when reinforcements land; what
  must not happen is a number that silently changes **meaning** while keeping its name.

> Players who read Visible Threat as a promise will feel the game break it when the Dealer resolves.

---

## March cost is shown on the lane, not as a scalar

The player sees **which socket windows the next march step would cut into**, drawn on the lane. **Not a
single engagement number.**

A summed engagement figure treats non-interchangeable sockets as fungible — the same reason it was
withdrawn as a balance tool (`03-march-clock.md`). Showing it as a headline number would teach the player a
model the design has explicitly rejected.

---

## The same rules at campaign scale

The run layer restates *Reveal Consequences, Not Conclusions* for the siege map, and it is the same
constraint with a different surface:

> **The siege map shows possible transformations and known costs, but never tells the player which
> strategic order is optimal.**

| Shown on the siege map | Never shown |
|---|---|
| **Exact, fixed action costs** in hours, before committing | A recommended order, or an ordering of orders |
| The **guaranteed minimum result** of an action — a Raid always removes at least one known candidate | A projected run-outcome score or win probability |
| The **bounded authored table** of what neglecting a front can do to it (`11-siege-geography.md`) | Which outcome from that table will occur |
| The Dealer's **recruitment row, replacement targets, and marked intent** (`06-dealer-and-enemies.md`) | A hidden difficulty state, or any signal derived from win rate or health |
| A concession's **certain cost and certain benefit**, both | Whether conceding is correct here |

Note the shape: **costs are exact; secondary payoff may be uncertain.** The player must always know what
they are definitely spending and the minimum they are buying. That is the campaign-scale version of showing
inputs at full fidelity and performing no synthesis.

**Two campaign-layer prohibitions worth naming as hard as the encounter ones:**

- **No deck score, and no siege-strength score.** A score is a verdict.
- **Dealer adaptation reads build composition only** — never win rate, health, loss streaks, or a hidden
  skill estimate. Adaptation the player cannot see the cause of is indistinguishable from rubber-banding,
  which is why recruitment is public and raidable rather than merely fair.

---

## Recovery

> **At least one redraw, reserve, or discard tool is always available. A poor card always has a use.**

This is the Randomness Creates Adaptation pillar's floor: bad draws must produce difficult decisions, never
automatic losses.

---

## Debug-only information

The following exist for instrumentation and must never reach a player-facing build:

- Exact bust probability
- Stand and hit expected output
- Combined utility

See `../prototype/VALIDATION.md` § Instrumentation. Gate these behind a debug flag from the first build so
the boundary is structural rather than remembered.
