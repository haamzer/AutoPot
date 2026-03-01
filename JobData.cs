using System.Collections.Generic;

namespace AutoPot.Data;

public enum MainStat
{
    STR,
    DEX,
    INT,
    MND
}

public static class JobData
{
    public static readonly Dictionary<uint, MainStat> JobMainStat = new()
    {
        { 19, MainStat.STR },
        { 21, MainStat.STR },
        { 32, MainStat.STR },
        { 37, MainStat.STR },

        { 20, MainStat.STR },
        { 22, MainStat.STR },
        { 34, MainStat.STR },
        { 39, MainStat.STR },
        { 41, MainStat.STR },

        { 30, MainStat.DEX },

        { 23, MainStat.DEX },
        { 31, MainStat.DEX },
        { 38, MainStat.DEX },

        { 25, MainStat.INT },
        { 27, MainStat.INT },
        { 35, MainStat.INT },
        { 42, MainStat.INT },

        { 24, MainStat.MND },
        { 28, MainStat.MND },
        { 33, MainStat.MND },
        { 40, MainStat.MND },
    };
}