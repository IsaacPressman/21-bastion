# 21 Bastion — Encounter Design Handoff

## Improved Encounters Addendum

*This handoff consolidates the encounter-design decisions made after Revision 7.1 and the run-layer addendum. It is focused only on making individual encounters richer, more legible, and more tactical without adding unnecessary blackjack rules. It is intended to be folded into the main gameplay handoff later.*

**Status labels:** DECIDED, DECIDED WITH CHANGE, OPEN, CUT.

---

## 0. Encounter Diagnosis

### DECIDED

The current encounter risks collapsing into:

> draw card → choose a vaguely sensible family → place it → decide whether to hit → repeat

The problem is **not insufficient decision count**. The problem is that the player often cannot form a concrete intention before drawing another card.

If the battlefield only communicates that a lane is vaguely weak, then Hit means:

> “Maybe another tower would help.”

The target is:

> “Lane one still leaks an armored soldier before socket 6. I need a specific kind of answer, and I know what another March step will cost me. I do not know whether the next card will solve it.”

### Encounter thesis

> **The player should never wonder why they might want another card. They should know exactly what battlefield problem remains, but not whether the next draw will solve it.**

This is the information boundary for the encounter layer.

---

## 1. The Tactical Loop

### DECIDED

The encounter loop is no longer described primarily as “draw and place.” It is:

1. **Read** — What will happen if the current plan resolves as-is?
2. **Diagnose** — Where and when does the formation fail?
3. **Commit** — What role and position does this card take?
4. **Observe delta** — What did that commitment fix, and what remains?
5. **Decide** — Is the remaining battlefield problem worth another draw and another March step?

Then repeat.

**Hit is step five, not the entire decision system.**

The goal of every encounter system below is to strengthen one of those five steps.

---

## 2. Information Contract

### 2.1 The base wave is fully known — DECIDED

Before the opening hand, the player sees the complete authored base wave:

- Enemy types
- Spawn order
- Spawn timing
- Lane assignment
- Lane stakes
- Spatial breakpoint abilities
- Empty-lane damage
- Dealer Vanguard rank, unit, and lane

The base wave is not a source of uncertainty.

The player must be able to form a battlefield plan before drawing.

### 2.2 Dealer uncertainty is narrow and located — DECIDED

The Dealer's hidden card remains unknown, but its **destination lane is visible from the start**.

The player therefore knows:

> “Something unknown is coming to lane two.”

They do not know its rank or resulting enemy type.

Any additional Dealer draws remain unknown until Dealer resolution.

This creates uncertainty without preventing intention.

### 2.3 Watchtower doctrine changes — DECIDED WITH CHANGE

Because hidden-card lane is now baseline information, Watchtower or similar doctrine no longer reveals destination.

A suitable upgraded information effect is:

> Reveal the hidden card's **rank class**: Low, Mid, High, or Court.

Exact implementation remains future-content work.

### 2.4 Next-draw preview — CUT FROM BASELINE

Do not show:

- Top three ranks in unknown order
- Rank band of the next player draw
- Any baseline next-card preview beyond remaining-rank composition

Revision 7.1 already gives the player the correct blackjack information: remaining rank counts and visibly marked busting ranks.

Next-draw preview is reserved for rare doctrine, commander, or rule-breaking effects.

---

## 3. Resolver Information Shown to the Player

### 3.1 Current committed state gets exact consequences — DECIDED

The deterministic resolver already knows what the current formation will do. Use that information.

Per lane, the player may see:

- Predicted leak count and leak damage
- Which enemy leaks
- First expected leak time
- Which breakpoint ability fires
- Effective damage still required before a relevant breakpoint
- Current effective damage delivered before that breakpoint
- Number of attacks or triggers each tower receives
- Which socket windows the next March step will remove
- Active runs
- Current standing-order effects

Example:

> **Lane 1 — Bastion**  
> 1 Armored Soldier leaks for 2 Bastion damage.  
> First leak: 11.4s.  
> Armor-effective damage required before socket 6: 9.0.  
> Current formation delivers: 7.0.

