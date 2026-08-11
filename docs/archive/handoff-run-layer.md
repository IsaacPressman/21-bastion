# 21 Bastion

## Rank Stacking & Continuous Siege Run Layer

**Consolidated Design Handoff**

> **Resolved product identity**
>
>
> 21 Bastion is a blackjack tower defense with a siege-shaped run. Encounters remain the primary game. The siege layer exists to create context, consequences, and the next battlefield problem - not to become a second strategy game larger than combat.

*Status: consolidated decision record following Gameplay Design Handoff Revision 7.1 and Addendum A; updated with campaign economy, opposing-shoe replacement, and victory/defeat rules.*

# 0. Purpose and Authority

This handoff consolidates the rank-stacking proposal, the run-layer triage, the continuous-siege direction, and the subsequent design review into one implementation-facing record. Where this document changes an Addendum A decision, the newer rule here supersedes it. Nothing in this handoff changes the encounter-level arithmetic, March Clock presets, Formation Strength curve, or deterministic resolver specified in Revision 7.1 unless stated explicitly.

> **Primary macro principle**
>
>
> The run is not a sequence of reward rooms. It is one ongoing siege. Between battles, the player issues one strategic order; that order changes the shoe, doctrine, Dealer, information, or geography, and the siege advances.

## Decision status vocabulary

| **Status**   | **Meaning**                                                     |
|--------------|-----------------------------------------------------------------|
| **DECIDED**  | Implement or plan around this rule.                             |
| **PROBE**    | Cheap test required before production scope expands.            |
| **DEFERRED** | Intentionally outside the next build; retain the design intent. |
| **CUT**      | Do not carry forward unless explicitly reopened.                |

## Resolved fork

DECIDED: 21 Bastion is a blackjack tower defense with a siege-shaped run. The encounter remains the reason to play; the campaign layer should occupy roughly the supporting third of the experience, not become a separate grand-strategy game. A useful conceptual target is approximately 70% encounter play and 30% campaign decisions, with the exact ratio determined by playtest pacing rather than enforced as a timer.

## The three run pillars

| **Pillar**      | **What changes**                                                                                         | **What it must not become**                 |
|-----------------|----------------------------------------------------------------------------------------------------------|---------------------------------------------|
| **Shoe**        | Rank distribution, native family identity, one modifier per card, deliberate Acquire/Cut/Rerank choices. | A linear deck-strength score.               |
| **Doctrine**    | A small set of behavior-changing global rules built over time.                                           | Twenty passive percentage relics.           |
| **Siege State** | Time, front condition, geography, Dealer recruitment, concessions, and pressure.                         | A punishment spiral or procedural map maze. |

# 1. Standing Run-Layer Constraints

- **Encounter first.** Every campaign mechanic must create a more interesting next encounter or be cut.

- **Rank count is sacred.** The game may change a card's character, history, family, modifier, or availability representation, but enemy pressure does not silently alter blackjack rank distribution.

- **One system per job.** If a new mechanism duplicates an existing pressure or reward, remove one of them.

- **Counter the build, never the player.** Dealer adaptation responds to what the player has chosen to build, never to win rate, health, or hidden difficulty-governor signals.

- **Pressure creates different problems, never punishment spirals.** Neglect and loss reshape the siege; they do not recursively make every later decision worse.

- **One ordinary defeat condition.** Territory and time change position; they do not function as hidden health bars. The run ends in defeat only when Bastion Health reaches zero.

- **Currencies have non-overlapping jobs.** Time buys ordinary campaign actions. Favor buys rare permission to bend encounter rules. Bastion Health measures survival. Chips are cut.

- **Costs are exact; core consequences are legible.** Secondary payoff may be uncertain. The player must always know what they are definitely spending and the minimum result they are buying.

- **Reveal consequences, not conclusions.** The siege map shows possible transformations and known costs, but never tells the player which strategic order is optimal.

- **No compounding multipliers across encounters.** Towers reset at encounter boundaries; geography and card identity may persist because they do not carry Formation Strength forward.

