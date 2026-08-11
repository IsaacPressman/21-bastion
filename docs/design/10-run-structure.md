# Run Structure — The Continuous Siege

Source: **Run Layer Handoff (consolidated)**, §§ 0, 1, 3, 9, 16. Supersedes Handoff Revision 7.1 § 19
wherever the two disagree.

> **Nothing in the run layer changes encounter-level arithmetic** — not the March Clock presets, not the
> Formation Strength curve, not the deterministic resolver. Where the run layer touches the encounter, it
> does so by changing *which battlefield you fight on and with what shoe*, never by editing the numbers
> inside a wave.

The prototype implements encounters, not runs. This document is context for pacing and for the decisions
that must not be foreclosed. See `../prototype/SCOPE.md` for what is actually being built.

---

## Resolved product identity

> **21 Bastion is a blackjack tower defense with a siege-shaped run.**

The encounter remains the reason to play. The campaign layer exists to create context, consequences, and
the next battlefield problem — **not to become a second strategy game larger than combat.**

Conceptual target: roughly **70% encounter play, 30% campaign decisions**. That ratio is a pacing target
settled by playtest, **not a timer to enforce**.

> Standard roguelite progression asks: what did I collect? 21 Bastion should ask: **what did I hold, what
> did I give up, and what kind of battlefield did those choices leave me?**

---

## The three run pillars

| Pillar | What changes | What it must not become |
|---|---|---|
| **Shoe** | Rank distribution, native family identity, one modifier per card, deliberate Acquire / Cut / Rerank choices | A linear deck-strength score |
| **Doctrine** | A small set of behavior-changing global rules built over time | Twenty passive percentage relics |
| **Siege State** | Time, front condition, geography, Dealer recruitment, concessions, pressure | A punishment spiral or a procedural map maze |

Each pillar's failure column is the thing to check a proposal against, not the aspiration column.

---

## Standing run-layer constraints

These sit alongside the encounter-level invariants in `../../CLAUDE.md` and bind every campaign mechanic.

1. **Encounter first.** Every campaign mechanic must create a more interesting *next encounter*, or be cut.
2. **Rank count is sacred.** The game may change a card's character, history, family, modifier, or
   availability — but **enemy pressure never silently alters blackjack rank distribution.** This is what
   forces the Reserve-copy rule in `08-deck-economy-progression.md` § Exhaustion.
3. **One system per job.** Unchanged from the encounter layer; a duplicating campaign mechanism means one
   of the two is deleted.
4. **Counter the build, never the player.** Dealer adaptation responds to what the player *built*. Never
   to win rate, health, loss streaks, or any hidden difficulty governor. See `06-dealer-and-enemies.md`
   § Public recruitment.
5. **Pressure creates different problems, never punishment spirals.** Neglect and loss *reshape* the
   siege; they do not recursively make every later decision worse.
6. **One ordinary defeat condition.** Territory and time change position. They are not hidden health bars.
7. **Currencies have non-overlapping jobs.** Time buys ordinary campaign actions; Favor buys rare
   permission to bend an encounter rule; Bastion Health measures survival. **Chips are cut.**
8. **Costs are exact; core consequences are legible.** Secondary payoff may be uncertain. The player must
   always know what they are definitely spending and the *minimum* result they are buying.
9. **Reveal consequences, not conclusions** — at campaign scale too. The siege map shows possible
   transformations and known costs, and never says which order is optimal (`09-information-and-ui.md`).
10. **No compounding multipliers across encounters.** Towers reset at encounter boundaries. Geography and
    card identity may persist **because they do not carry Formation Strength forward.**

---

## Shape: three phases of one siege, not three regions

The run is **one military situation** evolving over roughly **thirty to forty-five minutes**. The player
does not travel through disconnected rooms or biomes; they defend named districts around one Bastion while
the Dealer recruits, fronts advance, terrain changes, and time is spent.

**Revision 7.1's three regions become three phases of the same siege.**

| Phase | Campaign state | Player experience | Dealer state |
|---|---|---|---|
| **I — Encirclement** | Outer districts intact; generous geometry; first clock | Establish the shoe and early doctrine; learn which fronts matter | Generic recruitment; little adaptation |
| **II — Breach** | One or more fronts change state; concessions become meaningful | Exploit the build while it feels strong; choose what to protect | Public recruitment begins adapting, one beat behind |
| **III — Last Stand** | Scheduled final assault, **or triggered early** if every outer front is Lost or Conceded. Geography now reflects the run | Stress-test what was built; no further territorial retreat | Mature fixed-size opposing shoe, built from visible replacement decisions |

Geography and enemy composition evolve **continuously**. See `11-siege-geography.md`.

---

## Victory, defeat, and Last Stand

> **Bastion Health reaching zero is the only ordinary defeat condition.** Territory is position, not a
> second health bar. Time is an action clock, not a loss clock.

### Defeat

The run ends immediately when **Bastion Health reaches zero**. Health stays hard to restore so that
battlefield leakage carries campaign weight — the Bastion lane stake is the connective tissue between the
two scales.

**None of these directly end a run:**

- losing an outer district;
- voluntarily conceding an outer district;
- losing *every* outer district;
- reaching zero campaign hours in a phase.

### Time expiration

When a phase clock reaches zero, **the scheduled Dealer action or major assault occurs.** The player does
not lose because time expired. Time controls **when the enemy acts**, never whether the run continues.

### Last Stand

The campaign enters **Last Stand** when either:

