# Core Gameplay Loop

Source: Handoff Revision 7.1, § 3. **The tactical loop** is from the Improved Encounters Handoff, § 1.

This is the phase order the implementation must follow. It is the spine of the wave state machine.

---

## The tactical loop

The phase list below is the *machine's* view of a wave. The **player's** view is a five-step cycle, and
the Improved Encounters Handoff makes it the primary description of what an encounter is:

1. **Read** — what will happen if the current plan resolves as-is?
2. **Diagnose** — where and when does the formation fail?
3. **Commit** — what role and position does this card take?
4. **Observe delta** — what did that commitment fix, and what remains?
5. **Decide** — is the remaining battlefield problem worth another draw and another March step?

> **Hit is step five, not the entire decision system.**

That reframing is the point. The encounter is no longer described primarily as "draw and place": four of
the five steps happen before the blackjack decision, and every encounter system exists to strengthen one
of them. Steps 1 and 2 are the timeline and the exact committed-state forecast; step 3 is family, mode,
and socket; step 4 is counterfactual memory; step 5 is the March Clock. See
`14-encounter-timeline.md`.

The failure signal is the inverse of step 5: **if the player cannot say why they want another card, the
information layer has failed — and the response is not to add mechanics.**

---

## Before a wave

1. Reveal lane stakes, the base wave, and the Dealer's Vanguard — the upcard, **already deployed as a
   unit on the field**.
2. Deal the opening two cards. The march has not begun.
3. **Place each card: choose its family and socket. Family is now locked.**
4. Choose to hit or stand.
5. **Each hit advances the army by an escalating march step, paid before the card is revealed.**
6. Place the new card, replacing an existing tower if sockets are full.
7. On stand, the Dealer resolves: hidden card revealed, draws to 17, every card deployed as a unit.
8. **Adjustment window:** **one move total** — one tower relocates one socket, *or* two adjacent towers
   swap. Standing orders may be set or changed freely. Families are fixed. No further draws.
9. Lock and resolve.

**On bust:** the busting card is destroyed, Overload fires, placement locks, **the Dealer still resolves
in full**, and combat begins. Note that bust skips the adjustment window — placement locks immediately.

### Phase ordering that matters

- The Vanguard is on the field *before* the opening deal, not revealed at stand.
- The march step is paid *before* the card is revealed, not after. The player commits to the cost without
  knowing what they bought.
- The adjustment window opens *after* the Dealer has fully resolved and the whole army is visible.
- Family is locked at step 3/6 and never reopens.

---

## Combat

A **deterministic resolution of a fully previewed state**, not an input phase.

- No critical hits, no misses, no random targeting.
- Watchable, fast-forwardable, skippable.
- Standing orders execute automatically.
- A regular wave resolves in roughly **12–20 seconds** at normal speed.

> The forecast is exact because nothing happens live that could invalidate it.

Note that "the forecast" here means the **Final Forecast**, produced after Dealer resolution. During the
draw the game shows **Visible Threat**, which is exact against the revealed force only and is *not* a
prediction of the wave. See `05-battlefield.md` § Two Forecasts, Not One — the distinction is enforced in
the type system.

---

## After a wave

1. Review which lanes leaked, by how much, and why.
2. Between waves of an encounter, towers and shoe state persist.
   **Persisted towers revert to ×1.00 Formation Strength.**
   **Wave 2 is authored to disturb the Wave 1 solution** rather than to repeat it —
   `05-battlefield.md` § Wave 2 must disturb the Wave 1 solution.
3. After an encounter, **issue one strategic order.**

Step 3 is the run layer's cadence rule and it replaces 7.1's "take a reward and choose a route": there is
**no reward screen, then a map, then a shop** — the order *is* the progression decision, and it often
decides where the next encounter happens. Target **30–60 seconds**. See
`12-campaign-time-and-orders.md`. Towers do not survive the encounter boundary; **geography and card
identity do** (`11-siege-geography.md`).

---

## Implementation note

The wave is a small, explicit state machine with a hard boundary between the draw phase (decisions are
made) and the adjustment window (decisions are refined). Blurring that boundary is the failure mode the
design is guarding against — see the Commitments Are Made Under Uncertainty pillar.