# 2. Rank Stacking

> **Status: DECIDED - flag-gated for prototype**
>
>
> Rank stacking is included because it creates a second placement archetype - density versus spread - not because it is a free socket-pressure valve. Run the March Clock test arms with stacking off first, then repeat with the flag enabled.

## Core rule

Two towers of the same rank may occupy one socket as a single stack. Matching is by rank, not blackjack value: J+J stacks; J+Q does not. Aces are excluded. Prototype depth cap is two cards per stack.

| **Rule**               | **Committed behavior**                                                                                          |
|------------------------|-----------------------------------------------------------------------------------------------------------------|
| **Match**              | Same rank only.                                                                                                 |
| **Depth**              | 2 in prototype.                                                                                                 |
| **Aces**               | Cannot stack.                                                                                                   |
| **Power bonus**        | None.                                                                                                           |
| **Run eligibility**    | A stacked socket cannot participate in a run.                                                                   |
| **Family**             | The two cards may have different families; both behaviors originate from the shared socket.                     |
| **Formation Strength** | Each card layer retains its own multiplier. Stack power is the sum of each layer's individually modified power. |
| **Position**           | Both layers share socket, range origin, March exposure, and any positional penalties.                           |

## Why the multiplier rule changed

Addendum A proposed that a cross-wave stack inherit the lower multiplier. This handoff supersedes that rule. The lower-multiplier rule adds a hidden third cost to stacking and makes a fresh card lose power merely for sharing a socket. The intended trade is spatial: save capacity, lose coverage breadth and run eligibility. Each card therefore keeps its own Formation Strength contribution. This also prevents multiplier laundering because neither layer changes multiplier when stacked.

## Placement archetypes

|                          | **Spread**                  | **Density**                                |
|--------------------------|-----------------------------|--------------------------------------------|
| **Wants**                | Distinct consecutive ranks  | Duplicate ranks                            |
| **Board shape**          | Wide, adjacent, linked      | Concentrated strongpoints                  |
| **Primary value**        | Run adjacency and coverage  | Socket economy and multifunction positions |
| **Acquisition question** | Does this complete a chain? | Do I want another copy of this rank?       |

## Accepted risk and instrumentation

- Stacking softens forced replacement. Log stack-at-capacity rate, replacement rate, and whether players stack reflexively whenever a match exists.

- Stacking may worsen deep-placement dominance because concentrated power naturally prefers safe rear sockets. Diagnose socket geometry before taxing stacks.

- If stacking becomes automatic, first test a spatial or cadence cost such as a longer shared cooldown. Do not add a flat damage penalty by default.

# 3. The Continuous Siege

The run is one military situation evolving over roughly thirty to forty-five minutes. The player is not traveling through disconnected rooms. They are defending named districts around one Bastion while the Dealer recruits, fronts advance, terrain changes, and time is spent.

> **Macro identity**
>
>
> Standard roguelite progression asks: what did I collect? 21 Bastion should ask: what did I hold, what did I give up, and what kind of battlefield did those choices leave me?

## Persistent geography

DECIDED: the shape of the ground persists; towers do not persist across encounters. Geography is authored rather than procedural. Fronts have stable identities so players can learn them, plan around them, and remember how a run changed them.

| **Front**       | **Baseline identity**                                          | **Example persistent changes**                                                                          |
|-----------------|----------------------------------------------------------------|---------------------------------------------------------------------------------------------------------|
| **North Gate**  | Long approach; artillery-friendly; strategic bridge.           | Bridge intact / damaged / destroyed; path length shortened; rear socket exposed; Vault access lost.     |
| **River Works** | Infrastructure district; chokepoints; control families matter. | Workshop active / evacuated / scuttled; route split disabled; socket layout altered.                    |
| **East Ward**   | Commercial/civilian district; fast routes; economic stakes.    | Market open / looted / abandoned; alternate approach opened; Vault stake converted to Bastion pressure. |
| **The Bastion** | Inner defense and final stand.                                 | Becomes exposed as outer districts fail; final geometry reflects the run.                               |