This is consequence, not recommendation.

### 3.2 Total Engagement — debug/explanatory only — DECIDED

Do not use total engagement as the primary player-facing readout.

The timeline and per-socket engagement windows replace it.

The scalar may remain in debug tools and documentation.

---

## 4. The Encounter Timeline

### Verdict: DECIDED — required for the improved encounter prototype

The timeline is the primary visual language for tactical consequence.

It should communicate, in one place:

- Enemy spawn timing
- Enemy progression
- Tower engagement windows
- March advancement
- Slow and bunching
- Hold orders
- Positional enemy breakpoints
- Dealer reinforcements
- Which attacks are lost after a Hit

The intended read is not:

> “Entry moves from 1.5 to 4.0.”

It is:

> “If you draw again, this cannon loses two shots before the Siege Engine crosses socket 9.”

### 4.1 Timeline behavior

Each lane displays a deterministic time/path strip.

Enemy groups appear as moving or scheduled markers.

Tower firing/trigger windows appear as overlays.

March advancement visibly shifts the enemy schedule deeper into the tower windows.

### 4.2 Standing orders live on the timeline

Standing orders must visually alter the same display.

Examples:

**Hold** shortens or shifts the tower's firing window.

**Focus** highlights which enemy segment is receiving priority.

**Trigger on Group** marks which clump currently satisfies the trap trigger condition.

Standing orders should feel like editing the timeline, not opening a separate abstract menu.

---

## 5. Information Boundary for Candidate Placements

### DECIDED

The current committed formation receives exact forecast numbers.

A candidate placement should show **causal deltas**, but should not reduce every candidate to one sortable scalar score.

Good candidate-preview information:

- `Banner: survives → killed before socket 6`
- `Raider leak: 1 → 0`
- `Club 8 attacks: 3 → 2 after next March step`
- `Run: inactive → 3-card run`
- `Column: spread → compressed inside Barrage window`
- `Saboteur disable: fires → prevented`

Avoid turning every hover into:

> `Projected value: 5.1 → 3.4`

if that lets the player brute-force every socket until the smallest number appears.

### Rule

> **Before drawing, show the requirement. After drawing, show the consequences of candidate actions. Do not show the answer.**

### Counterfactual memory — DECIDED

After a card is committed, preserve the previous state long enough to show what that card changed.

Example:

> **Last placement: 4 Spade / Snare / Forward**  
> Lane 1 leak: 2 → 0  
> Banner: survives → killed  
> Next March step: Club 8 loses 1 attack

Players learn causality from deltas, not from absolute levels.

---

## 6. Translating Shortfall Into Card Language

### DECIDED WITH CHANGE

The encounter should make the remaining battlefield requirement concrete, but should not directly recommend a tower form and socket.

Do **not** display:

> “A mid-rank Siege Club here will solve this.”

That crosses too close to the oracle line.

Instead display:

> **Needs 2.1 more armor-effective damage before socket 6.**

Once a card is drawn, the candidate preview can reveal whether a particular deployment closes that requirement.

This preserves the intended mental step:

> battlefield requirement + drawn rank + available tower forms + geometry → player judgment

---

## 7. Prototype Tower Forms

### Verdict: DECIDED FOR PROTOTYPE

The prototype keeps only Clubs and Spades as families, but each family receives two forms.

These forms are not treated as extra complexity on top of four full-game families. They are a replacement for some of the tactical breadth removed when Hearts and Diamonds were cut from prototype scope.

### Club — Artillery

#### Barrage Club

Role: anti-group / splash.

- Faster firing
- Splash damage
- Weak against heavy armor
- Benefits strongly from compressed enemy groups

#### Siege Club

Role: anti-armor / priority target.

- Slower firing
- Strong single-target damage
- Armor penetration or high armor-effective damage
- Best against armored soldiers, Siege Engines, Standard Bearers, and other priority targets

### Spade — Control

