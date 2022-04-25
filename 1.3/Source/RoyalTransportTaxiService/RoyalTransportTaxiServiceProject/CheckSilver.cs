using RimWorld;
using Verse;

namespace RoyalTransportTaxiService
{
    static class BromsCheckSilver // was util from call traders mod
    {
        public static bool HasEnoughSilver(Map map, out int found)
        {
            found = 0;

            int need = 10 // was Settings.Cost;
            if (need == 0)
                return true;

            foreach (Thing t in TradeUtility.AllLaunchableThingsForTrade(map))
            {
                if (t.def == ThingDefOf.Silver)
                {
                    found += t.stackCount;
                    if (found >= need)
                        return true;
                }
            }
            return false;
        }
    }
}
