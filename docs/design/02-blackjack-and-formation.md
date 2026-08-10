# Blackjack System, Formation Strength, and the Power Curve

Source: Handoff Revision 7.1, §§ 5, 6, 8.

---

## Blackjack rules

Baseline rules remain **recognizable blackjack**. Number cards use their printed value, face cards count
as 10, Aces are 1 or 11.

> Choosing a defense family never changes blackjack value or shoe composition.

The two layers are orthogonal at the arithmetic level. This is deliberate — the tension comes from the
player combining them, not from the systems entangling.

| Action | Effect |
|---|---|
| **Hit** | Advance the march, *then* draw. Place the card, replacing an existing tower if sockets are full. |
| **Stand** | End drawing. The Dealer resolves and deploys in full, then the adjustment window opens. |
| **Split, Double Down** | Post-prototype. Intent only; no numbers, no implementation contract. |
| **Surrender, Insurance** | **Cut permanently.** |

---

## Formation Strength

The hand's final total sets a formation-wide multiplier applied to every tower placed this hand.

| Final Total | Formation Strength |
|---|---:|
| 21 (any card count) | ×1.60 |
| 20 | ×1.50 |
| 19 | ×1.40 |
| 18 | ×1.30 |
| 17 | ×1.20 |
| 16 | ×1.15 |
| 15 | ×1.10 |
| 14 | ×1.05 |
| 13 | ×1.00 |
| 12 | ×0.95 |
| 11 or below | ×0.90 |
| **Bust** | **×0.80** |

The curve spans 2.0×, against run links that can add roughly 35% to a well-placed board and an engagement
range spanning 17.0 down to 5.0. **The hand matters most on any single card; the board and the clock
matter more across a full formation.** That balance is the Placement Must Rival the Hand pillar stated
qualitatively — it is not a computation. Balance is measured through the resolver, never by multiplying
these factors together (`03-march-clock.md` § Total engagement is explanatory).

### Perfect Formation

Exactly 21 pulls the army back 3.0 units. **That is the entire bonus.** There is no separate multiplier
bump, attack-speed bonus, or card-count table. See `03-march-clock.md`.

### Wide Formation — deleted

Revision 6 granted +10% attack speed per card beyond the third. Against a march that cost 17–28% of
engagement at those same card counts, it was very nearly an exact refund — the two systems were fighting
over identical hands, at precisely the length where the march was supposed to bite.

Many-card decks now earn their keep through board width, run links, and the 21 pullback. **If that proves
insufficient, the fix is the march curve, not a new bonus.**

### Standing low

Standing below 17 is legal and costs real output. It is correct when:

- the formation already answers the wave,
- sockets are full and the forced replacement is bad,
- the march step would cost more than the marginal tower gains, or
- a relic rewards low totals.

---

## Card power curve

Tower power is **sublinear** in card value, approximately `value^0.7`.

| Value | A(1) | 2 | 3 | 4 | 5 | 6 | 7 | 8 | 9 | 10/J/Q/K | A(11) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Base power | 1.0 | 1.6 | 2.2 | 2.6 | 3.1 | 3.5 | 3.9 | 4.3 | 4.7 | 5.0 | 5.4 |

> A ten is five times the blackjack value of a two but only three times the tower power.

This is what makes low cards worth placing rather than purely worth avoiding, and it is the arithmetic
foundation of the thinning dilemma (`08-deck-economy-progression.md`).

### Output landmarks

Raw output before links and engagement, first-pass:

| Hand | Calculation | Output | Entry |
|---|---|---:|---:|
| Natural A + K, plus Ace Bastion | (5.4 + 5.0 + 5.0) × 1.60 | 24.64 | 0.0 |
| 2 + 3 + 4 + 5 + 7 = 21 | 13.4 × 1.60 | 21.44 | 4.5 |
| 6 + 7 + 8 = 21 | 11.7 × 1.60 | 18.72 | 0.0 |
| 2 + 4 + 6 + 8 = 20 | 12.0 × 1.50 | 18.00 | 4.0 |
| K + Q = 20 | 10.0 × 1.50 | 15.00 | 0.0 |
| 6 + 8 + 4 = 18 | 10.4 × 1.30 | 13.52 | 1.5 |
| 10 + 6 = 16 | 8.5 × 1.15 | 9.78 | 0.0 |
| 3 + 3 + 5 + 5 = 16 | 10.6 × 1.15 | 12.19 | 4.0 |

**The last pair is the design's signature comparison.** Two hands totaling sixteen: one with 25% more raw
output, the other with **38%** more engagement (18.0 against 13.0). They should never play the same, and
now they fail differently too. This comparison appears in the scripted validation battery — see
`../prototype/VALIDATION.md`.

> ⚠ **These are raw-output landmarks only. Do not multiply them by an engagement fraction to estimate
> board effectiveness** — see `03-march-clock.md` § Total engagement is explanatory, not a balance number.
> Entry position is listed beside each hand as **context for reading resolver output**, not as a factor to
> apply.

### Natural blackjack — the Ace Bastion

Natural blackjack grants the **Ace Bastion**: a free 5.0-power King-class anchor that does **not** count
as a hand card and **shares the hand's multiplier**.
