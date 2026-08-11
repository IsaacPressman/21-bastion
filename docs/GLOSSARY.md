# Glossary

Terms of art used across the design documents. Where a term has a precise numeric definition, the
authoritative value lives in `reference/tuning-constants.md`.

Encounter-layer and run-layer terms are interleaved alphabetically. Run-layer terms are marked **(run
layer)** — none of them is prototype scope except *rank stacking*, which is flag-gated.

---

**Ace Bastion** — A free 5.0-power King-class anchor granted on natural blackjack. Does not count as a hand
card; shares the hand's multiplier.

**Acquire** *(run layer)* — Reward verb: add a rank/card to the shoe. One of the three verbs that move
blackjack rank distribution, and like the other two it is a **player** choice.

**Adjustment window** — The phase after the Dealer fully resolves. **One move total**: relocate one tower
one socket, or swap two adjacent towers. Standing orders may be set or changed freely and do not consume
the move. Families are locked and no draws are permitted. A *response* window, not a *solving* window.
Skipped entirely on bust.

**Arm A / Arm B / Arm C** — Three march-curve presets shipped in one build, serving as the prototype's test
arms. **A** is the flat 1.0 control, **B** soft escalation, **C** hard escalation (the curve the design
specifies). ⚠ The letters were reassigned in Revision 7.1 — pre-7.1 text uses A for the as-specified curve.

**Ambush Spade** — Spade **form**: burst and precision. Higher one-time damage, limited trigger count or
long rearm, strong against one dangerous target crossing its trigger point. Paired with *Snare Spade*.

**Barrage Club** — Club **form**: anti-group. Faster firing, splash damage, weak against heavy armor, and
the beneficiary of *bunching*. Paired with *Siege Club*.

**Base wave** — The encounter's own enemy composition, before the Dealer's hand is added to it. **Fully
known before the opening hand** — types, spawn order, timing, lane assignment, and breakpoint abilities.
It is deliberately **not** a source of uncertainty.

**Breakpoint** — A position on the lane at which an enemy does something if it is still alive: the
Standard Bearer buffs, the Saboteur disables a tower, the Siege Engine fires at the Bastion, the
Lane-Switching Raider changes lane. Breakpoints give forward and middle positions **distinct tactical
jobs**, and **may** eventually reduce or eliminate the need for socket-specific range — but the validated
range-by-socket values stay authoritative until breakpoints are built and re-measured in isolation
(`reference/tuning-constants.md` § Known Discrepancies, 12).

**Bunching** — Deterministic column compression. Enemies have a minimum legal spacing and do not pass one
another, so a follower behind a slowed leader is speed-capped, and the column packs upstream. The
mechanism behind *Snare → bunch → Barrage*.

**Bastion (stake)** — A lane whose leaks damage Bastion health directly. The lethal lane.

**Bastion (the front)** *(run layer)* — The inner defense and site of the final stand. Becomes exposed as
outer districts fail; its final geometry reflects the run.

**Bastion Health** — The run's life total. Reaching zero is **the only ordinary defeat condition.** Hard to
restore, so that battlefield leakage carries campaign weight. Never spent on upgrades or orders.

**Candidate preview** — What a *possible* placement shows before it is committed: **causal deltas**
(`Raider leak: 1 → 0`, `Run: inactive → 3-card run`), never one sortable score. A single comparable number
would let the player brute-force every socket, which is the *solvable-puzzle risk*.

**Counterfactual memory** — After a card is committed, the previous state is preserved long enough to show
what that card changed. Players learn causality from deltas, not from absolute levels. Step 4 of the
*tactical loop*.

**Card history** *(run layer)* — A named tag a card earns from resolver events. Grants **no power and no
experience level** — it creates *eligibility* for a future Promote.

**Charter** *(run layer)* — One of two major per-run rewards, normally after the first two siege phases. A
Charter **changes a rule of the run**, never adds a passive percentage. The Last Line geometry principle is
explicitly **not** a Charter — a fix for a dominant strategy belongs in baseline geometry.

**Chips** — ⚠ **Cut.** The former general-purpose currency. There is no money resource in the baseline
run; **Time** pays for ordinary campaign actions.

**Compromised** *(run layer)* — Front state: still defensible, but geography, stakes, or services have
worsened. Produces a harder or *different* encounter without removing the front.

