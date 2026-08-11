# Doctrine and Charters

Source: **Run Layer Handoff (consolidated)**, § 8.

Full-game intent. **Doctrine replaces the relic layer as the persistent placement-layer progression** —
see § What happened to relics below, which is the part most likely to be misremembered.

---

## Doctrine

**Doctrine is the persistent placement-layer progression.** Target **four to seven behavior-changing
globals** by the end of a run, with **no expectation that every run reaches the maximum.**

Doctrine pieces are **built over one or two encounters.** The old fortification-project concept becomes the
**delivery mechanism** for doctrine rather than a separate progression track — one system per job.

| Example doctrine | Decision it changes |
|---|---|
| **Cross-Lane Triggers** | Spades may trigger from an adjacent lane; changes socket valuation |
| **Junction Network** | The junction counts as adjacent to every middle socket — **deliberately breaks baseline run topology** |
| **Field Reassignment** | The first card placed in each lane may be reassigned after Dealer reveal; a bounded escape from family lock |
| **Watchtower** | Reveals additional Dealer or lane information before commitment |
| **Machine Shop** | Expands Temper access and modifier choice quality |

Read that table as a specification of *shape*: every entry changes **a rule about how placement works**, and
not one of them is a percentage. "Twenty passive percentage relics" is the named failure mode
(`10-run-structure.md` § The three run pillars).

### Two entries worth reading carefully

**Junction Network** deliberately breaks the run-island rule in `04-cards-as-defenses.md` § Adjacency — the
rule that the junction is adjacent to neither lane, which exists precisely so the junction cannot become a
run *hub* and the auto-best socket. Making that reversal a **doctrine the player earns and can see** is the
sanctioned form. It is not license to soften the baseline rule.

**Field Reassignment** is the escape hatch for family locking that the risk register calls for
(`../prototype/RISKS-AND-ADDBACKS.md` § Locking family is too punishing) — *limited*, not a reopened
window. It is bounded twice over: **first card per lane only**, and **only after the Dealer reveal**. This
is the doctrine form of what Revision 7.1 called the Field Promotion relic.

> Doctrine count is an open question: **how many pieces can coexist before the encounter UI becomes
> unreadable?** Every doctrine is a rule the player must hold in mind while reading two separate
> consequence panels (`09-information-and-ui.md`). Four to seven is a first pass, not a budget cleared by
> anything.

---

## Charters

**Two major Charters per run**, normally after the first two siege phases.

> **A Charter changes a rule of the run** rather than adding a passive percentage.

Charters are the beat allowed to breathe past the thirty-to-sixty-second cadence target
(`12-campaign-time-and-orders.md`).

### What is explicitly not a Charter

> The **Last Line geometry principle is not a Charter.** Changing preferred socket depth belongs in
> **baseline geometry**, because it is a **fix for deep-placement dominance**, not a rare reward.

This is already settled in the prototype and settled the same way: deep-placement dominance was measured,
confirmed in all three arms, and remedied in **baseline geometry** by varying range with socket depth
(4.0 / 3.0 / 2.0, forward to rear) — `../ROADMAP.md` § Open Decision 2.

The general principle generalizes past this one case: **a fix for a dominant strategy must not ship as a
reward.** A reward that fixes a flaw makes the flaw the default experience for every run that does not roll
the reward.

---

## What happened to relics

Revision 7.1 § 16 listed eight named relics. **They are not deleted; the relic *layer* is.** The run layer
rejects "twenty passive percentage relics" as the shape of persistent progression and routes those
functions into doctrine, Charters, and campaign services instead.

Mapping the 7.1 list forward, as intent rather than specification:

| 7.1 relic | Effect | Where it lives now |
|---|---|---|
| **Field Promotion** | One family reassignment per encounter | **Doctrine** — Field Reassignment (bounded further) |
| **Watchtower**-shaped: **Card Counter** | Reveals a band for the Dealer's hidden card | **Doctrine** — Watchtower, or a Reconnoiter payoff |
| **Surveyor** | Adds one socket to each lane | **Charter or front geography.** See the note below |
| **Bridge Builder** | One card counts as wild in runs | Doctrine — a run-topology rule, alongside Junction Network |
| **True Colors** | One off-suit card counts as native per wave | Doctrine, once printed native suits exist |
| **Soft Landing** | One Ace-state intervention per encounter | **Favor spend** — the handoff names this exact effect |
| **Long Road** | Reduces the march curve for one encounter | ⚠ **Suspect.** See below |
| **Steady Table** | The first bust of a region does not destroy the card | Reframe per *phase*; regions no longer exist |

**Surveyor is load-bearing and must not be lost in the translation.** Adding a socket per lane is what makes
the **4-run reachable**, and the 4-run tier is currently absent from the prototype for geometric reasons
(`04-cards-as-defenses.md` § The 4-run is unreachable). Whatever grants extra sockets — Charter, doctrine,
or a front's geography — **unlocks a link tier**, and `TuningLoader` fails the load if socket count and run
lengths disagree. That guard is the thing that keeps the translation honest.

⚠ **Long Road is suspect under the run layer.** "Reduces the march curve for one encounter" is a passive
percentage on the design's most load-bearing curve, and the march curve is the encounter's own pressure
system. If a campaign effect can soften it, campaign time is feeding the March Clock — which
`12-campaign-time-and-orders.md` explicitly forbids. **Do not carry it forward without re-deciding it.**

**Extra adjustment moves remain a reward, not a baseline.** `05-battlefield.md` § If one move proves too
tight says the expansion path is adjustment points granted by relics and commanders. Under the run layer,
read that as **doctrine and Charters**. The rule it protects is unchanged: **the baseline does not widen.**

---

## Commanders

Unchanged in intent from `08-deck-economy-progression.md`: each commander has a starting shoe, a passive,
and a **distinct decision texture**, and **at most one launch commander may alter the Formation Strength
curve.**

The run layer gives commanders a second differentiation axis that did not exist before: **which front they
start Held**, and which doctrine projects they can begin early.

> A commander is a skin only if it produces the same decisions on the same battlefield. The siege layer
> makes "the same battlefield" a much harder condition to accidentally satisfy.
