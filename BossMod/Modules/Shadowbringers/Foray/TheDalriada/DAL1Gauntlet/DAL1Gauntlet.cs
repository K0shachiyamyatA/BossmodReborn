namespace BossMod.Shadowbringers.Foray.TheDalriada.DAl1Gauntlet;


[ModuleInfo(BossModuleInfo.Maturity.WIP, Contributors = "The Combat Reborn Team", GroupType = BossModuleInfo.GroupType.CFC, GroupID = 735, NameID = 9834)]
public class DAl1Gauntlet(WorldState ws, Actor primary) : BossModule(ws, primary, new ArenaBoundsCircle(new(0, 278), 25));