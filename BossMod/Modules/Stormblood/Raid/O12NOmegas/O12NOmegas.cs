namespace BossMod.Stormblood.Raid.O12NOmegas;

public enum OID : uint
{
    Helper = 0x233C, // R0.500, x30
    Ausgang = 0x1E850B, // R0.500, x1, EventObj type
    Optikmodul = 0x18D6, // R0.500, x1
    Boss = 0x247B, // R3.000, x1
    OmegaF = 0x247C, // R3.000, x1
}

public enum AID : uint
{
    AutoAttack = 870, // Boss->player, no cast, single-target
    AutoAttackF = 13094, // Boss/OmegaF->player, no cast, single-target

    SubjectSimulationF = 13041, // Boss->self, 2.0s cast, single-target
    SubjectSimulationM = 13044, // Boss->self, 2.0s cast, single-target

    SolarRay1 = 13071, // Boss->player, 5.0s cast, single-target // tankbuster
    SolarRay2 = 13072, // Boss/OmegaF->player, 5.0s cast, single-target // tankbuster

    EfficientBladework1 = 13043, // Boss->self, no cast, range 10 circle
    EfficientBladework2 = 13055, // Boss->self, 4.0s cast, range 10 circle // out

    Discharger = 13046, // Boss/OmegaF->self, no cast, range 100 circle // knockback 15 AwayFromOrigin after AID.SubjectSimulationF

    OptimizedFireIII1 = 13069, // Boss/OmegaF->self, 3.0s cast, single-target
    OptimizedFireIII2 = 13070, // Helper->players, 5.0s cast, range 5 circle
    OptimizedBlizzardIII = 13059, // Boss/OmegaF->self, 3.0s cast, range 40 width 10 cross

    LaserShower1 = 13073, // Boss->self, 4.0s cast, range 100 circle // Raidwides
    LaserShower2 = 13074, // Boss/OmegaF->self, 4.0s cast, range 100 circle
    LaserShower3 = 13079, // OmegaF->self, 55.0s cast, range 100 circle

    SyntheticShield = 13053, // Boss->self, 2.0s cast, single-target
    SyntheticBlades = 13057, // Boss/OmegaF->self, 2.0s cast, single-target

    SuperliminalSteel1 = 13061, // Boss/OmegaF->self, 3.0s cast, single-target
    SuperliminalSteel2 = 13062, // Helper->self, 3.0s cast, range 80 width 36 rect
    SuperliminalSteel3 = 13063, // Helper->self, 3.0s cast, range 80 width 36 rect

    ProgramAlpha = 13064, // Boss->self, 3.0s cast, single-target
    BeyondStrength = 13056, // Boss->self, 4.0s cast, range -40 donut

    Floodlight = 13065, // Helper->location, 3.0s cast, range 6 circle

    Spotlight1 = 13066, // Helper->players, 5.0s cast, range 6 circle
    Spotlight2 = 13082, // Helper->players, 5.0s cast, range 6 circle

    ElectricSlide = 13075, // OmegaF->location, no cast, range 6 circle
    GroundZero = 13076, // Boss->location, no cast, range 6 circle

    OptimizedPassageOfArms = 13078, // Boss->self, no cast, single-target

    CosmoMemory1 = 13083, // Boss->self, 5.0s cast, single-target
    CosmoMemory2 = 13084, // OmegaF->self, 5.0s cast, single-target
    CosmoMemory3 = 13085, // Optikmodul->self, 1.2s cast, range 100 circle

    Suppression1 = 13086, // Boss->self, 3.0s cast, single-target
    Suppression2 = 13087, // OmegaF->self, 3.0s cast, single-target

    OpticalLaser = 13088, // Optikmodul->self, 5.0s cast, range 100 width 16 rect

    OptimizedBladedance1 = 13089, // Boss->player, 5.0s cast, single-target // tankbuster
    OptimizedBladedance2 = 13090, // OmegaF->player, 5.0s cast, single-target // tankbuster

    OptimizedSagittariusArrow = 13091, // Boss->self, 5.0s cast, range 100 width 10 rect

    OptimizedMeteor1 = 13092, // OmegaF->self, 5.0s cast, single-target // spread cast Tankbuster
    OptimizedMeteor2 = 13093, // Helper->players, 5.0s cast, range 100 circle

    Firewall1 = 13202, // Boss->self, 3.0s cast, range 100 circle
    Firewall2 = 13203, // OmegaF->self, 3.0s cast, range 100 circle

    Resonance1 = 13204, // Boss->self, 3.0s cast, single-target
    Resonance2 = 13205, // OmegaF->self, 3.0s cast, single-targe

    UnknownWeaponskill1 = 13042, // Boss->self, no cast, single-target
    UnknownWeaponskill2 = 13045, // Boss/OmegaF->self, no cast, single-target
    UnknownWeaponskill3 = 13047, // Boss->self, no cast, single-target
    UnknownWeaponskill4 = 13048, // Boss->self, no cast, single-target
    UnknownWeaponskill5 = 13049, // Boss/OmegaF->self, no cast, single-target
    UnknownWeaponskill6 = 13050, // Boss/OmegaF->self, no cast, single-target
    UnknownWeaponskill7 = 13051, // Helper->self, no cast, single-target
    UnknownWeaponskill8 = 13052, // Helper->self, no cast, single-target
    UnknownWeaponskill9 = 13054, // Boss->self, no cast, single-target
    UnknownWeaponskill10 = 13058, // Boss->self, no cast, single-target
    UnknownWeaponskill11 = 13067, // Boss->location, no cast, ???
    UnknownWeaponskill12 = 13068, // OmegaF->location, no cast, ???
    UnknownWeaponskill13 = 13080, // Boss->self, no cast, single-target
    UnknownWeaponskill14 = 13081, // OmegaF->self, no cast, single-target
    UnknownWeaponskill15 = 13515, // Boss->self, no cast, single-target
    UnknownWeaponskill16 = 13516, // OmegaF->self, no cast, single-target
}

public enum SID : uint
{
    VulnerabilityUp = 202, // Boss/Helper->player, extra=0x1
    Superfluid = 1676, // Boss/OmegaF->Boss/OmegaF, extra=0xC8
    OmegaF = 1675, // Boss/OmegaF->Boss/OmegaF, extra=0xC7
    Swiftcast = 167, // none->player, extra=0x0
    OmegaM = 1674, // Boss->Boss, extra=0x0
    Invincibility = 671, // none->OmegaF, extra=0x0
    Omega = 1658, // none->Boss/OmegaF, extra=0x0
    PacketFilterM = 1660, // none->player, extra=0x0
    PacketFilterF = 1661, // none->player, extra=0x0
    LocalResonance = 1662, // none->Boss/OmegaF, extra=0x0
}

public enum IconID : uint
{
    Stackmarker = 62, // player
    Burn = 87, // player
    Spreadmarker = 96, // player
    Spreadmarker2 = 139, // player
}

[ModuleInfo(BossModuleInfo.Maturity.WIP, Contributors = "The Combat Reborn Team (LTS)", GroupType = BossModuleInfo.GroupType.CFC, GroupID = 590, NameID = 7633)]
public class O12NOmegas(WorldState ws, Actor primary) : BossModule(ws, primary, new(100, 100), new ArenaBoundsCircle(20));
