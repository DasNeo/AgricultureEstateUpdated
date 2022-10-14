// Decompiled with JetBrains decompiler
// Type: AgricultureEstate.EstateListVM
// Assembly: AgricultureEstate, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A4103D21-1273-439E-B48D-A11FAB56D6B9
// Assembly location: C:\Users\andre\Downloads\AgricultureEstate\bin\Win64_Shipping_Client\AgricultureEstate.dll

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AgricultureEstate
{
    public class EstateListVM : ViewModel
    {
        public static string SortType = "Village Name";

        public MBBindingList<EstateEntryVM> Estates { get; set; }

        public MBBindingList<EstateEntryVM> Title { get; set; }

        public EstateListVM()
        {
            this.Title = new MBBindingList<EstateEntryVM>();
            Title.Add(new EstateEntryVM(null, true));
            this.Estates = new MBBindingList<EstateEntryVM>();
            foreach (KeyValuePair<Settlement, VillageLand> villageLand in AgricultureEstateBehavior.VillageLands)
            {
                VillageLand land = villageLand.Value;
                if (land.OwnedPlots > 0 || land.OwnedUndevelopedPlots > 0)
                    Estates.Add(new EstateEntryVM(land, false));
            }
        }

        public void sort()
        {
            Estates.Clear();
            List<EstateEntryVM> estateEntryVmList = new List<EstateEntryVM>();
            foreach (KeyValuePair<Settlement, VillageLand> villageLand in AgricultureEstateBehavior.VillageLands)
            {
                VillageLand land = villageLand.Value;
                if (land.OwnedPlots > 0 || land.OwnedUndevelopedPlots > 0)
                    estateEntryVmList.Add(new EstateEntryVM(land, false));
            }
            estateEntryVmList.Sort(new Comparison<EstateEntryVM>(Compare));
            foreach (EstateEntryVM estateEntryVm in estateEntryVmList)
                Estates.Add(estateEntryVm);
        }

        private static int Compare(EstateEntryVM x, EstateEntryVM y)
        {
            if (x == null || x._village_land == null)
                return -1;
            if (y == null || y._village_land == null)
                return 1;
            switch (SortType)
            {
                case "Current Project":
                    return string.Compare(x._village_land.CurrentProject, y._village_land.CurrentProject);
                case "Current Project Progress":
                    return IntCompare(x._village_land.ProjectProgress, y._village_land.ProjectProgress);
                case "Last Day Income":
                    return IntCompare(x._village_land.LastDayIncome, y._village_land.LastDayIncome);
                case "Owned Plots":
                    return IntCompare(x._village_land.OwnedUndevelopedPlots, y._village_land.OwnedUndevelopedPlots);
                case "Owned Undeveloped Plots":
                    return IntCompare(x._village_land.OwnedPlots, y._village_land.OwnedPlots);
                case "Primary Production":
                    return string.Compare((x._village_land?.Village?.VillageType.PrimaryProduction.Name ?? new TextObject()).ToString(), (y._village_land?.Village?.VillageType.PrimaryProduction.Name ?? new TextObject()).ToString());
                case "Slaves":
                    return IntCompare(x._village_land.Prisoners.TotalManCount, y._village_land.Prisoners.TotalManCount);
                case "Stockpile":
                    return IntCompare(getCount(x._village_land.Stockpile), getCount(y._village_land.Stockpile));
                case "Village Name":
                    return string.Compare(x._village_land?.Village?.Name.ToString(), y._village_land?.Village?.Name.ToString());
                default:
                    return 0;
            }
        }

        private static int getCount(ItemRoster items)
        {
            int count = 0;
            foreach (ItemRosterElement itemRosterElement in items)
                count += itemRosterElement.Amount;
            return count;
        }

        private static int IntCompare(int x, int y)
        {
            if (x > y)
                return 1;
            return y > x ? -1 : 0;
        }

        public void Close() => AgricultureEstateBehavior.DeleteVMLayer2();

        public void Sort()
        {
            List<InquiryElement> inquiryElementList = new List<InquiryElement>();
            inquiryElementList.Add(new InquiryElement("Current Project Progress", "Current Project Progress", null));
            inquiryElementList.Add(new InquiryElement("Current Project", "Current Project", null));
            inquiryElementList.Add(new InquiryElement("Primary Production", "Primary Production", null));
            inquiryElementList.Add(new InquiryElement("Stockpile", "Stockpile", null));
            inquiryElementList.Add(new InquiryElement("Slaves", "Slaves", null));
            inquiryElementList.Add(new InquiryElement("Last Day Income", "Last Day Income", null));
            inquiryElementList.Add(new InquiryElement("Owned Undeveloped Plots", "Owned Undeveloped Plots", null));
            inquiryElementList.Add(new InquiryElement("Owned Plots", "Owned Plots", null));
            inquiryElementList.Add(new InquiryElement("Village Name", "Village Name", null));
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData("Sort by", "", inquiryElementList, true, 1, "Continue", null, (args =>
            {
                List<InquiryElement> source = args;
                if (source != null && !(source).Any())
                    return;
                InformationManager.HideInquiry();
                SortType = (args).Select(element => element?.Identifier.ToString() ?? "").First<string>();
                if (AgricultureEstateBehavior.estateListVM == null)
                    return;
                AgricultureEstateBehavior.estateListVM.sort();
            }), null, ""), false);
        }
    }
}
