using AgricultureEstate.Domain;
using Xunit;

namespace AgricultureEstate.Tests
{
    public class EstateEconomyTests
    {
        [Theory]
        [InlineData(0, 0, false, 0f)]
        [InlineData(10, 0, true, 0f)]
        [InlineData(10, 0, false, 100f)]
        [InlineData(10, 50, false, 50f)]
        [InlineData(10, 100, false, 0f)]
        [InlineData(10, 150, false, 0f)]
        public void RentUsesOnlyUnusedLand(int plots, int prisoners, bool looted, float expected) =>
            Assert.Equal(expected, EstateEconomy.CalculateRent(plots, prisoners, 1000f, 1f, looted));

        [Theory]
        [InlineData(0, false, 1f, 5f)]
        [InlineData(8, false, 1f, 1f)]
        [InlineData(0, true, 1f, 4f)]
        [InlineData(0, false, 0f, 0f)]
        public void DeclineUsesPatrolsPerkAndModifier(int patrols, bool perk, float modifier, float expected) =>
            Assert.Equal(expected, EstateEconomy.CalculateDecline(patrols, perk, modifier));

        [Theory]
        [InlineData(49, 0, 0f)]
        [InlineData(50, 0, 1f)]
        [InlineData(100, 0, 1f)]
        [InlineData(101, 0, 3f)]
        [InlineData(50, 8, 0.2f)]
        public void RevoltRiskPreservesMilitiaThresholds(int prisoners, int patrols, float expected) =>
            Assert.Equal(expected, EstateEconomy.CalculateRevoltRisk(prisoners, 10f, patrols), 5);
    }
}
