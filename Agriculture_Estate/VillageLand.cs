// Decompiled with JetBrains decompiler
// Type: AgricultureEstate.VillageLand
// Assembly: AgricultureEstate, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: A4103D21-1273-439E-B48D-A11FAB56D6B9
// Assembly location: C:\Users\andre\Downloads\AgricultureEstate\bin\Win64_Shipping_Client\AgricultureEstate.dll

using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;
using TaleWorlds.Localization;

namespace AgricultureEstate
{
    public class VillageLand
    {
        private int _avalible_plots;
        private int _owned_plots;
        private int _avalible_undeveloped_plots;
        private int _owned_undeveloped_plots;
        private TroopRoster _prisoners;
        private ItemRoster _stockpile;
        private Village? _village;
        private int _storage_capacity;
        private bool _sell_to_market;
        private int _gold;
        private string _current_project;
        private int _patrol_level;
        private Queue<string> _project_queue;
        private int _project_progress;
        private bool _buy_slaves;
        private int _last_day_income;

        public VillageLand(Village village)
        {
            this._avalible_plots = 10;
            this._avalible_undeveloped_plots = 20;
            this._owned_plots = 0;
            this._owned_undeveloped_plots = 0;
            this._prisoners = TroopRoster.CreateDummyTroopRoster();
            this._stockpile = new ItemRoster();
            this._gold = 0;
            this._current_project = "None";
            this._patrol_level = 0;
            this._project_queue = new Queue<string>();
            this._project_progress = 0;
            this._last_day_income = 0;
        }

        [SaveableProperty(1)]
        public int AvaliblePlots
        {
            get => this._avalible_plots;
            set => this._avalible_plots = value;
        }

        [SaveableProperty(2)]
        public int OwnedPlots
        {
            get => this._owned_plots;
            set => this._owned_plots = value;
        }

        [SaveableProperty(3)]
        public int AvalibleUndevelopedPlots
        {
            get => this._avalible_undeveloped_plots;
            set => this._avalible_undeveloped_plots = value;
        }

        [SaveableProperty(4)]
        public int OwnedUndevelopedPlots
        {
            get => this._owned_undeveloped_plots;
            set => this._owned_undeveloped_plots = value;
        }

        [SaveableProperty(5)]
        public Village? Village
        {
            get => this._village;
            set => this._village = value;
        }

        [SaveableProperty(6)]
        public TroopRoster Prisoners
        {
            get => this._prisoners;
            set => this._prisoners = value;
        }

        [SaveableProperty(7)]
        public ItemRoster Stockpile
        {
            get => this._stockpile;
            set => this._stockpile = value;
        }

        [SaveableProperty(8)]
        public int StorageCapacity
        {
            get => this._storage_capacity;
            set => this._storage_capacity = value;
        }

        [SaveableProperty(9)]
        public bool SellToMarket
        {
            get => this._sell_to_market;
            set => this._sell_to_market = value;
        }

        [SaveableProperty(10)]
        public int Gold
        {
            get => this._gold;
            set => this._gold = value;
        }

        [SaveableProperty(11)]
        public string CurrentProject
        {
            get => this._current_project;
            set => this._current_project = value;
        }

        [SaveableProperty(12)]
        public int PatrolLevel
        {
            get => this._patrol_level;
            set => this._patrol_level = value;
        }

        [SaveableProperty(13)]
        public Queue<string> ProjectQueue
        {
            get => this._project_queue;
            set => this._project_queue = value;
        }

        [SaveableProperty(14)]
        public int ProjectProgress
        {
            get => this._project_progress;
            set => this._project_progress = value;
        }

        [SaveableProperty(15)]
        public bool BuySlaves
        {
            get => this._buy_slaves;
            set => this._buy_slaves = value;
        }

        [SaveableProperty(16)]
        public int LastDayIncome
        {
            get => this._last_day_income;
            set => this._last_day_income = value;
        }

        public float SlaveDeclineRate() => (float)((5.0 - 0.5 * PatrolLevel) * (Hero.MainHero.GetPerkValue(DefaultPerks.Riding.MountedPatrols) ? 0.800000011920929 : 1.0));

        public float SlaveRevoltRisk => Prisoners.TotalManCount < 5.0 * ((double?)this.Village?.Militia ?? 0d) ? 0.0f : (10.0 * ((double?)this.Village?.Militia ?? 0d) < Prisoners.TotalManCount ? 3f : 1f) * (float)(1.0 - 0.10000000149011612 * PatrolLevel);

        public TextObject CurrentProjectL18N
        {
            get => new (this.ProjectName2L18N(this._current_project));
        }
        public TextObject[] GetProjectQueueArrayL18N()
        {
            string[] strArray = this._project_queue.ToArray();
            TextObject[] TOArray = new TextObject[strArray.Length];
            for (int i = 0; i < strArray.Length; i++)
            {
                TOArray[i] = new TextObject(this.ProjectName2L18N(strArray[i]));
            }
            return TOArray;
        }
        public string ProjectName2L18N(string projectName)
        {
            return projectName switch
            {
                "Increase Patrols" => "{=agricultureestate_ui_upgrade_patrols}Increase Patrols",
                "Land Clearance" => "{=agricultureestate_ui_land_clearance}Land Clearance",
                "Expand Storehouse" => "{=agricultureestate_ui_upgrade_storehouse}Expand Storehouse",
                "None" => "{=agricultureestate_no_current_project}No Current Project",
                _ => ""
            };
        }
    }
}