#### Snare Spade

Role: flow control.

- Lower direct damage
- Slows enemies
- Creates bunching/compression
- Sets up Barrage Clubs

#### Ambush Spade

Role: burst / precision trap.

- Higher one-time damage
- Limited trigger count or long rearm
- Strong against a specific dangerous target crossing its trigger point

### UI rule

The player chooses among four direct deployment forms:

- Barrage Club
- Siege Club
- Snare Spade
- Ambush Spade

Do not force a two-step menu of Family → Mode in the prototype.

Internally, family and mode may remain separate data fields.

### Commitment rule — DECIDED

When the card is placed, these lock together:

- Rank
- Family
- Mode
- Socket

The post-Dealer adjustment window may alter position according to the existing one-move rule, but family and mode remain fixed.

### Full-game mode structure — OPEN

Do not assume four families × two modes = eight live choices per card.

The prototype tests whether tactical forms add value.

Full-game options include:

- Modes only on some families
- Alternate forms as upgrades
- Some prototype modes becoming full families
- One form per family by default, with doctrine granting alternatives

No commitment yet.

---

## 8. Tower-to-Tower Tactical Interaction

### Snare → Bunch → Barrage — DECIDED

The prototype needs at least one interaction where one tower changes the battlefield so another tower becomes more effective.

Runs are positional synergy, but they are still a percentage bonus. The encounter also needs behavioral synergy.

### Deterministic bunching rule

Enemies have a minimum legal spacing and do not pass one another unless an explicit enemy ability says otherwise.

If a leading enemy is slowed and a following enemy would violate minimum spacing, the follower's speed is capped to maintain that spacing.

The result is column compression upstream of the slowed unit.

Barrage splash becomes stronger against that compressed group.

The timeline must visibly show the compression.

### Prototype interaction targets

The prototype should support at least three readable tactical interactions:

1. **Control → Splash**  
   Snare compresses a group; Barrage exploits it.

2. **Standing Order → Priority Damage**  
   Siege Club holds fire for the correct armored or high-priority target instead of wasting cooldown.

3. **Early Kill → Enemy Formation Disruption**  
   Kill a breakpoint enemy before its trigger and prevent a downstream threat.

The player should be building sequences, not only accumulating DPS.

---

## 9. Enemy Spatial Breakpoints

### Verdict: DECIDED — baseline solution to deep-placement dominance

Do not give sockets arbitrary statistical bonuses to create identity.

Socket identity emerges from **what enemies do at different points on the lane**.

Forward placement is valuable because some threats must be solved early.

Rear placement is valuable because it retains more engagement after March advancement.

Middle placement gains value through the junction, run topology, and breakpoint timing.

### Prototype breakpoint enemies

#### Standard Bearer

If alive when crossing a specified breakpoint, buffs nearby/following enemies.

The player may need to kill it before socket 6.

#### Saboteur

At its breakpoint, disables the nearest eligible tower for a **temporary duration**.

Do not begin with permanent destruction. Disabling one of three towers is already a large effect.

#### Siege Engine

If alive when crossing socket 9, fires a Bastion shot.

If killed before socket 9, it does not fire.

#### Lane-Switching Raider

At the junction, changes lane according to a deterministic, previewed rule.

The junction is the cleanest answer to this threat.

### Generalized tower destruction — NOT BASELINE

Do not make ordinary enemies broadly attack and destroy towers in the prototype.

Specific, telegraphed positional threats are enough to create placement risk without violating fairness.

---

## 10. The Junction's Job

### DECIDED

The junction is the **uncertainty hedge**.

Its identity is broader than “reduced contribution to both lanes.”

It should:

- Attack or influence either lane
- Intercept lane-switching threats
- Provide coverage when the Dealer hidden rank is unknown
- Interact with middle-socket standing orders across lanes where appropriate
- Trade raw specialization for flexibility

The junction should be intentionally attractive when uncertainty is concentrated in one part of the wave.

