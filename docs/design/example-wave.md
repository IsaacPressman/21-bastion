# Worked Example: A Full Wave

Source: Handoff Revision 7.1, § 18.

**Use this as an end-to-end acceptance test.** It exercises the march clock, run links, family locking,
lane stakes, the single-move adjustment window, Dealer resolution on bust, and both forecast types in one
pass. If the implementation reproduces every number below, the core systems agree with the design.

---

## Setup

- **Lane one — Bastion stakes.** Three armored soldiers.
- **Lane two — Vault stakes.** A fast reinforcement package; undefended it forecasts **6 damage**.
- **Dealer's Vanguard: a 10** — an armored soldier already standing at the head of lane one.

## Opening deal

Player is dealt **6** and **8**. Total **14, ×1.05**. Entry **0.0**, full **18.0** engagement.

Both committed as **Clubs — permanently** — at lane one's sockets **6** and **9**. No run: 6 and 8 are not
consecutive.

Lane two's **Visible Threat** reads **Open, 6.0** — *what the revealed force would do, not a promise about
the wave.*

## Third card

They hit. **Entry advances to 1.5; engagement drops to 16.5.** The step is paid before the card is seen.

The card is a **4**, reaching hard **18, ×1.30**. Committed as a **Spade** at lane two's socket **3**.

## The decision

Two panels now say different things.

**Hand panel:** Ace, 2, and 3 survive. A **3 reaches 21** and pulls the whole army back 3.0 units, back to
entry 0 and full engagement. A **2 reaches 20**. The rank display shows **six safe cards in a pile of
twenty-two**.¹ Everything else busts to ×0.80.

**Battlefield panel:** lane two's **Visible Threat** is **Open, 3.8** — *against the revealed force only.*
A 5 would form a 4–5 run at lane two's next socket, but a 5 busts. The next march step costs **2.5** units
— entry 4.0, engagement 13.0, **a 21% drop from here** — and it lands before the card is seen.

They weigh it: six safe cards out of twenty-two, a Vault lane worth reward rather than health, an armored
Vanguard in the lethal lane that the two Clubs are already handling, and a fourth card that costs a fifth
of their remaining engagement whether it helps or not.

**They stand.**

## Dealer resolution

The Dealer reveals a **6** — fast raiders — and draws a **7**, adding more. Total **23; the Dealer busts,
but that no longer matters. Their entire hand deploys anyway.**

Lane two's **Final Forecast** — against the now-complete army, before any adjustment — reads **Open, 5.1**.

## Adjustment window — one move

Families are locked, so the lone Spade in lane two stays a Spade.

**The player wants two things and can have exactly one:**

- the Spade forward, to catch the raiders earlier
- the socket-9 Club moved to the junction, to cover both lanes

**They take the Spade**, because a trap that fires late is worth nothing. They also set a **Hold** order so
it waits for the group rather than triggering on the lead scout — standing orders are free and do not
consume the move.

The **Final Forecast** updates after the adjustment: lane two now reads **Open, 3.4**. This is the
combat contract — the number combat must reproduce.

## Combat

Resolves in **fourteen seconds**. Lane one holds. Lane two leaks **3.4, exactly as the Final Forecast
said** — a chunk of the Vault, no Bastion damage.

---

## The counterfactual

> Had they hit and busted on a King: entry would have advanced to 4.0, Overload would have dealt 5.0 to
> lane two, the formation would have run at ×0.80, and the Dealer would have deployed **exactly the same
> army**. Worse in every direction, with one burst of consolation.

Lane two because it carried the **highest Visible Threat** — not because the King was aimed there. The
busting card is destroyed and never placed, so it inherits no lane and the player cannot steer it. The
hand panel showed **"Bust → Overload: Lane 2"** before they chose, alongside the ×0.80.

This is the shape bust is meant to have — see `07-bust-and-overload.md`.

---

## What this example demonstrates

| Element | Where it shows |
|---|---|
| March step paid before reveal | Third card: entry moves, *then* the 4 appears |
| Escalating cost | +1.5 then +2.5, framed as "21% drop from here" |
| Family locked | Spade stays a Spade through the adjustment window |
| Run links driving placement | The 4–5 run the player can see but cannot safely reach (sockets 3 and 6 are adjacent) |
| Overload is unsteerable | Counterfactual lands on lane two by threat, not by intent |
| Marked ranks, not percentages | "six safe cards in a pile of twenty-two" |
| Lane stakes driving triage | Vault leak accepted to protect the Bastion lane |
| Dealer resolves regardless | Dealer busts at 23; army deploys in full |
| **Visible Threat ≠ Final Forecast** | 6.0 → 3.8 during the draw, *then* 5.1 after Dealer resolution |
| **One move, not one per tower** | Two wanted moves, one taken, stated as a cost |
| Standing orders are free | Hold set *in addition to* the move |
| Final Forecast updates after adjustment | 5.1 before the move → 3.4 after, and combat matches 3.4 exactly |
| Separate panels | Hand and battlefield state the case; the player combines them |

Note especially the forecast sequence: **the number changes from 3.8 to 5.1 and this is not a broken
promise**, because the two figures answer different questions. Revision 7 showed the same movement while
calling both "the forecast" — which is precisely how trust in a forecast is lost. See `05-battlefield.md`
§ Two Forecasts, Not One.

---

¹ **Minor discrepancy, carried from Revision 7 and not corrected in 7.1.** With 3 player cards, 1 upcard,
and 1 hidden card removed, 21 cards remain in a 26-card shoe, not 22. The six safe cards (two each of A, 2,
3) are correct. Reproduce the *decision*, not the pile count. Logged in
`../reference/tuning-constants.md` § Known Discrepancies.
