# Bust and Overload

Source: Handoff Revision 7.1, § 13.

---

## What happens on bust

- The busting card is **destroyed and never placed**.
- Formation Strength for **this hand's towers** drops to **×0.80**.
- **Persisted towers from earlier waves are unaffected** — they are already at ×1.00.
- **The Dealer resolves in full.** Hidden card deploys, draws to 17, everything arrives.
- **Overload fires. Placement locks. The wave resolves.**

Note what is absent: there is no adjustment window on bust. The march step already paid is **not**
refunded.

Because persisted towers sit at ×1.00, **bust is scoped to the current hand** — a deliberate consequence
of the persistence rule in `05-battlefield.md`. Revision 6's locked multipliers meant a bust could drag six
towers down instead of three, which pushed late waves toward automatic stands.

---

## Overload

> **The busting card deals immediate damage equal to its base power. It does not scale with the amount by
> which the hand exceeded 21.**

Revision 6 scaled Overload with excess, which made busting at 28 strictly better than busting at 22 and
rewarded blowing out harder. **Capping at base power keeps bust productive without making it a strategy.**

Base power comes from the card power curve in `02-blackjack-and-formation.md` — e.g. a busting King deals
5.0, a busting 4 deals 2.6.

### Which lane it strikes

> **Overload strikes the lane with the highest current Visible Threat. Ties break toward the
> Bastion-staked lane.**

Revision 7.1 said "the lane where it was provisionally placed." **That phrasing was a bug, not a UI
problem** — it assumed a placement that never happens. On a bust the card is destroyed and **never placed
at all.**

Carrying a provisional target through the draw would mean **taxing every single hit with a declaration
click** to serve an outcome that fires maybe once in five hands, and it would leak the player's intent
into the interface.

The rule above is deterministic, adds **no new UI during the draw**, carries no provisional state, and is
**unsteerable** — the player cannot angle a bust into the lane they wanted. That last property is correct
given how much of this revision has been about stripping bust's bundled upside.

### Showing it

Show it as a **consequence, not a conclusion.** The hand panel's bust branch reads:

> **Bust → Overload: Lane 1**, alongside the ×0.80.

The player knows where it lands **before they hit**, which is exactly the information contract the pillars
ask for (`09-information-and-ui.md`).

---

## The shape this creates

> Bust now has **exactly one axis: your own penalty.** It costs a card, a multiplier, and the march step
> already paid, and it returns a single burst.

It is **clearly bad, occasionally the least bad option, and never a play you angle for.**

That is the acceptance criterion. If playtesting shows players angling for busts, Overload is too strong.
If bust never feels survivable, it is too weak. See `../prototype/VALIDATION.md` § Success Criteria:
*"Bust feels bad, occasionally correct, and never desirable."*
