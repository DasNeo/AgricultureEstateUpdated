using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace AgricultureEstate
{
    internal static class PerkDescriptionRegistrar
    {
        public static void Register()
        {
            AddPerkDescription(DefaultPerks.Riding.MountedPatrols, new TextObject("{=agricultureestate_perk_mountedpatrols}Agriculture Estate : Slave escape chance reduced by 20%").ToString());
            AddPerkDescription(DefaultPerks.Steward.ForcedLabor, new TextObject("{=agricultureestate_perk_forcedlabor}Agriculture Estate : Allows use of non bandit prisoners for slave labor").ToString());
            AddPerkDescription(DefaultPerks.Roguery.Manhunter, new TextObject("{=agricultureestate_perk_slavetrader}Agriculture Estate : Estates buy slaves at 20% reduced cost").ToString());
            AddPerkDescription(DefaultPerks.Trade.InsurancePlans, new TextObject("{=agricultureestate_perk_insuranceplans}Agriculture Estate : Half of the cost of land siezed durring war is returned").ToString());
            AddPerkDescription(DefaultPerks.Trade.RapidDevelopment, new TextObject("{=agricultureestate_perk_rapiddevelopment}Agriculture Estate : Half of the cost of land siezed durring war is returned").ToString());
            AddPerkDescription(DefaultPerks.Trade.TradeyardForeman, new TextObject("{=agricultureestate_perk_tradeyardforeman}Agriculture Estate : Estates in villages that have primary production clay, iron, cotton, or silver has 20% increased slave output").ToString());
            AddPerkDescription(DefaultPerks.Trade.GranaryAccountant, new TextObject("{=agricultureestate_perk_granaryaccountant}Agriculture Estate : Estates in villages that have primary production grain, olives, fish, date has 20% increased slave output").ToString());
            AddPerkDescription(DefaultPerks.Riding.Breeder, new TextObject("{=agricultureestate_perk_breeder}Agriculture Estate : Estates in villages that have primary production horses has 10% increased slave output").ToString());
            AddPerkDescription(DefaultPerks.Steward.Contractors, new TextObject("{=agricultureestate_perk_contractors}Agriculture Estate : Upgrades in estates cost 15% less").ToString());
            AddPerkDescription(DefaultPerks.Engineering.Foreman, new TextObject("{=agricultureestate_perk_foreman}Agriculture Estate : Completing a land clearance project adds 30 hearths to the village").ToString());
        }

        private static void AddPerkDescription(PerkObject perk, string description)
        {
            TextObject textObject = new TextObject(perk.Description.ToString() + "\n \n" + description);
            typeof(PropertyObject).GetField("_description", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).SetValue(perk, textObject);
        }

    }
}
