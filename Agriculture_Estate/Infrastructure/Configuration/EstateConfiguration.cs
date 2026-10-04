namespace AgricultureEstate
{
    internal static class EstateConfiguration
    {
        public static int PlotBuyPrice => Settings.Instance?.PlotBuyPrice ?? 800;
        public static int PlotSellPrice => Settings.Instance?.PlotSellPrice ?? 200;
        public static int UndevelopedPlotBuyPrice => Settings.Instance?.UndevelopedPlotBuyPrice ?? 400;
        public static int UndevelopedPlotSellPrice => Settings.Instance?.UndevelopedPlotSellPrice ?? 200;
        public static int ProjectCost => Settings.Instance?.ProjectCost ?? 20000;
        public static int ProjectDurationHours => (Settings.Instance?.ProjectTime ?? 10) * 24;
        public static float LandRentScale => Settings.Instance?.LandRentScale ?? 1;
        public static float SlaveProductionScale => Settings.Instance?.SlaveProductionScale ?? 1;
    }
}
