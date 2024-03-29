﻿namespace BossMod.Endwalker.Savage.P5SProtoCarbuncle;

// note: currently we use visual casts to determine all safespots, these happen much earlier than animation changes...
class RubyGlow3 : RubyGlowCommon
{
    private BitMask[] _aoeQuadrants = new BitMask[4]; // [i] = danger quardants at explosion #i, bits: 0=NW, 1=NE, 2=SW, 3=SE

    // note: it's somewhat simpler to count casts rather than activating/deactivating stones
    public RubyGlow3() : base(ActionID.MakeSpell(AID.RubyReflectionQuarter)) { }

    public override IEnumerable<AOEInstance> ActiveAOEs(BossModule module, int slot, Actor actor)
    {
        var aoeQuadrants = NumCasts switch
        {
            < 2 => _aoeQuadrants[0],
            < 4 => _aoeQuadrants[1],
            < 7 => _aoeQuadrants[2],
            < 10 => _aoeQuadrants[3],
            _ => new BitMask()
        };
        // TODO: correct explosion time
        foreach (var q in aoeQuadrants.SetBits())
            yield return new(ShapeQuadrant, QuadrantCenter(module, q));
    }

    public override void AddGlobalHints(BossModule module, GlobalHints hints)
    {
        if (_aoeQuadrants[2].Any())
        {
            var safe = (~_aoeQuadrants[2]).LowestSetBit();
            var safeWaymark = safe < 4 ? WaymarkForQuadrant(module, safe) : Waymark.Count;
            if (safeWaymark != Waymark.Count)
            {
                hints.Add($"Safespot for third: {safeWaymark}");
            }
        }
    }

    public override void OnCastStarted(BossModule module, Actor caster, ActorCastInfo spell)
    {
        int order = (AID)spell.Action.ID switch
        {
            AID.TopazClusterHit1 => 0,
            AID.TopazClusterHit2 => 1,
            AID.TopazClusterHit3 => 2,
            AID.TopazClusterHit4 => 3,
            _ => -1
        };

        if (order >= 0)
            _aoeQuadrants[order].Set(QuadrantForPosition(module, caster.Position));
    }
}