1. all outer defensive fronts are Lost or Conceded; **or**
2. the final siege phase reaches its scheduled Bastion assault.

**Entering Last Stand is not defeat.** It is the removal of further strategic retreat: Dealer recruitment
locks, no more outer concessions are possible, and the final Bastion battlefield is assembled from the
geography, doctrine, shoe state, Dealer shoe, Favor, and Bastion Health the run produced.

### Victory

**Survive and defeat the final Dealer assault at the Bastion with Bastion Health above zero.**

> This rule is **load-bearing for concession.** A player must be able to abandon every outer district and
> still have a path to victory, however difficult the resulting Last Stand becomes. A concession system
> whose logical conclusion is an unwinnable run is not a choice.

---

## Cadence

**After most encounters, the player issues exactly one strategic order.** There is no mandatory reward
screen followed by a map followed by a shop. The order *is* the progression decision, and it often
determines where the next encounter happens. Full order table in `12-campaign-time-and-orders.md`.

Target: **thirty to sixty seconds** per command phase, because the player sees one decision surface rather
than a stack of menus. Charters and rare major events may breathe longer.

> If the default between-encounter flow requires choosing a reward, shopping, promoting a card, and routing
> separately, **the structure has failed.**

---

## Time budget

⚠ **Revised against the run layer and not yet re-tuned.** Revision 7.1's budget allocated 6–9 min to
"rewards and deck decisions" and 4–6 min to "shops, events, routing"; both line items are absorbed by the
single strategic order, and neither survives in its old size.

| Activity | Budget | Note |
|---|---:|---|
| Hand decisions and placement | 14–19 min | Unchanged. Still the largest single block. |
| Combat resolution | 6–9 min | Unchanged. |
| **Strategic orders** | **6–10 min** | Replaces rewards + shops + routing. ~30–60 s per beat. |
| Charters, phase transitions, boss presentation | 2–4 min | The beats permitted to breathe. |
| **Total** | **30–45 min** | |

That lands campaign decisions at roughly a **quarter to a third** of the run, which is the 70/30 target.

If combat resolution grows past its budget, the game is drifting toward a watching experience — a named
risk. A regular wave should resolve in **12–20 seconds** (`01-core-loop.md`).

> ⚠ **The encounter budget is not restated by the run layer.** Revision 7.1's "twelve combat encounters,
> twenty-seven waves" has neither been confirmed nor superseded against the three-phase structure. Treat it
> as an un-reconciled first pass — see `../reference/tuning-constants.md` § Known Discrepancies.

---

## Escalation

Escalation now comes from **siege state** rather than from a region's content tier. The same three
escalation ideas survive, re-hung on the phases:

**Phase I — Encirclement.** Two lanes, three sockets, standard march curve, mixed lane stakes. Generous
geometry, because the player is still learning which fronts matter.

**Phase II — Breach.** Front-state changes bite: altered socket layouts, shortened or split paths, lost
services. Enemies that destroy or displace towers. Native-suit synergies strong enough that off-suit
genuinely costs. Concession becomes a live option.

**Phase III — Last Stand.** The final geometry *is* the run's history — a blown bridge, a scuttled works,
an abandoned ward. Dealers who alter card access. Simultaneous fronts. No further retreat.

> **Escalation must change how the player thinks, not how long they watch.**

Higher-health enemies and longer waves are the failure mode here, not the goal. Note that the run layer
supplies a *better* escalation lever than either: the Dealer's opposing shoe matures through visible
one-for-one replacements the player watched happen and chose not to raid.

---

## Run memory

A successful run should be recalled **geographically and strategically**: the eastern ward was abandoned,
the north bridge was blown, the Dealer's King recruitment was raided, and a Veteran 4 held the River Works.

Build identity still matters — but it should live *inside* the story of the siege rather than replace it.

> A memorable run should not be a list of relics. It should be a siege history: which district held, which
> bridge was destroyed, which Dealer recruitment was stopped, which card became a veteran, and which
> position the player deliberately abandoned so the Bastion could survive.

---

## Modes

| Mode | Description |
|---|---|
| **Standard Run** | The default siege. |
| **Daily Deal** | Fixed seed. |
| **Endless Siege** | Survival. Note that the ordinary defeat condition already supplies its end state. |
| **House Rules** | A menu selected before a run: Dealer hits soft 17, towers do not persist, native deployment only, minimum four cards per hand, doubled march curve, families reassignable. |
| **Challenge Contracts** | Handcrafted scenarios — a natural home for authored siege states. |

**House Rules is worth noting early for architecture reasons.** Every entry in that menu is a toggle on a
rule the prototype hardcodes — Dealer draw policy, persistence, off-suit deployment, minimum hand length,
march curve scale, family locking. Building those as configurable rule flags from the start costs little
and makes House Rules, the validation test arms (`../prototype/VALIDATION.md`), and **the rank-stacking
flag** (`05-battlefield.md` § Rank stacking) nearly free.

---

## Final run identity

At **hand** scale, the player asks whether another card is worth the ground it costs.
At **lane** scale, what can be left uncovered.
At **campaign** scale, what can be left undefended.

The same command principle repeats at three magnifications **without sharing a single hidden score.** Time
determines when the enemy acts, territory determines the position fought from, Favor permits rare
exceptions, and Bastion Health alone determines whether the run is still alive.

> **North-star sentence: Build the defense you need, spend only the ground you can afford, and decide what
> you are willing to lose.**
