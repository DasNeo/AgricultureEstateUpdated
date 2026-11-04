// Minimal doubles for the linked sale prefixes. No game binaries are loaded by the unit tests.
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class HarmonyPatch : Attribute
    {
        public string Method { get; }
        public HarmonyPatch(Type type, string method) { Method = method; }
    }
}
namespace AgricultureEstate
{
    internal static class AgricultureEstateBehavior
    {
        public static readonly Dictionary<Settlement, VillageLand> VillageLands = new Dictionary<Settlement, VillageLand>();
    }
    internal sealed class VillageLand
    {
        public bool BuySlaves;
        public int OwnedPlots;
        public TroopRoster Prisoners = new TroopRoster();
    }
    internal static class EstateConfiguration { public static bool AllowAllPrisonersAsSlaves; }
}
namespace AgricultureEstate.l18n
{
    internal static class Localization
    {
        public static string SetTextVariables(string text, params KeyValuePair<string, string?>[] variables) => text;
    }
}
namespace TaleWorlds.CampaignSystem
{
    internal enum Occupation { None, Bandit }
    internal sealed class CharacterObject { public bool IsHero; public Occupation Occupation; }
    internal sealed class Hero
    {
        public static Hero MainHero = new Hero();
        public int Gold;
        public string Name = "Seller";
        public bool GetPerkValue(object perk) => false;
    }
    internal sealed class Campaign
    {
        public static readonly Campaign Current = new Campaign();
        public readonly GameModels Models = new GameModels();
    }
    internal sealed class GameModels { public readonly RansomModel RansomValueCalculationModel = new RansomModel(); }
    internal sealed class RansomModel { public int PrisonerRansomValue(CharacterObject character, Hero hero) => 10; }
}
namespace TaleWorlds.CampaignSystem.CharacterDevelopment
{
    internal static class DefaultPerks
    {
        internal static class Roguery { public static readonly object Manhunter = new object(); }
    }
}
namespace TaleWorlds.CampaignSystem.Party
{
    internal sealed class MobileParty
    {
        public static readonly MobileParty MainParty = new MobileParty();
        public Settlement CurrentSettlement = null!;
    }
    internal sealed class PartyBase
    {
        public MobileParty MobileParty = null!;
        public Hero LeaderHero = null!;
        public TroopRoster PrisonRoster = new TroopRoster();
    }
}
namespace TaleWorlds.CampaignSystem.Roster
{
    internal sealed class TroopRosterElement
    {
        public CharacterObject Character = null!;
        public int Number;
    }
    internal sealed class TroopRoster
    {
        private readonly Dictionary<CharacterObject, int> _counts = new Dictionary<CharacterObject, int>();
        public int TotalManCount => _counts.Values.Sum();
        public static TroopRoster CreateDummyTroopRoster() => new TroopRoster();
        public int GetTroopCount(CharacterObject character) => _counts.TryGetValue(character, out int count) ? count : 0;
        public void AddToCounts(CharacterObject character, int count, bool a = false, int b = 0, int c = 0, bool d = true, int e = -1)
        {
            int total = GetTroopCount(character) + count;
            if (total < 0) throw new InvalidOperationException("Prisoner removed twice.");
            if (total == 0) _counts.Remove(character);
            else _counts[character] = total;
        }
        public IEnumerable<TroopRosterElement> GetTroopRoster() => _counts.Select(pair => new TroopRosterElement { Character = pair.Key, Number = pair.Value }).ToArray();
        public TroopRoster CloneRosterData()
        {
            var clone = new TroopRoster();
            foreach (var pair in _counts) clone.AddToCounts(pair.Key, pair.Value);
            return clone;
        }
    }
}
namespace TaleWorlds.CampaignSystem.Settlements
{
    internal sealed class Settlement
    {
        public bool IsTown;
        public bool IsCastle { get; set; }
        public string Name = "Town";
        public List<Village> BoundVillages = new List<Village>();
    }
    internal sealed class Village { public Settlement Settlement = null!; public string Name = "Village"; }
}
namespace TaleWorlds.CampaignSystem.Actions
{
    internal static class SellPrisonersAction { }
    internal static class GiveGoldAction
    {
        public static void ApplyBetweenCharacters(Hero from, Hero to, int gold, bool notify)
        {
            from.Gold -= gold;
            to.Gold += gold;
        }
    }
}
namespace TaleWorlds.Library
{
    internal sealed class InformationMessage { public InformationMessage(string text) { } }
    internal static class InformationManager { public static void DisplayMessage(InformationMessage message) { } }
}
