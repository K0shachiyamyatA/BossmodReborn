﻿namespace BossMod.Endwalker.Dungeon.D09Alzadaal.D093Kapikulu;

public enum OID : uint
{
    Boss = 0x36C1,
    Helper = 0x233C, // R0.500, x29, 523 type
    Unknown = 0x3931, // R4.400, x2 (spawn during fight)
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target
    BastingBlade = 28520, // Boss->self, 5.5s cast, range 60 width 15 rect
    BillowingBolts = 28528, // Boss->self, 5.0s cast, range 80 circle
    CrewelSlice = 28530, // Boss->player, 5.0s cast, single-target
    MagnitudeOpus1 = 28526, // Boss->self, 4.0s cast, single-target
    MagnitudeOpus2 = 28527, // Helper->players, 5.0s cast, range 6 circle
    ManaExplosion = 28523, // Helper->self, 3.0s cast, range 15 circle
    PowerSerge = 28522, // Boss->self, 6.0s cast, single-target
    SpinOut = 28515, // Boss->self, 3.0s cast, single-target
    UnknownWeaponskill1 = 28516, // Helper->player, 4.5s cast, single-target
    UnknownWeaponskill2 = 28517, // Boss->self, no cast, single-target
    UnknownWeaponskill3 = 28518, // Helper->self, no cast, range 10 circle
    UnknownWeaponskill4 = 28519, // Helper->self, 6.0s cast, range 6 width 6 rect
    UnknownWeaponskill5 = 28529, // Helper->self, 5.0s cast, range 5 width 40 rect
    UnkownAbility = 28514, // Boss->location, no cast, single-target
    WildWeave = 28521, // Boss->self, 4.0s cast, single-target
}
public enum SID : uint
{
    Dizzy = 2974, // Boss->player, extra=0x0
    Fetters = 2975, // Boss->player, extra=0xEC4
    Spinning = 2973, // Boss->player, extra=0x7
    StabWound1 = 3061, // none->player, extra=0x0
    StabWound2 = 3062, // none->player, extra=0x0
    VulnerabilityUp = 1789, // Helper->player, extra=0x1

}
public enum IconID : uint
{
    Icon_218 = 218, // player
    Icon_161 = 161, // player
}
public enum TetherID : uint
{
    Tether_177 = 177, // player->Boss
    Tether_188 = 188, // Boss->Helper
}

class D093KapikuluStates : StateMachineBuilder
{
    public D093KapikuluStates(BossModule module) : base(module)
    {
        TrivialPhase();
    }
}

[ModuleInfo(BossModuleInfo.Maturity.WIP, Contributors = "CombatReborn Team", GroupType = BossModuleInfo.GroupType.CFC, GroupID = 844, NameID = 11238)]
public class D093Kapikulu(WorldState ws, Actor primary) : BossModule(ws, primary, new ArenaBoundsRect(new(110, -68), 20, 15));