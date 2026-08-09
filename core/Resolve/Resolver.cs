using Bastion.Core.Board;
using Bastion.Core.Config;

namespace Bastion.Core.Resolve;

/// <summary>
/// The single deterministic simulation that drives both forecast and wave.
/// </summary>
/// <remarks>
/// <para>
/// <b>There is exactly one resolver.</b> Forecasts are resolver runs; the visible wave is a
/// presentation of a resolver run. The failure this prevents is well known and hard to reverse: a
/// fast "estimate" path and an accurate "real" path that drift apart under maintenance until the
/// forecast quietly becomes a lie. The design sells the Final Forecast as exact - "if it says a
/// lane leaks two, the wave leaks two" - so that drift would not be a bug to fix later. It would
/// invalidate the game. See docs/ARCHITECTURE.md.
/// </para>
/// <para>
/// <b>Two entry points, two return types, no flag.</b> <see cref="ResolveRevealed"/> answers what
/// the currently revealed force would do. <see cref="ResolveComplete"/> answers what the wave will
/// do. Only the second is a contract.
/// </para>
/// <para>
/// <b>Combat is deterministic.</b> No critical hits, no misses, no random targeting, no wall-clock,
/// no frame-rate coupling. Same input, same output, every run.
/// </para>
/// </remarks>
public static class Resolver
{
    /// <summary>
    /// Resolves against the revealed force only: the base wave plus the Dealer's Vanguard.
    /// </summary>
    /// <remarks>
    /// Exact about a smaller question. The interface must say so plainly - a player who reads this
    /// as a promise will feel the game break it when the Dealer's reinforcements land.
    /// </remarks>
    public static VisibleThreat ResolveRevealed(
        TuningData tuning,
        EncounterTuning encounter,
        BoardState board,
        RevealedArmy army)
    {
        ArgumentNullException.ThrowIfNull(army);

        WaveResolution resolution = Run(tuning, encounter, board, army.Spawns);

        // No timeline. A Visible Threat describes a force, not a wave that will run, so there is
        // nothing here for combat playback to animate.
        return new VisibleThreat { Lanes = resolution.Lanes };
    }

    /// <summary>
    /// Resolves against the complete army. <b>This is the combat contract.</b>
    /// </summary>
    public static FinalForecast ResolveComplete(
        TuningData tuning,
        EncounterTuning encounter,
        BoardState board,
        CompleteArmy army,
        OverloadStrike? overload = null)
    {
        ArgumentNullException.ThrowIfNull(army);

        WaveResolution resolution = Run(tuning, encounter, board, army.Spawns, overload);

        return new FinalForecast
        {
            Lanes = resolution.Lanes,
            Timeline = new WaveTimeline
            {
                Events = resolution.Events,
                DurationSeconds = resolution.DurationSeconds,
            },
        };
    }

    /// <summary>
    /// The shared core. Both public entry points come through here; only the input army and the
    /// wrapper type differ.
    /// </summary>
    private static WaveResolution Run(
        TuningData tuning,
        EncounterTuning encounter,
        BoardState board,
        IReadOnlyList<EnemySpawn> spawns,
        OverloadStrike? overload = null)
    {
        ArgumentNullException.ThrowIfNull(tuning);
        ArgumentNullException.ThrowIfNull(encounter);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(spawns);

        // The board and the army each carry the entry point, and they must not disagree. Both are
        // downstream of the same March Clock reading, so a mismatch means one of them was built
        // against a different moment in the hand - which would resolve cleanly and answer the
        // wrong question, the one failure mode a forecast contract cannot survive.
        foreach (EnemySpawn spawn in spawns)
        {
            if (Math.Abs(spawn.StartPosition - board.Entry) > 1e-9)
            {
                throw new InvalidOperationException(
                    $"Spawn {spawn.SpawnIndex} enters at {spawn.StartPosition} but the board's entry point is {board.Entry}. " +
                    "The army and the board disagree about where the march has reached.");
            }
        }

        List<TimelineEvent> events = [];
        List<LaneOutcome> lanes = [];
        double duration = 0.0;

        for (int lane = 0; lane < tuning.Geometry.Lanes; lane++)
        {
            List<EnemySpawn> laneSpawns = [.. spawns
                .Where(s => s.LaneIndex == lane)
                .OrderBy(s => s.SpawnIndex)];

            IReadOnlyList<TowerState> towers = board.TowersFiringInto(lane);

            // The Overload burst, if this bust struck this lane. It goes into both the live run and
            // the empty-lane baseline: it is not a tower, so "damage prevented" must keep measuring
            // only what the towers did, not what the burst did.
            double burst = overload is { } strike && strike.LaneIndex == lane ? strike.Damage : 0.0;

            LaneRun live = SimulateLane(tuning, lane, towers, laneSpawns, burst);

            // The empty-lane baseline: the same army, this lane's towers removed, the other lane
            // untouched. Running it rather than summing leak damage keeps "damage prevented"
            // meaningful if a lane ever grows a hazard that is not a tower.
            LaneRun bare = SimulateLane(tuning, lane, [], laneSpawns, burst);

            lanes.Add(new LaneOutcome
            {
                LaneIndex = lane,
                Stake = encounter.LaneStakes[lane],
                EmptyLaneDamage = bare.LeakDamage,
                PredictedDamage = live.LeakDamage,
                LeakedUnits = live.Leaked,
                TowerActivity = live.Activity,
            });

            events.AddRange(live.Events);
            duration = Math.Max(duration, live.DurationSeconds);
        }

        // OrderBy is stable, so within a tick and lane the emission order survives - which is the
        // pinned phase order below, and therefore part of the determinism contract.
        return new WaveResolution
        {
            Events = [.. events.OrderBy(e => e.Tick).ThenBy(e => e.LaneIndex)],
            Lanes = lanes,
            DurationSeconds = duration,
        };
    }