## Front state model

Each outer district uses a small authored state ladder, not procedural terrain. First implementation target: three outer fronts plus the Bastion. Every front is always in one of four campaign states:

| **State** | **Meaning** | **Run consequence** |
|---|---|---|
| **Held** | The district is under player control and its normal services and geometry remain available. | Full strategic access. |
| **Compromised** | The district is still defensible, but geography, stakes, or services have worsened. | Creates a harder or different encounter without removing the front. |
| **Lost** | The Dealer took the district through pressure or failed defense. | The district leaves normal routing and applies its authored loss consequence. |
| **Conceded** | The player deliberately abandoned or scuttled the district. | The district leaves normal routing, but the player receives the declared scuttle benefit. |

Lost and Conceded are both terminal outer-front states for ordinary routing, but they are not equivalent. A Lost district applies the enemy-favored authored consequence. A Conceded district applies a known compensating effect chosen by the player. Neither directly causes defeat.

Neglect transformations come from a bounded authored table of six to eight defined outcomes. The possible outcomes are shown before the player commits time elsewhere. The player may be uncertain which secondary payoff occurs, but must understand the guaranteed state change they are risking.

## Concession

DECIDED: the player may intentionally abandon or destroy territory. Concession must sometimes be strategically correct, not merely less bad. A concession trades one resource or service for a structural advantage elsewhere.

| **Concession**         | **Certain cost**                                      | **Certain benefit**                                                      |
|------------------------|-------------------------------------------------------|--------------------------------------------------------------------------|
| **Blow the bridge**    | Lose the long outer approach and its future services. | Remove a Dealer reinforcement route or collapse two approaches into one. |
| **Evacuate the Vault** | Forfeit some future economy.                          | Protect remaining reward from raids and shorten the defense obligation.  |
| **Scuttle the Works**  | Lose a Temper/repair service.                         | Deny enemy access and change path geometry in the player's favor.        |
| **Fall back**          | Lose an outer district state.                         | Preserve time, card readiness, or a critical inner defense.              |

Conceding a district never directly ends the run. Its strategic value comes from deliberately trading geography or services for time, denial, path simplification, or protection of a more important position.

## Campaign victory, defeat, and Last Stand

> **Status: DECIDED**
>
> **Bastion Health reaching zero is the only ordinary defeat condition.** Territory is position, not a second health bar. Time is an action clock, not a loss clock.

### Defeat

The run ends immediately when **Bastion Health reaches zero**. Health should remain difficult to restore so battlefield leakage carries campaign weight.

The following do **not** directly end a run:

- losing an outer district;
- voluntarily conceding an outer district;
- losing every outer district;
- reaching zero campaign hours in a phase.

### Time expiration

When a phase clock reaches zero, the scheduled Dealer action or major assault occurs. The player does not lose merely because time expired. Time controls **when the enemy acts**, not whether the run continues.

### Last Stand

The campaign enters **Last Stand** when either:

1. all outer defensive fronts are Lost or Conceded; or
2. the final siege phase reaches its scheduled Bastion assault.

Entering Last Stand is not defeat. It is the removal of further strategic retreat. Dealer recruitment locks, no more outer concessions are possible, and the final Bastion battlefield is assembled from the geography, doctrine, shoe state, Dealer shoe, Favor, and Bastion Health produced by the run.

### Victory

The player wins the run by surviving and defeating the final Dealer assault at the Bastion while Bastion Health remains above zero.

This rule is load-bearing for concession. A player must be able to abandon every outer district and still have a path to victory, however difficult the resulting Last Stand becomes.

# 4. Campaign Time

> **Status: DECIDED**
>
>
> Time is the run-scale analogue of the March Clock: more preparation costs ground. The two clocks never feed directly into one another. Campaign time advances fronts and changes geography/stakes; those changes set encounter difficulty.

## Clock structure

