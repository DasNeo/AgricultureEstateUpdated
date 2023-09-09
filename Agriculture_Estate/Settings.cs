// Decompiled with JetBrains decompiler
// Type: AgricultureEstate.Settings
// Assembly: AgricultureEstate, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A4103D21-1273-439E-B48D-A11FAB56D6B9
// Assembly location: C:\Users\andre\Downloads\AgricultureEstate\bin\Win64_Shipping_Client\AgricultureEstate.dll

using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace AgricultureEstate
{
    public class Settings : AttributeGlobalSettings<Settings>
    {

        [SettingPropertyInteger("Developed Plot Buy Price", 0, 100000, RequireRestart = false)]
        public int PlotBuyPrice { get; set; } = 800;
        [SettingPropertyInteger("Developed Plot Sell Price", 0, 100000, RequireRestart = false)]
        public int PlotSellPrice { get; set; } = 200;
        [SettingPropertyInteger("Undeveloped Plot Buy Price", 0, 100000, RequireRestart = false)]
        public int UndevelopedPlotBuyPrice { get; set; } = 400;
        [SettingPropertyInteger("Undeveloped Plot Sell Price", 0, 100000, RequireRestart = false)]
        public int UndevelopedPlotSellPrice { get; set; } = 200;
        [SettingPropertyInteger("Project Cost", 0, 500000, RequireRestart = false)]
        public int ProjectCost { get; set; } = 20000;
        [SettingPropertyInteger("Project Time", 1, 100, RequireRestart = false)]
        public int ProjectTime { get; set; } = 10;
        [SettingPropertyFloatingInteger("Scaling of rent for plots", 0, 20, RequireRestart = false)]
        public float LandRentScale { get; set; } = 1f;
        [SettingPropertyFloatingInteger("Scaling of slave production", 0, 20, RequireRestart = false)]
        public float SlaveProductionScale { get; set; } = 1f;
        [SettingPropertyFloatingInteger("Slave decline rate modifier", 0, 2, RequireRestart = false, HintText = "Setting the modifier to 0 disables slave decline.")]
        public float SlaveDeclineModifier { get; set; } = 1f;

        public override string Id => "AgricultureEstate";

        public override string DisplayName => "Agriculture Estate";

        public override string FolderName => "AgricultureEstate";

        public override string FormatType => "json";
    }
}
