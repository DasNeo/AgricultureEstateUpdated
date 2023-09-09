// Decompiled with JetBrains decompiler
// Type: AgricultureEstate.EstateEntryVM
// Assembly: AgricultureEstate, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A4103D21-1273-439E-B48D-A11FAB56D6B9
// Assembly location: C:\Users\andre\Downloads\AgricultureEstate\bin\Win64_Shipping_Client\AgricultureEstate.dll

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AgricultureEstate
{
    public class EstateEntryVM : ViewModel
    {
        public VillageLand? _village_land;
        private bool _is_title;

        public EstateEntryVM(VillageLand? land, bool Title)
        {
            this._village_land = land;
            this._is_title = Title;
        }

        public string Name => this._is_title ? new TextObject("{=agricultureestate_estate_village}Village").ToString() : (this._village_land?.Village?.Name ?? new TextObject()).ToString();

        public string OwnedPlots => this._is_title ? new TextObject("{=agricultureestate_owned_plots}Owned Plots").ToString() : this._village_land?.OwnedPlots.ToString() ?? new TextObject().ToString();

        public string OwnedUndevelopedPlots => this._is_title ? new TextObject("{=agricultureestate_owned_undev_plots}Owned Undeveloped Plots").ToString() : this._village_land?.OwnedUndevelopedPlots.ToString() ?? new TextObject().ToString();

        public string LastDayIncome => this._is_title ? new TextObject("{=agricultureestate_last_day_income}Last Day Income").ToString() : this._village_land?.LastDayIncome.ToString() ?? new TextObject().ToString();

        public string Slaves
        {
            get
            {
                if (this._is_title)
                    return new TextObject("{=agricultureestate_slaves}Slaves").ToString();
                int num = this._village_land?.Prisoners.TotalManCount ?? 0;
                string str1 = num.ToString();
                num = (this._village_land?.OwnedPlots ?? 0) * 10;
                string str2 = num.ToString();
                return str1 + "/" + str2;
            }
        }

        public string Stockpile
        {
            get
            {
                if (this._is_title)
                    return new TextObject("{=agricultureestate_stockpile}Stockpile").ToString();
                int num = 0;
                if (_village_land is not null)
                {
                    foreach (ItemRosterElement itemRosterElement in _village_land.Stockpile)
                        num += itemRosterElement.Amount;
                }
                return num.ToString() + "/" + this._village_land?.StorageCapacity.ToString();
            }
        }

        public string PrimaryProduction => this._is_title ? new TextObject("{=agricultureestate_primary_production}Primary Production").ToString() : (_village_land?.Village?.VillageType.PrimaryProduction.Name ?? new TextObject()).ToString();

        public string CurrentProject => this._is_title ? new TextObject("{=agricultureestate_current_project}Current Project").ToString() : _village_land?.CurrentProjectL18N.ToString() ?? "";

        public string CurrentProjectProgress => this._is_title ? new TextObject("{=agricultureestate_current_project_progress}Current Project Progress").ToString() : this._village_land?.ProjectProgress.ToString() + "/240";

        public void Click()
        {
            AgricultureEstateBehavior.DeleteVMLayer();
            AgricultureEstateBehavior.DeleteVMLayer2();
            Campaign.Current.EncyclopediaManager.GoToLink(_village_land?.Village?.Settlement.EncyclopediaLink);
        }
    }
}