    /// <summary>
    /// Simulates one lane, tick by tick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Lanes resolve independently.</b> Enemies never leave their lane and a junction tower
    /// fires into both at a reduced contribution each, so a lane's outcome depends on nothing
    /// outside it. See <see cref="BoardState.TowersFiringInto"/> for what would break that.
    /// </para>
    /// <para>
    /// <b>The phase order below is pinned and load-bearing.</b> Two towers that would each kill the
    /// same enemy produce different timelines depending which fires first, and an enemy in its
    /// final tick is either shot at or not depending whether firing precedes movement. Firing
    /// before moving is the choice: a tower gets its shot at an enemy that is about to leave. None
    /// of this is allowed to be incidental.
    /// </para>
    /// </remarks>
    private static LaneRun SimulateLane(
        TuningData tuning,
        int lane,
        IReadOnlyList<TowerState> towers,
        IReadOnlyList<EnemySpawn> spawns,
        double openingBurst = 0.0)
    {
        double dt = tuning.Sim.TickSeconds;
        double pathLength = tuning.Geometry.PathLength;
        int cooldownTicks = tuning.Sim.TicksIn(tuning.Towers.CooldownSeconds);
        bool undefended = towers.Count == 0;
        bool overloadSpent = openingBurst <= 0.0;

        List<TowerRuntime> guns = [.. towers.Select(t => new TowerRuntime(t, t.PositionOn(tuning), t.ShotDamageInLane(tuning)))];
        List<(int Tick, EnemySpawn Spawn)> schedule =
            [.. spawns.Select(s => (tuning.Sim.TicksIn(s.SpawnTime), s)).OrderBy(x => x.Item1).ThenBy(x => x.s.SpawnIndex)];

        List<LiveEnemy> active = [];
        List<TimelineEvent> events = [];
        List<LeakedUnit> leaked = [];

        int cursor = 0;
        int tick = 0;
        int leakDamage = 0;
        double lastEventTime = 0.0;

        // Every enemy either dies or reaches the end - the slow multiplier is bounded above zero
        // and does not stack, so nothing can be held in place. The cap is a guard against a future
        // change quietly making that untrue, and it fails loudly rather than hanging a playtest.
        int maxTicks = TickCeiling(tuning, schedule);

        while (cursor < schedule.Count || active.Count > 0)
        {
            if (tick > maxTicks)
            {
                throw new InvalidOperationException(
                    $"Lane {lane} did not resolve within {maxTicks} ticks. Something is holding enemies in place.");
            }

            double time = tick * dt;

            // Phase 1 - spawn, in spawn-index order.
            while (cursor < schedule.Count && schedule[cursor].Tick <= tick)
            {
                EnemySpawn spawn = schedule[cursor].Spawn;
                EnemyTuning type = tuning.Enemy(spawn.EnemyId);

                active.Add(new LiveEnemy
                {
                    SpawnIndex = spawn.SpawnIndex,
                    Type = type,
                    Source = spawn.Source,
                    Position = spawn.StartPosition,
                    Health = type.Health,
                });

                events.Add(new SpawnEvent
                {
                    Tick = tick,
                    Time = time,
                    LaneIndex = lane,
                    SpawnIndex = spawn.SpawnIndex,
                    EnemyId = spawn.EnemyId,
                    Source = spawn.Source,
                    Position = spawn.StartPosition,
                });

                lastEventTime = time;
                cursor++;
            }

            // Phase 1.5 - Overload, once, at the opening tick and before any tower fires. An
            // instantaneous strike at this lane on a bust, spending the busting card's base power on
            // the units present in spawn order, spilling a kill's remainder onto the next. It ignores
            // armor - it is raw card power, not a tower shot - and its victims are removed here, so
            // they are attributed to the burst rather than to a tower. NOT SPECIFIED BY THE DESIGN
            // how the burst distributes; see docs/reference/tuning-constants.md.
            if (!overloadSpent)
            {
                overloadSpent = true;
                double remaining = openingBurst;
                List<OverloadHit> hits = [];

                foreach (LiveEnemy enemy in active.OrderBy(e => e.SpawnIndex))
                {
                    if (remaining <= 0.0)
                    {
                        break;
                    }

                    double dealt = Math.Min(remaining, enemy.Health);
                    enemy.Health -= dealt;
                    remaining -= dealt;

                    if (enemy.Health <= 0)
                    {
                        enemy.Alive = false;
                    }

                    // Every unit touched, not only the dead: the unit the spill stops short of walks
                    // away damaged, and that has to be in the record for playback to draw it right.
                    hits.Add(new OverloadHit(enemy.SpawnIndex, dealt, !enemy.Alive));
                }

                events.Add(new OverloadEvent
                {
                    Tick = tick,
                    Time = time,
                    LaneIndex = lane,
                    Damage = openingBurst,
                    Hits = hits,
                });

                active.RemoveAll(e => !e.Alive);
                lastEventTime = time;
            }

            // Phase 2 - towers fire, in socket order, damage applied immediately.
            foreach (TowerRuntime gun in guns)
            {
                // Every tick, cooldown or not: what a tower could reach feeds the leak-cause
                // explanation, and that must not depend on whether it was ready this tick.
                Targeting.MarkInRange(gun.Tower, gun.Position, active);

                if (tick < gun.NextFireTick)
                {
                    continue;
                }

                LiveEnemy? target = Targeting.Choose(tuning, gun.Tower, gun.Position, active);

                if (target is null)
                {
                    // Still off cooldown: a tower holding fire under a standing order shoots the
                    // instant its condition is met, which is the whole point of pre-committing one.
                    gun.IdleTicks++;
                    continue;
                }

                Fire(tuning, gun, target, active, lane, tick, time, events);
                gun.NextFireTick = tick + cooldownTicks;
                lastEventTime = time;
            }

            // Phase 3 - remove the dead, in spawn order.
            if (active.Exists(e => !e.Alive))
            {
                foreach (LiveEnemy dead in active.Where(e => !e.Alive))
                {
                    events.Add(new DeathEvent
                    {
                        Tick = tick,
                        Time = time,
                        LaneIndex = lane,
                        SpawnIndex = dead.SpawnIndex,
                        EnemyId = dead.Type.Id,
                        KilledBy = dead.KilledBy,
                    });
                }

                active.RemoveAll(e => !e.Alive);
            }

            // Phase 4 - move survivors. A Standard bearer's aura hastens nearby same-lane units;
            // it composes on top of any Spade slow rather than replacing it.
            foreach (LiveEnemy enemy in active)
            {
                double speed = enemy.SpeedAt(time, tuning.Suits.Spades.SlowMultiplier);
                enemy.Position += speed * AuraFactor(enemy, active) * dt;
            }

            // Phase 5 - leak check, in spawn order.
            if (active.Exists(e => e.Position >= pathLength))
            {
                foreach (LiveEnemy escapee in active.Where(e => e.Position >= pathLength))
                {
                    LeakCause cause = undefended ? LeakCause.LaneUndefended
                        : !escapee.WasEverInRange ? LeakCause.NeverInRange
                        : LeakCause.OutDamaged;

                    leaked.Add(new LeakedUnit
                    {
                        SpawnIndex = escapee.SpawnIndex,
                        EnemyId = escapee.Type.Id,
                        LeakDamage = escapee.Type.LeakDamage,
                        Cause = cause,
                        RemainingHealthFraction = escapee.HealthFraction,
                    });

                    events.Add(new LeakEvent
                    {
                        Tick = tick,
                        Time = time,
                        LaneIndex = lane,
                        SpawnIndex = escapee.SpawnIndex,
                        EnemyId = escapee.Type.Id,
                        LeakDamage = escapee.Type.LeakDamage,
                        Cause = cause,
                    });

                    leakDamage += escapee.Type.LeakDamage;
                    lastEventTime = time;
                }

                active.RemoveAll(e => e.Position >= pathLength);
            }

            tick++;
        }

        return new LaneRun(
            events,
            leaked,
            [.. guns.Select(g => g.ToActivity())],
            leakDamage,
            lastEventTime);
    }

