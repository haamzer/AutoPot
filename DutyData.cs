using System.Collections.Generic;

namespace AutoPot.Data;

public static class DutyData
{
    public static readonly Dictionary<uint, DutyInfo> Ultimates = new()
    {
        { 733, new DutyInfo("UCoB", PotionTier.Grade3Tincture) },
        { 777, new DutyInfo("UWU", PotionTier.Grade4Tincture) },
        { 887, new DutyInfo("TEA", PotionTier.Grade6Tincture) },
        { 968, new DutyInfo("DSR", PotionTier.Grade8Tincture) },
        { 1122, new DutyInfo("TOP", PotionTier.Grade1Gemdraught) },
        { 1238, new DutyInfo("FRU", PotionTier.Grade4Gemdraught) },
    };
}

public class DutyInfo
{
    public string Name { get; }
    public PotionTier OptimalTier { get; }

    public DutyInfo(string name, PotionTier optimalTier)
    {
        Name = name;
        OptimalTier = optimalTier;
    }
}