**Concession** *(run layer)* — Deliberately abandoning or scuttling a position for a **known** structural
benefit. Both cost and benefit are certain. Must sometimes be strategically correct, not merely less bad.

**Conceded** *(run layer)* — Front state: the player abandoned the district and received the declared
scuttle benefit. Terminal for ordinary routing, but **not** equivalent to *Lost*, and not defeat.

**Cut** *(run layer)* — Reward verb: remove a chosen card permanently. Chosen probability surgery —
**never inflicted casually by enemies**, because enemy pressure may not edit rank distribution.

**Density** *(run layer)* — The placement archetype rank stacking creates: duplicate ranks, concentrated
strongpoints, socket economy. The opposite pole from *Spread*.

**Doctrine** *(run layer)* — The persistent placement-layer progression: four to seven
**behavior-changing globals** per run, built over one or two encounters. Replaces the relic layer. Must not
become twenty passive percentage relics.

**Empty-lane damage** — Damage a lane would take with no towers at all. The denominator for the Open/Held
threshold.

**Engagement** — Path distance over which a tower can fire. **17.0** summed across three occupied sockets
with entry at 0, under range-by-socket geometry. ⚠ **Explanatory, not a balance number** — the summed
figure treats non-interchangeable sockets as fungible and must never be multiplied into an output estimate.
Reported per socket.

**Entry point** — Where enemies enter the path. Normally 0; advanced by march steps, pulled back 3.0 by
reaching exactly 21.

**Exhausted** *(run layer)* — A one-encounter state for a veteran card. The card is replaced for the next
encounter by a **Reserve copy**; it does not stack toward injury or death.

**Family** — A card's defense suit (Club, Spade, and in the full game Heart, Diamond). **Chosen at
placement and permanent for the wave.** The design's primary commitment. Since the Improved Encounters
Handoff, placement locks **rank, family, form, and socket** together.

**Form** *(also **mode**)* — Which of two behaviors a family deploys as: **Barrage** or **Siege** for
Clubs, **Snare** or **Ambush** for Spades. Chosen at placement, **locked for the wave**, and presented as
four direct options rather than a Family → Mode submenu. Forms are a partial replacement for the breadth
lost when Hearts and Diamonds were cut, not an addition on top of it.

**Favor** — Rare command authority, spent to bend one encounter rule in a bounded way. **First-pass cap:
3.** Earned by **voluntarily accepting pre-resolution risk and protecting important stakes** — never by
hand quality, high totals, or "beating" a Final Forecast. Never a reward-floor currency.

**Field Reassignment** *(run layer)* — The doctrine that lets the first card placed in each lane be
reassigned after the Dealer reveal. The sanctioned, bounded escape from family locking.

**Forced replacement** — At socket capacity, a non-busting drawn card must replace an existing tower before
the player may stand. One of three pillars of decision density; not a safety valve.

**Final Forecast** — The per-lane outcome predicted after Dealer resolution, against the complete army.
**The combat contract**: if it says a lane leaks two, the wave leaks two. Distinct from *Visible Threat*,
and a distinct type in code. Being exact, it cannot be "outperformed."

**Forecast** — Ambiguous on its own since Revision 7.1; say *Visible Threat* or *Final Forecast*.

**Formation Strength** — The multiplier set by the hand's final total, applied to every tower placed this
hand. ×0.80 (bust) to ×1.60 (21).

**Front** *(run layer)* — A named, authored district defended during the siege. First implementation
target: **three outer fronts plus the Bastion** — North Gate, River Works, East Ward.

**Front state** *(run layer)* — One of **Held, Compromised, Lost, Conceded.** A front is always in exactly
one.

**Held** — A lane whose predicted leakage is below half of empty-lane damage. See *Open*.

**Held (front state)** *(run layer)* — A district under player control, with normal services and geometry
available. Unrelated to the lane label above.

**House Rules** — A pre-run menu of rule toggles (Dealer hits soft 17, no persistence, native-only, minimum
four cards, doubled march, reassignable families).

**Junction socket** — A single shared socket firing into either lane at reduced contribution. Face cards
occupy it without the penalty. Its job is to be the **uncertainty hedge** — covering the located unknown,
intercepting lane-switchers, trading specialization for flexibility — and it is a *run island*.

