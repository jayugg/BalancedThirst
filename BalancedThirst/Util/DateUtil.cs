using System;
using BalancedThirst.Systems;

namespace BalancedThirst.Util;

public class DateUtil
{
    public static bool IsHauntingKettleTime()
    {
        return IsHalloween(ConfigSystem.SyncedConfigData.HauntingKettleDays);
    }
    public static bool IsHalloween(int daysBefore = 1)
    {
        DateTime currentDate = DateTime.Now;
        DateTime halloweenDate = new DateTime(currentDate.Year, 10, 31);
        return currentDate > halloweenDate.AddDays(-daysBefore) && currentDate <= halloweenDate;
    }
}