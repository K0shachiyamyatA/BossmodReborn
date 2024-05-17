namespace BossMod.Stormblood.Trial.T07Byakko;

[ModuleInfo(BossModuleInfo.Maturity.WIP, Contributors = "The Combat Reborn Team", GroupType = BossModuleInfo.GroupType.CFC, GroupID = 243, NameID = 6221)]
public class T07Byakko(WorldState ws, Actor primary) : BossModule(ws, primary, new ArenaBoundsCircle(new(0, 0), 20));
