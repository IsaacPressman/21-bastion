# Pillars and Identity

Source: Handoff Revision 7.1, §§ 0–2 and Final Gameplay Identity.

---

## High concept

**21 Bastion** is a roguelite tower-defense game in which the player builds each wave's defenses by
playing blackjack.

Every drawn value becomes a physical defense. The player chooses what each card becomes and where it
stands, and lives with that choice. The hand's total sets formation-wide power. The Dealer's hand is the
army walking toward you.

**Core promise:** Build the perfect defense without going over 21.

**Central tension:** Every card is another defense — but the army is already marching, your sockets are
already full, and what you commit during the draw is what you fight with.

---

## The narrowed claim

Revision 6 claimed that a draw at total 8 could be made tense. **That claim is withdrawn.**

The March Clock was sized against path length rather than socket spacing, so a single step shaved one
tower's engagement window and left the other two untouched — a 5.6% cost against a third tower worth
roughly 50% of board power. Correcting the step size to bite at low totals makes five-card hands
unplayable. That window is very narrow, and building inside it was a mistake.

> Decision density comes from what a card becomes, where it goes, and what it displaces. Hit/stand is a
> live decision in the 14–19 band — roughly where blackjack has always put it — and the design's job is
> to make that band's stakes battlefield-specific, not to manufacture tension at 8.

This is a smaller claim than Revision 6 made. It is also one the prototype can actually test.

The March Clock is retained, reshaped, and given a different job: it prices *hand length* on an escalating
curve so that the fifth card is a deliberate, dangerous, precision play rather than a default. It is no
longer asked to make low totals tense.

---

## Design pillars

### Blackjack Builds the Battlefield

Every blackjack decision must directly change tower placement, defensive power, or enemy position.
Blackjack is never a separate minigame.

### Commitments Are Made Under Uncertainty

What a card becomes is decided when it is drawn, before the wave is fully known. Information arriving
later permits adjustment, not re-solving. A player who defers every decision until everything is revealed
is not playing the game.

### Reveal Consequences, Not Conclusions

Hand consequences and battlefield consequences are shown separately. The game never displays a combined
recommendation, an optimal-play percentage, or a green/red verdict. Combining them is the player's job.

### Placement Must Rival the Hand

If the hand multiplier swings output more than every placement decision combined, the tower-defense layer
is decoration. Socket scarcity, run links, forced replacement, and lane triage exist to keep the two
layers comparable.

### One System Per Job

Revision 6 had three systems keeping many-card decks viable and two making drawing costly, and they
fought. Each pressure in this design has exactly one mechanism behind it. **When a mechanism is added,
the one it duplicates is removed.**

### Randomness Creates Adaptation

Bad draws create difficult decisions, not automatic losses. A poor card always has a use, even an
inefficient one.

---

## Final gameplay identity

21 Bastion is not a blackjack game with tower-defense animations, and not a tower-defense game with cards
as a purchase menu.

Its identity is the moment a card lands face-up and the player has to decide, right then, what it becomes
and what it displaces — before the army is fully known, and knowing they cannot take it back.

The hand shows what another card could be. The field shows what another card would cost: in ground, in a
socket, in a tower you would have to tear down. The Dealer's army is standing there the whole time and
grows every time they draw. **Nothing on screen adds those together.**

> A version that hides the consequences is unfair. A version that shows the answer is solved. A version
> where every decision can be revised after the reveal has no decisions in it at all. Revision 7 is aimed
> at the space between those three.

---

## A standing note on numbers

Every number in this design is first-pass and expected to be wrong. No number carries a confidence
interval, validity window, or tolerance. Those are outputs of playtesting.