**Lane stakes** — What a leak in a given lane costs: Bastion health, the encounter's campaign reward, or
(full game) Works tower destruction. Assigned per encounter, shown before the opening deal.

**Last Stand** *(run layer)* — The campaign state entered when every outer front is Lost or Conceded, **or**
when the final phase reaches its scheduled Bastion assault. **Not defeat** — the removal of further
strategic retreat. Recruitment locks; the final battlefield is assembled from what the run produced.

**Leak** — Enemies reaching the end of a lane. Costs are set by that lane's stake.

**Lost** *(run layer)* — Front state: the Dealer took the district. Applies the enemy-favored authored
consequence. Terminal for ordinary routing, and **not defeat.**

**March Clock** — The system pricing hand length. Each card past the second advances the enemy entry point
by an escalating step, paid before the card is revealed. **Never modified by campaign time.**

**March step** — The individual advance: +1.5, +2.5, +3.5 for the 3rd, 4th, and 5th cards (Arm C).

**Muster** *(run layer)* — The strategic order that exposes Acquire, Cut, Repaint, or Rerank through a
diegetic recruitment or supply opportunity. 1–2h.

**Native / off-suit** — A card deployed into its printed suit gets full family behavior; off-suit gets the
generic form — full power, no keyword, no synergy. Full game only.

**One-for-one replacement** *(run layer)* — The Dealer recruitment contract: every normal recruit
**replaces** one existing Dealer card. The opposing shoe never grows.

**Open** — A lane whose predicted leakage is at least half of empty-lane damage. See *Held*.

**Opposing shoe** *(run layer)* — The Dealer's **fixed-size 26-card campaign shoe.** A different object
from the player's shoe that happens to share a size. ⚠ In the prototype the Dealer draws from the
*player's* shoe; the two have not been reconciled.

**Overload** — On bust, the destroyed card deals immediate damage equal to its base power to **the lane
with the highest current Visible Threat** (ties break toward the Bastion lane). **Does not scale with
excess.** Unsteerable: the card is never placed, so the player cannot aim it.

**Persistence** — Towers survive across the waves of an encounter, resetting at the encounter boundary.
**Persisted towers revert to ×1.00 Formation Strength.** Exists to create socket scarcity, not to bank
power. **Geography persists across encounters; towers do not.**

**Promote** *(run layer)* — Reward verb: grant a named battlefield behavior unlocked by the card's history.
Bounded by the one-modifier-per-card cap.

**Public recruitment** *(run layer)* — A visible row of **three** candidate cards, each paired with the
Dealer card it would replace, with the Dealer's intended pair **marked before the player acts.**

**Pullback** — The 3.0-unit retreat of the entry point granted for reaching exactly 21, at any card count.
Clamped at 0. The entire bonus for a perfect formation.

**Raid** *(run layer)* — The 3h strategic order that destroys, steals, or blocks one visible recruitment
candidate. **Mandatory to the design**, not optional content: without it, Dealer adaptation reads as
rubber-banding.

**Rank stacking** — Two same-rank towers occupying one socket as a stack. Rank, not value (J+J stacks, J+Q
does not). Depth 2, no Aces, **no power bonus**, **no run eligibility**, each layer keeps its own
multiplier. **Flag-gated, default off** in the prototype.

**Rerank ±1** *(run layer)* — Reward verb: change a card's rank by one, altering tower power, run
structure, and blackjack distribution at once.

**Repaint** *(run layer)* — Reward verb: change a card's native family without changing its rank.

**Reserve copy** *(run layer)* — A same-rank stand-in for an Exhausted or captured card: same rank and base
power, **no modifier, family bonus, veterancy, or promotion behavior.** The mechanism by which **rank count
stays sacred.**

**Resolver** — The single deterministic simulation driving both forecast and wave. Same schedule, stats,
targeting, rounding, and tie-breaking.

**Run island** — The junction socket, which is adjacent to neither lane and so can never be part of a run.
It buys breadth and forfeits synergy. A **stacked** socket is a run island for the same practical reason.