Time resets by siege phase rather than running as one opaque thirty-minute budget. Each phase begins with a clearly stated countdown to the next major assault. First-pass target: approximately eight campaign hours per phase, config-tunable. This gives the player a tractable planning horizon and prevents early mistakes from becoming impossible to price twenty minutes later.

## Action pricing principles

- Action costs are fixed and visible. Example starting costs: Scout 1h, Repair/Fortify 2h, Train/Temper 2h, Raid Dealer Supply 3h.

- The guaranteed result is visible. A Raid always lets the player remove or disrupt at least one known recruitment option; bonus loot may vary.

- Campaign time never modifies hand-scale March entry directly.

- When time crosses a front threshold, a visible front transformation occurs from its authored table.

- Reaching zero hours never causes defeat. It resolves the scheduled enemy action or assault and advances the siege.

## Campaign resources

The campaign uses three persistent resources/state currencies with separate jobs. **Chips are cut.** There is no general-purpose money resource in the baseline run.

| **Resource / state** | **Job** | **Never used for** |
|---|---|---|
| **Time** | Pays for ordinary campaign actions: Fortify, Muster, Train, Raid, Reconnoiter, and other preparation. | Tactical rule-breaking or emergency encounter manipulation. |
| **Favor** | Rare command authority spent to break a normal encounter rule in a bounded way. First-pass cap: **3**. | Routine repairs, card acquisition, reranking, or ordinary campaign services. |
| **Bastion Health** | Measures how close the run is to defeat. | Buying upgrades or paying for strategic orders. |

### Favor earning and spending

Favor rewards the behavior the encounter layer wants rather than good blackjack outcomes by themselves. It is earned for **voluntarily accepting meaningful pre-resolution risk and successfully protecting important stakes**. It is not awarded merely for reaching 20/21, producing high Formation Strength, or outperforming a deterministic Final Forecast.

Exact earning triggers are a tuning question, but prototype-eligible examples include: standing while the Visible Threat still shows a Bastion lane Open and then holding it after Dealer reveal; taking a flagged high-risk hand decision and finishing the encounter with no Bastion leakage; or accepting a costly forced replacement and preserving the threatened stake.

Favor spends are rare, explicit exceptions such as an emergency redraw, one additional Dealer-information reveal, one Ace-state intervention, or one family reassignment when an effect specifically permits it. These effects should be scarce enough that Favor feels like stored command authority rather than purple money.

# 5. One Strategic Order Between Encounters

The cadence rule is stricter than a traditional roguelite: after most encounters, the player issues exactly one strategic order. There is no mandatory reward screen followed by a map followed by a shop. The order itself is the progression decision and often determines where the next encounter happens.

| **Order**           | **What it does**                                                                                                        | **Typical cost**         |
|---------------------|-------------------------------------------------------------------------------------------------------------------------|--------------------------|
| **Hold / Redeploy** | Choose which front to defend next or remain on the current front. Conserves time but may let other fronts advance.       | 0-1h                     |
| **Fortify**         | Repair or alter persistent geography at a named front.                                                                  | 2h                       |
| **Muster**          | Acquire, Cut, Repaint, or Rerank a card through a diegetic recruitment/supply opportunity.                              | 1-2h                     |
| **Train**           | Temper or Promote one card; may begin or complete a doctrine project.                                                   | 2h                       |
| **Raid**            | Attack Dealer supply and interfere with public recruitment.                                                             | 3h                       |
| **Reconnoiter**     | Reveal additional front, Dealer, or geography information.                                                              | 1h                       |
| **Concede**         | Deliberately abandon/scuttle a position for a known structural benefit.                                                 | Varies; often saves time |

## Cadence target

Most command phases should resolve in roughly thirty to sixty seconds because the player sees one decision surface, not a stack of menus. Charters, region transitions, and rare major events may breathe longer. If the default between-encounter flow requires choosing a reward, shopping, promoting a card, and routing separately, the structure has failed.

# 6. Dealer Recruitment and the Opposing Shoe