    /// <summary>
    /// One tower's shot: primary hit, then Club splash, then Spade slow.
    /// </summary>
    private static void Fire(
        TuningData tuning,
        TowerRuntime gun,
        LiveEnemy target,
        IReadOnlyList<LiveEnemy> active,
        int lane,
        int tick,
        double time,
        List<TimelineEvent> events)
    {
        double raw = gun.ShotDamage;
        double applied = Hit(tuning, gun, target, raw, lane, tick, time, isSplash: false, events);

        gun.Record(raw, applied, time, killed: !target.Alive);

        // Clubs are artillery: the same shot lands on everything within the splash radius of where
        // it hit. Armor and the damage floor apply per hit, so a swarm and an armored soldier
        // caught in the same blast take different amounts.
        if (gun.Tower.Family == Family.Club
            && tuning.Suits.Clubs.SplashFraction > 0
            && tuning.Suits.Clubs.SplashRadius > 0)
        {
            double epicentre = target.Position;
            double splashDamage = raw * tuning.Suits.Clubs.SplashFraction;

            foreach (LiveEnemy other in active)
            {
                if (!other.Alive
                    || other.SpawnIndex == target.SpawnIndex
                    || Math.Abs(other.Position - epicentre) > tuning.Suits.Clubs.SplashRadius)
                {
                    continue;
                }

                double splashApplied = Hit(tuning, gun, other, splashDamage, lane, tick, time, isSplash: true, events);
                gun.RecordSplash(splashDamage, splashApplied, killed: !other.Alive);
            }
        }

        // Spades are traps and control: they damage and they slow. Refresh, never compound - two
        // Spades stacking to 0.36 speed would be a hard stop wearing the word "slow".
        if (gun.Tower.Family == Family.Spade && target.Alive)
        {
            double until = time + tuning.Suits.Spades.SlowSeconds;
            target.SlowUntil = Math.Max(target.SlowUntil, until);

            events.Add(new SlowEvent
            {
                Tick = tick,
                Time = time,
                LaneIndex = lane,
                Socket = gun.Tower.Socket,
                SpawnIndex = target.SpawnIndex,
                UntilTime = target.SlowUntil,
            });
        }
    }

