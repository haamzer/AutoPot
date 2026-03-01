using System.Collections.Generic;

namespace AutoPot.Data;

public enum PotionTier
{
    Grade3Tincture = 3,
    Grade4Tincture = 4,
    Grade5Tincture = 5,
    Grade6Tincture = 6,
    Grade7Tincture = 7,
    Grade8Tincture = 8,
    Grade1Gemdraught = 9,
    Grade2Gemdraught = 10,
    Grade3Gemdraught = 11,
    Grade4Gemdraught = 12,
}

public static class PotionData
{
    public static readonly Dictionary<(PotionTier Tier, MainStat Stat), uint> PotionItemIds = new()
    {
        { (PotionTier.Grade3Tincture, MainStat.STR), 19886 },
        { (PotionTier.Grade3Tincture, MainStat.DEX), 19887 },
        { (PotionTier.Grade3Tincture, MainStat.INT), 19889 },
        { (PotionTier.Grade3Tincture, MainStat.MND), 19890 },

        { (PotionTier.Grade4Tincture, MainStat.STR), 22447 },
        { (PotionTier.Grade4Tincture, MainStat.DEX), 22448 },
        { (PotionTier.Grade4Tincture, MainStat.INT), 22450 },
        { (PotionTier.Grade4Tincture, MainStat.MND), 22451 },

        { (PotionTier.Grade5Tincture, MainStat.STR), 27960 },
        { (PotionTier.Grade5Tincture, MainStat.DEX), 27961 },
        { (PotionTier.Grade5Tincture, MainStat.INT), 27963 },
        { (PotionTier.Grade5Tincture, MainStat.MND), 27964 },

        { (PotionTier.Grade6Tincture, MainStat.STR), 36109 },
        { (PotionTier.Grade6Tincture, MainStat.DEX), 36110 },
        { (PotionTier.Grade6Tincture, MainStat.INT), 36112 },
        { (PotionTier.Grade6Tincture, MainStat.MND), 36113 },

        { (PotionTier.Grade7Tincture, MainStat.STR), 37838 },
        { (PotionTier.Grade7Tincture, MainStat.DEX), 37839 },
        { (PotionTier.Grade7Tincture, MainStat.INT), 37841 },
        { (PotionTier.Grade7Tincture, MainStat.MND), 37842 },

        { (PotionTier.Grade8Tincture, MainStat.STR), 39727 },
        { (PotionTier.Grade8Tincture, MainStat.DEX), 39728 },
        { (PotionTier.Grade8Tincture, MainStat.INT), 39730 },
        { (PotionTier.Grade8Tincture, MainStat.MND), 39731 },

        { (PotionTier.Grade1Gemdraught, MainStat.STR), 44157 },
        { (PotionTier.Grade1Gemdraught, MainStat.DEX), 44158 },
        { (PotionTier.Grade1Gemdraught, MainStat.INT), 44160 },
        { (PotionTier.Grade1Gemdraught, MainStat.MND), 44161 },

        { (PotionTier.Grade2Gemdraught, MainStat.STR), 44163 },
        { (PotionTier.Grade2Gemdraught, MainStat.DEX), 44164 },
        { (PotionTier.Grade2Gemdraught, MainStat.INT), 44166 },
        { (PotionTier.Grade2Gemdraught, MainStat.MND), 44167 },

        { (PotionTier.Grade3Gemdraught, MainStat.STR), 45995 },
        { (PotionTier.Grade3Gemdraught, MainStat.DEX), 45996 },
        { (PotionTier.Grade3Gemdraught, MainStat.INT), 45998 },
        { (PotionTier.Grade3Gemdraught, MainStat.MND), 45999 },

        { (PotionTier.Grade4Gemdraught, MainStat.STR), 49234 },
        { (PotionTier.Grade4Gemdraught, MainStat.DEX), 49235 },
        { (PotionTier.Grade4Gemdraught, MainStat.INT), 49237 },
        { (PotionTier.Grade4Gemdraught, MainStat.MND), 49238 },
    };

    public static string GetTierName(PotionTier tier) => tier switch
    {
        PotionTier.Grade3Tincture => "Grade 3 Tincture",
        PotionTier.Grade4Tincture => "Grade 4 Tincture",
        PotionTier.Grade5Tincture => "Grade 5 Tincture",
        PotionTier.Grade6Tincture => "Grade 6 Tincture",
        PotionTier.Grade7Tincture => "Grade 7 Tincture",
        PotionTier.Grade8Tincture => "Grade 8 Tincture",
        PotionTier.Grade1Gemdraught => "Grade 1 Gemdraught",
        PotionTier.Grade2Gemdraught => "Grade 2 Gemdraught",
        PotionTier.Grade3Gemdraught => "Grade 3 Gemdraught",
        PotionTier.Grade4Gemdraught => "Grade 4 Gemdraught",
        _ => "Unknown"
    };

    public static string GetStatName(MainStat stat) => stat switch
    {
        MainStat.STR => "Strength",
        MainStat.DEX => "Dexterity",
        MainStat.INT => "Intelligence",
        MainStat.MND => "Mind",
        _ => "Unknown"
    };
}