> **Status: DECIDED**
>
>
> The Dealer adapts compositionally, not through hidden difficulty scaling and not through total-comparison battlefield bonuses. The Dealer should feel like another commander building an army in public.

## Opposing-shoe contract

The Dealer has a fixed-size **26-card campaign shoe**. Normal recruitment never increases its size. Every recruitment is a **one-for-one replacement**: one visible candidate replaces one existing Dealer card.

This keeps the opposing shoe readable and makes every intervention meaningful. A raided King matters because it prevented a specific 4 -> King composition shift, not because it removed one card from an ever-growing pile.

Normal recruitment may not permanently exceed the starting count of any elite category beyond explicit roster caps. The exact rank/family caps are content tuning, but the fixed-size shoe is not.

## Public recruitment

After relevant encounters, the Dealer receives a visible recruitment row of three candidate cards. Each candidate has a known rank, known enemy-unit identity, and a visible **replacement target** in the Dealer shoe. The Dealer's intended replacement is shown based on personality and current doctrine. The player can choose to spend time disrupting the row before it resolves.

| **Rule**                | **First-pass contract**                                                                                                                                      |
|-------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Dealer shoe size**    | Fixed at 26 cards under normal campaign recruitment.                                                                                                         |
| **Recruitment row**     | 3 visible candidate cards, each paired with the existing Dealer card it would replace.                                                                        |
| **Intent**              | Dealer's preferred candidate/replacement pair is marked before the player acts.                                                                               |
| **Recruitment cadence** | Normally 1 one-for-one replacement per strategic beat in phases where recruitment is active.                                                                 |
| **Raid**                | Costs campaign time and lets the player destroy, steal, or otherwise block one visible candidate before recruitment resolves.                                |
| **Adaptation lag**      | Phase II recruitment responds mainly to Phase I build signals; Phase III responds mainly to Phase II. No immediate counter-picking after a single encounter. |
| **Target signal**       | Build composition and repeated tactical commitments only. Never win rate, health, hidden skill estimates, or loss streaks.                                   |

## Why supply-line raiding is mandatory

Without a way to interfere, Dealer adaptation is experienced as rubber-banding: the game sees the player build something fun and manufactures its counter. Public recruitment plus raiding converts adaptation into an arms race. The player may decide that a visible King is worth three hours to remove, or intentionally allow it because the current formation handles siege engines well.

# 7. Shoe Progression and Card Identity

## Reward verbs

The baseline progression verbs are Acquire, Cut, Temper, Repaint, Promote, and Rerank +/-1. Bind remains cut. These verbs are delivered through campaign orders, consequences, captured supplies, and named services rather than a generic post-combat card reward every time.

| **Verb**        | **Meaning**                                                        | **Why it is interesting**                                                           |
|-----------------|--------------------------------------------------------------------|-------------------------------------------------------------------------------------|
| **Acquire**     | Add a rank/card to the shoe.                                       | Improves one tactical option while changing future blackjack distribution.          |
| **Cut**         | Remove a chosen card permanently.                                  | Chosen probability surgery; never inflicted casually by enemies.                    |
| **Temper**      | Add or change the card's one allowed modifier.                     | Changes battlefield behavior without stacking endless upgrades.                     |
| **Repaint**     | Change native family.                                              | Changes deck-family structure without changing blackjack rank.                      |
| **Promote**     | Grant a named battlefield behavior unlocked by the card's history. | Turns memorable play into future identity.                                          |
| **Rerank +/-1** | Change rank by one.                                                | Weakens/strengthens tower power, run structure, and blackjack distribution at once. |

## Card histories

Cards may accumulate named history tags from resolver events - for example, Held North Gate During the First Breach or Broke the Dealer's Siege Engine. Histories do not automatically grant power or experience levels. They create eligibility for future Promote choices. Each card may carry at most one gameplay modifier; history can remain as flavor beyond that cap.

## Exhaustion without rank loss

> **Superseding rule**
>
>
> An exhausted veteran is replaced for the next encounter by a Reserve copy of the same rank. The shoe keeps the same rank counts, bust probabilities, and run distribution; only the card's special identity is temporarily absent.

