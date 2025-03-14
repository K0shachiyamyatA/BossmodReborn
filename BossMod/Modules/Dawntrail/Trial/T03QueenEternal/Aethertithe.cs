namespace BossMod.Dawntrail.Trial.T03QueenEternal;

class Aethertithe(BossModule module) : Components.GenericAOEs(module)
{
    private AOEInstance? _aoe;
    private static readonly AOEShapeCone cone = new(100f, 35f.Degrees());

    public override ReadOnlySpan<AOEInstance> ActiveAOEs(int slot, Actor actor) => Utils.ZeroOrOne(ref _aoe);

    public override void OnEventEnvControl(byte index, uint state)
    {
        if (index != 0x00)
            return;
        Angle? angle = state switch
        {
            0x04000100 => -55f.Degrees(),
            0x08000100 => new Angle(),
            0x10000100 => 55f.Degrees(),
            _ => null
        };
        if (angle is Angle rot)
            _aoe = new(cone, WPos.ClampToGrid(Module.PrimaryActor.Position), rot, WorldState.FutureTime(5d));
    }

    public override void OnEventCast(Actor caster, ActorCastEvent spell)
    {
        if (spell.Action.ID is (uint)AID.Aethertithe1 or (uint)AID.Aethertithe2 or (uint)AID.Aethertithe3)
            _aoe = null;
    }
}
