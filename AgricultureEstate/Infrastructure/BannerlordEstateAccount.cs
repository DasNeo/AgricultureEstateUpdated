using AgricultureEstate.Application;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace AgricultureEstate.Infrastructure
{
    internal sealed class BannerlordEstateAccount : IEstateAccount
    {
        private readonly VillageLand _land;

        public BannerlordEstateAccount(VillageLand land) => _land = land;

        public int Gold => Hero.MainHero.Gold;

        public void Spend(int amount) => GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, amount, false);

        public void Receive(int amount)
        {
            _land.Village?.ChangeGold(amount);
            GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, amount, false);
        }
    }
}