It remains a worse choice when the player already knows exactly where maximum committed output is needed.

---

## 11. Wave 2 Authoring

### Verdict: DECIDED

The second wave of an encounter must not simply be “another hand.”

Wave 1 establishes a formation.

Wave 2 deliberately makes that formation imperfect.

Example:

**Wave 1**

- Lane 1: Swarm
- Lane 2: Armor

**Wave 2**

- Lane 1: Armor
- Lane 2: Fast

The player now faces:

- Persisted families that cannot be reassigned
- Existing run structure
- Occupied sockets
- Forced replacement
- Rank-stacking opportunities
- A changed tactical requirement

This is the primary reason encounter-scoped persistence exists.

### Authoring target

Wave 2 should disturb the Wave 1 solution without making it worthless.

The desired feeling is:

> “My old board still matters, but it is no longer the board I would build from scratch.”

---

## 12. Optional Opportunity Units

### Verdict: DECIDED

If all survival lanes are already Held, the player needs a reason to consider further risk.

Optional opportunities create ambition beyond survival and help create the desired safe-miss middle case for fourth- and fifth-card draws.

### Form

Do not present these primarily as checklist objectives.

Embed them physically into the wave as units or battlefield opportunities.

Examples:

#### Supply Courier

Kill before escape to interfere with the Dealer's next recruitment.

#### Paymaster

Kill before a breakpoint to gain +1 Favor.

#### Standard Wagon

Destroy before it crosses a breakpoint to prevent a future reinforcement benefit.

### Requirements

- Optional means optional.
- Failure does not make the encounter feel lost.
- They should create overcommitment temptation, not mandatory chores.
- Rough target: one meaningful opportunity per encounter, not every wave.

### Fifth-card relationship

Optional opportunities are a partial answer to the fifth-card binary problem.

A board may already survive, but a further card can still be defensible because it:

- Secures Favor
- Stops Dealer recruitment
- Completes a run
- Avoids a costly replacement later
- Enables a tactical interaction

Exact 21 remains spectacular, but a safe miss does not have to be worthless.

---

## 13. Favor and Encounter Risk

### DECIDED

Favor rewards **battlefield risk successfully taken**, not hand quality.

Do not grant Favor because the player:

- Reached 19+
- Reached 21
- Had only one safe rank remaining
- Drew a natural blackjack
- Otherwise produced a numerically impressive hand

Good Favor conditions must depend on a battlefield decision.

### Design test

> **Could the same hand earn or fail to earn this Favor depending on battlefield decisions?**

If yes, the trigger is probably valid.

If no, it is probably rewarding hand quality.

Good examples:

- Kill a risky Courier by taking an additional March step
- Preserve an important stake while pursuing an optional opportunity
- Successfully execute a high-risk tactical objective that could have been ignored

Favor remains capped and rare according to the run-layer handoff.

---

## 14. Placement Quality Target

### DECIDED — authoring metric

The goal is not more legal placements. The goal is **2–3 competing plausible placements** for important cards.

Bad state A:

> One obviously correct socket.

Bad state B:

> Seven nearly interchangeable sockets.

Target state:

> “Forward-left kills the Standard Bearer early; middle-right completes my run; the junction hedges against the unknown reinforcement.”

That is the placement decision the encounter should repeatedly produce.

---

## 15. Solvable-Puzzle Risk

### Identified risk

The game is deterministic and increasingly transparent.

That is intentional, but full information plus exhaustive candidate preview can turn placement into brute-force optimization.

A player should reason, not simply hover every combination until one scalar is smallest.

### Guardrails

- Candidate previews emphasize causal events and tradeoffs.
- Multiple stakes prevent every outcome from collapsing to one scalar.
- Optional opportunities add competing goals.
- Family/mode commitment creates irreversible choice.
- Dealer hidden rank preserves localized uncertainty.
- Candidate hover count is instrumented.

The problem is not that an optimal solution mathematically exists. The problem is if the interface makes it trivial to discover without understanding why.

---

## 16. Cognitive Load