- Prototype/full-game baseline uses one exhaustion state only: Fresh or Exhausted. Exhaustion does not stack toward injury or death.

- Reserve copy has the same blackjack rank and base tower power, but no modifier, native-family bonus, veterancy effect, or history-triggered promotion behavior.

- The original card returns after one encounter unless a special effect says otherwise.

- Enemy-inflicted permanent capture is rare, telegraphed, and recoverable. A captured card is represented in the player shoe by a Reserve of the same rank until the original is recovered, preserving rank count.

# 8. Doctrine and Charters

## Doctrine

Doctrine is the persistent placement-layer progression. Target four to seven behavior-changing globals by the end of a run, with no expectation that every run reaches the maximum. Doctrine pieces are built over one or two encounters; the old fortification-project concept becomes the delivery mechanism rather than a separate progression track.

| **Example doctrine**    | **Decision it changes**                                                                                      |
|-------------------------|--------------------------------------------------------------------------------------------------------------|
| **Cross-Lane Triggers** | Spades may trigger from an adjacent lane; changes socket valuation.                                          |
| **Junction Network**    | The junction counts as adjacent to every middle socket; deliberately breaks baseline run topology.           |
| **Field Reassignment**  | The first card placed in each lane may be reassigned after Dealer reveal; a bounded escape from family lock. |
| **Watchtower**          | Reveals additional Dealer or lane information before commitment.                                             |
| **Machine Shop**        | Expands Temper access and modifier choice quality.                                                           |

## Charters

Two major Charters per run, normally after the first two siege phases. A Charter changes a rule of the run rather than adding a passive percentage. The Last Line geometry principle is explicitly not a Charter: changing preferred socket depth belongs in baseline geometry because it is a fix for deep-placement dominance, not a rare reward.

# 9. Siege Phases and Full-Run Arc

The old three-region structure becomes three phases of the same siege. Geography and enemy composition evolve continuously; the player does not travel to disconnected biomes. Phase III is the scheduled Last Stand, but Last Stand may begin early if every outer front is Lost or Conceded.

| **Phase**            | **Campaign state**                                                                       | **Player experience**                                              | **Dealer state**                                            |
|----------------------|------------------------------------------------------------------------------------------|--------------------------------------------------------------------|-------------------------------------------------------------|
| **I - Encirclement** | Outer districts intact; generous geometry; first clock.                                  | Establish the shoe and early doctrine. Learn which fronts matter.  | Generic recruitment; little adaptation.                     |
| **II - Breach**      | One or more fronts change state; concessions become meaningful.                          | Exploit the build while it feels strong; choose what to protect.   | Public recruitment begins adapting one beat behind.         |
| **III - Last Stand** | Scheduled final assault, or triggered early if every outer front is Lost/Conceded. Geography now reflects the run. | Stress-test what was built; no further territorial retreat. | Mature fixed-size opposing shoe built from visible replacement decisions. |

## Run memory

A successful run should be recalled geographically and strategically: the eastern ward was abandoned, the north bridge was blown, the Dealer's King recruitment was raided, and a Veteran 4 held the River Works. Build identity matters, but it should live inside the story of the siege rather than replace it.

# 10. Shops, Events, and Rewards

Traditional shops and branching reward rooms are not baseline structure. Their useful functions are absorbed into campaign orders and named geography. A Quartermaster, market, foundry, or captured supply depot may temporarily expose Acquire/Cut/Rerank/Temper actions, but the player should not routinely leave the siege map for a conventional merchant loop. These services cost **Time** unless a specific effect says otherwise; Chips do not exist in the baseline economy.

## Consequence rewards

Rewards should often emerge from what happened: steal their 6, destroy their King, repaint a captured artillery card as native Club, or use a held Vault to fund a rerank. Every encounter has a reward floor so poor combat does not combine with siege-state variance into a downward spiral. Favor is not a generic reward-floor currency; it is awarded only through its risk-and-stake conditions.

