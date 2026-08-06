# Glossary

Terms of art used across the design documents. Where a term has a precise numeric definition, the
authoritative value lives in `reference/tuning-constants.md`.

---

**Ace Bastion** — A free 5.0-power King-class anchor granted on natural blackjack. Does not count as a hand
card; shares the hand's multiplier.

**Adjustment window** — The phase after the Dealer fully resolves. **One move total**: relocate one tower
one socket, or swap two adjacent towers. Standing orders may be set or changed freely and do not consume
the move. Families are locked and no draws are permitted. A *response* window, not a *solving* window.
Skipped entirely on bust.

**Arm A / Arm B / Arm C** — Three march-curve presets shipped in one build, serving as the prototype's test
arms. **A** is the flat 1.0 control, **B** soft escalation, **C** hard escalation (the curve the design
specifies). ⚠ The letters were reassigned in Revision 7.1 — pre-7.1 text uses A for the as-specified curve.

**Base wave** — The encounter's own enemy composition, before the Dealer's hand is added to it.

**Bastion (stake)** — A lane whose leaks damage Bastion health directly. The lethal lane.

**Bastion Health** — The run's life total. Reaches zero, the run ends. Hard to restore.

**Chips** — The common currency. Buys cards, removals, upgrades, and Bastion repair.

**Empty-lane damage** — Damage a lane would take with no towers at all. The denominator for the Open/Held
threshold.

**Engagement** — Path distance over which a tower can fire. 18.0 summed across three occupied sockets with
entry at 0. ⚠ **Explanatory, not a balance number** — the summed figure treats non-interchangeable sockets
as fungible and must never be multiplied into an output estimate. Reported per socket.

**Entry point** — Where enemies enter the path. Normally 0; advanced by march steps, pulled back 3.0 by
reaching exactly 21.

**Family** — A card's defense suit (Club, Spade, and in the full game Heart, Diamond). **Chosen at
placement and permanent for the wave.** The design's primary commitment.

**Favor** — Rare currency for rerolls, rule manipulation, and commander abilities. Earned by risk taken and
lanes held, never by hand quality.

**Forced replacement** — At socket capacity, a non-busting drawn card must replace an existing tower before
the player may stand. One of three pillars of decision density; not a safety valve.

**Final Forecast** — The per-lane outcome predicted after Dealer resolution, against the complete army.
**The combat contract**: if it says a lane leaks two, the wave leaks two. Distinct from *Visible Threat*,
and a distinct type in code.

**Forecast** — Ambiguous on its own since Revision 7.1; say *Visible Threat* or *Final Forecast*.

**Formation Strength** — The multiplier set by the hand's final total, applied to every tower placed this
hand. ×0.80 (bust) to ×1.60 (21).

**Held** — A lane whose predicted leakage is below half of empty-lane damage. See *Open*.

**House Rules** — A pre-run menu of rule toggles (Dealer hits soft 17, no persistence, native-only, minimum
four cards, doubled march, reassignable families).

**Junction socket** — A single shared socket firing into either lane at reduced contribution. Face cards
occupy it without the penalty.

**Lane stakes** — What a leak in a given lane costs: Bastion health, Vault reward, or (full game) Works
tower destruction. Assigned per encounter, shown before the opening deal.

**Leak** — Enemies reaching the end of a lane. Costs are set by that lane's stake.

**March Clock** — The system pricing hand length. Each card past the second advances the enemy entry point
by an escalating step, paid before the card is revealed.

**March step** — The individual advance: +1.5, +2.5, +3.5 for the 3rd, 4th, and 5th cards.

**Native / off-suit** — A card deployed into its printed suit gets full family behavior; off-suit gets the
generic form — full power, no keyword, no synergy. Full game only.

**Open** — A lane whose predicted leakage is at least half of empty-lane damage. See *Held*.

**Overload** — On bust, the destroyed card deals immediate damage equal to its base power to **the lane
with the highest current Visible Threat** (ties break toward the Bastion lane). **Does not scale with
excess.** Unsteerable: the card is never placed, so the player cannot aim it.

**Persistence** — Towers survive across the waves of an encounter, resetting at the encounter boundary.
**Persisted towers revert to ×1.00 Formation Strength.** Exists to create socket scarcity, not to bank
power.

**Pullback** — The 3.0-unit retreat of the entry point granted for reaching exactly 21, at any card count.
Clamped at 0. The entire bonus for a perfect formation.

**Resolver** — The single deterministic simulation driving both forecast and wave. Same schedule, stats,
targeting, rounding, and tie-breaking.

**Run island** — The junction socket, which is adjacent to neither lane and so can never be part of a run.
It buys breadth and forfeits synergy.

**Run link** — Consecutive card values in adjacent sockets. +15% / +25% for runs of 2 / 3; **runs cap at
3 in the prototype** because a 4-run is geometrically impossible. Direction-agnostic; Queen is wild (one
value, chosen at lock); Ace matches its current state. A tower belongs to at most one run — the longest
chain containing it. **The only link rule.**

**Shoe** — The 26-card draw pile (two of each rank, A–K). Persists across an encounter's waves; reshuffles
when fewer than eight remain.

**Socket** — A fixed tower position, at path positions 3, 6, and 9 per lane, plus the shared junction.
Seven total.

**Standing orders** — Pre-committed conditionals set in the adjustment window: Hold, Focus, Trigger on
group. Modeled exactly by the resolver.

**Thinning dilemma** — The deckbuilding tension between face-heavy (position and range) and many-card
(width and links) shoes. Neither may become default-correct.

**Vanguard** — The Dealer's upcard, deployed as a unit on the field from before the opening deal. Not a
number to be translated by the UI.

**Vault (stake)** — A lane whose leaks cost Chips and Favor from the encounter's reward.

**Visible Threat** — The per-lane outcome shown during the draw, modelled against **the revealed force
only** (base wave plus Vanguard). Exact about that smaller question and **explicitly not a prediction of
the wave.** Distinct from *Final Forecast*, and a distinct type in code.

**Wide Formation** — A **deleted** Revision 6 mechanic granting +10% attack speed per card past the third.
It refunded the march almost exactly. Named here so it is recognized if it tries to return in disguise.

**Works (stake)** — A lane whose leaks destroy a placed tower, preventing its persistence. Full game only.