**Run link** — Consecutive card values in adjacent sockets. +15% / +25% for runs of 2 / 3; **runs cap at
3 in the prototype** because a 4-run is geometrically impossible. Direction-agnostic; Queen is wild (one
value, chosen at lock); Ace matches its current state. A tower belongs to at most one run — the longest
chain containing it. **The only link rule.**

**Shoe** — The player's 26-card draw pile (two of each rank, A–K). Persists across an encounter's waves;
reshuffles when fewer than eight remain. See also *Opposing shoe*.

**Siege phase** *(run layer)* — One of three phases of the single continuous siege — **I Encirclement,
II Breach, III Last Stand.** Replaces Revision 7.1's three regions. Each phase has its own clock.

**Socket** — A fixed tower position, at path positions 3, 6, and 9 per lane, plus the shared junction.
Seven total. Range varies by socket: 4.0, 3.0, 2.0, forward to rear.

**Spread** *(run layer)* — The baseline placement archetype: distinct consecutive ranks, wide adjacent
boards, run links and coverage. The opposite pole from *Density*.

**Siege Club** — Club **form**: anti-armor and priority target. Slower firing, strong single-target damage,
armor penetration or high armor-effective damage. Paired with *Barrage Club*.

**Snare Spade** — Spade **form**: flow control. Lower direct damage, slows, and **creates bunching** that
sets up a Barrage Club. Paired with *Ambush Spade*.

**Opportunity unit** — An optional physical target embedded in the wave — Supply Courier, Standard Wagon —
that rewards battlefield risk rather than hand quality. **Optional means optional**: failing one must not
make the encounter feel lost. Partly an answer to the fifth-card binary, since a surviving board can still
justify another draw. **In the prototype its payout is encounter-local** (a cancelled reinforcement group,
a buff that never activates) — **never Favor, and never a substitute currency.** The Paymaster, which pays
Favor, is deferred to the run layer.

**Standing orders** — Pre-committed conditionals: Hold, Focus, Trigger on group. Modeled exactly by the
resolver. **Editable freely during planning and the adjustment window, locking only when combat begins**,
and they never consume the one positional move. Their effect must be visible on the *timeline*.

**Strategic order** *(run layer)* — The **single** command issued after most encounters: Hold/Redeploy,
Fortify, Muster, Train, Raid, Reconnoiter, or Concede. The order *is* the progression decision. Target
cadence 30–60 s.

**Temper** *(run layer)* — Reward verb: add or change a card's **one** allowed modifier.

**Thinning dilemma** — The deckbuilding tension between face-heavy (position and range) and many-card
(width and links) shoes. Neither may become default-correct.

**Time** *(run layer)* — Campaign hours. Pays for ordinary campaign actions, **resets per siege phase**
(first pass ~8h), and **never modifies hand-scale March entry.** Reaching zero triggers the scheduled enemy
action — it never causes defeat.

**Tactical loop** — The player's view of an encounter, in five steps: **Read, Diagnose, Commit, Observe
delta, Decide.** *Hit is step five, not the entire decision system.* Every encounter system exists to
strengthen one of the five.

**Timeline** *(encounter timeline)* — The deterministic time-and-path strip per lane: spawn timing, tower
engagement windows, March advancement, slow and bunching, Hold orders, breakpoints, reinforcements, and
**the attacks a Hit would cost**. The primary visual language for tactical consequence, and the mechanism
that keeps cognitive load down without adding a system.

**Vanguard** — The Dealer's upcard, deployed as a unit on the field from before the opening deal. Not a
number to be translated by the UI. The **hidden** card's rank stays unknown, but **its destination lane is
visible from the start.**

**Vault (stake)** — A lane whose leaks reduce the encounter's **campaign reward.** ⚠ Revision 7.1 said
"Chips and Favor"; Chips are cut and Favor is never a reward-floor currency.

**Visible Threat** — The per-lane outcome shown during the draw, modelled against **the revealed force
only** (base wave plus Vanguard). Exact about that smaller question and **explicitly not a prediction of
the wave.** Distinct from *Final Forecast*, and a distinct type in code.

**Wide Formation** — A **deleted** Revision 6 mechanic granting +10% attack speed per card past the third.
It refunded the march almost exactly. Named here so it is recognized if it tries to return in disguise.

**Works (stake)** — A lane whose leaks destroy a placed tower, preventing its persistence. Full game only.