# 11. Prototype and Probe Plan

## Encounter prototype remains primary

Nothing in this run-layer handoff should delay the Revision 7.1 vertical-slice question. Rank stacking is the only encounter mechanic added now, and it remains behind a flag. The full continuous-siege systems wait until the encounter prototype proves that card identity, placement, and hit/stand react to battlefield state.

## Rank-stacking sequence

1.  Run Flat, Soft, and Hard March presets with stacking disabled.

2.  Repeat the same scripted fixtures and organic encounter with stacking enabled.

3.  Compare forced-replacement frequency, stack-at-capacity rate, run frequency, placement depth, and many-card viability.

4.  If stacking becomes automatic at capacity, test one cost in isolation; do not change March and stacking simultaneously.

## Siege principle probe

Build only a menu-level probe after the encounter loop works: two visible fronts, one phase clock, four preparation actions with fixed costs, and no persistent geography simulation. The purpose is to test whether the self-similar pressure lands emotionally - whether paying three hours to repair a gate feels like the campaign-scale version of paying a March step to draw. The first siege-menu probe uses Time only; Favor enters once the encounter telemetry can identify the risk behaviors that should earn it.

| **Probe order** | **Cost** | **Guaranteed consequence**                                        |
|-----------------|----------|-------------------------------------------------------------------|
| **Scout**       | 1h       | Reveal one hidden front consequence or Dealer recruitment detail. |
| **Repair**      | 2h       | Improve one visible front state or path element.                  |
| **Train**       | 2h       | Temporarily improve/modify one named card for the next encounter. |
| **Raid**        | 3h       | Remove one visible Dealer recruitment candidate.                  |

## Probe success signals

- Players can explain what they bought with time and what they knowingly allowed to worsen elsewhere.

- Players sometimes conserve time rather than always spending the maximum available.

- Players describe the hand-scale March Clock and campaign clock as related kinds of pressure without being told the analogy.

- The campaign menu makes the next encounter more anticipated, not delayed.

# 12. Instrumentation for the Run Layer

| **System**             | **Log**                                                                                                                     |
|------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| **Stacking**           | Match opportunity, stack chosen, replacement alternative, capacity state, socket depth, families in stack.                  |
| **Time**               | Hours remaining, order selected, visible alternatives, front transformations triggered, unused time.                        |
| **Dealer recruitment** | Candidate row, replacement targets, intended pair, player raid choice, final one-for-one replacement, build signals used for next phase weighting. |
| **Geography**          | Front state before/after, Lost vs Conceded cause, path-length changes, socket changes, Last Stand trigger, next encounter modifier. |
| **Favor**              | Favor before/after, earning trigger, spend type, whether the spend changed the encounter decision or merely erased a mistake. |
| **Run survival**       | Bastion Health, phase time, scheduled assaults, outer fronts remaining, early-vs-scheduled Last Stand, final victory/defeat cause. |
| **Card identity**      | History tags earned, Promote choices, exhaustion, Reserve substitutions, modifier distribution.                             |
| **Cadence**            | Time spent on command screen, backtracking, number of distinct menus opened, next-encounter start latency.                  |

# 13. Production Sequencing

| **Stage**                       | **Build**                                                                                       | **Do not build yet**                     |
|---------------------------------|-------------------------------------------------------------------------------------------------|------------------------------------------|
| **A. Encounter vertical slice** | Revision 7.1 encounter, deterministic resolver, telemetry, March arms.                          | Run map, doctrine, Dealer recruitment.   |
| **B. Stacking pass**            | Flag-gated rank stacking and second pass of same fixtures.                                      | Stack-specific upgrades or rarity.       |
| **C. Siege menu probe**         | Two fronts, phase clock, four orders, visible consequences.                                     | Persistent geography simulation.         |
| **D. Four-encounter mini-run**  | Three named fronts, one phase, one doctrine project, public Dealer recruitment, one concession. | Three-phase campaign, many Charters.     |
| **E. Full run vertical slice**  | Three siege phases, geography history, Dealer adaptation lag, card histories, two Charters.     | Large modifier library, metaprogression. |

