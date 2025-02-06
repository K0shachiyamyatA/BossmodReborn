﻿namespace BossMod;

// information relevant for AI decision making process for a specific player
public sealed class AIHints
{
    public class Enemy(Actor actor, int priority, bool shouldBeTanked)
    {
        public const int PriorityPointless = -1; // attacking enemy won't improve your parse, but will give gauge and advance combo (e.g. boss locked to 1 HP, useless add in raid, etc)
        public const int PriorityInvincible = -2; // attacking enemy will have no effect at all besides breaking your combo, but hitting it with AOEs is fine
        public const int PriorityUndesirable = -3; // enemy can be attacked if targeted manually by a player, but should be considered forbidden for AOE actions (i.e. mobs that are not in combat, or are in combat with someone else's party)
        public const int PriorityForbidden = -4; // attacking this enemy will probably lead to a wipe; autoattacks and actions that target it will be forcibly prevented (if custom queueing is enabled)

        public Actor Actor = actor;
        public int Priority = priority;
        //public float TimeToKill;
        public float AttackStrength = 0.05f; // target's predicted HP percent is decreased by this amount (0.05 by default)
        public WPos DesiredPosition = actor.Position; // tank AI will try to move enemy to this position
        public Angle DesiredRotation = actor.Rotation; // tank AI will try to rotate enemy to this angle
        public float TankDistance = 2; // enemy will start moving if distance between hitboxes is bigger than this
        public bool ShouldBeTanked = shouldBeTanked; // tank AI will try to tank this enemy
        public bool PreferProvoking; // tank AI will provoke enemy if not targeted
        public bool ForbidDOTs; // if true, dots on target are forbidden
        public bool ShouldBeInterrupted; // if set and enemy is casting interruptible spell, some ranged/tank will try to interrupt
        public bool ShouldBeStunned; // if set, AI will stun if possible
        public bool StayAtLongRange; // if set, players with ranged attacks don't bother coming closer than max range (TODO: reconsider)
    }

    public enum SpecialMode
    {
        Normal,
        Pyretic, // pyretic/acceleration bomb type of effects - no movement, no actions, no casting allowed at activation time
        NoMovement, // no movement allowed
        Freezing, // should be moving at activation time
        Misdirection, // temporary misdirection - if current time is greater than activation, use special pathfinding codepath
    }

    public static readonly ArenaBounds DefaultBounds = new ArenaBoundsSquare(30);

    // information needed to build base pathfinding map (onto which forbidden/goal zones are later rasterized), if needed (lazy, since it's somewhat expensive and not always needed)
    public WPos PathfindMapCenter;
    public ArenaBounds PathfindMapBounds = DefaultBounds;
    public Bitmap.Region PathfindMapObstacles;

    // list of potential targets
    public readonly Enemy?[] Enemies = new Enemy?[100];
    public Enemy? FindEnemy(Actor? actor) => Enemies.BoundSafeAt(actor?.CharacterSpawnIndex ?? -1);

    // enemies in priority order
    public readonly List<Enemy> PotentialTargets = [];

    public int HighestPotentialTargetPriority;

    // forced target
    // this should be set only if either explicitly planned by user or by ai, otherwise it will be annoying to user
    public Actor? ForcedTarget;

    // low-level forced movement - if set, character will move in specified direction (ignoring casts, uptime, forbidden zones, etc), or stay in place if set to default
    public Vector3? ForcedMovement;

    // indicates to AI mode that it should try to interact with some object
    public Actor? InteractWithTarget;

    // positioning: list of shapes that are either forbidden to stand in now or will be in near future
    // AI will try to move in such a way to avoid standing in any forbidden zone after its activation or outside of some restricted zone after its activation, even at the cost of uptime
    public readonly List<(Func<WPos, float> shapeDistance, DateTime activation)> ForbiddenZones = [];

    // positioning: list of goal functions
    // AI will try to move to reach non-forbidden point with highest goal value (sum of values returned by all functions)
    // guideline: rotation modules should return 1 if it would use single-target action from that spot, 2 if it is also a positional, 3 if it would use aoe that would hit minimal viable number of targets, +1 for each extra target
    // other parts of the code can return small (e.g. 0.01) values to slightly (de)prioritize some positions, or large (e.g. 1000) values to effectively soft-override target position (but still utilize pathfinding)
    public readonly List<Func<WPos, float>> GoalZones = [];

    // positioning: next positional hint (TODO: reconsider, maybe it should be a list prioritized by in-gcds, and imminent should be in-gcds instead? or maybe it should be property of an enemy? do we need correct?)
    public (Actor? Target, Positional Pos, bool Imminent, bool Correct) RecommendedPositional;