### Identified risk

A drawn card potentially asks the player to consider:

- Rank
- Four deployment forms
- Socket
- Run structure
- Enemy breakpoints
- Standing orders
- March cost
- Lane stakes
- Dealer uncertainty

The answer is **not another mechanic**.

The timeline is the main compression mechanism.

### UI principle

Avoid making players mentally multiply stats where the resolver can show a physical consequence.

Prefer:

> “This tower gets two shots before the Banner crosses.”

Over:

> “Range 3.0 × enemy speed 0.65 × cooldown 1.4.”

The detailed numbers may remain inspectable, but the scheduling picture is primary.

---

## 17. Standing Orders

### DECIDED

Standing orders are part of encounter skill, not a secondary menu system.

Prototype orders remain:

- **Hold** — do not fire/trigger before a chosen point
- **Focus** — prioritize a defined enemy class or leading target
- **Trigger on Group** — wait until a minimum valid group exists

### Editing rule

Standing orders may be edited freely during planning and during the post-Dealer adjustment window.

They lock only when combat begins.

They do not consume the one positional adjustment move.

Their effect must be visible on the timeline.

---

## 18. Rank Stacking Sequencing

### DECIDED

Rank stacking remains behind a prototype flag.

Do not use stacking to compensate for an encounter that is not yet interesting.

Test order:

1. Improved information and timeline
2. Spatial breakpoint enemies
3. Tactical tower forms
4. Wave 2 counter-rotation
5. Optional opportunity unit
6. Validate base encounter
7. Then enable stacking and re-run

Stacking should deepen a functioning placement game, not rescue a shallow one.

---

## 19. Prototype Build Order

### DECIDED

Recommended implementation sequence:

1. **Fully known base wave**
2. **Timeline visualization**
3. **Exact current-state resolver statistics**
4. **Counterfactual deltas after commitment**
5. **Spatial breakpoint enemies**
6. **Snare → bunch → Barrage interaction**
7. **Four prototype tower forms**
8. **Visible lane for Dealer hidden card**
9. **Standing orders integrated into timeline**
10. **Wave 2 deliberately disturbs Wave 1**
11. **Optional physical opportunity unit**
12. **Junction as uncertainty hedge**
13. **Rank stacking flag**

Do not add more blackjack actions before this sequence is tested.

---

## 20. Instrumentation

### Required

#### Placement behavior

- Time per card placement
- Median placement time
- 90th percentile placement time
- Time by card number in hand
- Number of candidate forms hovered
- Number of candidate sockets hovered
- Number of times player moves between two competing options before commitment

#### Tactical understanding

- Whether player can explain the current battlefield shortfall before hitting
- Whether player can explain what the last card changed
- Whether player references timeline events, breakpoints, runs, or raw power when explaining placement

#### Candidate-space health

Occasionally ask:

> “Which placements were you seriously considering?”

Target: usually 2–3.

One repeatedly means the puzzle is too obvious.

Six or more repeatedly means the state is too noisy.

#### Timeline usage

- Whether player expands detailed stats or relies on timeline
- Whether March-step consequences are understood before drawing
- Whether standing-order changes are made from timeline information

#### Hover-bruteforce risk

Flag states where players inspect nearly every form/socket combination before committing.

If common, candidate preview is functioning as an oracle.

#### Optional opportunities

- How often the player pursues them
- How often pursuing them causes an additional Hit
- How often that Hit is a fourth or fifth card
- Whether safe misses remain tactically defensible
- Whether players describe opportunities as optional or mandatory

#### Wave 2

- Number of persisted towers retained
- Number replaced
- Number stacked when stacking flag is on
- Number of run links broken/preserved
- Whether Wave 2 causes materially different placement reasoning from Wave 1

---

## 21. Success Criteria

The improved encounter is working if:

- Before most Hit decisions, players can name the battlefield problem they are trying to solve.
- Players use the timeline to reason about attacks, March loss, slow, and enemy breakpoints.
- Important cards routinely present 2–3 plausible deployments.
- Players make different placements for the same rank under different enemy timing and stakes.
- Snare changes the value of Barrage in a way players notice and intentionally exploit.
- Forward placement is sometimes correct despite March exposure because an early breakpoint matters.
- Rear placement is sometimes correct because preserving engagement matters more.
- Junction placement is used as a hedge, not merely because no other socket is available.
- Wave 2 feels like adaptation to an existing board rather than a fresh setup.
- Optional opportunities sometimes motivate an otherwise unnecessary draw.
- Safe fourth/fifth-card misses are occasionally defensible for battlefield reasons.
- Players can explain what the last committed card bought them.
- Players do not routinely brute-force every hover combination.
- Placement remains brisk enough that the encounter does not become optimization homework.

---

## 22. Failure Signals

### The player still cannot say why they want another card

The information layer has failed. Do not add more mechanics.

### Players only compare leakage numbers

The encounter has collapsed into scalar minimization. Increase competing battlefield consequences, not hidden information.

### Everyone builds deep

Breakpoint enemies are too weak, too rare, or badly positioned. Fix enemy timing before touching socket bonuses.

### Everyone uses the same tower form for a rank

The four prototype forms are not tactically differentiated enough.

### Snare and Barrage are independently useful but never intentionally combined

The bunching interaction is too weak or too hard to read.

### Players hover every candidate before choosing

The forecast has become a brute-force oracle. Reduce sortable candidate outputs and emphasize causal tradeoffs.

### Players ignore optional opportunities

Their payoff is too small or too detached from the run.

### Players always pursue optional opportunities

They are mandatory objectives in disguise. Lower their payoff or increase their situationality.

### Wave 2 feels like Wave 1 with more enemies

Persistence is not producing adaptation. Rewrite encounter pairs before adding progression systems.

### Placement times explode

Do not add more decisions. Simplify presentation, reduce candidate forms, or make the timeline more legible.

---

## 23. Open Prototype Questions

These are deliberately left to playtesting rather than paper design.

1. What are the exact Barrage, Siege, Snare, and Ambush coefficients?
2. How much slow is needed for bunching to become tactically meaningful without becoming mandatory?
3. How severe should breakpoint abilities be?
4. Does the four-form prototype overload players?
5. Does exact current-state information make players thoughtful or simply encourage search?
6. How much candidate-preview detail can be shown before hover becomes an oracle?
7. Do optional opportunity units create meaningful marginal fourth/fifth-card draws?
8. Does Wave 2 counter-rotation make persistence interesting enough to justify keeping it?
9. Does the junction earn its role as an uncertainty hedge?
10. Once the base encounter works, does rank stacking deepen it or soften forced replacement too much?
11. Is the March Clock easier to understand through timeline consequences than through numeric explanation?
12. Are 2–3 plausible placements per important card achievable consistently through encounter authoring?

---

## 24. Explicitly Not Added

The improved encounter pass does **not** add:

- Baseline next-card preview
- Additional blackjack actions
- More formation multipliers
- Arbitrary socket stat bonuses
- Generalized enemy tower destruction
- Live combat clicking
- More than four prototype tower forms
- Player-facing optimal-play recommendations
- A combined tactical utility score

The diagnosis is insufficient causal consequence, not insufficient feature count.

---

## 25. Final Encounter Identity

21 Bastion's encounter should not feel like a hand of blackjack followed by tower placement.

It should feel like reading an approaching military problem through a card table.

The battlefield tells the player what will happen if nothing changes. The timeline tells them when it will happen. The current formation tells them why it fails. A drawn card gives them several concrete ways to alter that future, and committing the card changes the picture immediately.

Only then does the game ask whether they want another card.

The player knows the problem.

They know the price of trying again.

They know the kinds of outcomes another defense could create.

They do **not** know whether the next rank will be the answer.

> **Read. Diagnose. Commit. Observe. Decide whether to draw again.**

That is the improved encounter loop.
