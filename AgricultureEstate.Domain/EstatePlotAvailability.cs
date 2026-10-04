using System;

namespace AgricultureEstate.Domain
{
    public static class EstatePlotAvailability
    {
        public static int Adjust(int available, int previousSetting, int currentSetting) =>
            Math.Max(0, available + currentSetting - previousSetting);
    }
}
