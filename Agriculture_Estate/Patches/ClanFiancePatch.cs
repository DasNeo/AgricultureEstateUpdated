using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace AgricultureEstate
{
    class ClanFiancePatch
    {
        private static void Postfix(Clan clan, ref ExplainedNumber goldChange, bool applyWithdrawals = false)
        {
            if (clan == Hero.MainHero.Clan)
            {
                float num1 = 0.0f;
                float slaveIncome = 0f;
                foreach (KeyValuePair<Settlement, VillageLand> villageLand1 in AgricultureEstateBehavior.VillageLands)
                {
                    num1 += AgricultureEstateBehavior.CalculateGold(villageLand1.Value);
                    slaveIncome += villageLand1.Value.LastDayIncome;
                }
                slaveIncome -= num1;
                goldChange.Add(num1, new TextObject("{=agricultureestate_ui_income_unused_land}Rent from unused owned land"));
                if (slaveIncome >= 0)
                {
                    goldChange.Add(slaveIncome, new TextObject("{=agricultureestate_ui_income_slave_production}Sales from slave production"));
                }
            }
        }
    }
}