# 14. Resolved Decisions

| **Decision**                  | **Resolution**                                                                                  |
|-------------------------------|-------------------------------------------------------------------------------------------------|
| **Product fork**              | Blackjack tower defense with a siege-shaped run.                                                |
| **Map size**                  | Three named outer fronts plus the Bastion.                                                      |
| **Terrain**                   | Authored persistent geography; no procedural terrain baseline.                                  |
| **Time budget**               | Resets per siege phase; config-tunable first-pass clock.                                        |
| **Between-encounter cadence** | One strategic order on most beats.                                                              |
| **Dealer adaptation**         | Public three-card recruitment row; visible intent; build-based, lagged, raidable.               |
| **Dealer recruitment**        | Fixed 26-card opposing shoe; every normal recruit replaces one existing Dealer card.            |
| **Campaign economy**          | Chips cut. Time pays ordinary campaign actions; Favor is rare capped rule-breaking authority.   |
| **Favor cap**                 | First-pass maximum 3; earned through voluntary risk plus protected stakes, not hand quality.     |
| **Defeat condition**          | Bastion Health reaching zero is the only ordinary run-loss condition.                            |
| **Time expiration**           | Triggers the scheduled Dealer action/assault; never causes defeat by itself.                     |
| **Territory loss**            | Never directly defeats the player; all outer fronts gone triggers Last Stand.                    |
| **Victory condition**         | Defeat the final Dealer assault at the Bastion with Bastion Health above zero.                   |
| **Exhaustion**                | One-encounter exhaustion represented by same-rank Reserve; rank count unchanged.                |
| **Card upgrades**             | One gameplay modifier per card; history creates promotion eligibility rather than automatic XP. |
| **Stacking cross-wave power** | Each card layer keeps its own Formation Strength; no lower-multiplier collapse.                 |
| **Stack family rule**         | Same-rank cards may stack even with different families.                                         |
| **Shops**                     | Not a mandatory standalone loop; services appear diegetically through orders/geography.         |
| **Concession**                | Core campaign mechanic; must sometimes be the strongest strategic move.                         |

# 15. Open Questions That Remain

| **\#** | **Question**                                                                                            | **Blocks**                                |
|--------|---------------------------------------------------------------------------------------------------------|-------------------------------------------|
| 1      | What exact socket geometry removes deep-placement dominance without creating a new obvious best depth?  | Final March tuning and geography variants |
| 2      | What is the tuned phase-clock length and action-cost table after the menu probe?                        | Full siege pacing                         |
| 3      | How strongly should Dealer recruitment weighting react to build signals, and which signals are allowed? | Dealer adaptation model                   |
| 4      | Which exact encounter-risk events earn Favor, and does the first-pass cap of 3 create the intended scarcity? | Favor tuning                          |
| 5      | What are the final authored state transitions for each of the three outer fronts?                       | Persistent geography content              |
| 6      | How many doctrine pieces can coexist before the encounter UI becomes unreadable?                        | Doctrine launch budget                    |
| 7      | Does the stacking flag materially reduce forced replacement or only create a healthy third branch?      | Stack ship/cut                            |
| 8      | What are the final Charter rules after the baseline run loop is proven?                                 | Late-run variety                          |

# 16. Final Run Identity

At hand scale, the player asks whether another card is worth the ground it costs. At lane scale, the player asks what can be left uncovered. At campaign scale, the player asks what can be left undefended. The same command principle repeats at three magnifications without sharing a single hidden score. Time determines when the enemy acts, territory determines the position fought from, Favor permits rare exceptions, and Bastion Health alone determines whether the run is still alive.

A memorable run should not be a list of relics. It should be a siege history: which district held, which bridge was destroyed, which Dealer recruitment was stopped, which card became a veteran, and which position the player deliberately abandoned so the Bastion could survive.

**North-star sentence: Build the defense you need, spend only the ground you can afford, and decide what you are willing to lose.**
