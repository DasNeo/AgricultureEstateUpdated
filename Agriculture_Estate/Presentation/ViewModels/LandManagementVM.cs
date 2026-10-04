using AgricultureEstate.Application;
using AgricultureEstate.Domain;
using AgricultureEstate.l18n;
using Helpers;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace AgricultureEstate
{
    internal class LandManagementVM : ViewModel
    {
        private int PlotBuyPrice => EstateConfiguration.PlotBuyPrice;
        private int PlotSellPrice => EstateConfiguration.PlotSellPrice;
        private int UndevelopedPlotBuyPrice => EstateConfiguration.UndevelopedPlotBuyPrice;
        private int UndevelopedPlotSellPrice => EstateConfiguration.UndevelopedPlotSellPrice;
        private int ProjectCost => EstateConfiguration.ProjectCost;
        private string? _settlementImageID;
        private VillageLand _village_land;

        public LandManagementVM(VillageLand villageLand)
        {
            this._village_land = villageLand;
            if (Equals(this._village_land.Prisoners, null))
                this._village_land.Prisoners = TroopRoster.CreateDummyTroopRoster();
            if (this._village_land.Stockpile == null)
                this._village_land.Stockpile = new ItemRoster();
            if (this._village_land.Village == null)
                this._village_land.Village = Settlement.CurrentSettlement.Village;
            if (this._village_land.StorageCapacity < 500)
                this._village_land.StorageCapacity = 500;
            this.setVillageIcon();
        }

        private void setVillageIcon()
        {
            if (_village_land?.Village?.Settlement.Culture.StringId == "sturgia")
                SettlementImageID = "gui_bg_village_sturgia_t";
            else if (_village_land?.Village?.Settlement.Culture.StringId == "vlandia")
                SettlementImageID = "gui_bg_village_vlanda_t";
            else if (_village_land?.Village?.Settlement.Culture.StringId == "khuzait")
                SettlementImageID = "gui_bg_village_khuzait_t";
            else if (_village_land?.Village?.Settlement.Culture.StringId == "aserai")
                SettlementImageID = "gui_bg_village_aserai_t";
            else if (_village_land?.Village?.Settlement.Culture.StringId == "battania")
                SettlementImageID = "gui_bg_village_battania_t";
            else
                SettlementImageID = "gui_bg_village_empire_t";
        }

        [DataSourceProperty]
        public bool SellToMarket
        {
            get => this._village_land.SellToMarket;
            set
            {
                if (value == this._village_land.SellToMarket)
                    return;
                this._village_land.SellToMarket = value;
                this.OnPropertyChangedWithValue(value, nameof(SellToMarket));
            }
        }

        [DataSourceProperty]
        public bool BuySlaves
        {
            get => this._village_land.BuySlaves;
            set
            {
                if (value == this._village_land.BuySlaves)
                    return;
                this._village_land.BuySlaves = value;
                this.OnPropertyChangedWithValue(value, nameof(BuySlaves));
            }
        }

        [DataSourceProperty]
        public string CurrentProjectIconSting
        {
            get
            {
                if (this._village_land.CurrentProject == "Land Clearance")
                    return "ae_irrigation";
                if (this._village_land.CurrentProject == "Increase Patrols")
                    return "ae_patrols";
                return this._village_land.CurrentProject == "Expand Storehouse" ? "ae_storehouse" : "ae_storehouse"; // TODO: Add default image back in
            }
        }
        [DataSourceProperty]
        public string CloseString => new TextObject("{=agricultureestate_ui_close}Close").ToString();
        [DataSourceProperty]
        public string UpgradeStorehouseString => new TextObject("{=agricultureestate_ui_upgrade_storehouse}Expand Storehouse").ToString();
        [DataSourceProperty]
        public string UpgradePatrolsString => new TextObject("{=agricultureestate_ui_upgrade_patrols}Increase Patrols").ToString();
        [DataSourceProperty]
        public string LandClearanceString => new TextObject("{=agricultureestate_ui_land_clearance}Land Clearance").ToString();
        [DataSourceProperty]
        public string ProjectQueueString => new TextObject("{=agricultureestate_ui_project_queue}Project Queue").ToString();
        [DataSourceProperty]
        public string CancelString => new TextObject("{=agricultureestate_ui_cancel}Cancel").ToString();
        [DataSourceProperty]
        public string SellToMarketTitleString => new TextObject("{=agricultureestate_ui_sell_to_market}Sell to Market : ").ToString();
        [DataSourceProperty]
        public string ProductionDetailsString => new TextObject("{=agricultureestate_ui_production_details}Production Details").ToString();
        [DataSourceProperty]
        public string BuySlavesTitleString => new TextObject("{=agricultureestate_ui_buy_slaves}Buy Slaves :").ToString();
        [DataSourceProperty]
        public string LandUsageTitleString => new TextObject("{=agricultureestate_ui_land_usage}Land Usage : ").ToString();
        [DataSourceProperty]
        public string RevoltRiskTitleString => new TextObject("{=agricultureestate_ui_revolt_risk}Revolt Risk : ").ToString();
        [DataSourceProperty]
        public string SlaveDeclineTitleString => new TextObject("{=agricultureestate_ui_slave_decline}Slave Decline : ").ToString();
        [DataSourceProperty]
        public string AvailableLandPlotsString => new TextObject("{=agricultureestate_ui_availablelandplots}Available : ").ToString();
        [DataSourceProperty]
        public string OwnedLandPlotsString => new TextObject("{=agricultureestate_ui_owned_landplots}Owned : ").ToString();
        [DataSourceProperty]
        public string LandPlotsString => new TextObject("{=agricultureestate_ui_landplots}Land Plots").ToString();
        [DataSourceProperty]
        public string UndevLandPlotsString => new TextObject("{=agricultureestate_ui_undev_landplots}Undeveloped Land Plots").ToString();
        [DataSourceProperty]
        public string LandManagementString => new TextObject("{=agricultureestate_ui_landmanagement}Land Management").ToString();
        [DataSourceProperty]
        public string LedgerString => new TextObject("{=agricultureestate_ui_ledger}Ledger").ToString();
        [DataSourceProperty]
        public string CurrentProjecProgressString => this._village_land.CurrentProject == EstateProjects.None ? "0/0" : this._village_land.ProjectProgress.ToString() + "/" + EstateConfiguration.ProjectDurationHours;
        [DataSourceProperty]
        public string UpgradeString => this._village_land.CurrentProject == EstateProjects.None ? $"      {new TextObject("{=agricultureestate_upgrade_string}Upgrade")}      " : $"  {new TextObject("{=agricultureestate_add_to_queue}Add to Queue")}  ";
        [DataSourceProperty]
        public string CurrentProjectString => this._village_land.CurrentProjectL18N.ToString();
        [DataSourceProperty]
        public string SellToMarketString => this.SellToMarket ? new TextObject("{=agricultureestate_on}On").ToString() : new TextObject("{=agricultureestate_off}Off").ToString();
        [DataSourceProperty]
        public string BuyString => new TextObject("{=agricultureestate_ui_buy}Buy").ToString();
        [DataSourceProperty]
        public string SellString => new TextObject("{=agricultureestate_ui_sell}Sell").ToString();
        [DataSourceProperty]
        public string ManageString => new TextObject("{=agricultureestate_ui_manage}Manage").ToString();
        [DataSourceProperty]
        public string BuySlavesString => this.BuySlaves ? new TextObject("{=agricultureestate_on}On").ToString() : new TextObject("{=agricultureestate_off}Off").ToString();
        [DataSourceProperty]
        public string SellToMarketButton => this.SellToMarket ? "ButtonBrush1" : "ButtonBrush2";
        [DataSourceProperty]
        public string BuySlavesButton => this.BuySlaves ? "ButtonBrush1" : "ButtonBrush2";
        [DataSourceProperty]
        public string UpgradesTitle => new TextObject("{=agricultureestate_ui_upgrades}Upgrades").ToString();
        [DataSourceProperty]
        public string SlavesTitle => new TextObject("{=agricultureestate_ui_slaves}Slaves").ToString();
        [DataSourceProperty]
        public string CapacityText => new TextObject("{=agricultureestate_ui_capacity}Capacity : ").ToString();
        [DataSourceProperty]
        public string StockpileTitle => new TextObject("{=agricultureestate_ui_stockpile}Stockpile").ToString();
        [DataSourceProperty]
        public string CurrentProjectTitle => new TextObject("{=agricultureestate_ui_current_project}Current Project").ToString();
        [DataSourceProperty]
        public string ProgressTitle => new TextObject("{=agricultureestate_ui_progress}Progress : ").ToString();


        
        [DataSourceProperty]
        public string SettlementImageID
        {
            get => _settlementImageID ?? "";
            set
            {
                if (!(value != this._settlementImageID))
                    return;
                this._settlementImageID = value;
                this.OnPropertyChangedWithValue(value, nameof(SettlementImageID));
            }
        }
        [DataSourceProperty]
        public int AvaliblePlots
        {
            get => this._village_land.AvaliblePlots;
            set
            {
                if (value == this._village_land.AvaliblePlots)
                    return;
                this._village_land.AvaliblePlots = value;
                this.OnPropertyChangedWithValue(value, nameof(AvaliblePlots));
            }
        }
        [DataSourceProperty]
        public string SlaveCapacityString
        {
            get
            {
                int num = this._village_land.Prisoners.TotalManCount;
                string str1 = num.ToString();
                num = 10 * this.OwnedPlots;
                string str2 = num.ToString();
                return str1 + "/" + str2;
            }
        }
        [DataSourceProperty]
        public float RevoltRisk => this._village_land.SlaveRevoltRisk;
        [DataSourceProperty]
        public string RevoltRiskString => this.RevoltRisk.ToString("0.0") + "%";
        [DataSourceProperty]
        public float SlaveDecline => this._village_land.SlaveDeclineRate();
        [DataSourceProperty]
        public string SlaveDeclineString => this.SlaveDecline.ToString("0.0") + "%";
        [DataSourceProperty]
        public string LandUsageString => string.Format("{0:0.0}", (float)(this._village_land.OwnedPlots == 0 ? 0.0 : (double)Math.Max(0.0f, 100 * this._village_land.Prisoners.TotalManCount / (10f * _village_land.OwnedPlots)))) + "%";
        [DataSourceProperty]
        public string StockpileCapacityString
        {
            get
            {
                int num = 0;
                foreach (ItemRosterElement itemRosterElement in this._village_land.Stockpile)
                    num += itemRosterElement.Amount;
                return num.ToString() + "/" + this._village_land.StorageCapacity.ToString();
            }
        }
        [DataSourceProperty]
        public string AvaliblePlotsString => this.AvaliblePlots.ToString();
        [DataSourceProperty]
        public int OwnedPlots
        {
            get => this._village_land.OwnedPlots;
            set
            {
                if (value == this._village_land.OwnedPlots)
                    return;
                this._village_land.OwnedPlots = value;
                this.OnPropertyChangedWithValue(value, nameof(OwnedPlots));
            }
        }
        [DataSourceProperty]
        public string OwnedPlotsString => this.OwnedPlots.ToString();
        [DataSourceProperty]
        public int AvalibleUndevelopedPlots
        {
            get => this._village_land.AvalibleUndevelopedPlots;
            set
            {
                if (value == this._village_land.AvalibleUndevelopedPlots)
                    return;
                this._village_land.AvalibleUndevelopedPlots = value;
                this.OnPropertyChangedWithValue(value, nameof(AvalibleUndevelopedPlots));
            }
        }
        [DataSourceProperty]
        public string AvalibleUndevelopedPlotsString => this.AvalibleUndevelopedPlots.ToString();
        [DataSourceProperty]
        public int OwnedUndevelopedPlots
        {
            get => this._village_land.OwnedUndevelopedPlots;
            set
            {
                if (value == this._village_land.OwnedUndevelopedPlots)
                    return;
                this._village_land.OwnedUndevelopedPlots = value;
                this.OnPropertyChangedWithValue(value, nameof(OwnedUndevelopedPlots));
            }
        }
        [DataSourceProperty]
        public string OwnedUndevelopedPlotsString => this.OwnedUndevelopedPlots.ToString();

        public void Close() => EstateScreenController.DeleteVMLayer();

        public void ManageSlaves()
        {
            if (Input.IsKeyDown((InputKey)42) || Input.IsKeyDown((InputKey)54))
            {
                int num = this._village_land.OwnedPlots * 10 - this._village_land.Prisoners.TotalManCount;
                for (CharacterObject? bandit = this.getBandit(MobileParty.MainParty); bandit != null && num > 0; --num)
                {
                    MobileParty.MainParty.PrisonRoster.AddToCounts(bandit, -1, false, 0, 0, true, -1);
                    this._village_land.Prisoners.AddToCounts(bandit, 1, false, 0, 0, true, -1);
                    bandit = this.getBandit(MobileParty.MainParty);
                }
                EstateScreenController.DeleteVMLayer();
                EstateScreenController.CreateVMLayer(_village_land);
            }
            else
            {
                PartyScreenLogic partyScreenLogic = new PartyScreenLogic();
                try
                {
                    TroopRoster dummyTroopRoster = TroopRoster.CreateDummyTroopRoster();
                    foreach (TroopRosterElement troopRosterElement in _village_land.Prisoners.GetTroopRoster())
                        dummyTroopRoster.AddToCounts(troopRosterElement.Character, troopRosterElement.Number, false, 0, 0, true, -1);

                    PartyScreenLogicInitializationData initializationData = new PartyScreenLogicInitializationData()
                    {
                        LeftOwnerParty = null,
                        RightOwnerParty = PartyBase.MainParty,
                        LeftMemberRoster = TroopRoster.CreateDummyTroopRoster(),
                        LeftPrisonerRoster = dummyTroopRoster,
                        RightMemberRoster = PartyBase.MainParty.MemberRoster,
                        RightPrisonerRoster = PartyBase.MainParty.PrisonRoster,
                        LeftLeaderHero = null,
                        RightLeaderHero = PartyBase.MainParty.LeaderHero,
                        LeftPartyMembersSizeLimit = 0,
                        LeftPartyPrisonersSizeLimit = _village_land.OwnedPlots * 10,
                        RightPartyMembersSizeLimit = PartyBase.MainParty.PartySizeLimit,
                        RightPartyPrisonersSizeLimit = PartyBase.MainParty.PrisonerSizeLimit,
                        LeftPartyName = new TextObject("Slaves", null),
                        RightPartyName = PartyBase.MainParty.Name,
                        TroopTransferableDelegate = new IsTroopTransferableDelegate(TroopTransferableDelegate),
                        PartyPresentationDoneButtonDelegate = new PartyPresentationDoneButtonDelegate(ManageDone),
                        PartyPresentationDoneButtonConditionDelegate = null,
                        PartyPresentationCancelButtonActivateDelegate = null,
                        PartyPresentationCancelButtonDelegate = null,
                        IsDismissMode = true,
                        IsTroopUpgradesDisabled = false,
                        Header = new TextObject("Manage Slaves", null),
                        PartyScreenClosedDelegate = null,
                        TransferHealthiesGetWoundedsFirst = false,
                        ShowProgressBar = false,
                        MemberTransferState = PartyScreenLogic.TransferState.Transferable,
                        PrisonerTransferState = PartyScreenLogic.TransferState.Transferable,
                        AccompanyingTransferState = PartyScreenLogic.TransferState.NotTransferable,
                        PartyScreenMode = PartyScreenHelper.PartyScreenMode.PrisonerManage
                    };
                    partyScreenLogic.Initialize(initializationData);
                    PartyState state = Game.Current.GameStateManager.CreateState<PartyState>();
                    state.PartyScreenLogic = partyScreenLogic;
                    state.Initialize();
                    Game.Current.GameStateManager.PushState(state);
                }
                catch (Exception)
                {
                }
            }
        }

        private CharacterObject? getBandit(MobileParty party)
        {
            foreach (TroopRosterElement troopRosterElement in party.PrisonRoster.GetTroopRoster())
            {
                if ((troopRosterElement.Character.Occupation == Occupation.Bandit || Hero.MainHero.GetPerkValue(DefaultPerks.Steward.ForcedLabor)) && !(troopRosterElement.Character).IsHero)
                    return troopRosterElement.Character;
            }
            return null;
        }

        private bool TroopTransferableDelegate(
          CharacterObject character,
          PartyScreenLogic.TroopType type,
          PartyScreenLogic.PartyRosterSide side,
          PartyBase LeftOwnerParty)
        {
            return !character.IsHero && type != PartyScreenLogic.TroopType.Member && type == PartyScreenLogic.TroopType.Prisoner && (character.Occupation == Occupation.Bandit || Hero.MainHero.GetPerkValue(DefaultPerks.Steward.ForcedLabor));
        }

        private bool ManageDone(
          TroopRoster leftMemberRoster,
          TroopRoster leftPrisonRoster,
          TroopRoster rightMemberRoster,
          TroopRoster rightPrisonRoster,
          FlattenedTroopRoster takenPrisonerRoster,
          FlattenedTroopRoster releasedPrisonerRoster,
          bool isForced,
          PartyBase? leftParty = null,
          PartyBase? rightParty = null)
        {
            if (leftPrisonRoster.TotalManCount > _village_land.OwnedPlots * 10)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    Localization.SetTextVariables("{=agricultureestate_slave_capacity_exceeded}Slave Capacity of {SLAVE_CAPACITY} exceeded",
                    new KeyValuePair<string, string?>("SLAVE_CAPACITY", (_village_land.OwnedPlots * 10).ToString())).ToString()));
                return false;
            }
            this._village_land.Prisoners = leftPrisonRoster;
            return true;
        }

        public void ManageStockpile()
        {
            if (Input.IsKeyDown((InputKey)42) || Input.IsKeyDown((InputKey)54))
            {
                foreach (ItemRosterElement itemRosterElement in this._village_land.Stockpile)
                {
                    ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
                    EquipmentElement equipmentElement = itemRosterElement.EquipmentElement;
                    ItemObject itemObject = equipmentElement.Item;
                    int amount = itemRosterElement.Amount;
                    itemRoster.AddToCounts(itemObject, amount);
                }
                this._village_land.Stockpile = new ItemRoster();
                EstateScreenController.DeleteVMLayer();
                EstateScreenController.CreateVMLayer(_village_land);
            }
            else
            {
                InventoryScreenHelper.OpenScreenAsStash(this._village_land.Stockpile);
            }
        }

        public void Click1() => ExecuteCommand(EstateComposition.CreateManagement(_village_land).BuyPlot(false));
        public void Click2() => ExecuteCommand(EstateComposition.CreateManagement(_village_land).SellPlot(false));
        public void Click3() => ExecuteCommand(EstateComposition.CreateManagement(_village_land).BuyPlot(true));
        public void Click4() => ExecuteCommand(EstateComposition.CreateManagement(_village_land).SellPlot(true));

        private void ExecuteCommand(EstateCommandResult result)
        {
            string? message = result switch
            {
                EstateCommandResult.NotEnoughGold => "{=agricultureestate_not_enough_gold}Not enough gold",
                EstateCommandResult.PrisonerCapacityExceeded => "{=agricultureestate_slave_capacity_exceed_when_land_sold}Selling land will cause slaves to exceede capacity. Remove slaves before selling",
                EstateCommandResult.LandReservedForClearance => "{=agricultureestate_tried_selling_clearing_land}All owned land is being cleared.\nCancel land clearance project before selling",
                EstateCommandResult.NotEnoughUndevelopedPlots => "{=agricultureestate_not_enough_undev_plots}Not enough owned undeveloped plots",
                EstateCommandResult.MaximumPatrolLevel => "{=agricultureestate_max_patrol_upgrade}Can only be upgraded a max of 8 times",
                EstateCommandResult.QueueFull => "{=agricultureestate_project_queue_limit}Project queue limit {QUEUE_LIMIT}\nIncrease steward skill to increase project queue limit",
                _ => null
            };
            if (message != null)
            {
                InformationManager.DisplayMessage(new InformationMessage(new TextObject(message)
                    .SetTextVariable("QUEUE_LIMIT", EstateManagement.QueueLimit(Hero.MainHero.GetSkillValue(DefaultSkills.Steward))).ToString()));
            }
            if (result == EstateCommandResult.Success)
            {
                EstateScreenController.DeleteVMLayer();
                EstateScreenController.CreateVMLayer(_village_land);
            }
        }
        public void Click5()
        {
            if (Input.IsKeyDown(InputKey.LeftShift))
            {
                foreach (var land in AgricultureEstateBehavior.VillageLands)
                    land.Value.SellToMarket = !SellToMarket;
            }
            else
                SellToMarket = !SellToMarket;
            EstateScreenController.DeleteVMLayer();
            EstateScreenController.CreateVMLayer(_village_land);
        }

        public void Click6() => StartProject(EstateProjects.LandClearance);
        public void Click7() => StartProject(EstateProjects.IncreasePatrols);
        public void Click8() => StartProject(EstateProjects.ExpandStorehouse);

        private void StartProject(string project) => ExecuteCommand(
            EstateComposition.CreateManagement(_village_land).StartProject(project,
                Hero.MainHero.GetSkillValue(DefaultSkills.Steward),
                Hero.MainHero.GetPerkValue(DefaultPerks.Steward.Contractors)));

        public void Click9()
        {
            EstateComposition.CreateManagement(_village_land).CancelProject();
            ExecuteCommand(EstateCommandResult.Success);
        }
        public void Click10()
        {
            if (this.BuySlaves)
            {
                if (Input.IsKeyDown((InputKey)42) || Input.IsKeyDown((InputKey)54))
                {
                    foreach (KeyValuePair<Settlement, VillageLand> villageLand in AgricultureEstateBehavior.VillageLands)
                        villageLand.Value.BuySlaves = false;
                }
                else
                    this.BuySlaves = false;
            }
            else if (Input.IsKeyDown((InputKey)42) || Input.IsKeyDown((InputKey)54))
            {
                foreach (KeyValuePair<Settlement, VillageLand> villageLand in AgricultureEstateBehavior.VillageLands)
                    villageLand.Value.BuySlaves = true;
            }
            else
                this.BuySlaves = true;
            EstateScreenController.DeleteVMLayer();
            EstateScreenController.CreateVMLayer(_village_land);
        }

        public void Click11()
        {
            this.ExecuteEndHint();
            EstateScreenController.CreateVMLayer2();
        }

        public override void RefreshValues() => base.RefreshValues();

        public void ExecuteBeginHint1() => MBInformationManager.ShowHint(new TextObject("{=agricultureestate_hint_land_suitable}Land suitable to agriculture and resource extraction").ToString());

        public void ExecuteBeginHint2() => MBInformationManager.ShowHint(new TextObject("{=agricultureestate_hint_land_needs_clearing}Land that needs to be cleared before it can be put to use").ToString());

        public void ExecuteBeginHint3() => MBInformationManager.ShowHint(
            Localization.SetTextVariables("{=agricultureestate_hint_buy_land}Buy for {PLOT_BUY_PRICE}{GOLD_ICON} gold",
                new KeyValuePair<string, string?>("PLOT_BUY_PRICE", PlotBuyPrice.ToString()),
                new KeyValuePair<string, string?>("GOLD_ICON", null)).ToString());

        public void ExecuteBeginHint4() => MBInformationManager.ShowHint(
            Localization.SetTextVariables("{=agricultureestate_hint_sell_land}Sell for {PLOT_SELL_PRICE}{GOLD_ICON} gold",
                new KeyValuePair<string, string?>("PLOT_SELL_PRICE", PlotSellPrice.ToString()),
                new KeyValuePair<string, string?>("GOLD_ICON", null)).ToString());

        public void ExecuteBeginHint5() => MBInformationManager.ShowHint(
            Localization.SetTextVariables("{=agricultureestate_hint_buy_undev_land}Buy for {PLOT_UNDEV_BUY_PRICE}{GOLD_ICON} gold",
                new KeyValuePair<string, string?>("PLOT_UNDEV_BUY_PRICE", UndevelopedPlotBuyPrice.ToString()),
                new KeyValuePair<string, string?>("GOLD_ICON", null)).ToString());

        public void ExecuteBeginHint6() => MBInformationManager.ShowHint(
            Localization.SetTextVariables("{=agricultureestate_hint_sell_undev_land}Sell for {PLOT_UNDEV_SELL_PRICE}{GOLD_ICON} gold",
                new KeyValuePair<string, string?>("PLOT_UNDEV_SELL_PRICE", UndevelopedPlotSellPrice.ToString()),
                new KeyValuePair<string, string?>("GOLD_ICON", null)).ToString());

        public void ExecuteBeginHint7() => MBInformationManager.ShowHint(new TextObject("{=agricultureestate_hint_prisoner_as_slaves}Bandits prisoners can be used as labor.\nCapacity for slaves determined by number of owned plots\nShift click to quick deposit").ToString());

        public void ExecuteBeginHint8() => MBInformationManager.ShowHint(new TextObject("{=agricultureestate_hint_slave_decline}Slave decline is daily chance for each slaves to escape.\nRevolt risk is daily chance for slaves to violently rebel in mass.\nRevolt risk is increase if slaves outnumber the village militia\nBuilding upgrades can improve these number.\nLand not used by slave labor will generate a small amount of land rent every day").ToString());

        public void ExecuteBeginHint9()
        {
            string str = new TextObject("{=agricultureestate_estimated_daily_output}Estimated Daily Output").ToString();
            if (_village_land.Village is not null)
            {
                foreach ((ItemObject, float) production in (IEnumerable<(ItemObject, float)>)_village_land.Village.VillageType.Productions)
                {
                    if (production.Item2 > 0.0 && this._village_land.Prisoners.TotalManCount > 0)
                    {
                        float num = 1f;
                        if ((this._village_land.Village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("grain") || this._village_land.Village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("olives") || this._village_land.Village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("fish") || this._village_land.Village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("date_fruit")) && Hero.MainHero.GetPerkValue(DefaultPerks.Trade.GranaryAccountant))
                            num = 1.2f;
                        else if ((this._village_land.Village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("clay") || this._village_land.Village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("iron") || this._village_land.Village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("cotton") || this._village_land.Village.VillageType.PrimaryProduction == MBObjectManager.Instance.GetObject<ItemObject>("silver")) && Hero.MainHero.GetPerkValue(DefaultPerks.Trade.TradeyardForeman))
                            num = 1.2f;
                        else if (this._village_land.Village.VillageType.PrimaryProduction.Type == ItemObject.ItemTypeEnum.Horse && Hero.MainHero.GetPerkValue(DefaultPerks.Riding.Breeder))
                            num = 1.1f;
                        str += new TextObject("{=agricultureestate_estimated_daily_output_entry}{NEWLINE}{MULTIPLIER} X {ITEM}",
                            new Dictionary<string, object>()
                            {
                                { "MULTIPLIER", string.Format("{0:0.00}", (float)((double)num * 24.0 * EstateConfiguration.SlaveProductionScale * _village_land.Prisoners.TotalManCount * production.Item2 / 1000.0)) },
                                { "ITEM",  production.Item1.Name }
                            }).ToString();
                    }
                }
                float num1 = (float)(_village_land.Village.TradeTaxAccumulated * (this._village_land.OwnedPlots == 0 ? 0.0 : (double)Math.Max(0.0f, (float)((10.0 * _village_land.OwnedPlots - _village_land.Prisoners.TotalManCount) / (10.0 * _village_land.OwnedPlots)))) / 100.0) * _village_land.OwnedPlots * EstateConfiguration.LandRentScale;
                MBInformationManager.ShowHint(str + "\n" + new TextObject(
                    "{=agricultureestate_estimated_daily_output_rent}{MULTIPLIER}{GOLD_ICON} X Land Rent",
                    new Dictionary<string, object>() {
                        { "MULTIPLIER", string.Format("{0:0.00}", num1) }
                    }));
            }
        }

        public void ExecuteBeginHint10() => MBInformationManager.ShowHint(new TextObject("{=agricultureestate_hint_production}Goods produced will be added to the stockpile and can be withdrawn at a later time.\n  If storage capacity is exceeded, then production will stop.\nShift click to quick withdrawl all").ToString());

        public void ExecuteBeginHint11() => MBInformationManager.ShowHint(new TextObject("{=agricultureestate_hint_sell_goods_to_village}Goods produce can be set to be automatically sold to the village market.\nBe aware the the village market tend to pay less for goods than town markets\nClick to turn {STATUS}").SetTextVariable("STATUS", (this.SellToMarket ? new TextObject("{=agricultureestate_off}Off").ToString() : new TextObject("{=agricultureestate_on}On").ToString())).ToString());

        public void ExecuteBeginHint12() => MBInformationManager.ShowHint(
            Localization.SetTextVariables("{=agricultureestate_hint_land_clearing}Land Clearance will convert 1 owned undeveloped plot into a normal plot\nCost: {LAND_CLEARING_COST}{GOLD_ICON}\nEach additional plot of land cleared provides a small increase to village growth rate\nTime: 240 hours",
                new KeyValuePair<string, string?>("LAND_CLEARING_COST", (EstateEconomy.CalculateProjectCost(ProjectCost, Hero.MainHero.GetPerkValue(DefaultPerks.Steward.Contractors))).ToString()),
                new KeyValuePair<string, string?>("GOLD_ICON", null)).ToString());

        public void ExecuteBeginHint13() => MBInformationManager.ShowHint(
            Localization.SetTextVariables("{=agricultureestate_hint_patrol_upgrade}Increasing patrol decreases escape chance by 0.5% and revolt risk by 0.1% per level.  This Upgrade can be done a max of 8 times\nCost: {PATROL_UPGRADE_COST}{GOLD_ICON}\nTime: 240 hours",
                new KeyValuePair<string, string?>("PATROL_UPGRADE_COST", (EstateEconomy.CalculateProjectCost(ProjectCost, Hero.MainHero.GetPerkValue(DefaultPerks.Steward.Contractors))).ToString()),
                new KeyValuePair<string, string?>("GOLD_ICON", null)).ToString());

        public void ExecuteBeginHint14() => MBInformationManager.ShowHint(
            Localization.SetTextVariables("{=agricultureestate_hint_storehouse_upgrade}Expanding Storehouse increases storage capacity by 500\nCost: {STOREHOUSE_UPGRADE_COST}{GOLD_ICON}\nTime: 240 hours",
                new KeyValuePair<string, string?>("STOREHOUSE_UPGRADE_COST", (EstateEconomy.CalculateProjectCost(ProjectCost, Hero.MainHero.GetPerkValue(DefaultPerks.Steward.Contractors))).ToString()),
                new KeyValuePair<string, string?>("GOLD_ICON", null)).ToString());

        public void ExecuteBeginHint15() => MBInformationManager.ShowHint(new TextObject("{=agricultureestate_hint_abort_upgrade}All progress will be lost and gold cost will not be refunded").ToString());

        public void ExecuteBeginHint16()
        {
            string str = "";
            if (this._village_land.ProjectQueue == null || TaleWorlds.Core.Extensions.IsEmpty<string>(_village_land.ProjectQueue))
            {
                if (this._village_land.ProjectQueue == null)
                    this._village_land.ProjectQueue = new Queue<string>();
                str = new TextObject("{=agricultureestate_build_queue_empty}Build Queue Empty").ToString();
            }
            else
            {
                TextObject[] array = this._village_land.GetProjectQueueArrayL18N();
                for (int index = 0; index < array.Length; ++index)
                    str += new TextObject("{=agricultureestate_build_queue_hint}{ORDER}-{BUILDING}{NEWLINE}", new Dictionary<string, object>() { { "ORDER", (index + 1).ToString() }, { "BUILDING", array[index] } }).ToString();
            }
            MBInformationManager.ShowHint(str);
        }

        public void ExecuteBeginHint17() => MBInformationManager.ShowHint(new TextObject("{=agricultureestate_hint_auto_buy_prisoners}Automatically buy bandits prisoners from any party that visits this village\nClick to turn {STATUS}")
          .SetTextVariable("STATUS", (this.BuySlaves ? new TextObject("{=agricultureestate_off}Off").ToString() : new TextObject("{=agricultureestate_on}On").ToString())).ToString()
          + new TextObject("{=agricultureestate_shift_click_hint}{NEWLINE}Shift click will set all estates").ToString());

        public void ExecuteBeginHint18() => MBInformationManager.ShowHint(new TextObject("{=agricultureestate_hint_show_ledger}Open bussiness ledger that show stats of all owned estates").ToString());

        public void ExecuteEndHint() => MBInformationManager.HideInformations(); //InformationManager.HideInformations();
    }
}
