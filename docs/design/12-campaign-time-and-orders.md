# Campaign Time, Resources, and Strategic Orders

Source: **Run Layer Handoff (consolidated)**, §§ 4, 5, 10, 11.

Full-game intent, with one prototype-adjacent consequence: **Chips are cut from the baseline economy**,
which changes what a Vault-stake leak costs today (`05-battlefield.md` § Lane stakes).

---

## Campaign time

> Time is the **run-scale analogue of the March Clock**: more preparation costs ground. The two clocks
> **never feed directly into one another.** Campaign time advances fronts and changes geography and stakes;
> those changes set encounter difficulty. **Campaign time never modifies hand-scale March entry.**

That separation is not a nicety. Two clocks that feed each other are one clock with extra steps, and the
self-similarity the design wants comes from the two pressures being *the same shape*, not the same number.

### Clock structure

Time **resets by siege phase** rather than running as one opaque thirty-minute budget.

Each phase begins with a **clearly stated countdown to the next major assault.** First-pass target:
**approximately eight campaign hours per phase**, config-tunable.

> Per-phase reset gives the player a tractable planning horizon and **prevents an early mistake from
> becoming impossible to price twenty minutes later** — the punishment-spiral failure mode, at the clock
> level.

### Action pricing principles

- **Action costs are fixed and visible.** First-pass costs: Scout 1h, Repair/Fortify 2h, Train/Temper 2h,
  Raid Dealer Supply 3h.
- **The guaranteed result is visible.** A Raid always lets the player remove or disrupt at least one known
  recruitment option; bonus loot may vary.
- When time **crosses a front threshold**, a visible front transformation occurs from that front's authored
  table (`11-siege-geography.md`).
- **Reaching zero hours never causes defeat.** It resolves the scheduled enemy action or assault and
  advances the siege.

---

## Campaign resources

Three persistent resources with **separate, non-overlapping jobs.**

> **Chips are cut.** There is no general-purpose money resource in the baseline run.

| Resource | Job | Never used for |
|---|---|---|
| **Time** | Ordinary campaign actions: Fortify, Muster, Train, Raid, Reconnoiter, and other preparation | Tactical rule-breaking or emergency encounter manipulation |
| **Favor** | Rare command authority, spent to break a normal encounter rule in a bounded way. First-pass cap: **3** | Routine repairs, card acquisition, reranking, or ordinary campaign services |
| **Bastion Health** | Measures how close the run is to defeat | Buying upgrades or paying for strategic orders |

### Why Chips were cut

Chips and Time were both "the currency you spend on ordinary things," which is one job with two
mechanisms — the failure the One System Per Job pillar exists to catch. Time is the better of the two
because **spending it advances the siege**, so a purchase has a battlefield consequence rather than only an
opportunity cost.

### Favor: earning and spending

Favor rewards **the behavior the encounter layer wants**, not good blackjack outcomes.

> **Earned for voluntarily accepting meaningful pre-resolution risk and successfully protecting important
> stakes.** Not for reaching 20/21, not for high Formation Strength, and not for outperforming a
> deterministic Final Forecast.

The last exclusion is structural: the Final Forecast is exact, so "outperforming it" is not a thing that
can happen. Anything that pays out on it is measuring a bug.

Exact triggers are a tuning question. Prototype-eligible examples:

- standing while the **Visible Threat** still shows a Bastion lane **Open**, and then holding it after the
  Dealer reveals;
- taking a flagged high-risk hand decision and finishing the encounter with **no Bastion leakage**;
- accepting a costly **forced replacement** and preserving the threatened stake.

Note that all three are *pre-resolution* commitments scored *post-resolution*. That is what keeps Favor
from paying the player twice for a good hand.

**Spends are rare, explicit exceptions:** an emergency redraw, one additional Dealer-information reveal,
one Ace-state intervention, or one family reassignment where an effect specifically permits it.

