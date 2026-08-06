# Run Structure

Source: Handoff Revision 7.1, § 19.

Full-game structure. The prototype implements encounters, not runs — this is context for pacing targets.

---

## Shape

**Three regions, 30–45 minutes.**

Each region:

- two regular encounters (two waves each)
- one elite (two waves)
- one Dealer boss (three waves)
- two or three noncombat nodes

**Twelve combat encounters, twenty-seven waves.**

---

## Time budget

| Activity | Budget |
|---|---:|
| Hand decisions and placement | 14–19 min |
| Combat resolution | 6–9 min |
| Rewards and deck decisions | 6–9 min |
| Shops, events, routing | 4–6 min |
| Transitions and boss presentation | 2–4 min |
| **Total** | **30–45 min** |

The largest single block is **hand decisions and placement**. If combat resolution grows past its budget,
the game is drifting toward a watching experience — a named risk. A regular wave should resolve in
**12–20 seconds** at normal speed (`01-core-loop.md`).

---

## Escalation

**Region 1 — Foundation.** Two lanes, three sockets, standard march curve, mixed lane stakes.

**Region 2 — Pressure.** A third lane in some encounters. Enemies that destroy or displace towers.
Shifting sockets. Split. Native-suit synergies strong enough that off-suit genuinely costs.

**Region 3 — Distortion.** Linked hands and simultaneous fronts. Dealers who alter card access.
Destructible terrain that invalidates prior placement. Optional altered-threshold packages.

> **Escalation must change how the player thinks, not how long they watch.**

Higher-health enemies and longer waves are the failure mode here, not the goal.

---

## Modes

| Mode | Description |
|---|---|
| **Standard Run** | The default. |
| **Daily Deal** | Fixed seed. |
| **Endless Siege** | Survival. |
| **House Rules** | A menu selected before a run: Dealer hits soft 17, towers do not persist, native deployment only, minimum four cards per hand, doubled march curve, families reassignable. |
| **Challenge Contracts** | Handcrafted scenarios. |

**House Rules is worth noting early for architecture reasons.** Every entry in that menu is a toggle on a
rule the prototype hardcodes — Dealer draw policy, persistence, off-suit deployment, minimum hand length,
march curve scale, family locking. Building those as configurable rule flags from the start costs little
and makes both House Rules and the validation test arms (`../prototype/VALIDATION.md`) nearly free.