    /// <summary>
    /// Applies one hit through armor and records it.
    /// </summary>
    /// <remarks>
    /// <b>Armor cannot reduce a hit below 0.25</b> (docs/design/06-dealer-and-enemies.md), and
    /// Spade traps and Kings ignore half of flat armor. Note the <c>Min</c>: the floor is a limit on
    /// what armor may take away, not a minimum hit. A 0.1 shot against an unarmored target stays
    /// 0.1 rather than being rounded up to the floor, because armor did not reduce it.
    /// </remarks>
    private static double Hit(
        TuningData tuning,
        TowerRuntime gun,
        LiveEnemy enemy,
        double raw,
        int lane,
        int tick,
        double time,
        bool isSplash,
        List<TimelineEvent> events)
    {
        double armor = gun.Tower.IgnoresHalfArmor
            ? enemy.Type.FlatArmor * (1.0 - tuning.Combat.HalfArmorBypassFraction)
            : enemy.Type.FlatArmor;

        double applied = Math.Max(Math.Min(raw, tuning.Combat.ArmorDamageFloor), raw - armor);

        enemy.Health -= applied;

        if (enemy.Health <= 0)
        {
            enemy.Alive = false;
            enemy.KilledBy = gun.Tower.Socket;
        }

        events.Add(new ShotEvent
        {
            Tick = tick,
            Time = time,
            LaneIndex = lane,
            Socket = gun.Tower.Socket,
            TargetSpawnIndex = enemy.SpawnIndex,
            DamageBeforeArmor = raw,
            DamageApplied = applied,
            IsSplash = isSplash,
        });

        return applied;
    }