> These must be scarce enough that Favor feels like **stored command authority rather than purple money.**
> The cap of 3 is the enforcement mechanism, and whether it produces the intended scarcity is an open
> question (`../ROADMAP.md`).

⚠ Favor is **not** a generic reward-floor currency. Every encounter has a reward floor
(§ Consequence rewards below); Favor is not how it is paid.

---

## One strategic order between encounters

The cadence rule is stricter than a traditional roguelite: **after most encounters, the player issues
exactly one strategic order.**

| Order | What it does | Typical cost |
|---|---|---|
| **Hold / Redeploy** | Choose which front to defend next, or remain on the current one. Conserves time but may let other fronts advance | 0–1h |
| **Fortify** | Repair or alter persistent geography at a named front | 2h |
| **Muster** | Acquire, Cut, Repaint, or Rerank a card through a diegetic recruitment or supply opportunity | 1–2h |
| **Train** | Temper or Promote one card; may begin or complete a doctrine project | 2h |
| **Raid** | Attack Dealer supply and interfere with public recruitment | 3h |
| **Reconnoiter** | Reveal additional front, Dealer, or geography information | 1h |
| **Concede** | Deliberately abandon or scuttle a position for a known structural benefit | Varies; often *saves* time |

**The order is the progression decision**, and it often determines where the next encounter happens. There
is no reward screen, then a map, then a shop.

Reward verbs (Acquire, Cut, Temper, Repaint, Promote, Rerank) are defined in
`08-deck-economy-progression.md`; doctrine projects in `13-doctrine-and-charters.md`.

### Cadence target

**Thirty to sixty seconds** per command phase, because the player sees **one decision surface**. Charters,
phase transitions, and rare major events may breathe longer.

> If the default between-encounter flow requires choosing a reward, shopping, promoting a card, and routing
> separately, **the structure has failed.**

---

## Shops, events, and rewards

**Traditional shops and branching reward rooms are not baseline structure.** Their useful functions are
absorbed into campaign orders and named geography.

A Quartermaster, market, foundry, or captured supply depot **may temporarily expose** Acquire / Cut /
Rerank / Temper actions — but the player should not routinely leave the siege map for a conventional
merchant loop. **These services cost Time.**

### Consequence rewards

Rewards should often **emerge from what happened**: steal their 6, destroy their King, repaint a captured
artillery card as native Club, use a held Vault to fund a rerank.

**Every encounter has a reward floor**, so that poor combat does not combine with siege-state variance into
a downward spiral. The floor is paid in ordinary campaign terms — not in Favor.

---

## The siege menu probe

> **Build only a menu-level probe, and only after the encounter loop works.** Two visible fronts, one phase
> clock, four preparation actions with fixed costs, **no persistent geography simulation.**

| Probe order | Cost | Guaranteed consequence |
|---|---|---|
| **Scout** | 1h | Reveal one hidden front consequence or Dealer recruitment detail |
| **Repair** | 2h | Improve one visible front state or path element |
| **Train** | 2h | Temporarily improve or modify one named card for the next encounter |
| **Raid** | 3h | Remove one visible Dealer recruitment candidate |

**The first probe uses Time only.** Favor enters once encounter telemetry can identify the risk behaviors
that should earn it — which is a dependency on the encounter instrumentation, not on the campaign build.

The probe's purpose is to test whether the **self-similar pressure lands emotionally**: whether paying
three hours to repair a gate feels like the campaign-scale version of paying a March step to draw.

### Probe success signals

- Players can explain **what they bought with time and what they knowingly allowed to worsen elsewhere.**
- Players **sometimes conserve time** rather than always spending the maximum available.
- Players describe the hand-scale March Clock and the campaign clock as **related kinds of pressure without
  being told the analogy.**
- The campaign menu makes the next encounter **more anticipated, not delayed.**

Sequencing and the rest of the probe plan: `../prototype/VALIDATION.md` § The run layer.
