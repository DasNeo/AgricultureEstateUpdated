using AgricultureEstate.Domain;
using Xunit;

namespace AgricultureEstate.Tests
{
    public class EstatePlotAvailabilityTests
    {
        [Theory]
        [InlineData(6, 10, 15, 11)] // Four purchased plots remain purchased.
        [InlineData(16, 20, 30, 26)]
        [InlineData(6, 10, 8, 4)]
        [InlineData(2, 10, 0, 0)] // Reductions cannot remove owned land.
        [InlineData(12, 10, 15, 17)] // Cleared and resold land is preserved.
        [InlineData(6, 10, 10, 6)] // Old saves with default settings stay unchanged.
        public void ChangesAdjustOnlyRemainingInventory(int available, int previous, int current, int expected)
        {
            int adjusted = EstatePlotAvailability.Adjust(available, previous, current);
            Assert.Equal(expected, adjusted);
            Assert.Equal(expected, EstatePlotAvailability.Adjust(adjusted, current, current));
        }
    }
}
