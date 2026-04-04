namespace BOTF3D.Core
{
    public enum CivEnum
    {
        FED,
        ROM,
        KLING,
        CARD,
        DOM,
        BORG,
        TERRAN,
        #region minors
        ACAMARIANS,
        AKAALI,
        AKRITIRIANS,
        ALDEANS,
        ALGOLIANS,
        ALSAURIANS,
        ANDORIANS,
        ANGOSIANS,
        ANKARI,
        ANTEDEANS,
        ANTICANS,
        ARBAZAN,
        ARDANANS,
        ARGRATHI,
        ARKARIANS,
        ATREANS,
        AXANAR,
        BAJORANS,
        BAKU,
        BANDI,
        BANEANS,
        BARZANS,
        BENZITES,
        BETAZOIDS,
        BILANAIANS,
        BOLIANS,
        BOMAR,
        BOSLICS,
        BOTHA,
        BREELLIANS,
        BREEN,
        BREKKIANS,
        BYNARS,
        CAIRN,
        CALDONIANS,
        CAPELLANS,
        CHALNOTH,
        CORIDAN,
        CORVALLENS,
        CYTHERIANS,
        DELTANS,
        DENOBULANS,
        DEVORE,
        DOPTERIANS,
        DOSI,
        DRAI,
        DREMANS,
        EDO,
        ELAURIANS,
        ELAYSIANS,
        ENTHARANS,
        EVORA,
        EXCALBIANS,
        FERENGI,
        FLAXIANS,
        GORN,
        GRAZERITES,
        HAAKONIANS,
        HALKANS,
        HAZARI,
        HEKARANS,
        HIROGEN,
        HORTA,
        IYAARANS,
        JNAII,
        KAELON,
        KAREMMA,
        KAZON,
        KELLERUN,
        KESPRYTT,
        KLAESTRON,
        KRADIN,
        KREETASSANS,
        KRIOSIANS,
        KTARIANS,
        LEDOSIANS,
        LISSEPIANS,
        LOKIRRIM,
        LURIANS,
        MALCORIANS,
        MALON,
        MAQUIS,
        MARKALIANS,
        MERIDIANS,
        MINTAKANS,
        MIRADORN,
        MIZARIANS,
        MOKRA,
        MONEANS,
        NAUSICAANS,
        NECHANI,
        NEZU,
        NORCADIANS,
        NUMIRI,
        NUUBARI,
        NYRIANS,
        OCAMPA,
        ORIONS,
        ORNARANS,
        PAKLED,
        PARADANS,
        QUARREN,
        RAKHARI,
        RAKOSANS,
        RAMATIANS,
        RIGELIANS,
        RISIANS,
        RUTIANS,
        SELAY,
        SHELIAK,
        SIKARIANS,
        SKRREEA,
        SONA,
        SULIBAN,
        TAKARANS,
        TAKARIANS,
        TAKTAK,
        TALARIANS,
        TALAXIANS,
        TALOSIANS,
        TAMARIANS,
        TANUGANS,
        TELLARITES,
        TEPLANS,
        THOLIANS,
        TILONIANS,
        TLANI,
        TRABE,
        TRILL,
        TROGORANS,
        TZENKETHI,
        ULLIANS,
        VAADWAUR,
        VENTAXIANS,
        VHNORI,
        VIDIIANS,
        VISSIANS,
        VORGONS,
        VORI,
        VULCANS,
        WADI,
        XANTHANS,
        XEPOLITES,
        XINDI,
        XYRILLIANS,
        YADERANS,
        YRIDIANS,
        ZAHL,
        ZAKDORN,
        ZALKONIANS,
        ZIBALIANS,
        #endregion
        ZZUNINHABITED1,
        #region More Uninhabited
        ZZUNINHABITED2,
        ZZUNINHABITED3,
        ZZUNINHABITED4,
        ZZUNINHABITED5,
        ZZUNINHABITED6,
        ZZUNINHABITED7,
        ZZUNINHABITED8,
        ZZUNINHABITED9,
        ZZUNINHABITED10,
        ZZUNINHABITED11,
        ZZUNINHABITED12,
        ZZUNINHABITED13,
        ZZUNINHABITED14,
        ZZUNINHABITED15,
        ZZUNINHABITED16,
        ZZUNINHABITED17,
        ZZUNINHABITED18,
        ZZUNINHABITED19,
        ZZUNINHABITED20,
        ZZUNINHABITED21,
        ZZUNINHABITED22,
        ZZUNINHABITED23,
        ZZUNINHABITED24,
        ZZUNINHABITED25,
        ZZUNINHABITED26,
        ZZUNINHABITED27,
        ZZUNINHABITED28,
        ZZUNINHABITED29,
        ZZUNINHABITED30,
        ZZUNINHABITED31,
        ZZUNINHABITED32,
        ZZUNINHABITED33,
        ZZUNINHABITED34,
        ZZUNINHABITED35,
        ZZUNINHABITED36,
        ZZUNINHABITED37,
        ZZUNINHABITED38,
        ZZUNINHABITED39,
        ZZUNINHABITED40,
        ZZUNINHABITED41,
        ZZUNINHABITED42,
        ZZUNINHABITED43,
        ZZUNINHABITED44,
        ZZUNINHABITED45,
        ZZUNINHABITED46,
        ZZUNINHABITED47,
        ZZUNINHABITED48,
        ZZUNINHABITED49,
        ZZUNINHABITED50,
        ZZUNINHABITED51,
        ZZUNINHABITED52,
        ZZUNINHABITED53,
        None,
        #endregion
    }
    public enum GalaxyMapType
    {
        CANON,
        RANDOM,
        RING,
        WHATEVER
    }
    public enum GalaxySize
    {
        SMALL,
        MEDIUM,
        LARGE,
        PONDEROUS
    }
    public enum TechLevel
    {
        EARLY,
        DEVELOPED,
        ADVANCED,
        SUPREME
    }
    public enum GameMode
    {
        SINGLEPLAYER,
        MULTIPLAYER
    }
    public enum SystemData
    {
        Sys_Int,
        X_Vector3,
        Y_Vector3,
        Z_Vector3,
        Name,
        Civ_Owner,
        Sys_Type,
        Star_Type,
        Planet_1,
        Moons_1,
        Planet_2,
        Moons_2,
        Planet_3,
        Moons_3,
        Planet_4,
        Moons_4,
        Planet_5,
        Moons_5,
        Planet_6,
        Moons_6,
        Planet_7,
        Moons_7,
        Planet_8,
        Moons_8
    }

    public enum ShipType
    {
        Scout,
        Destroyer,
        Cruiser,
        LtCruiser,
        HvyCruiser,
        Transport,
        OneMore
    }

    public enum GalaxyObjectType
    {
        /// <summary>
        /// These galactic objects can be a 'system' 
        /// ( BlueStar through Nebulas and down to the UniComplex) 
        /// system GalaxyObjectTypes are inhabited with a planet or are planet like in the case of the UniComplex. 
        /// Also, system can be uninhabited star systems or nebulas with habitable planet so available for colonization by a transport ship in a visiting fleet
        /// The station will be treated as its own class with fields/properties, but like a fleet that does not move 
        /// The galactic objects black-holes and wormholes will have their own class with fields and/properties
        /// The Target Destination is user defined location in deep space.
        /// </summary>
        None = 0,
        BlueStar,
        WhiteStar,
        YellowStar,
        OrangeStar,
        RedStar,
        Nebula,
        OmarianNebula,
        OrionNebula,
        UniComplex,
        Station,// this and above isHabitable = true
        TargetDestination,// isHabitable = false
        UnknownFleet,
        Fleet,
        BlackHole,// isHabitable = false
        WormHole,// isHabitable = false
        SomeBadAssGalacticObject,
    }
    public enum SystemOnOffButtons
    {
        FactoryOnButton,
        FactoryOffButton,
        PowerPlantOnButton,
        PowerPlantOffButton,
        ShipyardOnButton,
        ShipyardOffbutton,
        ShieldGeneratorOnButton,
        ShieldGeneratorOffbutton,
        OrbitalBatteryOnButton,
        OrbitalBatteryOffButton,
        ResearchCenterOnButton,
        ResearchCenterOffButton,

    }

    public enum PlanetType
    {
        H_uninhabitable,
        J_gasGiant,
        M_habitable,
        L_marginalForLife,
        K_marsLike,
        Moon

    }
    public enum CombatOrders
    {
        Engage, // default order
        Rush,
        Retreat,
        Formation, // aka protect transports
        TargetTransports,
        None,
    }
    public enum StarSysFacilityType
    {
        PowerPlanet,
        Factory,
        Shipyard,
        ShieldGenerator,
        OrbitalBattery,
        ResearchCenter
    }
    public enum CombatType
    {
        None,
        DeepSpaceCombat,
        StarSystemCombat,
        StarSystemInvasion
    }
    public enum GalaxyClickMode
    {
        Normal,
        SetDestination,
        SelectForShipDeploy,
        SelectForShipMerge
        // Future extensions could include:
        // Ping, AttackTarget, etc. New Fleet comes from a UI button, not click mode.
    }
    public enum Menu
    {
        None,
        SystemsMenu,
        ASystemMenu,
        BuildMenu,
        FleetMenu,
        AFleetMenu,
        ShipDeployMenu,
        DiplomacyMenu,
        ADiplomacyMenu,
        IntellMenu,
        EncyclopedianMenu,
        FirstContactMenu,
        HabitableSysMenu,
        Combat
    }
    public enum BuildMenuType
    {
        None,
        Factory,
        PowerPlant,
        Shipyard,
        ShieldGenerator,
        OrbitalBattery,
        ResearchCenter
    }
    public enum DiplomaticStatus
    {
        Neutral,
        Allied,
        AtWar,
        Truce,
        Vassal,
        Protectorate
    }
    public enum LayerType
    {
        Default,
        Additive,
        Single

    }
    public enum AudioCategory
    {
        Music,
        SFX,
        UI,
        Weapon,
        Ambient,
        Voice
    }
    public enum DiplomacyStatusEnum // between two civs held in the DiplomacyData
    {
        War = -20,
        ColdWar = 0,
        Hostile = 20,
        UnFriendly = 40,
        Neutral = 60,
        Friendly = 80,
        Allied = 100,
        Membership = 120
    }
    public enum NegotiationPloysEnum // Diplomacy AI uses to change relations
    {
        OfferTrade, // default
        DeclareWar,
        Sanctions,
        ThreatenAction,
        OfferCulturalExchange,
        OfferTech,
        OfferAid,
        OfferAlliance
    }
    public enum SecretActionsEnum // Secret actions that can be used by the AI or player to change relations
    {
        GatherIntelligence, // default
        Sabotage,
        Disinformation,
        IntellectualTheft,
        Combat
    }
    public enum DiplomaticEventEnum // Diplomacy AI uses to move relations                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       
    {
        War,
        DiscoveredSabotage,
        DiscoveredDisinformation,
        DiscoveredIntellectualTheft,
        CulturalExchange,
        Trade,
        ShareTech,
        GiveAid,
        Alliance
    }
    public enum WarLikeEnum // held by the civ
    {
        Warlike = -2,
        Aggressive = -1,
        Neutral = 0,
        Peaceful = 1,
        Pacifist = 2
    }
    public enum XenophobiaEnum // held by the civ
    {
        Xenophobia = -2,
        Intolerant = -1,
        Indifferent = 0,
        Sympathetic = 1,
        Compassion = 2
    }
    public enum RuthlessEnum
    {
        Ruthless = -2,
        Callous = -1,
        Regulated = 0,
        Ethical = 1,
        Honorable = 2
    }
    public enum GreedyEnum
    {
        Greedy = -2,
        Materialistic = -1,
        Transactional = 0,
        Egaliterian = 1,
        Idealistic = 2
    }
}