# Siege Geography, Fronts, and Concession

Source: **Run Layer Handoff (consolidated)**, § 3.

Full-game intent. Nothing here is prototype scope — see `../prototype/SCOPE.md`. It is written down now
because two of its rules constrain the encounter layer, and both are cheap to honor early and expensive to
retrofit:

- **Geography persists across encounters; towers do not.** Persistence remains scoped to the waves of an
  encounter (`05-battlefield.md` § Persistence).
- **A battlefield is assembled from front state**, so the encounter must be able to take its path length,
  socket layout, lane stakes, and route structure as *inputs* rather than as constants.

---

## Persistent geography

> **DECIDED: the shape of the ground persists; towers do not persist across encounters.**

Geography is **authored, not procedural.** Fronts have stable identities so players can learn them, plan
around them, and remember how a run changed them. A procedurally generated map would make "the north
bridge was blown" a coordinate rather than a memory.

**First implementation target: three outer fronts plus the Bastion.**

| Front | Baseline identity | Example persistent changes |
|---|---|---|
| **North Gate** | Long approach; artillery-friendly; strategic bridge | Bridge intact / damaged / destroyed; path length shortened; rear socket exposed; Vault access lost |
| **River Works** | Infrastructure district; chokepoints; control families matter | Workshop active / evacuated / scuttled; route split disabled; socket layout altered |
| **East Ward** | Commercial / civilian district; fast routes; economic stakes | Market open / looted / abandoned; alternate approach opened; Vault stake converted to Bastion pressure |
| **The Bastion** | Inner defense and final stand | Becomes exposed as outer districts fail; final geometry reflects the run |

Note what the example changes actually are: **path length, socket layout, route structure, and lane
stakes.** Every one is an input the resolver already consumes. None of them is a modifier applied on top of
a wave, and none carries Formation Strength forward.

> The North Gate's "artillery-friendly" identity and the River Works' "control families matter" are the
> campaign layer's route back into family choice. A front that rewards Clubs makes the family lock
> (`04-cards-as-defenses.md`) a campaign-scale decision without touching the lock rule itself.

---

## Front state model

Each outer district uses a **small authored state ladder**, not procedural terrain. Every front is always
in exactly one of four campaign states:

| State | Meaning | Run consequence |
|---|---|---|
| **Held** | Under player control; normal services and geometry available | Full strategic access |
| **Compromised** | Still defensible, but geography, stakes, or services have worsened | A harder or *different* encounter, without removing the front |
| **Lost** | The Dealer took the district through pressure or failed defense | Leaves normal routing; applies its authored **loss** consequence |
| **Conceded** | The player deliberately abandoned or scuttled the district | Leaves normal routing; the player receives the declared **scuttle benefit** |

**Lost and Conceded are both terminal for ordinary routing, and they are not equivalent.** A Lost district
applies the enemy-favored authored consequence. A Conceded district applies a known compensating effect the
player chose. **Neither directly causes defeat** (`10-run-structure.md` § Victory, defeat, and Last Stand).

That distinction is the whole reason concession is a mechanic rather than a failure state. If Lost and
Conceded resolved the same way, conceding would only ever be a way to lose faster.

### Neglect

Neglect transformations come from a **bounded authored table of six to eight defined outcomes.**

> **The possible outcomes are shown before the player commits time elsewhere.** The player may be uncertain
> which secondary payoff occurs, but must understand the guaranteed state change they are risking.

This is the campaign-scale form of *costs are exact, secondary payoff may be uncertain*. It is also why the
table is bounded and authored: an unbounded neglect table is a punishment spiral wearing content.

---

## Concession

> **DECIDED: the player may intentionally abandon or destroy territory. Concession must sometimes be
> strategically correct, not merely less bad.**

A concession trades one resource or service for a **structural advantage elsewhere.**

| Concession | Certain cost | Certain benefit |
|---|---|---|
| **Blow the bridge** | Lose the long outer approach and its future services | Remove a Dealer reinforcement route, or collapse two approaches into one |
| **Evacuate the Vault** | Forfeit some future economy | Protect remaining reward from raids; shorten the defense obligation |
| **Scuttle the Works** | Lose a Temper / repair service | Deny enemy access and change path geometry in the player's favor |
| **Fall back** | Lose an outer district state | Preserve time, card readiness, or a critical inner defense |

Both columns are **certain**. That is the design rule, not table formatting: a concession whose benefit is
a probability is a gamble, and gambling already has a home in this game.

Conceding a district **never directly ends the run.** Its value comes from deliberately trading geography
or services for **time, denial, path simplification, or protection of a more important position.**

### Why concession has to be able to win

A player must be able to concede **every** outer district and still reach a winnable Last Stand
(`10-run-structure.md`). If total concession is a guaranteed loss, then concession is not a strategy — it
is a slower defeat, and every player learns to treat it as one after a single run.

---

## What this asks of the encounter layer

Nothing yet. But two things must stay true so the campaign layer can attach later without a rewrite:

1. **The encounter's geometry is data, not constants.** Path length, socket positions, socket count, lane
   count, and lane stakes already live in `data/tuning.json` and are read through one derivation each
   (`../reference/tuning-constants.md`). Keep it that way — a front state is a geometry override.
2. **Lane stakes are per-encounter inputs**, already true (`05-battlefield.md` § Lane stakes). "East Ward's
   Vault stake converted to Bastion pressure" is a front state rewriting a stake assignment, which the
   encounter layer supports today.

> An encounter that reads any of these from a hardcoded value has quietly made front states impossible.
> That is the one way the prototype can foreclose the run layer, and it is already prohibited by hard
> invariant 10 in `../../CLAUDE.md`.