    /// <summary>
    /// The speed multiplier a unit gets from nearby aura carriers in its lane, or 1.0 if none.
    /// </summary>
    /// <remarks>
    /// Only the Standard bearer carries an aura, and only within its own lane - enemies never leave
    /// their lane, so this reads nothing outside it and does not couple the two lane simulations.
    /// Multiple carriers do not compound: the strongest aura wins, matching the Spade slow's
    /// refresh-not-stack rule. A unit does not buff itself. NOT SPECIFIED BY THE DESIGN.
    /// </remarks>
    private static double AuraFactor(LiveEnemy enemy, IReadOnlyList<LiveEnemy> active)
    {
        double factor = 1.0;

        foreach (LiveEnemy carrier in active)
        {
            if (!carrier.Alive
                || ReferenceEquals(carrier, enemy)
                || carrier.Type.Aura is not { } aura
                || Math.Abs(carrier.Position - enemy.Position) > aura.Radius)
            {
                continue;
            }

            factor = Math.Max(factor, aura.SpeedMultiplier);
        }

        return factor;
    }

    /// <summary>
    /// A generous upper bound on how long a lane can take, used only as a hang guard.
    /// </summary>
    private static int TickCeiling(TuningData tuning, IReadOnlyList<(int Tick, EnemySpawn Spawn)> schedule)
    {
        if (schedule.Count == 0)
        {
            return 0;
        }

        double slowestSpeed = schedule
            .Select(s => tuning.Enemy(s.Spawn.EnemyId).Speed)
            .Min() * tuning.Suits.Spades.SlowMultiplier;

        double crossing = tuning.Geometry.PathLength / slowestSpeed;

        return schedule[^1].Tick + tuning.Sim.TicksIn(crossing) + 100;
    }

    /// <summary>Everything one lane's simulation produced.</summary>
    private sealed record LaneRun(
        List<TimelineEvent> Events,
        List<LeakedUnit> Leaked,
        List<TowerActivity> Activity,
        int LeakDamage,
        double DurationSeconds);

    /// <summary>A tower while the simulation is running, plus what it did.</summary>
    private sealed class TowerRuntime(TowerState tower, double position, double shotDamage)
    {
        private double _before;
        private double _applied;
        private int _shots;
        private int _kills;
        private double? _first;
        private double? _last;

        internal TowerState Tower { get; } = tower;
        internal double Position { get; } = position;

        /// <summary>Damage per shot in this lane, already carrying any junction penalty.</summary>
        internal double ShotDamage { get; } = shotDamage;

        internal int NextFireTick { get; set; }
        internal int IdleTicks { get; set; }

        internal void Record(double before, double applied, double time, bool killed)
        {
            _shots++;
            _before += before;
            _applied += applied;
            _first ??= time;
            _last = time;

            if (killed)
            {
                _kills++;
            }
        }

        /// <summary>A splash hit is part of the same shot, so it adds damage but not a shot count.</summary>
        internal void RecordSplash(double before, double applied, bool killed)
        {
            _before += before;
            _applied += applied;

            if (killed)
            {
                _kills++;
            }
        }

        internal TowerActivity ToActivity() => new()
        {
            Socket = Tower.Socket,
            Card = Tower.Card,
            Family = Tower.Family,
            Shots = _shots,
            DamageBeforeArmor = _before,
            DamageApplied = _applied,
            Kills = _kills,
            IdleTicks = IdleTicks,
            FirstFireTime = _first,
            LastFireTime = _last,
        };
    }
}

/// <summary>
/// A resolver run, before it is wrapped as one forecast or the other.
/// </summary>
/// <remarks>
/// Internal on purpose. Nothing outside the resolver may hold an unlabelled resolution: the whole
/// point of the two forecast types is that a caller must say which question it asked.
/// </remarks>
internal sealed record WaveResolution
{
    internal required IReadOnlyList<TimelineEvent> Events { get; init; }
    internal required IReadOnlyList<LaneOutcome> Lanes { get; init; }
    internal required double DurationSeconds { get; init; }
}
