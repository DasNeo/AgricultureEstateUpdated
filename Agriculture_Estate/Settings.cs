// Decompiled with JetBrains decompiler
// Type: AgricultureEstate.Settings
// Assembly: AgricultureEstate, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A4103D21-1273-439E-B48D-A11FAB56D6B9
// Assembly location: C:\Users\andre\Downloads\AgricultureEstate\bin\Win64_Shipping_Client\AgricultureEstate.dll

using MCM.Abstractions.Attributes.v1;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.Localization;

namespace AgricultureEstate
{
    public class Settings : AttributeGlobalSettings<Settings>
    {

        [SettingPropertyInteger("{=agricultureestate_mcm_dev_buy}Developed Plot Buy Price", 0, 100000, RequireRestart = false, Order = 0)]
        public int PlotBuyPrice { get; set; } = 800;
        [SettingPropertyInteger("{=agricultureestate_mcm_dev_sell}Developed Plot Sell Price", 0, 100000, RequireRestart = false, Order = 1)]
        public int PlotSellPrice { get; set; } = 200;
        [SettingPropertyInteger("{=agricultureestate_mcm_undev_buy}Undeveloped Plot Buy Price", 0, 100000, RequireRestart = false, Order = 2)]
        public int UndevelopedPlotBuyPrice { get; set; } = 400;
        [SettingPropertyInteger("{=agricultureestate_mcm_undev_sell}Undeveloped Plot Sell Price", 0, 100000, RequireRestart = false, Order = 3)]
        public int UndevelopedPlotSellPrice { get; set; } = 200;
        [SettingPropertyInteger("{=agricultureestate_mcm_proj_cost}Project Cost", 0, 500000, RequireRestart = false, Order = 4)]
        public int ProjectCost { get; set; } = 20000;
        [SettingPropertyInteger("{=agricultureestate_mcm_proj_time}Project Time", 1, 100, RequireRestart = false, Order = 5)]
        public int ProjectTime { get; set; } = 10;
        [SettingPropertyFloatingInteger("{=agricultureestate_mcm_scale_rent}Scaling of rent for plots", 0, 20, RequireRestart = false, Order = 6)]
        public float LandRentScale { get; set; } = 1f;
        [SettingPropertyFloatingInteger("{=agricultureestate_mcm_scale_production}Scaling of slave production", 0, 20, RequireRestart = false, Order = 7)]
        public float SlaveProductionScale { get; set; } = 1f;
        [SettingPropertyFloatingInteger("{=agricultureestate_mcm_slave_decline}Slave decline rate modifier", 0, 2, RequireRestart = false, Order = 8, HintText = "{=agricultureestate_mcm_slave_decline_hint}Setting the modifier to 0 disables slave decline.")]
        public float SlaveDeclineModifier { get; set; } = 1f;

        public override string Id => "AgricultureEstate";

        public override string DisplayName => "Agriculture Estate";

        public override string FolderName => "AgricultureEstate";

        public override string FormatType => "json";
    }
}