    // orientation restrictions (e.g. for gaze attacks): a list of forbidden orientation ranges, now or in near future
    // AI will rotate to face allowed orientation at last possible moment, potentially losing uptime
    public readonly List<(Angle center, Angle halfWidth, DateTime activation)> ForbiddenDirections = [];

    // closest special movement/targeting/action mode, if any
    public (SpecialMode mode, DateTime activation) ImminentSpecialMode;

    // for misdirection: if forced movement is set, make real direction be within this angle
    public Angle MisdirectionThreshold;

    // predicted incoming damage (raidwides, tankbusters, etc.)
    // AI will attempt to shield & mitigate
    public readonly List<(BitMask players, DateTime activation)> PredictedDamage = [];

    // maximal time we can spend casting before we need to move
    // this is used by the action queue to skip casts that we won't be able to finish and execute lower-priority fallback actions instead
    public float MaxCastTime = float.MaxValue;
    public bool ForceCancelCast;

    // actions that we want to be executed, gathered from various sources (manual input, autorotation, planner, ai, modules, etc.)
    public readonly ActionQueue ActionsToExecute = new();

    // buffs to be canceled asap
    public readonly List<(uint statusId, ulong sourceId)> StatusesToCancel = [];

    // misc stuff to execute
    public bool WantJump;
    public bool WantDismount;

    // clear all stored data
    public void Clear()
    {
        PathfindMapCenter = default;
        PathfindMapBounds = DefaultBounds;
        PathfindMapObstacles = default;
        Array.Fill(Enemies, null);
        PotentialTargets.Clear();
        ForcedTarget = null;
        ForcedMovement = null;
        InteractWithTarget = null;
        ForbiddenZones.Clear();
        GoalZones.Clear();
        RecommendedPositional = default;
        ForbiddenDirections.Clear();
        ImminentSpecialMode = default;
        MisdirectionThreshold = 15.Degrees();
        PredictedDamage.Clear();
        MaxCastTime = float.MaxValue;
        ForceCancelCast = false;
        ActionsToExecute.Clear();
        StatusesToCancel.Clear();
        WantJump = false;
        WantDismount = false;
    }

    public void PrioritizeTargetsByOID(uint oid, int priority = 0)
    {
        var count = PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var h = PotentialTargets[i];
            if (h.Actor.OID == oid)
            {
                ref var hPriority = ref h.Priority;
                hPriority = priority ^ ((hPriority ^ priority) & -(hPriority > priority ? 1 : 0)); // Math.Max(priority, h.Priority)
            }
        }
    }
    public void PrioritizeTargetsByOID<OID>(OID oid, int priority = 0) where OID : Enum => PrioritizeTargetsByOID((uint)(object)oid, priority);

    public void PrioritizeTargetsByOID(uint[] oids, int priority = 0)
    {
        var count = PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var h = PotentialTargets[i];
            if (oids.Contains(h.Actor.OID))
            {
                ref var hPriority = ref h.Priority;
                hPriority = priority ^ ((hPriority ^ priority) & -(hPriority > priority ? 1 : 0)); // Math.Max(priority, h.Priority)
            }
        }
    }

    public void PrioritizeAll()
    {
        var count = PotentialTargets.Count;
        for (var i = 0; i < count; ++i)
        {
            var h = PotentialTargets[i];
            h.Priority &= ~(h.Priority >> 31); // Math.Max(h.priority, 0)
        }
    }

    public void InteractWithOID(WorldState ws, uint oid) => InteractWithTarget = ws.Actors.FirstOrDefault(a => a.OID == oid && a.IsTargetable);
    public void InteractWithOID<OID>(WorldState ws, OID oid) where OID : Enum => InteractWithOID(ws, (uint)(object)oid);

    public void AddForbiddenZone(Func<WPos, float> shapeDistance, DateTime activation = new()) => ForbiddenZones.Add((shapeDistance, activation));
    public void AddForbiddenZone(AOEShape shape, WPos origin, Angle rot = new(), DateTime activation = new()) => ForbiddenZones.Add((shape.Distance(origin, rot), activation));

    public void AddSpecialMode(SpecialMode mode, DateTime activation)
    {
        if (ImminentSpecialMode == default || ImminentSpecialMode.activation > activation)
            ImminentSpecialMode = (mode, activation);
    }

    // normalize all entries after gathering data: sort by priority / activation timestamp
    // TODO: note that the name is misleading - it actually happens mid frame, before all actions are gathered (eg before autorotation runs), but further steps (eg ai) might consume previously gathered data
    public void Normalize()
    {
        PotentialTargets.SortByReverse(x => x.Priority);
        HighestPotentialTargetPriority = Math.Max(0, PotentialTargets.FirstOrDefault()?.Priority ?? 0);
        ForbiddenZones.SortBy(e => e.activation);
        ForbiddenDirections.SortBy(e => e.activation);
        PredictedDamage.SortBy(e => e.activation);
    }

    public void InitPathfindMap(Pathfinding.Map map)
    {
        PathfindMapBounds.PathfindMap(map, PathfindMapCenter);
        if (PathfindMapObstacles.Bitmap != null)
        {
            var offX = -PathfindMapObstacles.Rect.Left;
            var offY = -PathfindMapObstacles.Rect.Top;
            var r = PathfindMapObstacles.Rect.Clamped(PathfindMapObstacles.Bitmap.FullRect).Clamped(new(0, 0, map.Width, map.Height), offX, offY);
            for (var y = r.Top; y < r.Bottom; ++y)
                for (var x = r.Left; x < r.Right; ++x)
                    if (PathfindMapObstacles.Bitmap[x, y])
                        map.PixelMaxG[(y + offY) * map.Width + x + offX] = -900;
        }
    }

    // query utilities
    public List<Enemy> PotentialTargetsEnumerable => PotentialTargets;
    public List<Enemy> PriorityTargets
    {
        get
        {
            var count = PotentialTargets.Count;
            var targets = new List<Enemy>();
            for (var i = 0; i < count; ++i)
            {
                var e = PotentialTargets[i];
                if (e.Priority != HighestPotentialTargetPriority)
                    break;
                targets.Add(e);
            }
            return targets;
        }
    }

    public IEnumerable<Enemy> ForbiddenTargets
    {
        get
        {
            var count = PotentialTargets.Count;
            var targets = new List<Enemy>();
            for (var i = count - 1; i >= 0; --i)
            {
                var e = PotentialTargets[i];
                if (e.Priority > Enemy.PriorityUndesirable)
                    break;
                targets.Add(e);
            }
            return targets;
        }
    }

    // TODO: verify how source/target hitboxes are accounted for by various aoe shapes
    public int NumPriorityTargetsInAOE(Func<Enemy, bool> pred) => ForbiddenTargets.Any(pred) ? 0 : PriorityTargets.Count(pred);
    public int NumPriorityTargetsInAOECircle(WPos origin, float radius) => NumPriorityTargetsInAOE(a => TargetInAOECircle(a.Actor, origin, radius));
    public int NumPriorityTargetsInAOECone(WPos origin, float radius, WDir direction, Angle halfAngle) => NumPriorityTargetsInAOE(a => TargetInAOECone(a.Actor, origin, radius, direction, halfAngle));
    public int NumPriorityTargetsInAOERect(WPos origin, WDir direction, float lenFront, float halfWidth, float lenBack = 0) => NumPriorityTargetsInAOE(a => TargetInAOERect(a.Actor, origin, direction, lenFront, halfWidth, lenBack));
    public bool TargetInAOECircle(Actor target, WPos origin, float radius) => target.Position.InCircle(origin, radius + target.HitboxRadius);
    public bool TargetInAOECone(Actor target, WPos origin, float radius, WDir direction, Angle halfAngle) => target.Position.InCircleCone(origin, radius + target.HitboxRadius, direction, halfAngle);
    public bool TargetInAOERect(Actor target, WPos origin, WDir direction, float lenFront, float halfWidth, float lenBack = 0) => target.Position.InRect(origin, direction, lenFront + target.HitboxRadius, lenBack, halfWidth);

    // goal zones
    // simple goal zone that returns 1 if target is in range, useful for single-target actions
    public Func<WPos, float> GoalSingleTarget(WPos target, float radius, float weight = 1f)
    {
        var effRsq = radius * radius;
        return p => (p - target).LengthSq() <= effRsq ? weight : 0;
    }
    public Func<WPos, float> GoalSingleTarget(Actor target, float range, float weight = 1f) => GoalSingleTarget(target.Position, range + target.HitboxRadius, weight);

    // simple goal zone that returns 1 if target is in range (usually melee), 2 if it's also in correct positional
    public Func<WPos, float> GoalSingleTarget(WPos target, Angle rotation, Positional positional, float radius)
    {
        if (positional == Positional.Any)
            return GoalSingleTarget(target, radius); // more efficient implementation
        var effRsq = radius * radius;
        var targetDir = rotation.ToDirection();
        return p =>
        {
            var offset = p - target;
            var lsq = offset.LengthSq();
            if (lsq > effRsq)
                return 0; // out of range
            // note: this assumes that extra dot is cheaper than sqrt?..
            var front = targetDir.Dot(offset);
            var side = Math.Abs(targetDir.Dot(offset.OrthoL()));
            var inPositional = positional switch
            {
                Positional.Flank => side > Math.Abs(front),
                Positional.Rear => -front > side,
                Positional.Front => front > side, // TODO: reconsider this, it's not a real positional?..
                _ => false
            };
            return inPositional ? 2 : 1;
        };
    }
    public Func<WPos, float> GoalSingleTarget(Actor target, Positional positional, float range = 2.6f) => GoalSingleTarget(target.Position, target.Rotation, positional, range + target.HitboxRadius);

    // simple goal zone that returns number of targets in aoes; note that performance is a concern for these functions, and perfection isn't required, so eg they ignore forbidden targets, etc
    public Func<WPos, float> GoalAOECircle(float radius)
    {
        var count = PriorityTargets.Count;
        var targets = new (WPos pos, float radius)[count];
        for (var i = 0; i < count; ++i)
        {
            var e = PriorityTargets[i];
            targets[i] = (e.Actor.Position, e.Actor.HitboxRadius);
        }
        return p =>
        {
            var countInCircle = 0;
            for (var i = 0; i < count; ++i)
            {
                var t = targets[i];
                if (t.pos.InCircle(p, radius + t.radius))
                    ++countInCircle;
            }

            return countInCircle;
        };
    }

    public Func<WPos, float> GoalAOECone(Actor primaryTarget, float radius, Angle halfAngle)
    {
        var count = PriorityTargets.Count;
        var targets = new (WPos pos, float radius)[count];
        for (var i = 0; i < count; ++i)
        {
            var e = PriorityTargets[i];
            targets[i] = (e.Actor.Position, e.Actor.HitboxRadius);
        }
        var aimPoint = primaryTarget.Position;
        var effRange = radius + primaryTarget.HitboxRadius;
        var effRsq = effRange * effRange;
        return p =>
        {
            var toTarget = aimPoint - p;
            var lenSq = toTarget.LengthSq();
            if (lenSq > effRsq)
                return 0;
            var dir = toTarget / MathF.Sqrt(lenSq);
            var countInCone = 0;
            for (var i = 0; i < count; ++i)
            {
                var t = targets[i];
                if (t.pos.InCircleCone(p, radius + t.radius, dir, halfAngle))
                    ++countInCone;
            }

            return countInCone;
        };
    }

    public Func<WPos, float> GoalAOERect(Actor primaryTarget, float lenFront, float halfWidth, float lenBack = 0f)
    {
        var count = PriorityTargets.Count;
        var targets = new (WPos pos, float radius)[count];
        for (var i = 0; i < count; ++i)
        {
            var e = PriorityTargets[i];
            targets[i] = (e.Actor.Position, e.Actor.HitboxRadius);
        }
        var aimPoint = primaryTarget.Position;
        var effRange = lenFront + primaryTarget.HitboxRadius;
        var effRsq = effRange * effRange;

        return p =>
        {
            var toTarget = aimPoint - p;
            var lenSq = toTarget.LengthSq();
            if (lenSq > effRsq)
                return 0;

            var dir = toTarget / MathF.Sqrt(lenSq);

            var countInRect = 0;
            for (var i = 0; i < count; ++i)
            {
                if (targets[i].pos.InRect(p, dir, lenFront, lenBack, halfWidth))
                    ++countInRect;
            }

            return countInRect;
        };
    }

    // combined goal zone: returns 'aoe' priority if targets hit are at or above minimum, otherwise returns 'single-target' priority
    public Func<WPos, float> GoalCombined(Func<WPos, float> singleTarget, Func<WPos, float> aoe, int minAOETargets)
    {
        if (minAOETargets >= 50)
            return singleTarget; // assume aoe is never efficient, so don't bother
        return p =>
        {
            var aoeTargets = aoe(p) - minAOETargets;
            return aoeTargets >= 0 ? 3 + aoeTargets : singleTarget(p);
        };
    }

    // goal zone that returns a value between 0 and weight depending on distance to point; useful for downtime movement targets
    public Func<WPos, float> GoalProximity(WPos destination, float maxDistance, float maxWeight)
    {
        var invDist = 1f / maxDistance;
        return p =>
        {
            var dist = (p - destination).Length();
            var weight = 1f - Math.Clamp(invDist * dist, 0f, 1f);
            return maxWeight * weight;
        };
    }

    public WPos ClampToBounds(WPos position) => PathfindMapCenter + PathfindMapBounds.ClampToBounds(position - PathfindMapCenter);
}
